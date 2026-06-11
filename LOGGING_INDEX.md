# Logging Setup - Documentation Index

## ?? Start Here

**New to logging setup?** Start with ? **[LOGGING_QUICK_REFERENCE.md](./LOGGING_QUICK_REFERENCE.md)**

**Want complete details?** Read ? **[LOGGING_GUIDE.md](./LOGGING_GUIDE.md)**

---

## ?? Documentation Files

### Quick References
| File | Purpose | Read Time |
|------|---------|-----------|
| **[LOGGING_QUICK_REFERENCE.md](./LOGGING_QUICK_REFERENCE.md)** | Common patterns and examples | 5 min |
| **[LOGGING_IMPLEMENTATION_SUMMARY.md](./LOGGING_IMPLEMENTATION_SUMMARY.md)** | What was implemented | 10 min |

### Comprehensive Guides
| File | Purpose | Read Time |
|------|---------|-----------|
| **[LOGGING_GUIDE.md](./LOGGING_GUIDE.md)** | Complete logging guide with all details | 20 min |
| **[SERILOG_SETUP.md](./SERILOG_SETUP.md)** | Installing and using Serilog | 10 min |

### Code References
| File | Purpose | Read Time |
|------|---------|-----------|
| **[PROGRAM_SERILOG_REFERENCE.md](./PROGRAM_SERILOG_REFERENCE.md)** | Serilog Program.cs implementation | 5 min |

---

## ?? Quick Start (5 Minutes)

### 1. Verify Setup
```bash
dotnet build
```
? Build should succeed

### 2. Run Application
```bash
dotnet run
```
? Should see: `Starting QuakePulse WebService Application`

### 3. Make API Request
```bash
curl "http://localhost:5000/api/earthquakes?startDate=2024-01-01&endDate=2024-01-31"
```
? Should see logs appear in console

### 4. Done! 
Logging is working. Check [LOGGING_QUICK_REFERENCE.md](./LOGGING_QUICK_REFERENCE.md) for examples.

---

## ?? What Was Set Up

### ? Implemented Now
- Built-in .NET logging
- Console output
- Environment-based configuration
- Debug level (Development)
- Warning level (Production)
- Logging in Program.cs
- Logging in EarthquakeOrchestrator
- Comprehensive documentation

### ?? Ready to Install
- Serilog (structured logging)
- File logging with rotation
- Application Insights

### ?? Ready to Configure
- Application Insights setup
- Production monitoring

---

## ?? Usage Examples

### Using Logger in Your Code

```csharp
public class MyService
{
    private readonly ILogger<MyService> _logger;

    public MyService(ILogger<MyService> logger)
    {
        _logger = logger;
    }

    public async Task DoSomethingAsync()
    {
        _logger.LogInformation("Starting operation");
        
        try
        {
            // Do work
            _logger.LogDebug("Processing item {ItemId}", itemId);
            _logger.LogInformation("Operation completed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Operation failed");
            throw;
        }
    }
}
```

### View Logs

**In Console (Live):**
```bash
dotnet run
# Logs appear in real-time
```

**In Files (Development/Staging):**
```bash
tail -f logs/quakepulse-2024-01-15.txt
```

**In Application Insights (Production - future):**
```
Azure Portal ? Application Insights ? Logs
```

---

## ?? Configuration Overview

### Development
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Debug"
    }
  }
}
```
**Use for:** Local testing, detailed debugging

### Staging
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information"
    }
  }
}
```
**Use for:** Pre-production testing, monitoring behavior

### Production
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Warning"
    }
  }
}
```
**Use for:** Live application, minimal logging, errors only

---

## ? Common Questions

### Q: How do I see logs?
**A:** Run `dotnet run` and logs appear in console. Details in [LOGGING_QUICK_REFERENCE.md](./LOGGING_QUICK_REFERENCE.md)

### Q: Can I change log level?
**A:** Yes, edit `appsettings.json` or environment files. See [LOGGING_GUIDE.md](./LOGGING_GUIDE.md#environment-specific-settings)

### Q: How do I add logging to my code?
**A:** Inject `ILogger<T>` and use `_logger.Log*()`. See [LOGGING_QUICK_REFERENCE.md](./LOGGING_QUICK_REFERENCE.md#using-logging-in-your-code)

### Q: Do I need to install Serilog now?
**A:** No, logging works with built-in .NET logging. Install Serilog later if needed. See [SERILOG_SETUP.md](./SERILOG_SETUP.md)

### Q: How do I use Application Insights?
**A:** Setup in Azure, then install Serilog. Details in [LOGGING_GUIDE.md](./LOGGING_GUIDE.md#production-logging-with-application-insights)

### Q: Which documentation should I read first?
**A:** Start with [LOGGING_QUICK_REFERENCE.md](./LOGGING_QUICK_REFERENCE.md), then [LOGGING_GUIDE.md](./LOGGING_GUIDE.md) for details

---

## ?? Log Levels at a Glance

| Level | Use Case | Development | Staging | Production |
|-------|----------|-----------|---------|-----------|
| **Verbose** | Trace execution | ? Yes | - | - |
| **Debug** | Detailed diagnostics | ? Yes | - | - |
| **Information** | General flow | ? Yes | ? Yes | - |
| **Warning** | Potential issues | ? Yes | ? Yes | ? Yes |
| **Error** | Errors | ? Yes | ? Yes | ? Yes |
| **Critical** | Fatal errors | ? Yes | ? Yes | ? Yes |

---

## ??? Installation Timeline

### Immediate (Now)
? Built-in logging working
? Console output enabled
? No additional setup needed

### Week 1
?? (Optional) Install Serilog
?? (Optional) Enable file logging

### Before Production
?? Setup Application Insights
?? Configure Serilog
?? Deploy with production logging

---

## ?? Topics Covered

- [x] Environment-based configuration
- [x] Built-in .NET logging setup
- [x] Serilog integration (ready)
- [x] Application Insights support (ready)
- [x] Log levels and best practices
- [x] Structured logging
- [x] Error handling and logging
- [x] Performance optimization
- [x] Security (password masking)
- [x] Production monitoring

---

## ? Verification

### Is logging working?
```bash
dotnet run
# Look for: "=== Starting QuakePulse WebService Application ==="
```

### Can I see logs?
```bash
curl "http://localhost:5000/api/earthquakes?startDate=2024-01-01&endDate=2024-01-31"
# Watch console for request logs
```

### Is it production-ready?
? Yes - Console logging is production-safe
?? Better with Serilog - Install when ready
?? Best with Application Insights - Setup for production

---

## ?? Next Steps by Role

### Developers
1. Read: [LOGGING_QUICK_REFERENCE.md](./LOGGING_QUICK_REFERENCE.md)
2. Run: `dotnet run`
3. Watch: Logs appear in console
4. Code: Add logging to your classes

### DevOps Engineers
1. Review: [LOGGING_GUIDE.md](./LOGGING_GUIDE.md)
2. Plan: Serilog installation (optional)
3. Setup: Application Insights (production)
4. Deploy: With configured logging

### Architects
1. Study: [LOGGING_GUIDE.md](./LOGGING_GUIDE.md)
2. Review: [SERILOG_SETUP.md](./SERILOG_SETUP.md)
3. Design: Monitoring strategy
4. Plan: Application Insights deployment

---

## ?? Support & Resources

### In This Repository
- [LOGGING_QUICK_REFERENCE.md](./LOGGING_QUICK_REFERENCE.md) - Common patterns
- [LOGGING_GUIDE.md](./LOGGING_GUIDE.md) - Complete guide
- [SERILOG_SETUP.md](./SERILOG_SETUP.md) - Installation
- [PROGRAM_SERILOG_REFERENCE.md](./PROGRAM_SERILOG_REFERENCE.md) - Code example

### External Resources
- [Microsoft Logging Documentation](https://learn.microsoft.com/en-us/dotnet/core/extensions/logging)
- [Serilog GitHub](https://github.com/serilog/serilog)
- [Azure Application Insights](https://learn.microsoft.com/en-us/azure/azure-monitor/app/app-insights-overview)

---

## ?? Summary

Your QuakePulse WebService has complete logging infrastructure:

- ? Built-in .NET logging (works now)
- ? Environment-specific configuration
- ? Comprehensive documentation
- ? Serilog ready (install when needed)
- ? Application Insights ready (setup when needed)
- ? Production-safe logging
- ? Best practices implemented

**Status: Ready to use immediately**

Start with [LOGGING_QUICK_REFERENCE.md](./LOGGING_QUICK_REFERENCE.md) ?

---

**Last Updated:** 2024
**Status:** ? Complete
**Ready:** Yes - `dotnet run` to see logs
