using Microsoft.AspNetCore.Mvc;
using QuakePulse_WebService.Models.Api;
using QuakePulse_WebService.Services;

namespace QuakePulse_WebService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TransformController : ControllerBase
{
    private readonly IEarthquakeService _service;

    public TransformController(IEarthquakeService service)
    {
        _service = service;
    }

    // POST /api/transform
    [HttpPost]
    public async Task<IActionResult> Transform([FromBody] TransformRequest request)
    {
        if (request?.Data == null || request.Data.Count == 0)
            return BadRequest(new { message = "Data array is required." });

        var result = await _service.TransformToHtmlAsync(request);
        return Ok(result);
    }
}
