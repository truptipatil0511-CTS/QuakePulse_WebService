using System.Text;
using QuakePulse_WebService.Models.Internal;

namespace QuakePulse_WebService.Integration;

public class UsgsApiService : IUsgsApiService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<UsgsApiService> _logger;

    public UsgsApiService(HttpClient httpClient, ILogger<UsgsApiService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<string> GetEarthquakeDataAsync(EarthquakeQuery query)
    {
        var url = BuildUrl(query);
        return await FetchAsync(url);
    }

    public async Task<string> GetEarthquakeByIdAsync(string id)
    {
        var url = $"query?format=geojson&eventid={Uri.EscapeDataString(id)}";
        return await FetchAsync(url);
    }

    private async Task<string> FetchAsync(string url)
    {
        try
        {
            _logger.LogInformation("[API] Calling USGS API: {Url}", url);
            var response = await _httpClient.GetAsync(url);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError(
                    "[API] USGS API call failed. Status={Status} ({StatusCode}) Url={Url}",
                    response.StatusCode, (int)response.StatusCode, url);
                throw new HttpRequestException($"USGS API returned {(int)response.StatusCode}");
            }

            var body = await response.Content.ReadAsStringAsync();
            _logger.LogInformation(
                "[API] Success response received. Status={Status} Bytes={Bytes} Url={Url}",
                (int)response.StatusCode, body.Length, url);
            return body;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[ERROR] Exception occurred while calling USGS API. Url={Url} Message={Message}", url, ex.Message);
            throw;
        }
    }

    private string BuildUrl(EarthquakeQuery query)
    {
        var sb = new StringBuilder("query?format=geojson");
        sb.Append($"&starttime={query.StartDate:yyyy-MM-dd}");
        sb.Append($"&endtime={query.EndDate:yyyy-MM-dd}");

        if (query.MinMagnitude.HasValue) sb.Append($"&minmagnitude={query.MinMagnitude}");
        if (query.MaxMagnitude.HasValue) sb.Append($"&maxmagnitude={query.MaxMagnitude}");
        if (query.MinDepth.HasValue)     sb.Append($"&mindepth={query.MinDepth}");
        if (query.MaxDepth.HasValue)     sb.Append($"&maxdepth={query.MaxDepth}");
        if (query.Limit.HasValue)        sb.Append($"&limit={query.Limit}");
        if (query.Offset.HasValue)       sb.Append($"&offset={query.Offset}");

        if (query.Lat.HasValue && query.Lon.HasValue)
        {
            sb.Append($"&latitude={query.Lat}&longitude={query.Lon}");
            if (query.Radius.HasValue)
                sb.Append($"&maxradiuskm={query.Radius}");
        }

        if (!string.IsNullOrEmpty(query.SortBy))
        {
            var orderby = query.SortBy.ToLower() switch
            {
                "magnitude" => "magnitude",
                "date"      => "time",
                _           => "time"
            };
            sb.Append($"&orderby={orderby}");
        }

        return sb.ToString();
    }
}
