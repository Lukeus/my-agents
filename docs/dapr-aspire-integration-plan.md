# Dapr and .NET Aspire Integration Plan

## Problem Statement

The AI Orchestration Multi-Agent Framework currently uses Azure-specific services (Event Hub, Service Bus, Cosmos DB, Azure SQL) with direct SDK integration and docker-compose/Kubernetes for local development and deployment. The goal is to modernize the development experience by:

1. **Adding Dapr** - To abstract infrastructure dependencies (pub/sub, state store, service invocation) and improve portability
2. **Adding .NET Aspire** - To enhance local development experience with better orchestration, service discovery, and observability using Rancher Desktop

This will enable:
- Infrastructure-agnostic microservices (cloud or on-prem)
- Simplified local development without Azure dependencies
- Better developer experience with service discovery and unified dashboard
- Consistent patterns for distributed application concerns

## Current State Analysis

### Architecture Overview
- **Tech Stack**: .NET 9, ASP.NET Core, Microsoft Semantic Kernel 1.67.1
- **Microservices**: 5 agent APIs (Notification, DevOps, TestPlanning, Implementation, ServiceDesk)
- **Clean Architecture**: Domain → Application → Infrastructure → Presentation layers
- **Current Deployment**: 
  - Local: docker-compose with Ollama, SQL Server
  - Production: Azure Kubernetes Service (AKS) with Helm charts

### Current Infrastructure Integration

**Event Publishing (`src/Infrastructure/Agents.Infrastructure.Events/`)**:
- Direct Azure Event Hub SDK (`Azure.Messaging.EventHubs`)
- Direct Azure Service Bus SDK (`Azure.Messaging.ServiceBus`)
- Event Grid integration via `Agents.Infrastructure.EventGrid`

**State Management**:
- SQL Server via EF Core (`Agents.Infrastructure.Persistence.SqlServer`)
- Cosmos DB (`Agents.Infrastructure.Persistence.CosmosDB`)

**Service Communication**:
- Direct HTTP calls between services
- No current service mesh or sidecar pattern

**Configuration Files**:
- `appsettings.json` in each API project
- Connection strings for SqlServer
- LLM provider configuration (Ollama/Azure OpenAI)
- Example from `Agents.API.Notification`:
  ```json
  {
    "ConnectionStrings": {
      "SqlServer": "Server=(localdb)\\mssqllocaldb;Database=AgentsDB;..."
    },
    "LLMProvider": {
      "ProviderType": "Ollama",
      "Ollama": { "Endpoint": "http://localhost:11434" }
    }
  }
  ```

**Current Deployment Manifests**:
- Docker Compose: `docker-compose.yml` (5 services + SQL Server + Ollama)
- Helm Chart: `helm/agents/` with values.yaml defining replicas, resources, HPA
- Kubernetes: `k8s/base/` and `k8s/overlays/` using Kustomize

### Key Infrastructure Components

**Event Publishers** (`Agents.Infrastructure.Events`):
- `EventHubPublisher.cs` - Uses `EventHubProducerClient`
- `ServiceBusPublisher.cs` - Uses `ServiceBusClient`

**Database Contexts**:
- `AgentsDbContext` in `Agents.Infrastructure.Persistence.SqlServer`
- Cosmos DB repositories in `Agents.Infrastructure.Persistence.CosmosDB`

**API Entry Points** (e.g., `Agents.API.Notification/Program.cs`):
```csharp
builder.Services.AddLLMProvider(builder.Configuration);
builder.Services.AddSingleton<IPromptLoader, PromptLoader>();
builder.Services.AddSingleton<IEventPublisher, MockEventPublisher>();
builder.Services.AddSingleton<INotificationChannelFactory, NotificationChannelFactory>();
builder.Services.AddScoped<NotificationAgent>();
builder.Services.AddHealthChecks();
```

## Proposed Integration Strategy

### Phase 1: Add Dapr Support (Weeks 1-2)

#### 1.1 Install Dapr Runtime
**Local Development (Rancher Desktop)**:
- Install Dapr CLI on Windows
- Initialize Dapr in Kubernetes mode with Rancher Desktop
- Configure default components (Redis for state/pub-sub)

**Tasks**:
- Install Dapr CLI: `powershell -Command "iwr -useb https://raw.githubusercontent.com/dapr/cli/master/install/install.ps1 | iex"`
- Initialize Dapr: `dapr init -k --runtime-version 1.13`
- Verify: `dapr status -k`

#### 1.2 Create Dapr Component Definitions

**File Structure**:
```
dapr/
├── components/
│   ├── local/                    # For local development
│   │   ├── pubsub-redis.yaml    # Redis pub/sub
│   │   ├── statestore-redis.yaml # Redis state store
│   │   └── sqlserver.yaml        # SQL Server binding
│   ├── dev/                      # For dev environment
│   │   ├── pubsub-servicebus.yaml
│   │   ├── statestore-cosmos.yaml
│   │   └── sqlserver-azure.yaml
│   └── prod/                     # For production
│       ├── pubsub-servicebus.yaml
│       ├── statestore-cosmos.yaml
│       └── sqlserver-azure.yaml
├── configuration/
│   └── tracing.yaml              # OpenTelemetry tracing
└── README.md
```

**Component Examples**:

`dapr/components/local/pubsub-redis.yaml`:
```yaml
apiVersion: dapr.io/v1alpha1
kind: Component
metadata:
  name: agents-pubsub
spec:
  type: pubsub.redis
  version: v1
  metadata:
  - name: redisHost
    value: localhost:6379
  - name: redisPassword
    value: ""
```

`dapr/components/dev/pubsub-servicebus.yaml`:
```yaml
apiVersion: dapr.io/v1alpha1
kind: Component
metadata:
  name: agents-pubsub
spec:
  type: pubsub.azure.servicebus
  version: v1
  metadata:
  - name: connectionString
    secretKeyRef:
      name: azure-secrets
      key: serviceBusConnectionString
```

#### 1.3 Create Dapr Infrastructure Abstraction Layer

**New Infrastructure Project**: `src/Infrastructure/Agents.Infrastructure.Dapr/`

```
Agents.Infrastructure.Dapr/
├── Agents.Infrastructure.Dapr.csproj
├── PubSub/
│   ├── DaprEventPublisher.cs       # Implements IEventPublisher
│   └── DaprEventSubscriber.cs
├── State/
│   ├── DaprStateStore.cs           # State management abstraction
│   └── IDaprStateStore.cs
├── ServiceInvocation/
│   ├── DaprServiceClient.cs        # Service-to-service calls
│   └── IDaprServiceClient.cs
└── Extensions/
    └── DaprServiceCollectionExtensions.cs
```

**DaprEventPublisher Implementation**:
```csharp
using Dapr.Client;
using Agents.Domain.Core.Events;

public class DaprEventPublisher : IEventPublisher
{
    private readonly DaprClient _daprClient;
    private const string PubSubName = "agents-pubsub";

    public DaprEventPublisher(DaprClient daprClient)
    {
        _daprClient = daprClient;
    }

    public async Task PublishAsync(IDomainEvent domainEvent, CancellationToken cancellationToken = default)
    {
        var topicName = domainEvent.GetType().Name.ToLowerInvariant();
        await _daprClient.PublishEventAsync(PubSubName, topicName, domainEvent, cancellationToken);
    }
}
```

**DaprStateStore Implementation**:
```csharp
using Dapr.Client;

public class DaprStateStore : IDaprStateStore
{
    private readonly DaprClient _daprClient;
    private const string StateStoreName = "agents-statestore";

    public async Task<T?> GetStateAsync<T>(string key, CancellationToken cancellationToken = default)
    {
        return await _daprClient.GetStateAsync<T>(StateStoreName, key, cancellationToken: cancellationToken);
    }

    public async Task SaveStateAsync<T>(string key, T value, CancellationToken cancellationToken = default)
    {
        await _daprClient.SaveStateAsync(StateStoreName, key, value, cancellationToken: cancellationToken);
    }
}
```

#### 1.4 Update API Projects to Support Dapr

**Modify each API's Program.cs** (e.g., `Agents.API.Notification/Program.cs`):

```csharp
using Agents.Infrastructure.Dapr.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Add Dapr support
builder.Services.AddDaprClient();
builder.Services.AddDaprEventPublisher(); // Extension method

// Keep existing services
builder.Services.AddLLMProvider(builder.Configuration);
builder.Services.AddSingleton<IPromptLoader, PromptLoader>();

// Conditionally use Dapr or existing implementation
if (builder.Configuration.GetValue<bool>("Dapr:Enabled"))
{
    builder.Services.AddSingleton<IEventPublisher, DaprEventPublisher>();
}
else
{
    builder.Services.AddSingleton<IEventPublisher, ServiceBusPublisher>();
}

var app = builder.Build();

// Add Dapr pub/sub subscription endpoints
app.MapSubscribeHandler();

// Add specific event subscriptions
app.MapPost("/events/notification-sent", 
    [Topic("agents-pubsub", "notificationsentevent")]
    async (NotificationSentEvent evt, ILogger<Program> logger) =>
    {
        logger.LogInformation("Received notification sent event: {EventId}", evt.Id);
        return Results.Ok();
    });
```

**Update csproj files**:
```xml
<ItemGroup>
  <PackageReference Include="Dapr.AspNetCore" Version="1.13.0" />
  <PackageReference Include="Dapr.Client" Version="1.13.0" />
</ItemGroup>
```

#### 1.5 Update Dockerfiles for Dapr Sidecar

**Modified Dockerfile** (add daprd sidecar annotations):
No changes needed to Dockerfile itself - Dapr sidecar runs as separate container.

#### 1.6 Update Docker Compose for Local Development

**New `docker-compose.dapr.yml`**:
```yaml
version: '3.8'

services:
  notification-api:
    build:
      context: .
      dockerfile: src/Presentation/Agents.API.Notification/Dockerfile
    ports:
      - "5001:8080"
      - "50001:50001"  # Dapr HTTP port
    environment:
      - ASPNETCORE_ENVIRONMENT=Development
      - Dapr__Enabled=true
    networks:
      - agents-network

  notification-dapr:
    image: "daprio/daprd:1.13.0"
    command: [
      "./daprd",
      "-app-id", "notification-api",
      "-app-port", "8080",
      "-dapr-http-port", "3500",
      "-dapr-grpc-port", "50001",
      "-components-path", "/components",
      "-config", "/configuration/config.yaml"
    ]
    volumes:
      - "./dapr/components/local:/components"
      - "./dapr/configuration:/configuration"
    depends_on:
      - notification-api
    network_mode: "service:notification-api"

  # Add similar pairs for other agents...

  redis:
    image: "redis:7-alpine"
    ports:
      - "6379:6379"
    networks:
      - agents-network

  sqlserver:
    image: mcr.microsoft.com/mssql/server:2022-latest
    # ... existing config

  ollama:
    image: ollama/ollama:latest
    # ... existing config

networks:
  agents-network:
    driver: bridge
```

#### 1.7 Update Kubernetes Manifests for Dapr

**Install Dapr on Rancher Desktop Kubernetes**:
```bash
helm repo add dapr https://dapr.github.io/helm-charts/
helm repo update
helm upgrade --install dapr dapr/dapr --namespace dapr-system --create-namespace
```

**Update Helm Chart** (`helm/agents/templates/deployment.yaml`):
```yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: {{ .Values.notificationAgent.name }}
  annotations:
    dapr.io/enabled: "true"
    dapr.io/app-id: "notification-api"
    dapr.io/app-port: "8080"
    dapr.io/config: "agents-config"
    dapr.io/log-level: "info"
spec:
  replicas: {{ .Values.notificationAgent.replicaCount }}
  template:
    metadata:
      labels:
        app: {{ .Values.notificationAgent.name }}
      annotations:
        dapr.io/enabled: "true"
        dapr.io/app-id: "notification-api"
        dapr.io/app-port: "8080"
    spec:
      containers:
      - name: {{ .Values.notificationAgent.name }}
        image: {{ .Values.image.registry }}/{{ .Values.notificationAgent.image.repository }}:{{ .Values.image.tag }}
        # ... rest of container spec
```

**Create Dapr Configuration** (`helm/agents/templates/dapr-config.yaml`):
```yaml
apiVersion: dapr.io/v1alpha1
kind: Configuration
metadata:
  name: agents-config
  namespace: {{ .Release.Namespace }}
spec:
  tracing:
    samplingRate: "1"
    zipkin:
      endpointAddress: "http://zipkin:9411/api/v2/spans"
  metric:
    enabled: true
```

### Phase 2: Add .NET Aspire Support (Weeks 3-4)

#### 2.1 Create Aspire AppHost Project

**New Project Structure**:
```
src/AppHost/
├── Agents.AppHost/
│   ├── Agents.AppHost.csproj
│   ├── Program.cs
│   ├── appsettings.json
│   └── Properties/
│       └── launchSettings.json
```

**Agents.AppHost.csproj**:
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net9.0</TargetFramework>
    <IsAspireHost>true</IsAspireHost>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Aspire.Hosting.AppHost" Version="9.0.0" />
    <PackageReference Include="Aspire.Hosting.Dapr" Version="9.0.0" />
    <PackageReference Include="Aspire.Hosting.SqlServer" Version="9.0.0" />
    <PackageReference Include="Aspire.Hosting.Redis" Version="9.0.0" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\Presentation\Agents.API.Notification\Agents.API.Notification.csproj" />
    <ProjectReference Include="..\Presentation\Agents.API.DevOps\Agents.API.DevOps.csproj" />
    <ProjectReference Include="..\Presentation\Agents.API.TestPlanning\Agents.API.TestPlanning.csproj" />
    <ProjectReference Include="..\Presentation\Agents.API.Implementation\Agents.API.Implementation.csproj" />
    <ProjectReference Include="..\Presentation\Agents.API.ServiceDesk\Agents.API.ServiceDesk.csproj" />
  </ItemGroup>
</Project>
```

**Program.cs**:
```csharp
var builder = DistributedApplication.CreateBuilder(args);

// Add infrastructure resources
var sqlServer = builder.AddSqlServer("sqlserver")
    .WithDataVolume()
    .AddDatabase("agentsdb");

var redis = builder.AddRedis("redis")
    .WithDataVolume();

// Add Dapr components
var pubsub = builder.AddDapr("agents-pubsub", "pubsub.redis", new()
{
    { "redisHost", "redis:6379" }
});

var statestore = builder.AddDapr("agents-statestore", "state.redis", new()
{
    { "redisHost", "redis:6379" }
});

// Add Ollama for local LLM
var ollama = builder.AddContainer("ollama", "ollama/ollama", "latest")
    .WithHttpEndpoint(11434, 11434)
    .WithDataVolume();

// Add agent APIs with Dapr sidecars
var notificationApi = builder.AddProject<Projects.Agents_API_Notification>("notification-api")
    .WithReference(sqlServer)
    .WithReference(redis)
    .WithReference(ollama)
    .WithDaprSidecar(new DaprSidecarOptions
    {
        AppId = "notification-api",
        AppPort = 8080,
        DaprHttpPort = 3500,
        DaprGrpcPort = 50001,
        Config = "agents-config"
    })
    .WithEnvironment("Dapr__Enabled", "true");

var devopsApi = builder.AddProject<Projects.Agents_API_DevOps>("devops-api")
    .WithReference(sqlServer)
    .WithReference(redis)
    .WithReference(ollama)
    .WithDaprSidecar(new DaprSidecarOptions
    {
        AppId = "devops-api",
        AppPort = 8080,
        DaprHttpPort = 3501,
        DaprGrpcPort = 50002
    })
    .WithEnvironment("Dapr__Enabled", "true");

// Add remaining agent APIs...
var testplanningApi = builder.AddProject<Projects.Agents_API_TestPlanning>("testplanning-api")
    .WithReference(sqlServer)
    .WithReference(redis)
    .WithReference(ollama)
    .WithDaprSidecar(new DaprSidecarOptions { AppId = "testplanning-api", AppPort = 8080 });

var implementationApi = builder.AddProject<Projects.Agents_API_Implementation>("implementation-api")
    .WithReference(sqlServer)
    .WithReference(redis)
    .WithReference(ollama)
    .WithDaprSidecar(new DaprSidecarOptions { AppId = "implementation-api", AppPort = 8080 });

var servicedeskApi = builder.AddProject<Projects.Agents_API_ServiceDesk>("servicedesk-api")
    .WithReference(sqlServer)
    .WithReference(redis)
    .WithReference(ollama)
    .WithDaprSidecar(new DaprSidecarOptions { AppId = "servicedesk-api", AppPort = 8080 });

builder.Build().Run();
```

#### 2.2 Create Service Defaults Project

**New Project**: `src/Shared/Agents.ServiceDefaults/`

**Agents.ServiceDefaults.csproj**:
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Aspire.StackExchange.Redis" Version="9.0.0" />
    <PackageReference Include="Aspire.Microsoft.EntityFrameworkCore.SqlServer" Version="9.0.0" />
    <PackageReference Include="Microsoft.Extensions.Http.Resilience" Version="9.0.0" />
    <PackageReference Include="Microsoft.Extensions.ServiceDiscovery" Version="9.0.0" />
    <PackageReference Include="OpenTelemetry.Exporter.OpenTelemetryProtocol" Version="1.9.0" />
    <PackageReference Include="OpenTelemetry.Extensions.Hosting" Version="1.9.0" />
    <PackageReference Include="OpenTelemetry.Instrumentation.AspNetCore" Version="1.9.0" />
    <PackageReference Include="OpenTelemetry.Instrumentation.Http" Version="1.9.0" />
    <PackageReference Include="OpenTelemetry.Instrumentation.Runtime" Version="1.9.0" />
  </ItemGroup>
</Project>
```

**Extensions.cs**:
```csharp
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;

namespace Agents.ServiceDefaults;

public static class Extensions
{
    public static IHostApplicationBuilder AddServiceDefaults(this IHostApplicationBuilder builder)
    {
        builder.ConfigureOpenTelemetry();
        builder.AddDefaultHealthChecks();
        builder.Services.AddServiceDiscovery();
        builder.Services.ConfigureHttpClientDefaults(http =>
        {
            http.AddStandardResilienceHandler();
            http.AddServiceDiscovery();
        });

        return builder;
    }

    public static IHostApplicationBuilder ConfigureOpenTelemetry(this IHostApplicationBuilder builder)
    {
        builder.Logging.AddOpenTelemetry(logging =>
        {
            logging.IncludeFormattedMessage = true;
            logging.IncludeScopes = true;
        });

        builder.Services.AddOpenTelemetry()
            .WithMetrics(metrics =>
            {
                metrics.AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddRuntimeInstrumentation();
            })
            .WithTracing(tracing =>
            {
                tracing.AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation();
            });

        builder.AddOpenTelemetryExporters();

        return builder;
    }

    private static IHostApplicationBuilder AddOpenTelemetryExporters(this IHostApplicationBuilder builder)
    {
        var useOtlpExporter = !string.IsNullOrWhiteSpace(
            builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"]);

        if (useOtlpExporter)
        {
            builder.Services.ConfigureOpenTelemetryMeterProvider(
                metrics => metrics.AddOtlpExporter());

            builder.Services.ConfigureOpenTelemetryTracerProvider(
                tracing => tracing.AddOtlpExporter());
        }

        return builder;
    }

    public static IHostApplicationBuilder AddDefaultHealthChecks(this IHostApplicationBuilder builder)
    {
        builder.Services.AddHealthChecks()
            .AddCheck("self", () => HealthCheckResult.Healthy(), ["live"]);

        return builder;
    }
}
```

#### 2.3 Update API Projects for Aspire

**Modify each API's Program.cs** (beginning):
```csharp
using Agents.ServiceDefaults;

var builder = WebApplication.CreateBuilder(args);

// Add Aspire service defaults (telemetry, health checks, resilience)
builder.AddServiceDefaults();

// Add Dapr support
builder.Services.AddDaprClient();

// Reference connection strings from Aspire
builder.AddSqlServerDbContext<AgentsDbContext>("agentsdb");
builder.AddRedisClient("redis");

// ... rest of existing configuration
```

**Update csproj files**:
```xml
<ItemGroup>
  <ProjectReference Include="..\..\Shared\Agents.ServiceDefaults\Agents.ServiceDefaults.csproj" />
</ItemGroup>
```

#### 2.4 Configure Rancher Desktop

**Prerequisites**:
- Install Rancher Desktop (already using it per requirements)
- Enable Kubernetes in Rancher Desktop
- Configure Docker Desktop integration

**Setup Steps**:
1. Open Rancher Desktop
2. Preferences → Kubernetes: Enable Kubernetes
3. Set Container Runtime: containerd
4. Set Kubernetes Version: 1.28+
5. Enable Kubernetes Port Forwarding

#### 2.5 Create Development Launch Configuration

**New file**: `.vscode/launch.json` (or update existing)
```json
{
  "version": "0.2.0",
  "configurations": [
    {
      "name": "Launch Aspire AppHost",
      "type": "coreclr",
      "request": "launch",
      "preLaunchTask": "build",
      "program": "${workspaceFolder}/src/AppHost/Agents.AppHost/bin/Debug/net9.0/Agents.AppHost.dll",
      "args": [],
      "cwd": "${workspaceFolder}/src/AppHost/Agents.AppHost",
      "stopAtEntry": false,
      "serverReadyAction": {
        "action": "openExternally",
        "pattern": "\\bNow listening on:\\s+(https?://\\S+)",
        "uriFormat": "%s/dashboard"
      },
      "env": {
        "ASPNETCORE_ENVIRONMENT": "Development",
        "DOTNET_ENVIRONMENT": "Development"
      }
    }
  ]
}
```

### Phase 3: Migration and Testing (Week 5)

#### 3.1 Create Migration Toggle

**Configuration flag** in `appsettings.json`:
```json
{
  "FeatureFlags": {
    "UseDapr": true,
    "UseAspire": true
  }
}
```

#### 3.2 Parallel Implementation Strategy

Keep both implementations side-by-side:
- `Agents.Infrastructure.Events` (existing Azure SDK)
- `Agents.Infrastructure.Dapr` (new Dapr abstraction)

Use dependency injection to switch:
```csharp
if (builder.Configuration.GetValue<bool>("FeatureFlags:UseDapr"))
{
    builder.Services.AddDaprInfrastructure();
}
else
{
    builder.Services.AddAzureInfrastructure();
}
```

#### 3.3 Testing Plan

**Unit Tests**:
- Test Dapr abstractions with mock DaprClient
- Test event publishing and consumption
- Test state store operations

**Integration Tests**:
- Run with Redis (local Dapr components)
- Run with Azure services (production Dapr components)
- Verify compatibility with existing tests

**E2E Testing**:
- Test full workflow in Aspire locally
- Test deployment to Rancher Desktop Kubernetes
- Test deployment to AKS with Dapr

### Phase 4: Documentation and Deployment (Week 6)

#### 4.1 Update Documentation

**Files to update**:
- `README.md` - Add Aspire quickstart
- `docs/local-development-aspire.md` - New guide
- `docs/dapr-integration.md` - New guide
- `infrastructure/README.md` - Update with Dapr components
- `k8s/README.md` - Update with Dapr annotations

#### 4.2 Update CI/CD Pipelines

**GitHub Actions** (`.github/workflows/`):
- Add Dapr CLI installation step
- Add Dapr component deployment
- Update image build to support Dapr

#### 4.3 Create Runbooks

**Operational guides**:
- Troubleshooting Dapr sidecars
- Monitoring Dapr metrics
- Debugging with Aspire dashboard
- Migration checklist from Azure SDK to Dapr

## Benefits Summary

### Development Experience
- **Unified Dashboard**: Aspire dashboard shows all services, logs, traces in one place
- **Simplified Setup**: `dotnet run` from AppHost starts entire system
- **Service Discovery**: No hardcoded URLs - services discover each other
- **Hot Reload**: Changes reflected immediately in Aspire

### Operational Benefits
- **Portability**: Switch from Redis → Service Bus without code changes
- **Observability**: Built-in distributed tracing and metrics
- **Resilience**: Dapr provides retry policies, circuit breakers
- **Sidecar Pattern**: Infrastructure concerns separated from application code

### Cloud Flexibility
- **Multi-Cloud**: Same code works on Azure, AWS, GCP
- **Hybrid**: Run some services on-prem, others in cloud
- **Local-First**: Full development environment without cloud dependencies

## Risks and Mitigations

| Risk | Impact | Mitigation |
|------|--------|-----------|
| Learning curve for Dapr | Medium | Provide training, start with simple components |
| Aspire is new (9.0 released late 2024) | Medium | Keep existing docker-compose as fallback |
| Performance overhead of sidecars | Low | Benchmark and optimize, Dapr is production-ready |
| Complexity in debugging distributed traces | Medium | Use Aspire dashboard and Zipkin integration |
| Breaking changes for existing deployments | High | Parallel implementation, gradual migration |

## Success Criteria

1. ✅ All 5 agent APIs running in Aspire with Dapr sidecars
2. ✅ Local development using Redis for pub/sub and state
3. ✅ Production using Azure Service Bus and Cosmos DB via Dapr
4. ✅ Aspire dashboard showing all services, logs, and traces
5. ✅ Deployment to Rancher Desktop Kubernetes successful
6. ✅ Deployment to AKS with Dapr control plane successful
7. ✅ All existing unit and integration tests passing
8. ✅ Documentation updated with new developer workflow

## Timeline

| Phase | Duration | Deliverables |
|-------|----------|-------------|
| Phase 1: Dapr Setup | 2 weeks | Dapr runtime, components, abstractions |
| Phase 2: Aspire Integration | 2 weeks | AppHost, ServiceDefaults, dashboard |
| Phase 3: Migration & Testing | 1 week | Tests passing, validation complete |
| Phase 4: Docs & Deployment | 1 week | Updated docs, deployed to environments |
| **Total** | **6 weeks** | Fully integrated Dapr + Aspire solution |

## Next Steps (Pending Approval)

1. Install Dapr CLI and initialize on local machine
2. Create `Agents.Infrastructure.Dapr` project
3. Create Aspire AppHost project
4. Implement DaprEventPublisher
5. Update one API (Notification) as proof of concept
6. Run and validate in Aspire dashboard

## Open Questions

1. Should we deprecate direct Azure SDK usage or keep both long-term?
2. What's the migration timeline for production workloads?
3. Do we need separate Aspire configurations for different environments?
4. Should we use Dapr actors for stateful agent patterns?
5. Integration with existing Prometheus/Grafana monitoring?

---

**Document Version**: 1.0  
**Last Updated**: 2025-11-13  
**Status**: Awaiting Approval
