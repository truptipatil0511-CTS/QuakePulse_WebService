# Configuration Files Comparison

## Side-by-Side Configuration Comparison

### Logging Configuration

```
DEVELOPMENT                  STAGING                      PRODUCTION
?????????????????           ??????????????????           ??????????????????
"Default": "Debug"          "Default": "Information"     "Default": "Warning"
(Most Verbose)              (Balanced)                   (Least Verbose)
```

### Redis Configuration

```
DEVELOPMENT                           STAGING / PRODUCTION
????????????????????????????????????  ?????????????????????????????????????????
ConnectionString:                     ConnectionString:
  localhost:6379                        *.redis.cache.windows.net:6380,
  (No SSL)                              password=key,ssl=true
  
TTL: 10 minutes                       TTL: 20-30 minutes
(Short - Quick testing)               (Long - Reduced API calls)
```

### HTTP Client Settings

```
?????????????????????????????????????????????????????????????
? Setting      ? Development  ? Staging      ? Production   ?
?????????????????????????????????????????????????????????????
? Timeout      ? 30 seconds   ? 35 seconds   ? 45 seconds   ?
? Retries      ? 3            ? 4            ? 5            ?
? Use Case     ? Fast feedback? Balanced     ? Reliability  ?
?????????????????????????????????????????????????????????????
```

### Cache Stampede Prevention

```
???????????????????????????????????????????????????????????????????
? Setting            ? Development  ? Staging      ? Production   ?
???????????????????????????????????????????????????????????????????
? Lock Duration      ? 30 sec       ? 45 sec       ? 60 sec       ?
? Lock Wait Time     ? 100 ms       ? 150 ms       ? 200 ms       ?
? Max Wait Attempts  ? 300          ? 200          ? 150          ?
? Max Total Wait     ? 30 seconds   ? 30 seconds   ? 30 seconds   ?
???????????????????????????????????????????????????????????????????
```

## Complete Configuration Comparison

### appsettings.Development.json
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Debug",              ? Most detailed logging
      "Microsoft.AspNetCore": "Information",
      "Microsoft": "Information"
    }
  },
  "AllowedHosts": "*",                 ? Accept all hosts
  "Redis": {
    "ConnectionString": "localhost:6379",  ? Local Redis
    "DefaultTTLMinutes": 10            ? Short TTL for testing
  },
  "CacheSettings": {
    "Enabled": true,
    "LockDurationSeconds": 30,         ? Quick lock timeout
    "LockWaitTimeMs": 100,
    "MaxLockWaitAttempts": 300
  },
  "ApiSettings": {
    "HttpClientTimeoutSeconds": 30,    ? Fast timeout for dev feedback
    "RetryAttempts": 3
  }
}
```

### appsettings.Staging.json
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",        ? Moderate logging
      "Microsoft.AspNetCore": "Warning",
      "Microsoft": "Warning"
    }
  },
  "AllowedHosts": "*.staging.yourdomain.com",  ? Restricted hosts
  "Redis": {
    "ConnectionString": "your-staging-redis.redis.cache.windows.net:6380,password=***,ssl=true",
    "DefaultTTLMinutes": 20            ? Medium TTL
  },
  "CacheSettings": {
    "Enabled": true,
    "LockDurationSeconds": 45,         ? Moderate lock timeout
    "LockWaitTimeMs": 150,
    "MaxLockWaitAttempts": 200
  },
  "ApiSettings": {
    "HttpClientTimeoutSeconds": 35,    ? Balanced timeout
    "RetryAttempts": 4
  }
}
```

### appsettings.Production.json
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Warning",            ? Only critical issues
      "Microsoft.AspNetCore": "Error",
      "Microsoft": "Error"
    }
  },
  "AllowedHosts": "*.yourdomain.com",  ? Restricted hosts
  "Redis": {
    "ConnectionString": "your-azure-redis.redis.cache.windows.net:6380,password=***,ssl=true",
    "DefaultTTLMinutes": 30            ? Long TTL for performance
  },
  "CacheSettings": {
    "Enabled": true,
    "LockDurationSeconds": 60,         ? Long lock timeout
    "LockWaitTimeMs": 200,
    "MaxLockWaitAttempts": 150
  },
  "ApiSettings": {
    "HttpClientTimeoutSeconds": 45,    ? Higher tolerance
    "RetryAttempts": 5                 ? More resilient
  }
}
```

## Configuration Decision Matrix

### When to Use Each Environment

```
DEVELOPMENT
?? When: Local machine development
?? Redis: Docker or local installation
?? Logging: Full debug output
?? TTL: Short (test cache behavior frequently)
?? Retries: Minimal (fail fast for testing)

STAGING  
?? When: Testing before production
?? Redis: Azure Redis Cache
?? Logging: Info level (track behavior)
?? TTL: Medium (simulate prod behavior)
?? Retries: Moderate (test resilience)

PRODUCTION
?? When: Live application
?? Redis: Azure Redis Cache
?? Logging: Errors only (minimize noise)
?? TTL: Long (optimize performance)
?? Retries: Maximum (highest reliability)
```

## Environment Variable Examples

### Development
```bash
ASPNETCORE_ENVIRONMENT=Development
Redis__ConnectionString=localhost:6379
```

### Staging
```bash
ASPNETCORE_ENVIRONMENT=Staging
Redis__ConnectionString=staging-redis.redis.cache.windows.net:6380,password=KEY,ssl=true
ASPNETCORE_URLS=https://+:443;http://+:80
```

### Production
```bash
ASPNETCORE_ENVIRONMENT=Production
Redis__ConnectionString=prod-redis.redis.cache.windows.net:6380,password=KEY,ssl=true
ASPNETCORE_URLS=https://+:443
ASPNETCORE_HTTPS_PORT=443
```

## File Size & Complexity

```
appsettings.json           ~200 bytes   (Base configuration)
appsettings.Development.json ~350 bytes   (Slightly larger)
appsettings.Staging.json   ~350 bytes   (Similar to Dev)
appsettings.Production.json ~350 bytes   (Same structure)
```

## Redis Connection String Format

### Development (Local)
```
localhost:6379
```

### Azure (Staging/Production)
```
your-redis-name.redis.cache.windows.net:6380,password=your-access-key,ssl=true
```

Key differences:
- **Port:** 6380 instead of 6379 (SSL port)
- **SSL:** `ssl=true` required for Azure
- **Authentication:** Password required
- **Hostname:** Full Azure resource name

## Summary Table

| Aspect | Development | Staging | Production |
|--------|-----------|---------|-----------|
| **Environment** | Local | Azure | Azure |
| **Logging Level** | DEBUG | INFO | WARNING |
| **Redis Type** | Local | Azure | Azure |
| **Cache TTL** | 10 min | 20 min | 30 min |
| **Timeout** | 30s | 35s | 45s |
| **Retries** | 3 | 4 | 5 |
| **Logging Output** | Verbose | Balanced | Minimal |
| **Security** | Low | High | High |
| **Performance** | Fast feedback | Balanced | Optimized |
| **Failure Mode** | Fail fast | Moderate recovery | High recovery |

## Key Takeaways

? Development = Verbose, local, fast feedback
? Staging = Medium logging, Azure resources, balanced
? Production = Minimal logging, Azure resources, maximum reliability

Always match the environment to your deployment target!

---

**Last Updated:** 2024
**Configuration Scheme:** .NET 8 + ASP.NET Core
