# Implementation Checklist: Environment-Specific Configuration

## ? Completed Tasks

### Configuration Files Created
- [x] `appsettings.json` - Base configuration with defaults
- [x] `appsettings.Development.json` - Local development setup
- [x] `appsettings.Staging.json` - Pre-production environment
- [x] `appsettings.Production.json` - Production environment

### Documentation Created
- [x] `CONFIGURATION_GUIDE.md` - Comprehensive configuration guide
- [x] `CONFIG_FILES_SUMMARY.md` - Quick reference
- [x] `SECURITY_CONFIGURATION.md` - Security best practices
- [x] `CONFIG_COMPARISON.md` - Side-by-side comparison
- [x] This checklist document

### Code Updates
- [x] Fixed RedisCacheService logging
- [x] Updated Program.cs with proper configuration
- [x] Build successful ?

---

## ?? Next Steps for Local Development

### Step 1: Verify Configuration Files Exist
```bash
# Check all configuration files are present
ls -la appsettings*.json

# Expected output:
# appsettings.json
# appsettings.Development.json
# appsettings.Staging.json
# appsettings.Production.json
```

### Step 2: Set Up Local Redis
Choose ONE option:

#### Option A: Docker (Easiest)
```bash
# Run Redis container
docker run --name quakepulse-redis -p 6379:6379 redis:latest

# Verify it's running
docker ps
```

#### Option B: Docker Compose
```bash
# Navigate to project directory
cd QuakePulse_WebService

# Create docker-compose.yml (if not exists)
# Run with compose
docker-compose up -d redis
```

#### Option C: Local Installation
```bash
# Windows - Use Memurai or WSL2
# macOS - brew install redis
# Linux - sudo apt-get install redis-server

# Start Redis
redis-server

# Test connection
redis-cli ping  # Should return: PONG
```

### Step 3: Run Application
```bash
# Default (Development environment)
dotnet run

# Explicitly set environment
export ASPNETCORE_ENVIRONMENT=Development  # Linux/Mac
set ASPNETCORE_ENVIRONMENT=Development     # Windows
dotnet run
```

### Step 4: Verify Setup
```bash
# Check in browser
http://localhost:5000/swagger

# Look for these log messages:
# - "Application started"
# - No Redis connection errors
```

---

## ?? For Staging Deployment

### Step 1: Prepare Azure Resources
- [ ] Create Azure Redis Cache instance
  - SKU: Standard (recommended)
  - Capacity: C0 (512MB) for testing
  - Enable "Non-SSL port" if needed (not recommended)

### Step 2: Get Connection String
```bash
# From Azure Portal:
# Redis instance ? Access keys ? Primary connection string

# Format:
# your-redis.redis.cache.windows.net:6380,password=***,ssl=true
```

### Step 3: Update appsettings.Staging.json
```json
"Redis": {
  "ConnectionString": "your-redis.redis.cache.windows.net:6380,password=YOUR_KEY,ssl=true",
  "DefaultTTLMinutes": 20
}
```

### Step 4: Deploy to Staging
```bash
# Build for staging
dotnet publish -c Release -o ./publish-staging

# Set environment variable in Azure App Service
# ASPNETCORE_ENVIRONMENT = Staging

# Deploy
# (Via Azure DevOps, GitHub Actions, or manual upload)
```

---

## ?? For Production Deployment

### Step 1: Security Setup - Azure Key Vault
```bash
# Create Key Vault
az keyvault create --name MyKeyVault --resource-group MyResourceGroup

# Add secrets
az keyvault secret set --vault-name MyKeyVault \
  --name "Redis--ConnectionString" \
  --value "your-redis.redis.cache.windows.net:6380,password=***,ssl=true"
```

### Step 2: Update Program.cs (Optional)
```csharp
// Add Key Vault support
var keyVaultUrl = new Uri("https://myvault.vault.azure.net/");
builder.Configuration.AddAzureKeyVault(
    keyVaultUrl,
    new DefaultAzureCredential()
);
```

### Step 3: Set Environment in Azure Portal
App Service ? Configuration ? Application Settings:
- Key: `ASPNETCORE_ENVIRONMENT`
- Value: `Production`

### Step 4: Build & Deploy Production Image
```bash
# Build for production
dotnet publish -c Release -o ./publish-prod

# Or use Docker
docker build -t quakepulse:prod .
docker tag quakepulse:prod myregistry.azurecr.io/quakepulse:latest
docker push myregistry.azurecr.io/quakepulse:latest
```

---

## ? Verification Checklist

### Local Development ?

- [ ] Redis is running (redis-cli ping returns PONG)
- [ ] Application starts without errors
- [ ] Swagger UI accessible at http://localhost:5000/swagger
- [ ] No connection string errors in logs
- [ ] Cache operations working (check orchestrator logs)

### Testing Configuration Loading

```bash
# See which config files are being used
# Add to Program.cs temporarily:

var env = builder.Environment.EnvironmentName;
Console.WriteLine($"Environment: {env}");

var redisConn = builder.Configuration["Redis:ConnectionString"];
Console.WriteLine($"Redis Connection: {redisConn}");

// Expected output (Development):
// Environment: Development
// Redis Connection: localhost:6379
```

### Testing with Different Environments

```bash
# Test Development
set ASPNETCORE_ENVIRONMENT=Development
dotnet run

# Test Production (with local Redis)
set ASPNETCORE_ENVIRONMENT=Production
set Redis__ConnectionString=localhost:6379
dotnet run
```

---

## ?? Configuration Status

### Files Created
| File | Status | Environment |
|------|--------|-------------|
| appsettings.json | ? Created | Base |
| appsettings.Development.json | ? Created | Development |
| appsettings.Staging.json | ? Created | Staging |
| appsettings.Production.json | ? Created | Production |

### Documentation
| Document | Status |
|----------|--------|
| CONFIGURATION_GUIDE.md | ? Created |
| CONFIG_FILES_SUMMARY.md | ? Created |
| CONFIG_COMPARISON.md | ? Created |
| SECURITY_CONFIGURATION.md | ? Created |
| This Checklist | ? Created |

### Build Status
- [x] Build successful
- [x] No compilation errors
- [x] RedisCacheService fixed

---

## ?? Troubleshooting

### Issue: "Connection refused" on localhost:6379

**Solution:**
```bash
# Check if Redis is running
redis-cli ping

# If not, start Redis
redis-server          # Linux/Mac
memurai              # Windows
# Or use Docker
docker run -p 6379:6379 redis:latest
```

### Issue: Wrong configuration loading

**Solution:**
```bash
# Check environment variable
echo %ASPNETCORE_ENVIRONMENT%  # Windows
echo $ASPNETCORE_ENVIRONMENT   # Linux/Mac

# Should return: Development, Staging, or Production
```

### Issue: Azure Redis SSL Certificate Error

**Solution:**
```bash
# Ensure connection string has ssl=true
# Format: hostname:6380,password=key,ssl=true

# Test with redis-cli
redis-cli -h your-redis.redis.cache.windows.net -p 6380 -a your-password --tls ping
```

### Issue: Configuration not updating

**Solution:**
1. Clean build: `dotnet clean && dotnet build`
2. Verify appsettings.*.json files are in output directory
3. Check file permissions (must be readable)
4. Restart application

---

## ?? Configuration Deployment Timeline

```
Development (You here!)
?? Local Redis setup
?? Test application locally
?? Verify configuration loading

Staging (Next)
?? Create Azure Redis
?? Update appsettings.Staging.json
?? Deploy to App Service
?? Test with Azure resources

Production (Final)
?? Create Azure Key Vault
?? Store secrets securely
?? Set environment variables in Azure
?? Deploy with ASPNETCORE_ENVIRONMENT=Production
?? Monitor application
```

---

## ?? Summary

### What's Been Set Up
? **Separate configuration files** for Development, Staging, and Production
? **Documented settings** with clear explanations
? **Security best practices** for production secrets
? **Environment-specific values** for Redis, logging, and timeouts

### What You Need to Do
1. **Now:** Set up local Redis and test with Development config
2. **Before Staging:** Get Azure Redis connection string
3. **Before Production:** Set up Key Vault for secrets

### Configuration Ready? 
? Yes! Your application is now ready for multi-environment deployment.

---

## ?? Additional Resources

- [CONFIGURATION_GUIDE.md](./CONFIGURATION_GUIDE.md) - Detailed configuration documentation
- [CONFIG_COMPARISON.md](./CONFIG_COMPARISON.md) - Side-by-side configuration comparison
- [SECURITY_CONFIGURATION.md](./SECURITY_CONFIGURATION.md) - Security best practices
- [ASP.NET Core Configuration Docs](https://learn.microsoft.com/aspnet/core/fundamentals/configuration)
- [Azure Key Vault Integration](https://learn.microsoft.com/azure/key-vault/general/overview)

---

## ? Done!

Your QuakePulse WebService is now configured for Development, Staging, and Production environments!

**Current Status:** ? Ready for local development

Next: Set up Redis and run `dotnet run`

---

**Last Updated:** 2024
**Version:** 1.0 - Initial Setup Complete
