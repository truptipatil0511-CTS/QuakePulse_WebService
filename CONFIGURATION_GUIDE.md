# Configuration Guide for QuakePulse WebService

## Overview
This guide explains how to use environment-specific configuration files for Development, Staging, and Production environments.

## Configuration Files

### 1. **appsettings.json** (Base/Default)
Contains default values used across all environments. This file is always loaded first.

**Location:** `appsettings.json`

```json
{
  "Logging": { ... },
  "AllowedHosts": "*",
  "Redis": { "ConnectionString": "localhost:6379", ... },
  "CacheSettings": { ... }
}
```

### 2. **appsettings.Development.json** (Local Development)
Overrides base settings for local development with relaxed security and verbose logging.

**Location:** `appsettings.Development.json`

**Key Settings:**
- **Logging Level:** `Debug` - Maximum verbosity for troubleshooting
- **Redis:** `localhost:6379` - Local Redis instance
- **Cache TTL:** 10 minutes - Shorter expiry for testing
- **HTTP Timeout:** 30 seconds
- **Retry Attempts:** 3

**Usage:**
```sh
# Run locally (Development is default)
dotnet run

# Or explicitly
ASPNETCORE_ENVIRONMENT=Development dotnet run
```

### 3. **appsettings.Staging.json** (Pre-Production Testing)
Configuration for staging/testing environment with Azure resources.

**Location:** `appsettings.Staging.json`

**Key Settings:**
- **Logging Level:** `Information` - Balanced logging
- **Redis:** Azure Redis with SSL enabled
- **Cache TTL:** 20 minutes
- **HTTP Timeout:** 35 seconds
- **Retry Attempts:** 4

**Usage:**
```sh
# Deploy to staging
ASPNETCORE_ENVIRONMENT=Staging dotnet run
```

### 4. **appsettings.Production.json** (Production)
Hardened configuration for production environment with minimal logging and optimized performance.

**Location:** `appsettings.Production.json`

**Key Settings:**
- **Logging Level:** `Warning/Error` - Only critical issues logged
- **Redis:** Azure Redis with SSL enabled
- **Cache TTL:** 30 minutes - Longer expiry for reduced API calls
- **HTTP Timeout:** 45 seconds - Higher tolerance for slow networks
- **Retry Attempts:** 5 - More resilient
- **AllowedHosts:** Restricted to specific domain

**Usage:**
```sh
# Deploy to production
ASPNETCORE_ENVIRONMENT=Production dotnet run
```

## Configuration Hierarchy

Settings are applied in this order (later ones override earlier ones):

```
appsettings.json (base)
    ?
appsettings.{ASPNETCORE_ENVIRONMENT}.json (environment-specific)
    ?
Environment Variables
    ?
Command-line arguments
```

## Environment Variables

You can override any setting using environment variables:

### Development
```bash
set ASPNETCORE_ENVIRONMENT=Development
set Redis__ConnectionString=localhost:6379
dotnet run
```

### Production (Azure Example)
```bash
set ASPNETCORE_ENVIRONMENT=Production
set Redis__ConnectionString=your-azure-redis.redis.cache.windows.net:6380,password=yourkey,ssl=true
set ASPNETCORE_HTTPS_PORT=443
dotnet run
```

### Linux/macOS
```bash
export ASPNETCORE_ENVIRONMENT=Production
export Redis__ConnectionString=your-azure-redis.redis.cache.windows.net:6380,password=yourkey,ssl=true
dotnet run
```

## Configuration Settings Explained

### Logging Configuration

| Environment | Default Level | Use Case |
|-----------|--------------|----------|
| Development | `Debug` | Full debugging information |
| Staging | `Information` | Track application flow |
| Production | `Warning/Error` | Only critical errors |

### Redis Configuration

| Environment | Connection String | SSL | TTL |
|-----------|-------------------|-----|-----|
| Development | `localhost:6379` | No | 10 min |
| Staging | `*.redis.cache.windows.net:6380` | Yes | 20 min |
| Production | `*.redis.cache.windows.net:6380` | Yes | 30 min |

### Cache Stampede Prevention

| Setting | Development | Staging | Production |
|---------|------------|---------|-----------|
| Lock Duration | 30 sec | 45 sec | 60 sec |
| Wait Time | 100 ms | 150 ms | 200 ms |
| Max Wait Attempts | 300 | 200 | 150 |

### API Client Settings

| Environment | Timeout | Retries | Use Case |
|-----------|---------|---------|----------|
| Development | 30s | 3 | Fast feedback |
| Staging | 35s | 4 | Test resilience |
| Production | 45s | 5 | High reliability |

## How to Configure for Azure

### Step 1: Update appsettings.Production.json

```json
{
  "Redis": {
    "ConnectionString": "your-redis-name.redis.cache.windows.net:6380,password=your-access-key,ssl=true",
    "DefaultTTLMinutes": 30
  }
}
```

### Step 2: Set Environment Variable in Azure Portal

For Azure App Service:
1. Go to **Configuration** ? **Application Settings**
2. Add new setting:
   - **Name:** `ASPNETCORE_ENVIRONMENT`
   - **Value:** `Production`
3. Add Redis connection:
   - **Name:** `Redis__ConnectionString`
   - **Value:** Your Azure Redis connection string

### Step 3: Publish Application

```bash
# Build for production
dotnet publish -c Release -o ./publish

# Or deploy directly
dotnet azure webapp publish QuakePulse --resource-group MyResourceGroup
```

## Docker Configuration

### Using docker-compose with Environment-Specific Settings

```yaml
version: '3.8'

services:
  app:
    build: .
    environment:
      - ASPNETCORE_ENVIRONMENT=Development
      - Redis__ConnectionString=redis:6379
    depends_on:
      - redis
  
  redis:
    image: redis:latest
    ports:
      - "6379:6379"
```

### Building for Production

```bash
# Build production image
docker build -t quakepulse:prod .

# Run with production config
docker run -e ASPNETCORE_ENVIRONMENT=Production \
  -e Redis__ConnectionString=your-azure-redis:6380,password=key,ssl=true \
  quakepulse:prod
```

## Secrets Management

### Development (Local - NOT for Production)
Store sensitive data in `appsettings.Development.json` only for local testing.

### Production (Azure Key Vault)
1. Create Azure Key Vault
2. Store secrets:
   - `Redis--ConnectionString`
   - `UsgsApi--ApiKey` (if needed)
3. Reference in application:

```csharp
var keyVaultUrl = "https://your-vault.vault.azure.net/";
builder.Configuration.AddAzureKeyVault(
    new Uri(keyVaultUrl),
    new DefaultAzureCredential()
);
```

## Validating Configuration

### Check Current Environment
```csharp
// In Program.cs or controller
var env = app.Environment.EnvironmentName;
Console.WriteLine($"Running in: {env}"); // Output: Development, Staging, or Production
```

### Reading Configuration Values
```csharp
var cacheSettings = configuration.GetSection("CacheSettings");
var enabled = cacheSettings.GetValue<bool>("Enabled");
var ttl = configuration.GetValue<int>("Redis:DefaultTTLMinutes");
```

## Environment-Specific Tips

### Development
- ? Verbose logging for debugging
- ? Lower cache TTL for quick testing
- ? Local Redis for no dependencies
- ? Don't enable HTTPS redirect

### Staging
- ? Mirror production setup
- ? Use Azure resources
- ? Enable security features
- ? Monitor performance

### Production
- ? Minimal logging (errors only)
- ? Long cache TTL for performance
- ? Azure Redis for reliability
- ? HTTPS enforced
- ? Restricted AllowedHosts

## Troubleshooting

### Configuration Not Being Applied

1. Check environment variable:
   ```bash
   echo %ASPNETCORE_ENVIRONMENT%  # Windows
   echo $ASPNETCORE_ENVIRONMENT   # Linux/Mac
   ```

2. Verify file exists:
   ```bash
   ls -la appsettings.*.json
   ```

3. Check configuration in code:
   ```csharp
   var value = builder.Configuration["Redis:ConnectionString"];
   Console.WriteLine($"Redis Connection: {value}");
   ```

### Redis Connection Issues

```bash
# Test local Redis
redis-cli ping  # Should return: PONG

# Test Azure Redis
redis-cli -h your-redis.redis.cache.windows.net -p 6380 -a your-password ping
```

## Summary

| Aspect | Development | Staging | Production |
|--------|-----------|---------|-----------|
| **File** | `appsettings.Development.json` | `appsettings.Staging.json` | `appsettings.Production.json` |
| **Redis** | Local | Azure | Azure |
| **Logging** | Debug | Info | Warning |
| **TTL** | 10 min | 20 min | 30 min |
| **HTTPS** | Optional | Required | Required |
| **Secrets** | Local files | Key Vault | Key Vault |

---

**Last Updated:** 2024
**Version:** 1.0
