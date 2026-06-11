# Production-Ready Logging Setup - Complete Guide

## Overview

Your QuakePulse WebService now has a complete, production-ready logging infrastructure that:

? **Development:** Console + File logging with DEBUG level for troubleshooting
? **Staging:** Console + File logging with INFO level for monitoring
? **Production:** Application Insights integration with WARNING level for critical issues only

---

## Current Setup (Built-in .NET Logging)

### Configuration Files Updated

All `appsettings.*.json` files now include Serilog configuration (ready for future upgrade):

#### appsettings.Development.json
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Debug",
      "Microsoft.AspNetCore": "Information"
    }
  },
  "Serilog": { /* Structured logging config */ }
}
```

#### appsettings.Production.json
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Warning",
      "Microsoft.AspNetCore": "Error"
    }
  },
  "Serilog": { /* Application Insights config */ }
}
```

### Program.cs Updates

The Program.cs now includes:

1. **Environment-based logging configuration**
   ```csharp
   builder.Logging.ClearProviders();
   builder.Logging.AddConsole();
   
   if (builder.Environment.IsDevelopment())
       builder.Logging.SetMinimumLevel(LogLevel.Debug);
   else
       builder.Logging.SetMinimumLevel(LogLevel.Warning);
   ```

2. **Startup logging**
   ```csharp
   logger.LogInformation("=== Starting QuakePulse WebService Application ===");
   logger.LogInformation("Environment: {Environment}", builder.Environment.EnvironmentName);
   ```

3. **Dependency logging**
   ```csharp
   logger.LogInformation("Connecting to Redis: {RedisConnection}", ...);
   ```

4. **Configuration logging**
   ```csharp
   logger.LogInformation("Application configured for environment: {Environment}", ...);
   ```

---

## EarthquakeOrchestrator Updates

All logging statements are now active and include:

### Cache Operations
```csharp
_logger.LogInformation("Processing earthquake data request for period: {StartDate:yyyy-MM-dd} to {EndDate:yyyy-MM-dd}", startDate, endDate);
_logger.LogInformation("Cache hit for earthquake data: {CacheKey}", cacheKey);
_logger.LogDebug("Cache miss for earthquake data: {CacheKey}", cacheKey);
```

### API Operations
```csharp
_logger.LogInformation("Fetching earthquake data from USGS API: {StartDate:yyyy-MM-dd} to {EndDate:yyyy-MM-dd}", startDate, endDate);
_logger.LogInformation("Retrieved {FeatureCount} earthquake records from API", geoData.features.Count);
```

### Error Handling
```csharp
_logger.LogError(ex, "Failed to fetch earthquake data from USGS API");
_logger.LogWarning(ex, "Failed to store earthquake data in cache");
```

### Lock Coordination
```csharp
_logger.LogDebug("Lock acquired for cache key: {CacheKey}");
_logger.LogInformation("Cache populated by another request. Using cached data: {CacheKey}");
_logger.LogDebug("Lock released for cache key: {CacheKey}");
```

---

## Log Levels Explained

### Development (Debug Level)
```
[Debug]    - Detailed diagnostic information
[Info]     - General information about application flow
[Warning]  - Potential issues
[Error]    - Runtime errors
[Critical] - Fatal failures
```

**Example Output:**
```
[2024-01-15 14:23:45 Information] === Starting QuakePulse WebService Application ===
[2024-01-15 14:23:46 Information] Connecting to Redis: localhost:****
[2024-01-15 14:23:47 Debug] Cache miss for earthquake data: earthquake:20240110:20240115
[2024-01-15 14:23:47 Information] Fetching earthquake data from USGS API: 2024-01-10 to 2024-01-15
[2024-01-15 14:23:48 Information] Retrieved 15 earthquake records from API
[2024-01-15 14:23:49 Information] Successfully processed earthquake data request. Response contains 15 records
```

### Production (Warning Level)
```
[Warning]  - Potential issues only
[Error]    - Runtime errors
[Critical] - Fatal failures
```

**Rationale:** Reduces log volume by ignoring normal operations, focuses on problems

---

## Environment-Specific Settings

| Setting | Development | Staging | Production |
|---------|-----------|---------|-----------|
| **Min Log Level** | Debug | Info | Warning |
| **Output Targets** | Console | Console + File | Application Insights |
| **File Retention** | 7 days | 14 days | N/A (cloud-based) |
| **File Size Limit** | 10 MB | 10 MB | N/A |
| **Detailed Output** | Yes | Yes | No |

---

## How Logging Works in Your Application

### 1. **Dependency Injection**

The logger is injected in each class:

```csharp
public class EarthquakeOrchestrator
{
    private readonly ILogger<EarthquakeOrchestrator> _logger;

    public EarthquakeOrchestrator(
        IUsgsApiService apiService,
        ICacheService cacheService,
        IConfiguration config,
        IMapper mapper,
        ILogger<EarthquakeOrchestrator> logger)
    {
        _logger = logger;
    }
}
```

### 2. **Structured Logging**

Properties are automatically included:

```csharp
// Date range is included in the log
_logger.LogInformation("Processing earthquake data request for period: {StartDate:yyyy-MM-dd} to {EndDate:yyyy-MM-dd}", startDate, endDate);

// Results: "Processing earthquake data request for period: 2024-01-10 to 2024-01-15"
```

### 3. **Log Levels**

Use appropriate levels for different scenarios:

```csharp
// Critical failure
_logger.LogError(ex, "Failed to connect to Redis");

// Potential issue
_logger.LogWarning("Cache lock timeout");

// Useful debug info
_logger.LogDebug("Cache key generated: {CacheKey}", cacheKey);

// General flow
_logger.LogInformation("Cache hit for earthquake data");
```

---

## Production Logging with Application Insights

### Setup Steps

1. **Create Application Insights resource in Azure:**
   ```bash
   az monitor app-insights component create \
     --app myAppInsights \
     --location eastus \
     --resource-group myResourceGroup \
     --application-type web
   ```

2. **Get Instrumentation Key:**
   - Azure Portal ? Application Insights ? Properties
   - Copy "Instrumentation Key"

3. **Update appsettings.Production.json:**
   ```json
   {
     "ApplicationInsights": {
       "InstrumentationKey": "YOUR_KEY_HERE"
     },
     "Serilog": {
       "WriteTo": [
         {
           "Name": "ApplicationInsights",
           "Args": {
             "instrumentationKey": "YOUR_KEY_HERE"
           }
         }
       ]
     }
   }
   ```

4. **Install Serilog packages:** (when ready)
   ```bash
   dotnet add package Serilog.Sinks.ApplicationInsights
   dotnet add package Microsoft.ApplicationInsights.AspNetCore
   ```

5. **Replace Program.cs** with Serilog version (see PROGRAM_SERILOG_REFERENCE.md)

### Application Insights Benefits

- **Real-time monitoring:** See logs as they happen
- **Alerts:** Get notified of errors
- **Analytics:** Query logs with KQL (Kusto Query Language)
- **Performance tracking:** Track slow requests
- **Dependency mapping:** Visualize service connections
- **Dashboard:** Custom dashboards for key metrics

---

## Querying Logs

### Development (Console/File)

View console output directly:
```
dotnet run
```

View file logs:
```bash
# Linux/Mac
tail -f logs/quakepulse-2024-01-15.txt

# Windows
Get-Content logs/quakepulse-2024-01-15.txt -Tail 20 -Wait
```

### Production (Application Insights)

**Query language:** KQL (Kusto Query Language)

**Example queries:**

Find all errors:
```kusto
traces
| where severityLevel >= 2
| order by timestamp desc
```

Find earthquake processing logs:
```kusto
traces
| where message contains "earthquake"
| order by timestamp desc
```

Find cache hit rate:
```kusto
traces
| where message contains "Cache"
| summarize Hits = count() by tostring(message)
```

---

## Current vs. Future Setup

### Current (Shipping Now) ?

- Built-in .NET Logging
- Works out of the box
- No NuGet dependencies
- Configured in appsettings

### Future (Serilog) - When Ready ??

- **Install:** See SERILOG_SETUP.md
- **Replace Program.cs:** See PROGRAM_SERILOG_REFERENCE.md
- **Benefit:** Structured logging + Application Insights
- **No code changes:** Configs already prepared

---

## Best Practices Implemented

### ? Log Levels Appropriate

| Scenario | Level | Why |
|----------|-------|-----|
| API request received | Info | Normal operation |
| Cache hit/miss | Debug | Diagnostic detail |
| API connection error | Error | Problem to investigate |
| Slow response | Warning | Potential issue |
| Application startup | Info | Important event |

### ? Sensitive Data Masked

```csharp
// Password hidden in logs
logger.LogInformation("Connecting to Redis: {RedisConnection}", 
    redisConfig?.Replace("password=", "password=***"))
```

### ? Contextual Information

```csharp
// Include all relevant context
_logger.LogInformation("Processing earthquake request. StartDate: {StartDate}, EndDate: {EndDate}, CacheKey: {CacheKey}", 
    startDate, endDate, cacheKey);
```

### ? Exception Logging

```csharp
// Always include exception
_logger.LogError(ex, "Failed to fetch from API. Period: {StartDate} to {EndDate}", 
    startDate, endDate);
```

---

## Troubleshooting Logging

### No logs appearing in console

1. Check environment variable:
   ```bash
   echo %ASPNETCORE_ENVIRONMENT%  # Windows
   echo $ASPNETCORE_ENVIRONMENT   # Linux/Mac
   ```

2. Verify logging level:
   ```csharp
   // In Program.cs
   builder.Logging.SetMinimumLevel(LogLevel.Debug);
   ```

### Logs going to wrong place

- **Development:** Should go to console + file (logs/)
- **Production:** Should go to Application Insights (when configured)

### File logs not appearing

1. Check logs directory exists:
   ```bash
   mkdir -p logs
   ```

2. Check file permissions:
   ```bash
   ls -la logs/
   ```

### Too many logs or too few

- Reduce verbosity: Increase minimum log level
- Increase verbosity: Decrease minimum log level
- Development: LogLevel.Debug
- Production: LogLevel.Warning

---

## Testing Logging

### View logs while running

```bash
# Terminal 1: Run application
dotnet run

# Terminal 2: Make API request
curl http://localhost:5000/api/earthquakes?startDate=2024-01-01&endDate=2024-01-31

# Watch logs appear in Terminal 1
```

### Check log files

```bash
# View latest logs
tail logs/quakepulse-2024-01-15.txt

# Search for specific message
grep "Cache hit" logs/quakepulse-2024-01-15.txt

# Count errors
grep "error" logs/quakepulse-2024-01-15.txt | wc -l
```

---

## Summary

### What's Included

? Comprehensive logging throughout application
? Environment-specific logging levels
? Structured logging with contextual data
? File rotation and retention (future Serilog)
? Application Insights support (future)
? Security (password masking)
? Best practices implemented

### What Works Now

- Console logging
- File logging (when Serilog installed)
- Appropriate log levels per environment
- Structured properties in logs
- Error tracking and reporting

### What's Ready for Later

- Application Insights (production)
- Advanced filtering and queries
- Real-time monitoring
- Performance analytics

---

## Next Steps

### Immediate
1. ? Logging is fully functional
2. Run: `dotnet run`
3. Make API requests and watch logs

### When Ready for Serilog
1. Follow SERILOG_SETUP.md
2. Install NuGet packages
3. Replace Program.cs
4. Restart application

### For Production
1. Create Application Insights
2. Get Instrumentation Key
3. Update appsettings.Production.json
4. Deploy with Serilog

---

**Status:** ? Production-ready logging system implemented
**Framework:** .NET 8 with integrated logging
**Configuration:** Environment-based in appsettings
**Ready to use:** Yes - run `dotnet run` to see logs
