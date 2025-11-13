# Troubleshooting Guide

## Overview

This guide covers common issues encountered when working with the AI Orchestration Multi-Agent Framework using .NET Aspire and Dapr.

## Aspire AppHost Issues

### Issue: APIs Start Then Immediately Stop

**Symptoms:**
- Aspire dashboard shows APIs as "Finished" (red status)
- Services briefly appear as "Running" then stop
- No endpoints available

**Common Causes:**

#### 1. Database Migration Failure
**Problem**: API tries to run EF Core migrations on startup but fails
**Solution**:
```csharp
// In Program.cs, temporarily disable auto-migration
// var connectionString = builder.Configuration.GetConnectionString("SqlServer");
// if (!string.IsNullOrEmpty(connectionString))
// {
//     await app.Services.MigrateDatabaseAsync();
// }
```

**Permanent Fix**: Run migrations separately
```powershell
dotnet ef database update --project src/Infrastructure/Agents.Infrastructure.Persistence.SqlServer
```

#### 2. Missing Configuration
**Problem**: Required configuration values are missing
**Solution**: Check `appsettings.json` has all required sections:
```json
{
  "Dapr": {
    "Enabled": true
  },
  "LLMProvider": {
    "ProviderType": "Ollama",
    "Ollama": {
      "Endpoint": "http://localhost:11434"
    }
  }
}
```

### Issue: Dapr Sidecars Show as "Finished"

**Symptoms:**
- All APIs running but Dapr sidecars have red "Finished" status
- Dapr executable processes exit immediately

**Root Cause**: Dapr runtime (`daprd.exe`) is not installed

**Solution**:
```powershell
# 1. Initialize Dapr runtime
dapr init

# 2. Verify installation
dapr --version  # Should show CLI version
daprd --version  # Should show runtime version

# 3. If daprd not in PATH, copy to accessible location
Copy-Item "C:\Users\<username>\.dapr\bin\*" "C:\dapr\" -Force

# 4. Restart AppHost
dotnet run --project src/AppHost/Agents.AppHost/Agents.AppHost.csproj
```

**Verification**:
- Check Aspire dashboard - all Dapr sidecars should show green "Running" status
- Each sidecar will have HTTP endpoint (usually port 512xx range)

## Docker Issues

### Issue: Containers Won't Start

**Symptoms:**
- Aspire dashboard shows container resources as "Exited"
- Port conflicts

**Solution 1 - Port Conflicts**:
```powershell
# Check what's using the port
netstat -ano | findstr "6379"  # Redis
netstat -ano | findstr "1433"  # SQL Server
netstat -ano | findstr "11434" # Ollama

# Kill process if needed
taskkill /PID <process-id> /F
```

**Solution 2 - Docker Not Running**:
```powershell
# Start Docker Desktop
Start-Process "C:\Program Files\Docker\Docker\Docker Desktop.exe"

# Wait for Docker to be ready
docker ps
```

### Issue: SQL Server Container Fails to Start

**Symptoms:**
- SQL Server container exits immediately
- Error: "SQL Server failed to start"

**Solution**:
```powershell
# Check Docker logs
docker logs <container-id>

# Common issues:
# 1. Not enough memory (needs 2GB minimum)
# 2. Invalid SA password
# 3. Port 1433 already in use

# Verify memory allocation in Docker Desktop settings
# Settings → Resources → Memory (set to 4GB or higher)
```

## Dapr Component Issues

### Issue: Dapr Components Not Found

**Symptoms:**
- Error: "component agents-pubsub not found"
- Pub/sub or state store operations fail

**Solution**:
```powershell
# Verify component files exist
ls src/AppHost/Agents.AppHost/dapr-components/

# Should contain:
# - pubsub.yaml
# - statestore.yaml

# Verify components are copied to output
ls src/AppHost/Agents.AppHost/bin/Debug/net9.0/dapr-components/
```

**If missing**, check `.csproj`:
```xml
<ItemGroup>
  <None Include="dapr-components\**\*" CopyToOutputDirectory="PreserveNewest" />
</ItemGroup>
```

### Issue: Cannot Connect to Redis for Pub/Sub

**Symptoms:**
- Dapr errors about Redis connection refused
- Pub/sub publish operations fail

**Solution**:
```yaml
# Check dapr-components/pubsub.yaml
apiVersion: dapr.io/v1alpha1
kind: Component
metadata:
  name: agents-pubsub
spec:
  type: pubsub.redis
  version: v1
  metadata:
  - name: redisHost
    value: localhost:6379  # Update to actual Redis host
  - name: redisPassword
    value: ""
```

**Verify Redis is running**:
```powershell
# Check Redis container
docker ps | findstr redis

# Test Redis connection
docker exec <redis-container-id> redis-cli ping
# Should return: PONG
```

## API Testing Issues

### Issue: API Returns 404 Not Found

**Symptoms:**
- curl/browser shows "404 Not Found"
- API is running in Aspire dashboard

**Common Causes**:

#### 1. Wrong Endpoint Path
**Problem**: Accessing root path `/` instead of controller endpoint
**Solution**:
```powershell
# Wrong
curl https://localhost:7108/

# Correct
curl https://localhost:7108/api/DevOps/health -k

# Or use Swagger
# https://localhost:7108/swagger
```

#### 2. HTTPS Certificate Issues
**Problem**: Self-signed certificate rejected
**Solution**:
```powershell
# Use -k flag to ignore certificate
curl https://localhost:7108/api/DevOps/health -k

# Or use HTTP endpoint if available
curl http://localhost:5253/api/Implementation/health
```

### Issue: API Returns 500 Internal Server Error

**Symptoms:**
- API responds but returns 500 error
- Swagger shows error message

**Diagnosis**:
1. Check Aspire Dashboard → Structured Logs
2. Filter by the specific API
3. Look for exception stack traces

**Common Fixes**:
```csharp
// 1. Missing LLM configuration
"LLMProvider": {
  "ProviderType": "Ollama",
  "Ollama": {
    "Endpoint": "http://ollama:11434"
  }
}

// 2. Database connection issues
"ConnectionStrings": {
  "SqlServer": "Server=sqlserver;Database=agentsdb;..."
}

// 3. Dapr not enabled when expected
"Dapr": {
  "Enabled": true
}
```

## Build and Test Issues

### Issue: Build Fails with "Project not found"

**Symptoms:**
- `dotnet build` fails with project reference errors
- Missing `.csproj` files

**Solution**:
```powershell
# Restore all projects
dotnet restore

# If still failing, clean and rebuild
dotnet clean
dotnet build
```

### Issue: Tests Fail with "Pending Model Changes"

**Symptoms:**
- Integration tests fail with EF Core migration errors
- Error: "The model for context 'AgentsDbContext' has pending changes"

**Solution**:
```powershell
# Create new migration
cd src/Infrastructure/Agents.Infrastructure.Persistence.SqlServer
dotnet ef migrations add <MigrationName> --startup-project ../../Presentation/Agents.API.Notification

# Apply migration
dotnet ef database update --startup-project ../../Presentation/Agents.API.Notification
```

### Issue: Unit Tests Fail with Null Reference

**Symptoms:**
- Dapr unit tests fail
- Moq setup issues

**Solution**: Check FluentAssertions version
```xml
<!-- Use version 7.0.0 (Apache 2.0 license) -->
<PackageReference Include="FluentAssertions" Version="7.0.0" />
```

## Performance Issues

### Issue: Aspire Dashboard is Slow

**Symptoms:**
- Dashboard takes long to load
- Logs/traces not appearing

**Solution**:
```powershell
# 1. Clear browser cache
# 2. Restart AppHost
# 3. Check system resources (CPU/Memory)

# If still slow, reduce logging verbosity
# In appsettings.Development.json:
{
  "Logging": {
    "LogLevel": {
      "Default": "Warning",  # Changed from "Information"
      "Microsoft.AspNetCore": "Warning"
    }
  }
}
```

### Issue: High Memory Usage

**Symptoms:**
- System slows down when running Aspire
- Docker containers consuming lots of memory

**Solution**:
```powershell
# Check Docker memory usage
docker stats

# Stop unused containers
docker stop <container-id>

# Adjust Docker Desktop memory limit
# Settings → Resources → Memory → 8GB (or lower if needed)

# Stop services you don't need
# Comment out in AppHost Program.cs:
// var ollama = builder.AddContainer("ollama", "ollama/ollama", "latest")
//     .WithHttpEndpoint(11434, 11434, "http");
```

## Configuration Issues

### Issue: Environment Variables Not Working

**Symptoms:**
- Configuration values not being picked up
- Default values being used instead

**Solution**:
```csharp
// Check configuration loading order (last wins):
// 1. appsettings.json
// 2. appsettings.{Environment}.json
// 3. User secrets
// 4. Environment variables
// 5. Command-line arguments

// Debug configuration
var config = app.Services.GetRequiredService<IConfiguration>();
var daprEnabled = config.GetValue<bool>("Dapr:Enabled");
Console.WriteLine($"Dapr Enabled: {daprEnabled}");
```

### Issue: Dapr Not Using Correct Components Path

**Symptoms:**
- Dapr can't find components
- Using default Dapr components instead of custom ones

**Solution**:
```csharp
// In AppHost Program.cs, verify components path
var componentsPath = Path.Combine(AppContext.BaseDirectory, "dapr-components");
Console.WriteLine($"Components path: {componentsPath}");
Console.WriteLine($"Path exists: {Directory.Exists(componentsPath)}");
```

## Deployment Issues (AKS)

### Issue: Pods Stuck in "ImagePullBackOff"

**Symptoms:**
- Kubernetes pods won't start
- Error pulling Docker image from ACR

**Solution**:
```bash
# Verify ACR credentials
kubectl get secret acr-secret -n agents-<env> -o yaml

# Test ACR connectivity
az acr check-health --name <acr-name>

# Recreate image pull secret
kubectl delete secret acr-secret -n agents-<env>
kubectl create secret docker-registry acr-secret \
  --namespace=agents-<env> \
  --docker-server=<acr-login-server> \
  --docker-username=<acr-username> \
  --docker-password=<acr-password>
```

### Issue: Dapr Sidecar Not Injecting

**Symptoms:**
- Pod only has 1 container (should have 2)
- Dapr features not working

**Solution**:
```yaml
# Check deployment annotations
apiVersion: apps/v1
kind: Deployment
metadata:
  name: notification-api
spec:
  template:
    metadata:
      annotations:
        dapr.io/enabled: "true"  # Must be string "true"
        dapr.io/app-id: "notification-api"
        dapr.io/app-port: "8080"
```

**Verify Dapr installation**:
```bash
dapr status -k
# Should show dapr-operator, dapr-sidecar-injector, dapr-placement
```

## Getting Help

### Debug Checklist

1. ✅ Check Aspire Dashboard logs for errors
2. ✅ Verify all containers are running (`docker ps`)
3. ✅ Test API health endpoints
4. ✅ Check Dapr sidecar status
5. ✅ Verify configuration files (appsettings.json)
6. ✅ Review recent code changes
7. ✅ Check GitHub Actions workflow status

### Log Collection

```powershell
# Collect logs from Aspire
# 1. Open Aspire Dashboard
# 2. Navigate to "Structured" logs
# 3. Filter by resource (API name)
# 4. Export or screenshot errors

# Collect Docker logs
docker logs <container-id> > container.log

# Collect Dapr logs
# In Aspire dashboard → View logs for dapr-cli executable
```

### Common Error Messages and Solutions

| Error Message | Cause | Solution |
|---------------|-------|----------|
| "daprd: command not found" | Dapr runtime not installed | Run `dapr init` |
| "Connection refused" | Service not running | Check Aspire dashboard |
| "404 Not Found" | Wrong endpoint path | Use `/api/{Controller}/{Action}` |
| "500 Internal Server Error" | Application error | Check logs in Aspire dashboard |
| "ImagePullBackOff" | Can't pull Docker image | Verify ACR credentials |
| "Pending model changes" | EF migration needed | Run `dotnet ef database update` |
| "Component not found" | Dapr component missing | Check dapr-components/ directory |

## Support Resources

- **Documentation**: `docs/` folder in repository
- **Architecture**: `docs/architecture.md`
- **Deployment**: `docs/deployment.md`
- **CI/CD**: `docs/cicd-guide.md`
- **Aspire Docs**: https://learn.microsoft.com/en-us/dotnet/aspire/
- **Dapr Docs**: https://docs.dapr.io/

## Reporting Issues

When reporting issues, please include:

1. **Environment**: OS, .NET version, Docker version
2. **Steps to reproduce**: Exact commands run
3. **Expected behavior**: What should happen
4. **Actual behavior**: What actually happened
5. **Logs**: From Aspire dashboard or Docker
6. **Screenshots**: If applicable
7. **Configuration**: Relevant appsettings.json sections (redact secrets!)

**Template**:
```
## Environment
- OS: Windows 11
- .NET: 9.0.0
- Docker: 24.0.6
- Dapr CLI: 1.14.0

## Steps to Reproduce
1. Run `dotnet run --project src/AppHost/Agents.AppHost/Agents.AppHost.csproj`
2. Navigate to https://localhost:7108/api/DevOps/health
3. Observe error

## Expected
Should return {"status":"healthy"}

## Actual
Returns 404 Not Found

## Logs
[Paste relevant logs here]
```
