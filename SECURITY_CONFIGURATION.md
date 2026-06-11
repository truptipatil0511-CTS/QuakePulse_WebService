# QuakePulse WebService - Configuration & Secrets Management

## For .gitignore or sensitive files

# Environment-specific secrets (DO NOT COMMIT)
appsettings.Production.secrets.json
appsettings.Staging.secrets.json
appsettings.Local.json

# User secrets (stored outside repo in user secrets manager)
secrets.json

# Visual Studio
.vs/
.vscode/

# Build output
bin/
obj/
publish/

# Local environment files
.env
.env.local
.env.*.local

# IDE
*.user
*.sln.user
.idea/

# OS
.DS_Store
Thumbs.db

---

## Recommended Configuration Strategy for Production

### Option 1: Azure Key Vault (RECOMMENDED)
Instead of storing secrets in appsettings.Production.json:

1. Create Azure Key Vault
2. Store secrets:
   - `Redis--ConnectionString`
   - `UsgsApi--ApiKey`
3. Reference in Program.cs

```csharp
// Program.cs
var keyVaultUrl = new Uri("https://yourkeyvault.vault.azure.net/");
builder.Configuration.AddAzureKeyVault(
    keyVaultUrl,
    new DefaultAzureCredential()
);
```

### Option 2: Azure App Service Configuration
Set secrets in Azure Portal ? Configuration ? Application Settings

### Option 3: Environment Variables
Set in deployment pipeline (GitHub Actions, Azure DevOps)

```yaml
# GitHub Actions Example
env:
  ASPNETCORE_ENVIRONMENT: Production
  Redis__ConnectionString: ${{ secrets.REDIS_CONNECTION }}
```

---

## Current Configuration Files

### Public (Safe to Commit) ?
- ? `appsettings.json` - Base configuration
- ? `appsettings.Development.json` - Local development (localhost connections)
- ? `appsettings.Staging.json` - Staging template (needs placeholder values)

### Requires Secrets Management ??
- ?? `appsettings.Production.json` - **Replace connection strings before deploying**

---

## Pre-Deployment Checklist

Before deploying to Azure:

- [ ] Update `Redis__ConnectionString` in Azure Portal (not in config file)
- [ ] Enable Managed Identity for Azure service authentication
- [ ] Set `ASPNETCORE_ENVIRONMENT=Production` in Azure App Service
- [ ] Verify Redis SSL settings (port 6380 with ssl=true)
- [ ] Test connection from App Service to Redis
- [ ] Review AllowedHosts to match your domain

---

## Local Development Setup

For local development, appsettings.Development.json is safe to use as-is:

```json
{
  "Redis": {
    "ConnectionString": "localhost:6379"  // Local development - safe
  }
}
```

This assumes you're running Redis locally via Docker or local installation.

---

## Summary

? **Development:** Safe to commit to repo
? **Staging:** Use placeholders, replace with Azure values
? **Production:** Store secrets in Azure Key Vault or App Service Configuration

Never commit production connection strings or passwords to version control!
