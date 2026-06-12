using Microsoft.AspNetCore.Mvc;
using QuakePulse_WebService.Models.Api;
using QuakePulse_WebService.Models.Internal;
using QuakePulse_WebService.Services;

namespace QuakePulse_WebService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class EarthquakeController : ControllerBase
{
    private readonly IEarthquakeService _service;

    public EarthquakeController(IEarthquakeService service)
    {
        _service = service;
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
        return Ok(result);
    }

    // GET /api/earthquake/latest?limit=10
    [HttpGet("latest")]
    public async Task<IActionResult> GetLatest([FromQuery] int limit = 10)
    {
        var result = await _service.GetLatestAsync(limit);
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
            StartDate = DateTime.UtcNow.AddDays(-30).Date,
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
            StartDate    = startDate ?? DateTime.UtcNow.AddDays(-7).Date,
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
            StartDate = startDate ?? DateTime.UtcNow.AddDays(-7).Date,
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
