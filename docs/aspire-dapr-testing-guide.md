# Aspire and Dapr Testing Guide

## Overview
This guide provides step-by-step instructions for testing the AI Orchestration Multi-Agent Framework with .NET Aspire and Dapr integration.

---

## Prerequisites

### Required Software
- ✅ **.NET 9 SDK** - Already installed
- ✅ **Visual Studio Code** or **Visual Studio 2022**
- ✅ **Docker Desktop** or **Rancher Desktop** (for containers)
- ⚠️ **Dapr CLI** (optional for manual Dapr testing)

### Verify Prerequisites
```bash
# Check .NET version
dotnet --version  # Should show 9.x

# Check Docker
docker --version

# Check Dapr (if installed)
dapr --version
```

---

## Running the Application with Aspire

### Option 1: Command Line (Recommended)
```bash
# From the repository root
dotnet run --project src/AppHost/Agents.AppHost/Agents.AppHost.csproj
```

**Expected Output:**
```
Building...
info: Aspire.Hosting.DistributedApplication[0]
      Aspire version: 8.2.2+a6e341ebbdbc2b322f354f30523bc1a84b01e56a
info: Aspire.Hosting.DistributedApplication[0]
      Distributed application starting.
info: Aspire.Hosting.DistributedApplication[0]
      Now listening on: http://localhost:15000
info: Aspire.Hosting.DistributedApplication[0]
      Application started. Press Ctrl+C to shut down.
```

### Option 2: Visual Studio Code
1. Open the project in VS Code
2. Press `F5` or use "Run and Debug" panel
3. Select "Launch Aspire AppHost" configuration
4. Dashboard opens automatically

### Option 3: Visual Studio 2022
1. Open `Agents.sln`
2. Set `Agents.AppHost` as startup project
3. Press `F5` to start debugging
4. Dashboard opens in browser

---

## Accessing the Aspire Dashboard

**Dashboard URL**: http://localhost:15000

### Dashboard Features
1. **Resources Tab**: View all services, databases, and containers
2. **Console Logs**: Real-time logs from all services
3. **Structured Logs**: Filtered and searchable logs
4. **Traces**: Distributed tracing across services
5. **Metrics**: Performance metrics and health checks

---

## Test Scenarios

### Test 3.6: Verify All Services Start

**Objective**: Confirm all 5 agent APIs and infrastructure components start successfully.

**Steps:**
1. Run the AppHost: `dotnet run --project src/AppHost/Agents.AppHost/Agents.AppHost.csproj`
2. Open dashboard: http://localhost:15000
3. Navigate to "Resources" tab

**Expected Results:**
- ✅ **SQL Server** container running
- ✅ **Redis** container running  
- ✅ **Ollama** container running
- ✅ **notification-api** running with Dapr sidecar
- ✅ **devops-api** running with Dapr sidecar
- ✅ **testplanning-api** running with Dapr sidecar
- ✅ **implementation-api** running with Dapr sidecar
- ✅ **servicedesk-api** running with Dapr sidecar
- ✅ All health checks passing (green status)

**Troubleshooting:**
- If services fail to start, check Console Logs for error messages
- Ensure ports 15000, 5001-5005 are not in use
- Verify Docker is running

---

### Test 3.7: Dapr Pub/Sub Integration

**Objective**: Verify events can be published via Dapr and consumed by services.

**Preparation:**
Dapr is automatically enabled via the AppHost with environment variable:
```csharp
.WithEnvironment("Dapr__Enabled", "true")
```

**Manual Test Steps:**

#### Option 1: Via Dapr CLI (if installed)
```bash
# Publish a test event to notification-api
dapr publish --publish-app-id notification-api --pubsub agents-pubsub --topic notificationsentevent --data '{\"eventId\":\"test-123\",\"message\":\"Test notification\"}'
```

#### Option 2: Via HTTP API
```bash
# Publish via Dapr HTTP endpoint
curl -X POST http://localhost:3500/v1.0/publish/agents-pubsub/notificationsentevent \
  -H "Content-Type: application/json" \
  -d '{
    "eventId": "test-123",
    "occurredAt": "2025-11-13T02:00:00Z",
    "correlationId": "00000000-0000-0000-0000-000000000000",
    "causationId": null,
    "recipientEmail": "test@example.com",
    "subject": "Test",
    "body": "Test notification"
  }'
```

#### Option 3: Via Swagger UI
1. Navigate to notification-api Swagger: http://localhost:5001/swagger
2. Find the endpoint that publishes events
3. Execute a POST request with test data

**Expected Results:**
- ✅ Event published successfully (200 OK)
- ✅ Event appears in Aspire Dashboard logs
- ✅ Dapr sidecar logs show event transmission
- ✅ Redis shows pub/sub activity (if monitoring)

**Verification:**
Check Aspire Dashboard → Console Logs → Filter by "notification-api":
```
info: Publishing event NotificationSentEvent with ID test-123 to topic notificationsentevent
info: Successfully published event NotificationSentEvent with ID test-123
```

---

### Test 3.8: State Store Operations

**Objective**: Verify Dapr state store read/write operations with Redis persistence.

**Test Approach:**
Since the infrastructure uses `DaprStateStore`, we can test it programmatically or via HTTP.

#### Option 1: Unit Test (Already Completed)
✅ We have 25 unit tests covering all state store operations in:
- `tests/unit/Agents.Infrastructure.Dapr.Tests/DaprStateStoreTests.cs`

#### Option 2: Integration Test via Dapr HTTP API
```bash
# Save state
curl -X POST http://localhost:3500/v1.0/state/agents-statestore \
  -H "Content-Type: application/json" \
  -d '[
    {
      "key": "test-key",
      "value": "test-value"
    }
  ]'

# Retrieve state
curl http://localhost:3500/v1.0/state/agents-statestore/test-key
```

#### Option 3: Verify Redis Persistence
```bash
# Connect to Redis container
docker exec -it <redis-container-id> redis-cli

# Check keys
KEYS *

# Get value
GET test-key
```

**Expected Results:**
- ✅ State saved successfully (204 No Content)
- ✅ State retrieved correctly
- ✅ Data persisted in Redis
- ✅ Bulk operations work for multiple keys

---

### Test 3.9: Observability in Aspire Dashboard

**Objective**: Verify comprehensive observability features in Aspire.

**Areas to Verify:**

#### 1. Service Health Checks
**Steps:**
1. Navigate to Resources tab in dashboard
2. Check each service status

**Expected:**
- ✅ All services show green/healthy status
- ✅ Health check endpoints responding: `/health` and `/alive`
- ✅ Startup probes completed successfully

#### 2. Structured Logging
**Steps:**
1. Navigate to Structured Logs tab
2. Filter by service (e.g., "notification-api")
3. Apply severity filter (Information, Warning, Error)

**Expected:**
- ✅ Logs from all services visible
- ✅ Log levels properly categorized
- ✅ Timestamps accurate
- ✅ Correlation IDs present for tracking

#### 3. Distributed Tracing
**Steps:**
1. Navigate to Traces tab
2. Trigger an API call to any service
3. View the trace details

**Expected:**
- ✅ Traces show request flow across services
- ✅ Spans show timing for each operation
- ✅ Parent-child relationships visible
- ✅ Dapr sidecar calls tracked

Example trace structure:
```
notification-api (parent)
  └─ HTTP GET /health
     └─ Dapr sidecar call
        └─ Redis state store
```

#### 4. Metrics and Performance
**Steps:**
1. Navigate to Metrics tab
2. Select a service
3. View performance graphs

**Expected:**
- ✅ Request rate (requests/second)
- ✅ Response times (p50, p90, p99)
- ✅ Error rates
- ✅ Memory and CPU usage

---

## Common Issues and Solutions

### Issue: Port Already in Use
**Symptom:** AppHost fails to start with "address already in use" error

**Solution:**
```bash
# Find process using port 15000 (Aspire Dashboard)
netstat -ano | findstr :15000

# Kill the process
taskkill /PID <process-id> /F
```

### Issue: Docker Not Running
**Symptom:** Container resources fail to start

**Solution:**
1. Start Docker Desktop or Rancher Desktop
2. Verify: `docker ps`
3. Restart AppHost

### Issue: Dapr Sidecar Not Starting
**Symptom:** APIs start but Dapr functionality doesn't work

**Solution:**
1. Check if `Dapr__Enabled` environment variable is set to "true"
2. Verify Dapr components exist in `dapr/components/local/`
3. Check AppHost logs for Dapr initialization errors

### Issue: Services Can't Connect to Redis
**Symptom:** State store or pub/sub operations fail

**Solution:**
1. Verify Redis container is running: `docker ps | grep redis`
2. Check Redis logs in Aspire Dashboard
3. Ensure connection string references `redis` service name (not localhost)

### Issue: Missing Ollama Models
**Symptom:** LLM calls fail with model not found

**Solution:**
```bash
# Pull the model
docker exec <ollama-container-id> ollama pull llama3.2

# Verify
docker exec <ollama-container-id> ollama list
```

---

## Manual Testing Checklist

Use this checklist to verify the complete integration:

### Infrastructure
- [ ] SQL Server container running
- [ ] Redis container running
- [ ] Ollama container running
- [ ] All containers have persistent volumes

### Services
- [ ] notification-api running
- [ ] devops-api running
- [ ] testplanning-api running
- [ ] implementation-api running
- [ ] servicedesk-api running

### Dapr Integration
- [ ] Dapr sidecars attached to all APIs
- [ ] Pub/sub component (agents-pubsub) accessible
- [ ] State store component (agents-statestore) accessible
- [ ] Events can be published via Dapr
- [ ] State can be saved and retrieved

### Aspire Features
- [ ] Dashboard accessible at http://localhost:15000
- [ ] All services visible in Resources tab
- [ ] Logs visible and filterable
- [ ] Traces showing distributed calls
- [ ] Metrics displaying performance data
- [ ] Health checks all passing

### API Functionality
- [ ] Swagger UI accessible for each API
- [ ] API endpoints responding
- [ ] Service-to-service communication working
- [ ] LLM integration functional

---

## Performance Benchmarks

After successful testing, document baseline performance:

| Metric | Target | Actual |
|--------|--------|--------|
| AppHost startup time | < 30s | ___ |
| API response time (p50) | < 100ms | ___ |
| API response time (p99) | < 500ms | ___ |
| Memory per API | < 200MB | ___ |
| Total system memory | < 2GB | ___ |
| Container startup time | < 10s | ___ |

---

## Next Steps After Testing

1. **Document Results**: Update `docs/phase3-migration-testing-summary.md` with test outcomes
2. **Create Issues**: File issues for any failures or unexpected behavior
3. **Optimize**: Address performance bottlenecks identified
4. **Deploy**: Proceed to deployment testing (Phase 4)

---

## References
- Aspire Documentation: https://learn.microsoft.com/en-us/dotnet/aspire/
- Dapr Documentation: https://docs.dapr.io/
- Phase 3 Summary: `docs/phase3-migration-testing-summary.md`
- Troubleshooting Guide: `docs/troubleshooting.md` (to be created)

---

**Document Version**: 1.0  
**Last Updated**: 2025-11-13  
**Status**: Ready for Testing
