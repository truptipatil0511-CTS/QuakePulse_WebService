namespace QuakePulse_WebService.Common;

public class ApiSettings
{
    public int HttpClientTimeoutSeconds { get; set; } = 30;
    public int RetryAttempts { get; set; } = 3;
}

public class CorsSettings
{
    public string PolicyName { get; set; } = "AllowAngularApp";
    public string[] AllowedOrigins { get; set; } = Array.Empty<string>();
}

public class CorrelationIdSettings
{
    public string HeaderName { get; set; } = "X-Correlation-ID";
}

public class HealthCheckSettings
{
    public string Path { get; set; } = "/api/health";
}

public class EarthquakeDefaults
{
    public int FilterLookbackDays { get; set; } = 30;
    public int MapLookbackDays { get; set; } = 7;
    public int TableLookbackDays { get; set; } = 7;
    public int LatestLimit { get; set; } = 10;
}
