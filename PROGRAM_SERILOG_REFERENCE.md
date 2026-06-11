# Program-Serilog.cs - Advanced Production Logging Setup

> **Note:** This is an alternative Program.cs that uses Serilog for advanced structured logging.
> To use this, install Serilog NuGet packages first (see SERILOG_SETUP.md)

```csharp
using Polly;
using Serilog;
using Serilog.Events;
using StackExchange.Redis;
using QuakePulse_WebService.Caching;
using QuakePulse_WebService.Integration;
using QuakePulse_WebService.Mappers.Profiles;
using QuakePulse_WebService.Orchestration;
using QuakePulse_WebService.Services;

// Configure Serilog before building the host
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    Log.Information("=== Starting QuakePulse WebService Application ===");

    var builder = WebApplication.CreateBuilder(args);

    // Read Serilog configuration from appsettings
    builder.Host.UseSerilog((context, services, configuration) =>
        configuration
            .ReadFrom.Configuration(context.Configuration)
            .ReadFrom.Services(services)
            .Enrich.FromLogContext()
            .Enrich.WithMachineName()
            .Enrich.WithThreadId()
            .Enrich.WithProperty("Application", "QuakePulse.WebService")
            .Enrich.WithProperty("Environment", context.HostingEnvironment.EnvironmentName)
    );

    var usgsApiBaseAddress = builder.Configuration["ExternalApis:UsgsEarthquakeApi:BaseAddress"]
        ?? throw new InvalidOperationException("USGS API base address is not configured.");

    // Add services to the container
    builder.Services.AddControllers();
    builder.Services.AddScoped<IEarthquakeOrchestrator, EarthquakeOrchestrator>();
    builder.Services.AddScoped<IEarthquakeService, EarthquakeService>();

    builder.Services.AddHttpClient<IUsgsApiService, UsgsApiService>(client =>
    {
        client.BaseAddress = new Uri(usgsApiBaseAddress);
        client.Timeout = TimeSpan.FromSeconds(30);
    })
        .AddTransientHttpErrorPolicy(policy =>
            policy.WaitAndRetryAsync(3, retryAttempt =>
            {
                var delay = TimeSpan.FromSeconds(Math.Pow(2, retryAttempt));
                Log.Debug("Retrying USGS API call. Attempt {RetryAttempt}, Delay: {Delay}ms", 
                    retryAttempt + 1, delay.TotalMilliseconds);
                return delay;
            }));

    builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
    {
        var logger = sp.GetRequiredService<ILogger<IConnectionMultiplexer>>();
        var redisConfig = builder.Configuration.GetSection("Redis")["ConnectionString"];
        
        try
        {
            logger.LogInformation("Connecting to Redis: {RedisConnection}", 
                redisConfig?.Replace("password=", "password=***") ?? "localhost:6379");
            return ConnectionMultiplexer.Connect(redisConfig ?? "localhost:6379");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to connect to Redis");
            throw;
        }
    });

    builder.Services.AddScoped<ICacheService, RedisCacheService>();

    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen();
    builder.Services.AddAutoMapper(typeof(EarthquakeProfile));

    var app = builder.Build();

    // Log configuration info at startup
    Log.Information("Application configured for environment: {Environment}", 
        app.Environment.EnvironmentName);
    Log.Information("Redis TTL (minutes): {TTL}", 
        builder.Configuration.GetValue<int>("Redis:DefaultTTLMinutes"));

    // Configure the HTTP request pipeline
    if (app.Environment.IsDevelopment())
    {
        Log.Information("Development environment - enabling Swagger and detailed logging");
        app.UseSwagger();
        app.UseSwaggerUI();
    }
    else
    {
        Log.Information("Production environment - Swagger disabled, minimal logging active");
    }

    app.UseHttpsRedirection();
    app.UseAuthorization();
    app.MapControllers();

    Log.Information("=== QuakePulse WebService Application Started Successfully ===");
    await app.RunAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.Information("=== Shutting down QuakePulse WebService Application ===");
    await Log.CloseAndFlushAsync();
}
```

## Key Differences from Default Program.cs

### 1. **Structured Logging**
- Every log entry includes context and structured properties
- Machine name, thread ID, and application name automatically included
- Can query logs programmatically

### 2. **File-Based Logging (Development)**
- Logs written to `logs/quakepulse-YYYY-MM-DD.txt` files
- Daily rolling with size-based limits
- Detailed formatting with timestamps and context

### 3. **Application Insights (Production)**
- All logs automatically sent to Azure Application Insights
- Real-time monitoring and alerting
- Performance tracking and diagnostics

### 4. **Graceful Shutdown**
- `Log.CloseAndFlushAsync()` ensures all logs are flushed before exit
- Important for production reliability

## How to Switch to Serilog

1. **Install Serilog packages:**
   ```bash
   dotnet add package Serilog Serilog.AspNetCore Serilog.Sinks.Console Serilog.Sinks.File Serilog.Sinks.ApplicationInsights
   ```

2. **Replace Program.cs:**
   - Copy the code from this file
   - Paste into your Program.cs
   - Build and run

3. **Verify Installation:**
   ```bash
   dotnet build
   dotnet run
   ```
   You should see logs in console and in `logs/` folder

## Log Levels with Serilog

| Level | Usage | Example |
|-------|-------|---------|
| **Verbose** | Trace program flow | "Entering method X" |
| **Debug** | Detailed diagnostic | "Cache key generated: xyz" |
| **Information** | General information | "Application started" |
| **Warning** | Potential issues | "Slow API response detected" |
| **Error** | Runtime errors | "Failed to connect to Redis" |
| **Fatal** | Critical failures | "Application cannot continue" |

## Configuration Reference

All Serilog settings in `appsettings.json`:

```json
{
  "Serilog": {
    "MinimumLevel": {
      "Default": "Information",
      "Override": {
        "Microsoft": "Warning",
        "Microsoft.AspNetCore": "Warning"
      }
    },
    "WriteTo": [
      {
        "Name": "Console",
        "Args": {
          "theme": "Serilog.Sinks.SystemConsole.Themes.AnsiConsoleTheme::Code",
          "outputTemplate": "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}"
        }
      },
      {
        "Name": "File",
        "Args": {
          "path": "logs/quakepulse-.txt",
          "rollingInterval": "Day",
          "fileSizeLimitBytes": 10485760,
          "retainedFileCountLimit": 7
        }
      }
    ],
    "Enrich": [
      "FromLogContext",
      "WithMachineName",
      "WithThreadId"
    ]
  }
}
```

---

**Status:** Advanced logging option - requires NuGet packages
**Recommended for:** Production deployments
**Installation time:** ~2 minutes
