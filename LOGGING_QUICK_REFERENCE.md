# Logging Quick Reference

## Current Setup Status

? **Built-in .NET Logging** - Active now
?? **Serilog Ready** - Configuration prepared, install when needed
?? **Application Insights Ready** - Configuration prepared, setup when needed

---

## Using Logging in Your Code

### Inject Logger

```csharp
public class MyService
{
    private readonly ILogger<MyService> _logger;

    public MyService(ILogger<MyService> logger)
    {
        _logger = logger;
    }
}
```

### Log at Different Levels

```csharp
// Information - General flow
_logger.LogInformation("Starting process for {UserId}", userId);

// Debug - Detailed diagnostic
_logger.LogDebug("Cache key generated: {CacheKey}", cacheKey);

// Warning - Potential issue
_logger.LogWarning("Response time exceeded threshold: {Duration}ms", duration);

// Error - Runtime error
_logger.LogError(ex, "Failed to save user {UserId}", userId);

// Critical - Fatal error
_logger.LogCritical(ex, "Database connection lost");
```

### With Properties

```csharp
// Single property
_logger.LogInformation("User login: {UserId}", userId);

// Multiple properties
_logger.LogInformation("Processing request. UserId: {UserId}, Action: {Action}, Duration: {Duration}ms", 
    userId, action, duration);

// With exception
_logger.LogError(ex, "Failed to process file {FileName}", fileName);
```

---

## Configuration by Environment

### Development
```
Logging Level: DEBUG
Output: Console
Files: logs/quakepulse-YYYY-MM-DD.txt (7 days retained)
Use: Full details for troubleshooting
```

### Staging
```
Logging Level: INFORMATION
Output: Console + Files
Files: logs/quakepulse-YYYY-MM-DD.txt (14 days retained)
Use: Monitor application behavior
```

### Production
```
Logging Level: WARNING
Output: Application Insights (when configured)
Files: None (cloud-based)
Use: Alert on errors only
```

---

## Viewing Logs

### While Running (Development)

```bash
# Watch logs in real-time
dotnet run
# Logs appear in console as they happen
```

### In Files (Development/Staging)

```bash
# Linux/Mac - Watch new logs
tail -f logs/quakepulse-2024-01-15.txt

# Windows - View last 20 lines
Get-Content logs/quakepulse-2024-01-15.txt -Tail 20

# Search for specific message
grep "error" logs/quakepulse-2024-01-15.txt
```

### In Application Insights (Production)

```kusto
// All logs
traces
| order by timestamp desc

// Only errors
traces
| where severityLevel >= 2

// Specific service
traces
| where message contains "Orchestrator"
```

---

## Best Practices

### ? DO

```csharp
// ? Include context
_logger.LogInformation("User {UserId} logged in from {IpAddress}", userId, ipAddress);

// ? Use appropriate level
if (timeoutSeconds > 30) _logger.LogWarning("Slow request detected");

// ? Include exceptions
_logger.LogError(ex, "Failed to process {ItemId}", itemId);

// ? Format dates
_logger.LogInformation("Process ran on {Date:yyyy-MM-dd}", date);
```

### ? DON'T

```csharp
// ? Log sensitive data
_logger.LogInformation("Password: {Password}", password); // NO!

// ? Ignore exceptions
_logger.LogError("Something went wrong"); // Missing exception

// ? Use string concatenation
_logger.LogInformation($"User {userId} logged in"); // Use structured logging instead

// ? Log at wrong level
_logger.LogError("Cache miss detected"); // Should be Debug or Info
```

---

## Performance Tips

### Development
- Verbose logging is fine
- Log everything you need for debugging
- Files are automatically rotated

### Production
- Only log warnings and errors
- Reduces I/O and network overhead
- Faster response times

### Staging
- Info level balances detail and performance
- Good for monitoring behavior
- Helps validate production setup

---

## Common Patterns

### Cache Operations

```csharp
// Check cache
if (cachedData != null)
{
    _logger.LogInformation("Cache hit: {CacheKey}", cacheKey);
    return cachedData;
}

_logger.LogDebug("Cache miss: {CacheKey}", cacheKey);
```

### API Calls

```csharp
try
{
    _logger.LogInformation("Calling API: {Url}", url);
    var result = await _apiClient.GetAsync(url);
    _logger.LogInformation("API call succeeded. Status: {StatusCode}", result.StatusCode);
    return result;
}
catch (Exception ex)
{
    _logger.LogError(ex, "API call failed. Url: {Url}", url);
    throw;
}
```

### Database Operations

```csharp
try
{
    _logger.LogDebug("Saving user {UserId} to database", userId);
    await _db.SaveUserAsync(user);
    _logger.LogInformation("User saved successfully. UserId: {UserId}", userId);
}
catch (Exception ex)
{
    _logger.LogError(ex, "Failed to save user {UserId}", userId);
    throw;
}
```

---

## Enabling Serilog

When ready for advanced logging:

### Step 1: Install Packages
```bash
dotnet add package Serilog Serilog.AspNetCore Serilog.Sinks.Console Serilog.Sinks.File
```

### Step 2: Update Program.cs
See PROGRAM_SERILOG_REFERENCE.md

### Step 3: Run
```bash
dotnet build
dotnet run
```

### Step 4: View Structured Logs
Logs now include machine name, thread ID, and rich context

---

## Log File Locations

### Development
```
logs/quakepulse-2024-01-15.txt
logs/quakepulse-2024-01-14.txt
logs/quakepulse-2024-01-13.txt
(keeps 7 days)
```

### Staging
```
logs/quakepulse-2024-01-15.txt
logs/quakepulse-2024-01-14.txt
... (keeps 14 days)
```

### Production
No files (uses Application Insights)

---

## Troubleshooting

| Problem | Solution |
|---------|----------|
| No logs showing | Check ASPNETCORE_ENVIRONMENT is set |
| Logs in wrong place | Verify environment matches config |
| Too much logging | Reduce LogLevel in appsettings |
| Not enough logging | Set LogLevel to Debug |
| Can't find log files | Check logs/ directory exists |
| Old logs not deleted | Manual cleanup (retention configured in appsettings) |

---

## Summary

**Current State:** ? Logging fully functional
**Built-in .NET:** ? Console logging
**Serilog:** ?? Configuration ready, install when needed
**Application Insights:** ?? Configuration ready, setup when needed

**Start using:** `dotnet run` and watch console logs

**Next level:** Follow SERILOG_SETUP.md when ready

---

*For detailed information, see LOGGING_GUIDE.md*
