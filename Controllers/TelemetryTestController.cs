using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.AspNetCore.Mvc;

namespace QuakePulse_WebService.Controllers;

[ApiController]
[Route("api/test-ai")]
public class TelemetryTestController : ControllerBase
{
    private readonly ILogger<TelemetryTestController> _logger;
    private readonly TelemetryClient _telemetryClient;
    private readonly TelemetryConfiguration _telemetryConfiguration;
    private readonly IConfiguration _configuration;
    private readonly IWebHostEnvironment _environment;

    public TelemetryTestController(
        ILogger<TelemetryTestController> logger,
        TelemetryClient telemetryClient,
        TelemetryConfiguration telemetryConfiguration,
        IConfiguration configuration,
        IWebHostEnvironment environment)
    {
        _logger = logger;
        _telemetryClient = telemetryClient;
        _telemetryConfiguration = telemetryConfiguration;
        _configuration = configuration;
        _environment = environment;
    }

    // GET /api/test-ai
    [HttpGet]
    public IActionResult Get()
    {
        var correlationId = HttpContext.Items["CorrelationId"]?.ToString() ?? Guid.NewGuid().ToString();

        _logger.LogInformation("AI-TEST: Information log via ILogger. CorrelationId={CorrelationId}", correlationId);
        _logger.LogWarning("AI-TEST: Warning log via ILogger. CorrelationId={CorrelationId}", correlationId);
        _logger.LogError("AI-TEST: Error log via ILogger. CorrelationId={CorrelationId}", correlationId);

        _telemetryClient.TrackEvent("AI-TEST-Event", new Dictionary<string, string>
        {
            ["CorrelationId"] = correlationId,
            ["Source"] = "TelemetryTestController",
            ["Environment"] = _environment.EnvironmentName
        });

        _telemetryClient.TrackTrace("AI-TEST: Direct trace via TelemetryClient",
            Microsoft.ApplicationInsights.DataContracts.SeverityLevel.Information,
            new Dictionary<string, string> { ["CorrelationId"] = correlationId });

        try
        {
            throw new InvalidOperationException("AI-TEST: Synthetic exception for telemetry validation");
        }
        catch (Exception ex)
        {
            _telemetryClient.TrackException(ex, new Dictionary<string, string>
            {
                ["CorrelationId"] = correlationId
            });
        }

        _telemetryClient.Flush();

        var connStr = _telemetryConfiguration.ConnectionString;
        var hasConnStr = !string.IsNullOrWhiteSpace(connStr);
        var endpoint = hasConnStr
            ? System.Text.RegularExpressions.Regex.Match(connStr, @"IngestionEndpoint=([^;]+)").Groups[1].Value
            : null;

        return Ok(new
        {
            status = "Telemetry test events emitted",
            correlationId,
            environment = _environment.EnvironmentName,
            diagnostics = new
            {
                connectionStringConfigured = hasConnStr,
                connectionStringSource = GetConnectionStringSource(),
                ingestionEndpoint = endpoint,
                instrumentationKeyPresent = !string.IsNullOrWhiteSpace(_telemetryConfiguration.InstrumentationKey),
                telemetryChannelType = _telemetryConfiguration.TelemetryChannel?.GetType().Name,
                activeSamplingEnabled = _configuration.GetValue("ApplicationInsights:EnableAdaptiveSampling", false),
                samplingMaxItemsPerSecond = _configuration["ApplicationInsights:SamplingSettings:MaxTelemetryItemsPerSecond"]
            },
            emittedTelemetry = new[]
            {
                "ILogger.LogInformation",
                "ILogger.LogWarning",
                "ILogger.LogError",
                "TelemetryClient.TrackEvent (AI-TEST-Event)",
                "TelemetryClient.TrackTrace",
                "TelemetryClient.TrackException"
            },
            timestamp = DateTime.UtcNow,
            note = "Check Application Insights ? Logs (KQL) in 2-5 minutes. Use the correlationId to filter."
        });
    }

    private string GetConnectionStringSource()
    {
        if (!string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("APPLICATIONINSIGHTS_CONNECTION_STRING")))
            return "Environment Variable (APPLICATIONINSIGHTS_CONNECTION_STRING)";

        if (!string.IsNullOrWhiteSpace(_configuration["ApplicationInsights:ConnectionString"]))
            return "Configuration (ApplicationInsights:ConnectionString)";

        return "NOT CONFIGURED";
    }
}
