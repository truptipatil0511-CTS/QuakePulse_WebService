using Microsoft.AspNetCore.Mvc;
using QuakePulse_WebService.Models.Api;

namespace QuakePulse_WebService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class LogsController : ControllerBase
{
    private readonly ILogger<LogsController> _logger;

    public LogsController(ILogger<LogsController> logger)
    {
        _logger = logger;
    }

    // POST /api/logs
    [HttpPost]
    public IActionResult Post([FromBody] FrontendLogRequest request)
    {
        using (_logger.BeginScope(new Dictionary<string, object>
        {
            ["CorrelationId"] = request.CorrelationId,
            ["Source"] = "Frontend",
            ["ClientTimestamp"] = request.Timestamp
        }))
        {
            switch (request.Level)
            {
                case "Error":
                    _logger.LogError("[Frontend] {Message}", request.Message);
                    break;
                case "Warning":
                    _logger.LogWarning("[Frontend] {Message}", request.Message);
                    break;
                default:
                    _logger.LogInformation("[Frontend] {Message}", request.Message);
                    break;
            }
        }

        return Ok(new { success = true });
    }
}
