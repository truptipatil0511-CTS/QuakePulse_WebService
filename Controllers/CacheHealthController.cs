using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using StackExchange.Redis;

namespace QuakePulse_WebService.Controllers;

[ApiController]
[Route("api/cache")]
public class CacheHealthController : ControllerBase
{
    private const string TestKeyPrefix = "healthcheck:cache";
    private static readonly TimeSpan TestTtl = TimeSpan.FromSeconds(30);

    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<CacheHealthController> _logger;

    public CacheHealthController(IServiceProvider serviceProvider, ILogger<CacheHealthController> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    // GET /api/cache/health
    [HttpGet("health")]
    public async Task<IActionResult> Get()
    {
        var multiplexer = _serviceProvider.GetService<IConnectionMultiplexer>();

        if (multiplexer is null)
        {
            return Ok(new
            {
                status = "Disabled",
                message = "Redis cache is not enabled in configuration.",
                timestamp = DateTime.UtcNow
            });
        }

        var testKey = $"{TestKeyPrefix}:{Guid.NewGuid():N}";
        var testValue = $"ping-{DateTime.UtcNow:O}";
        var stopwatch = new Stopwatch();

        bool writeSuccess = false;
        bool readSuccess = false;
        bool ttlValid = false;
        long writeMs = 0;
        long readMs = 0;
        string? readValue = null;

        try
        {
            var db = multiplexer.GetDatabase();

            stopwatch.Restart();
            writeSuccess = await db.StringSetAsync(testKey, testValue, TestTtl);
            writeMs = stopwatch.ElapsedMilliseconds;

            stopwatch.Restart();
            var value = await db.StringGetAsync(testKey);
            readMs = stopwatch.ElapsedMilliseconds;
            readValue = value.HasValue ? value.ToString() : null;
            readSuccess = readValue == testValue;

            var remainingTtl = await db.KeyTimeToLiveAsync(testKey);
            ttlValid = remainingTtl is { TotalSeconds: > 0 } && remainingTtl <= TestTtl;

            await db.KeyDeleteAsync(testKey);

            var healthy = writeSuccess && readSuccess && ttlValid;
            var response = new
            {
                status = healthy ? "Healthy" : "Unhealthy",
                timestamp = DateTime.UtcNow,
                writeSuccess,
                readSuccess,
                ttlValid,
                writeLatencyMs = writeMs,
                readLatencyMs = readMs,
                totalLatencyMs = writeMs + readMs,
                endpoint = multiplexer.GetEndPoints().FirstOrDefault()?.ToString()
            };

            if (!healthy)
            {
                _logger.LogWarning(
                    "Redis health check returned Unhealthy. Write={Write}, Read={Read}, Ttl={Ttl}",
                    writeSuccess, readSuccess, ttlValid);
                return StatusCode(StatusCodes.Status503ServiceUnavailable, response);
            }

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Redis health check failed");

            return StatusCode(StatusCodes.Status503ServiceUnavailable, new
            {
                status = "Unhealthy",
                timestamp = DateTime.UtcNow,
                writeSuccess,
                readSuccess,
                ttlValid,
                writeLatencyMs = writeMs,
                readLatencyMs = readMs,
                error = ex.Message
            });
        }
    }
}
