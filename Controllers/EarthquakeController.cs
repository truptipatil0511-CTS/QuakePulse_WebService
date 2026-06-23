using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using QuakePulse_WebService.Common;
using QuakePulse_WebService.Models.Api;
using QuakePulse_WebService.Models.Internal;
using QuakePulse_WebService.Services;

namespace QuakePulse_WebService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class EarthquakeController : ControllerBase
{
    private readonly IEarthquakeService _service;
    private readonly EarthquakeDefaults _defaults;

    public EarthquakeController(IEarthquakeService service, IOptions<EarthquakeDefaults> defaults)
    {
        _service = service;
        _defaults = defaults.Value;
    }

    // GET /api/earthquake?startDate=&endDate=&minMagnitude=&maxMagnitude=&location=&sortBy=&limit=&offset=
    [HttpGet]
    public async Task<IActionResult> Get(
        [FromQuery] DateTime startDate,
        [FromQuery] DateTime endDate,
        [FromQuery] double? minMagnitude,
        [FromQuery] double? maxMagnitude,
        [FromQuery] string? location,
        [FromQuery] string? sortBy,
        [FromQuery] int? limit,
        [FromQuery] int? offset)
    {
        var query = new EarthquakeQuery
        {
            StartDate = startDate,
            EndDate = endDate.AddDays(1),
            MinMagnitude = minMagnitude,
            MaxMagnitude = maxMagnitude,
            Location = location,
            SortBy = sortBy,
            Limit = limit,
            Offset = offset
        };
        var result = await _service.GetEarthquakesAsync(query);

        Response.Headers.CacheControl = "public, max-age=60, s-maxage=60";
        Response.Headers.ETag = $"\"{result.Metadata.Count}-{result.Data.FirstOrDefault()?.Id ?? "empty"}\"";

        return Ok(result);
    }

    // GET /api/earthquake/latest?limit=10
    [HttpGet("latest")]
    public async Task<IActionResult> GetLatest([FromQuery] int? limit)
    {
        var result = await _service.GetLatestAsync(limit ?? _defaults.LatestLimit);
        return Ok(result);
    }

    // GET /api/earthquake/filter?lat=&lon=&radius=&minDepth=&maxDepth=
    [HttpGet("filter")]
    public async Task<IActionResult> Filter(
        [FromQuery] double? lat,
        [FromQuery] double? lon,
        [FromQuery] double? radius,
        [FromQuery] double? minDepth,
        [FromQuery] double? maxDepth)
    {
        var query = new EarthquakeQuery
        {
            StartDate = DateTime.UtcNow.AddDays(-_defaults.FilterLookbackDays).Date,
            EndDate = DateTime.UtcNow.Date,
            Lat = lat,
            Lon = lon,
            Radius = radius,
            MinDepth = minDepth,
            MaxDepth = maxDepth
        };
        var result = await _service.GetEarthquakesAsync(query);
        return Ok(result);
    }

    // GET /api/earthquake/map
    //   ?startDate=&endDate=&minMagnitude=&maxMagnitude=&location=
    //   &minDepth=&maxDepth=&sortBy=&limit=&offset=
    //
    // Mirrors the parameter signature of /api/earthquake so the map view
    // can respect the same filters as the list view. Returns GeoJSON
    // FeatureCollection consumable by Azure Maps / Leaflet / Mapbox.
    [HttpGet("map")]
    public async Task<IActionResult> GetMap(
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate,
        [FromQuery] double? minMagnitude,
        [FromQuery] double? maxMagnitude,
        [FromQuery] string? location,
        [FromQuery] double? minDepth,
        [FromQuery] double? maxDepth,
        [FromQuery] string? sortBy,
        [FromQuery] int? limit,
        [FromQuery] int? offset)
    {
        var query = new EarthquakeQuery
        {
            StartDate    = startDate ?? DateTime.UtcNow.AddDays(-_defaults.MapLookbackDays).Date,
            EndDate      = endDate   ?? DateTime.UtcNow.Date,
            MinMagnitude = minMagnitude,
            MaxMagnitude = maxMagnitude,
            Location     = location,
            MinDepth     = minDepth,
            MaxDepth     = maxDepth,
            SortBy       = sortBy,
            Limit        = limit,
            Offset       = offset
        };
        var result = await _service.GetMapDataAsync(query);
        return Ok(result);
    }

    // GET /api/earthquake/table
    [HttpGet("table")]
    public async Task<IActionResult> GetTable(
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate)
    {
        var query = new EarthquakeQuery
        {
            StartDate = startDate ?? DateTime.UtcNow.AddDays(-_defaults.TableLookbackDays).Date,
            EndDate = endDate ?? DateTime.UtcNow.Date
        };
        var result = await _service.GetTableAsync(query);
        return Ok(result);
    }

    // GET /api/earthquake/{id}
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(string id)
    {
        var result = await _service.GetEarthquakeByIdAsync(id);
        if (result == null) return NotFound(new { message = $"Earthquake '{id}' not found." });
        return Ok(result);
    }

    // POST /api/earthquake/assistant
    [HttpPost("assistant")]
    public async Task<IActionResult> Assistant([FromBody] AssistantRequest request)
    {
        if (string.IsNullOrWhiteSpace(request?.Query))
            return BadRequest(new { message = "Query is required." });

        var result = await _service.QueryAssistantAsync(request);
        return Ok(result);
    }
}
