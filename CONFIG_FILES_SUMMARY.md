# Configuration Files Summary

## Project Structure

```
QuakePulse_WebService/
??? Program.cs
??? appsettings.json                 (BASE - Default settings)
??? appsettings.Development.json     (Development environment)
??? appsettings.Staging.json         (Staging environment)
??? appsettings.Production.json      (Production environment)
??? CONFIGURATION_GUIDE.md           (This guide)
?
??? Caching/
?   ??? ICacheService.cs
?   ??? RedisCacheService.cs
?
??? Orchestration/
?   ??? IEarthquakeOrchestrator.cs
?   ??? EarthquakeOrchestrator.cs
?
??? Integration/
?   ??? IUsgsApiService.cs
?
??? ...other files
```

## Configuration Files Overview

### 1. **appsettings.json** - BASE/SHARED DEFAULTS
Contains common settings for all environments:
- External API base URL
- Default logging configuration
- Cache settings structure
- API client settings

### 2. **appsettings.Development.json** - LOCAL DEVELOPMENT
Optimized for local development:
- Debug-level logging
- Local Redis at localhost:6379
- Short cache TTL (10 minutes)
- Fast timeouts for quick feedback

### 3. **appsettings.Staging.json** - PRE-PRODUCTION
Staging/testing environment:
- Information-level logging
- Azure Redis connection
- Medium cache TTL (20 minutes)
- Balanced timeout settings

### 4. **appsettings.Production.json** - PRODUCTION
Hardened production configuration:
- Warning/Error-level logging only
- Azure Redis with SSL
- Long cache TTL (30 minutes)
- Extended timeouts for reliability
- Restricted AllowedHosts

## How Configuration Loading Works

```
1. appsettings.json is ALWAYS loaded first
2. Then appsettings.{ASPNETCORE_ENVIRONMENT}.json is loaded (if exists)
3. Environment-specific settings OVERRIDE base settings
4. Environment variables can OVERRIDE config files
```

## Quick Start

### Run Locally (Development)
```bash
# Automatic (Development is default)
dotnet run

# Or explicit
ASPNETCORE_ENVIRONMENT=Development dotnet run
```

### Run with Docker
```bash
# Uses Development config by default
docker-compose up --build
```

### Deploy to Azure (Production)
```bash
# Build for production
dotnet publish -c Release -o ./publish

# Set environment variable in Azure Portal:
# ASPNETCORE_ENVIRONMENT = Production
```

## Configuration Keys Used in Code

### In Program.cs
```csharp
var usgsApiBaseAddress = builder.Configuration["ExternalApis:UsgsEarthquakeApi:BaseAddress"]
var redisConnection = builder.Configuration["Redis:ConnectionString"]
```

### In EarthquakeOrchestrator.cs
```csharp
var ttlMinutes = _config.GetValue<int>("Redis:DefaultTTLMinutes", 10)
```

## Important Notes

1. **Keep Secrets Out of Version Control**
   - Never commit production Redis passwords in config files
   - Use Azure Key Vault for production secrets
   - Only store dev credentials in Development config

2. **File Copy Strategy**
   - Ensure all appsettings.*.json files are set to "Copy if newer" in Visual Studio
   - Or add to .csproj:
     ```xml
     <ItemGroup>
       <Content Include="appsettings.*.json" CopyToOutputDirectory="PreserveNewest" />
     </ItemGroup>
     ```

3. **Environment Variable Format**
   - Use double underscore `__` instead of colons for nested keys
   - Example: `Redis__ConnectionString` instead of `Redis:ConnectionString`

4. **CI/CD Integration**
   - Set ASPNETCORE_ENVIRONMENT in your pipeline
   - GitHub Actions, Azure DevOps, etc.

## Verification Commands

### Check which config is loaded
```bash
dotnet run  # Look at console output logs
```

### Test Redis connection
```bash
# Development
redis-cli ping

# Production (Azure)
redis-cli -h your-redis.redis.cache.windows.net -p 6380 -a your-password ping
```

### See active configuration
```csharp
// Add to Program.cs before app.Build()
var env = builder.Environment.EnvironmentName;
Console.WriteLine($"Environment: {env}");
```

## Next Steps

1. ? Created configuration files for Dev, Staging, Prod
2. ?? Update Production Redis connection string in appsettings.Production.json
3. ?? Set ASPNETCORE_ENVIRONMENT variable in Azure Portal
4. ?? Enable Azure Key Vault for sensitive data
5. ?? Test locally with Development config
6. ?? Deploy to Azure with Production config

---

**For detailed instructions, see CONFIGURATION_GUIDE.md**
