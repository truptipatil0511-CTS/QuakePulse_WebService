using AutoMapper;
using QuakePulse_WebService.Caching;
using QuakePulse_WebService.Integration;
using QuakePulse_WebService.Models.Api;
using QuakePulse_WebService.Models.External;
using QuakePulse_WebService.Models.Internal;
using System.Text;
using System.Text.Json;

namespace QuakePulse_WebService.Orchestration;

public class EarthquakeOrchestrator : IEarthquakeOrchestrator
{
    private readonly ICacheService _cacheService;
    private readonly CacheSettings _cacheSettings;
    private readonly int _ttlMinutes;
    private readonly IUsgsApiService _apiService;
    private readonly IMapper _mapper;
    private readonly ILogger<EarthquakeOrchestrator> _logger;

    public EarthquakeOrchestrator(
        IUsgsApiService apiService,
        ICacheService cacheService,
        IConfiguration config,
        IMapper mapper,
        ILogger<EarthquakeOrchestrator> logger)
    {
        _apiService = apiService ?? throw new ArgumentNullException(nameof(apiService));
        _cacheService = cacheService ?? throw new ArgumentNullException(nameof(cacheService));
        _cacheSettings = config.GetSection("CacheSettings").Get<CacheSettings>() ?? new CacheSettings();
        _ttlMinutes = config.GetValue<int>("Redis:DefaultTTLMinutes", 10);
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    // -----------------------------------------------------------------------
    // GET /api/earthquake  +  GET /api/earthquake/filter
    // -----------------------------------------------------------------------
    public async Task<EarthquakeListResponse> GetEarthquakesAsync(EarthquakeQuery query)
    {
        _logger.LogInformation("GetEarthquakesAsync: {StartDate:yyyy-MM-dd} to {EndDate:yyyy-MM-dd}", query.StartDate, query.EndDate);

        if (!_cacheSettings.Enabled)
            return await FetchAndBuildListAsync(query);

        var cacheKey = GenerateCacheKey(query);
        var cached = await GetFromCacheAsync<EarthquakeListResponse>(cacheKey);
        if (cached != null)
        {
            cached.Metadata.Cached = true;
            _logger.LogInformation("Cache hit: {CacheKey}", cacheKey);
            return cached;
        }

        return await WithLockAsync(cacheKey, async () =>
        {
            // Double-check after acquiring lock
            var doubleCheck = await GetFromCacheAsync<EarthquakeListResponse>(cacheKey);
            if (doubleCheck != null) { doubleCheck.Metadata.Cached = true; return doubleCheck; }

            var result = await FetchAndBuildListAsync(query);
            await StoreToCacheAsync(cacheKey, result);
            return result;
        }, () => WaitForCacheAsync<EarthquakeListResponse>(cacheKey));
    }

    // -----------------------------------------------------------------------
    // GET /api/earthquake/{id}
    // -----------------------------------------------------------------------
    public async Task<EarthquakeDto?> GetEarthquakeByIdAsync(string id)
    {
        _logger.LogInformation("GetEarthquakeByIdAsync: {Id}", id);
        try
        {
            var json = await _apiService.GetEarthquakeByIdAsync(id);
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

            // USGS returns a FeatureCollection for eventid queries
            var root = JsonSerializer.Deserialize<GeoJsonRoot>(json, options);
            var feature = root?.features?.FirstOrDefault();
            return feature == null ? null : _mapper.Map<EarthquakeDto>(feature);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch earthquake by id: {Id}", id);
            throw;
        }
    }

    // -----------------------------------------------------------------------
    // GET /api/earthquake/latest
    // -----------------------------------------------------------------------
    public Task<EarthquakeListResponse> GetLatestAsync(int limit)
    {
        var query = new EarthquakeQuery
        {
            StartDate = DateTime.UtcNow.AddDays(-7).Date,
            EndDate = DateTime.UtcNow.Date,
            SortBy = "date",
            Limit = limit
        };
        return GetEarthquakesAsync(query);
    }

    // -----------------------------------------------------------------------
    // GET /api/earthquake/map
    // -----------------------------------------------------------------------
    public async Task<GeoJsonResponse> GetMapDataAsync(EarthquakeQuery query)
    {
        _logger.LogInformation(
            "GetMapDataAsync: {StartDate:yyyy-MM-dd}→{EndDate:yyyy-MM-dd} mag={MinMag}-{MaxMag} depth={MinDepth}-{MaxDepth} loc='{Location}'",
            query.StartDate, query.EndDate,
            query.MinMagnitude, query.MaxMagnitude,
            query.MinDepth, query.MaxDepth,
            query.Location ?? "");

        // Reuse the same filter-and-cache pipeline as the list endpoint.
        // The GeoJSON response is a projection over the same filtered dataset,
        // so map and list views stay in lockstep.
        var list = await GetEarthquakesAsync(query);

        return new GeoJsonResponse
        {
            Features = list.Data.Select(e => new GeoJsonFeature
            {
                Geometry = new GeoJsonGeometry
                {
                    // GeoJSON convention: [longitude, latitude, depth]
                    Coordinates = new[] { e.Longitude, e.Latitude, e.Depth }
                },
                Properties = new Dictionary<string, object?>
                {
                    ["id"]        = e.Id,
                    ["magnitude"] = e.Magnitude,
                    ["place"]     = e.Location,
                    ["time"]      = e.Time,
                    ["depth"]     = e.Depth,      // duplicate of coords[2] — enables data-driven styling without indexing
                    ["status"]    = e.Status,
                    ["title"]     = e.Title,
                    ["url"]       = e.Url
                }
            }).ToList()
        };
    }

    // -----------------------------------------------------------------------
    // GET /api/earthquake/table
    // -----------------------------------------------------------------------
    public async Task<HtmlResponse> GetTableAsync(EarthquakeQuery query)
    {
        var list = await GetEarthquakesAsync(query);
        return new HtmlResponse
        {
            Html = BuildHtmlTable(list.Data),
            Source = list.Metadata.Cached ? "cache" : "rule-based"
        };
    }

    // -----------------------------------------------------------------------
    // Private helpers
    // -----------------------------------------------------------------------
    private async Task<EarthquakeListResponse> FetchAndBuildListAsync(EarthquakeQuery query)
    {
        var json = await _apiService.GetEarthquakeDataAsync(query);
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        GeoJsonRoot? root;

        try
        {
            root = JsonSerializer.Deserialize<GeoJsonRoot>(json, options);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to deserialize USGS GeoJSON");
            throw new InvalidOperationException("Failed to deserialize earthquake data.", ex);
        }

        var features = root?.features ?? new List<Feature>();
        _logger.LogInformation("Fetched {Count} records from USGS", features.Count);

        var earthquakes = _mapper.Map<List<EarthquakeDto>>(features);

        // In-memory location text filter (USGS API doesn't support free-text place search)
        if (!string.IsNullOrWhiteSpace(query.Location))
        {
            var loc = query.Location.ToLowerInvariant();
            earthquakes = earthquakes
                .Where(e => e.Location?.ToLowerInvariant().Contains(loc) == true)
                .ToList();
        }

        // Default sort (when not delegated to USGS via orderby) is magnitude desc
        if (string.IsNullOrEmpty(query.SortBy) || query.SortBy.Equals("magnitude", StringComparison.OrdinalIgnoreCase))
            earthquakes = earthquakes.OrderByDescending(e => e.Magnitude).ToList();

        return new EarthquakeListResponse
        {
            Metadata = new ResponseMetadata { Count = earthquakes.Count },
            Data = earthquakes
        };
    }

    private static string BuildHtmlTable(List<EarthquakeDto> earthquakes)
    {
        var sb = new StringBuilder();
        sb.AppendLine("<table border=\"1\" cellpadding=\"6\" cellspacing=\"0\" style=\"border-collapse:collapse;font-family:sans-serif;\">");
        sb.AppendLine("  <thead style=\"background:#2c3e50;color:#fff;\">");
        sb.AppendLine("    <tr><th>ID</th><th>Time (UTC)</th><th>Magnitude</th><th>Location</th><th>Depth (km)</th><th>Status</th><th>URL</th></tr>");
        sb.AppendLine("  </thead>");
        sb.AppendLine("  <tbody>");

        foreach (var e in earthquakes)
        {
            var link = string.IsNullOrEmpty(e.Url) ? "-" : $"<a href=\"{e.Url}\" target=\"_blank\">details</a>";
            sb.AppendLine($"    <tr><td>{e.Id}</td><td>{e.Time:yyyy-MM-dd HH:mm:ss}</td><td>{e.Magnitude:F1}</td><td>{e.Location}</td><td>{e.Depth:F1}</td><td>{e.Status}</td><td>{link}</td></tr>");
        }

        sb.AppendLine("  </tbody>");
        sb.AppendLine("</table>");
        return sb.ToString();
    }

    private string GenerateCacheKey(EarthquakeQuery q)
    {
        var sb = new StringBuilder($"earthquake:{q.StartDate:yyyyMMdd}:{q.EndDate:yyyyMMdd}");
        if (q.MinMagnitude.HasValue) sb.Append($":mn{q.MinMagnitude}");
        if (q.MaxMagnitude.HasValue) sb.Append($":mx{q.MaxMagnitude}");
        if (q.MinDepth.HasValue)     sb.Append($":md{q.MinDepth}");
        if (q.MaxDepth.HasValue)     sb.Append($":xd{q.MaxDepth}");
        if (q.Lat.HasValue)          sb.Append($":lat{q.Lat}");
        if (q.Lon.HasValue)          sb.Append($":lon{q.Lon}");
        if (q.Radius.HasValue)       sb.Append($":r{q.Radius}");
        if (!string.IsNullOrEmpty(q.Location)) sb.Append($":loc{q.Location.ToLowerInvariant()}");
        if (!string.IsNullOrEmpty(q.SortBy))   sb.Append($":s{q.SortBy}");
        if (q.Limit.HasValue)        sb.Append($":l{q.Limit}");
        if (q.Offset.HasValue)       sb.Append($":o{q.Offset}");
        return sb.ToString();
    }

    private async Task<T?> GetFromCacheAsync<T>(string key)
    {
        try { return await _cacheService.GetAsync<T>(key); }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Cache get failed for key: {Key}", key);
            return default;
        }
    }

    private async Task StoreToCacheAsync<T>(string key, T value)
    {
        try
        {
            await _cacheService.SetAsync(key, value, _ttlMinutes);
            _logger.LogDebug("Cached {Key} (TTL: {TTL}m)", key, _ttlMinutes);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Cache set failed for key: {Key}", key);
        }
    }

    private async Task<T> WithLockAsync<T>(string cacheKey, Func<Task<T>> onLockAcquired, Func<Task<T>> onLockHeld)
    {
        var lockKey = $"{cacheKey}:lock";
        var lockValue = Guid.NewGuid().ToString();
        bool lockAcquired = false;

        try
        {
            lockAcquired = await _cacheService.AcquireLockAsync(lockKey, lockValue, _cacheSettings.LockDurationSeconds);
            return lockAcquired ? await onLockAcquired() : await onLockHeld();
        }
        finally
        {
            if (lockAcquired)
                await _cacheService.ReleaseLockAsync(lockKey, lockValue);
        }
    }

    private async Task<T> WaitForCacheAsync<T>(string cacheKey) where T : new()
    {
        for (int i = 0; i < _cacheSettings.MaxLockWaitAttempts; i++)
        {
            await Task.Delay(_cacheSettings.LockWaitTimeMs);
            var data = await GetFromCacheAsync<T>(cacheKey);
            if (data != null) return data;
        }

        _logger.LogWarning("Timeout waiting for cache key: {CacheKey}", cacheKey);
        return new T();
    }
}
