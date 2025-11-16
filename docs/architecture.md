# Architecture Overview

This document provides a comprehensive overview of the AI Orchestration Multi-Agent Framework architecture, including design principles, layer structure, and key patterns.

## Table of Contents

- [Core Principles](#core-principles)
- [Architecture Style](#architecture-style)
- [System Context](#system-context)
- [Layer Structure](#layer-structure)
- [Event-Driven Architecture](#event-driven-architecture)
- [Agent Architecture](#agent-architecture)
- [Data Flow](#data-flow)
- [Design Patterns](#design-patterns)
- [Technology Decisions](#technology-decisions)

## Core Principles

The framework is built on seven fundamental principles:

1. **Clean Architecture**: Strict separation of concerns with clear boundaries between Domain, Application, Infrastructure, and Presentation layers
2. **Event-Driven**: Agents communicate asynchronously via Dapr pub/sub (backed by Redis locally, Azure Service Bus in production)
3. **Infrastructure Agnostic**: Dapr abstractions enable portability across cloud providers and on-premises
4. **Prompt-Driven**: All agent behavior is defined in versioned prompt files stored in a repository
5. **Environment Agnostic**: Seamless switching between Ollama (development) and Azure OpenAI (production) via configuration
6. **Developer Experience First**: .NET Aspire provides unified local development with integrated observability
7. **Cloud Native**: Designed for Kubernetes deployment with Dapr sidecars, horizontal scaling, and comprehensive observability

## Architecture Style

### Microservices Architecture

Each agent is deployed as an independent microservice with:
- Dedicated REST API
- Independent scaling
- Isolated failure domains
- Service-to-service communication via events

### Event-Driven Architecture with Dapr

Agents communicate through **Dapr pub/sub** which abstracts the underlying messaging infrastructure:

**Local Development:**
- **Redis Streams**: Pub/sub component for local testing
- **Redis**: State store for agent state management

**Production:**
- **Azure Service Bus**: Dapr pub/sub component for production messaging
- **Azure Cosmos DB**: Dapr state store component for distributed state

**Benefits:**
- Switch messaging backends without code changes
- Consistent API across environments
- Built-in retries, circuit breakers, and observability
- Cloud-agnostic architecture

## System Context

### C4 Context Diagram

The system interacts with the following external systems:

```mermaid
C4Context
    title System Context - AI Agent Orchestration Framework with Dapr
    
    Person(user, "Developer/User", "Interacts with agents via API")
    System(agentSystem, "AI Agent Framework", "Multi-agent orchestration with Dapr")
    System_Ext(dapr, "Dapr Runtime", "Service mesh and building blocks")
    
    System_Ext(azureOpenAI, "Azure OpenAI", "Production LLM")
    System_Ext(ollama, "Ollama", "Development LLM")
    System_Ext(github, "GitHub", "Prompt repository")
    System_Ext(serviceBus, "Azure Service Bus", "Production pub/sub via Dapr")
    System_Ext(cosmosDb, "Azure Cosmos DB", "Production state store via Dapr")
    System_Ext(redis, "Redis", "Local pub/sub and state via Dapr")
    System_Ext(appInsights, "Application Insights", "Observability and monitoring")
    
    Rel(user, agentSystem, "Sends requests", "HTTPS/REST")
    Rel(agentSystem, dapr, "Uses building blocks", "HTTP/gRPC")
    Rel(dapr, serviceBus, "Pub/sub (prod)", "AMQP")
    Rel(dapr, redis, "Pub/sub & state (dev)", "Redis Protocol")
    Rel(dapr, cosmosDb, "State store (prod)", "HTTPS")
    Rel(agentSystem, azureOpenAI, "Invokes LLM", "HTTPS")
    Rel(agentSystem, ollama, "Invokes LLM (dev)", "HTTP")
    Rel(agentSystem, github, "Fetches prompts", "HTTPS")
    Rel(agentSystem, appInsights, "Telemetry", "HTTPS")
```

## Layer Structure

The framework follows Clean Architecture with four distinct layers:

### 1. Domain Layer

**Purpose**: Core business rules and enterprise logic

**Components**:
- Entities: Core business objects (Agent, Prompt, Event)
- Value Objects: Immutable objects with no identity (Email, Priority)
- Domain Events: Business events (NotificationSent, CodeGenerated)
- Interfaces: Contracts for external dependencies

**Key Principles**:
- No dependencies on other layers
- Pure business logic
- Framework-agnostic

**Example Structure**:
```
Domain/
├── Agents.Domain.Core/
│   ├── Entities/
│   │   ├── Agent.cs
│   │   ├── Prompt.cs
│   │   └── DomainEvent.cs
│   ├── ValueObjects/
│   │   ├── AgentId.cs
│   │   └── PromptVersion.cs
│   ├── Events/
│   │   └── AgentExecutedEvent.cs
│   └── Interfaces/
│       ├── IEventPublisher.cs
│       └── IPromptLoader.cs
├── Agents.Domain.Notification/
├── Agents.Domain.DevOps/
├── Agents.Domain.TestPlanning/
├── Agents.Domain.Implementation/
└── Agents.Domain.ServiceDesk/
```

### 2. Application Layer

**Purpose**: Use cases and orchestration logic

**Components**:
- Agents: Core agent implementations using Semantic Kernel
- Commands: Write operations (CQRS pattern)
- Queries: Read operations (CQRS pattern)
- Event Handlers: React to domain events
- Validators: Input validation logic

**Key Principles**:
- Depends only on Domain layer
- Orchestrates domain logic
- Technology-agnostic use cases

**Example Structure**:
```
Application/
├── Agents.Application.Core/
│   ├── Interfaces/
│   ├── DTOs/
│   ├── Behaviors/
│   └── Exceptions/
├── Agents.Application.Notification/
│   ├── NotificationAgent.cs
│   ├── Commands/
│   │   └── SendNotificationCommand.cs
│   ├── Queries/
│   │   └── GetNotificationStatusQuery.cs
│   ├── EventHandlers/
│   │   └── NotificationSentEventHandler.cs
│   └── Validators/
│       └── SendNotificationValidator.cs
```

### 3. Infrastructure Layer

**Purpose**: External concerns and third-party integrations

**Components**:
- LLM Providers: Azure OpenAI and Ollama implementations
- Event Infrastructure: Event Grid, Event Hubs, Service Bus clients
- Persistence: Cosmos DB and Azure SQL repositories
- Monitoring: Application Insights, Prometheus exporters

**Key Principles**:
- Implements interfaces from Domain/Application layers
- Contains all third-party dependencies
- Environment-specific implementations

**Example Structure**:
```
Infrastructure/
├── Agents.Infrastructure.LLM/
│   ├── AzureOpenAIProvider.cs
│   ├── OllamaProvider.cs
│   └── SemanticKernelConfiguration.cs
├── Agents.Infrastructure.EventGrid/
│   ├── EventGridPublisher.cs
│   └── EventGridSubscriber.cs
├── Agents.Infrastructure.EventHub/
├── Agents.Infrastructure.ServiceBus/
├── Agents.Infrastructure.Persistence/
│   ├── CosmosDb/
│   └── SqlServer/
└── Agents.Infrastructure.Monitoring/
```

### 4. Presentation Layer

**Purpose**: API endpoints and user interfaces

**Components**:
- REST APIs: ASP.NET Core Web APIs
- Controllers: Handle HTTP requests
- DTOs: Request/response models
- Swagger: API documentation

**Key Principles**:
- Depends on Application layer
- Handles HTTP concerns only
- Thin layer - no business logic

**Example Structure**:
```
Presentation/
├── Agents.API.Notification/
│   ├── Controllers/
│   │   └── NotificationController.cs
│   ├── Models/
│   │   ├── SendNotificationRequest.cs
│   │   └── NotificationResponse.cs
│   └── Program.cs
├── Agents.API.DevOps/
├── Agents.API.TestPlanning/
├── Agents.API.Implementation/
├── Agents.API.ServiceDesk/
└── Agents.API.BimClassification/
```

## Event-Driven Architecture

### Event Schema

All events follow a standardized schema:

```json
{
  "id": "guid",
  "eventType": "agents.{domain}.{action}",
  "subject": "context/path",
  "eventTime": "ISO8601",
  "dataVersion": "1.0",
  "metadataVersion": "1",
  "data": {
    "correlationId": "guid",
    "causationId": "guid",
    "agentId": "string",
    "payload": {}
  }
}
```

### Event Types by Domain

**Notification Agent**
- `agents.notification.send.requested`
- `agents.notification.sent.completed`
- `agents.notification.delivery.confirmed`
- `agents.notification.delivery.failed`

**DevOps Agent**
- `agents.devops.project.updated`
- `agents.devops.workflow.triggered`
- `agents.devops.issue.created`
- `agents.devops.sprint.analyzed`

**TestPlanning Agent**
- `agents.testplanning.spec.generated`
- `agents.testplanning.strategy.created`
- `agents.testplanning.coverage.analyzed`

**Implementation Agent**
- `agents.implementation.code.generated`
- `agents.implementation.review.completed`
- `agents.implementation.refactoring.suggested`

**ServiceDesk Agent**
- `agents.servicedesk.ticket.created`
- `agents.servicedesk.ticket.triaged`
- `agents.servicedesk.solution.suggested`
- `agents.servicedesk.ticket.escalated`

**BimClassification Agent**
- `agents.bimclassification.suggestion.generated`
- `agents.bimclassification.suggestion.approved`
- `agents.bimclassification.suggestion.rejected`
- `agents.bimclassification.classification.applied`

### Inter-Agent Communication

Agents communicate through event subscriptions:

```mermaid
sequenceDiagram
    participant User as User/Client
    participant TestAPI as TestPlanning API
    participant TestSidecar as Dapr Sidecar (Test)
    participant PubSub as Pub/Sub Component<br/>(Redis/Service Bus)
    participant ImplSidecar as Dapr Sidecar (Impl)
    participant ImplAPI as Implementation API
    participant StateStore as State Store<br/>(Redis/Cosmos DB)
    participant NotifSidecar as Dapr Sidecar (Notif)
    participant NotifAPI as Notification API
    
    User->>TestAPI: POST /api/TestPlanning/execute
    TestAPI->>TestAPI: Generate test specification
    TestAPI->>TestSidecar: Publish(TestSpecGeneratedEvent)
    TestSidecar->>PubSub: Publish to agents-pubsub
    
    PubSub->>ImplSidecar: Deliver event
    ImplSidecar->>ImplAPI: POST /testspec-generated
    ImplAPI->>ImplAPI: Generate code implementation
    ImplAPI->>ImplSidecar: SaveState(implementation-data)
    ImplSidecar->>StateStore: Save to agents-statestore
    ImplAPI->>ImplSidecar: Publish(CodeGeneratedEvent)
    ImplSidecar->>PubSub: Publish to agents-pubsub
    
    PubSub->>NotifSidecar: Deliver event
    NotifSidecar->>NotifAPI: POST /code-generated
    NotifAPI->>NotifAPI: Prepare notification
    NotifAPI->>NotifSidecar: Publish(NotificationSentEvent)
    NotifSidecar->>PubSub: Publish to agents-pubsub
    
    Note over TestAPI,NotifAPI: All service-to-service communication<br/>happens through Dapr sidecars<br/>using pub/sub pattern
```

## Local Development with Aspire

### Aspire AppHost Architecture

.NET Aspire provides unified local development orchestration:

```mermaid
graph TB
    subgraph "Aspire AppHost"
        AppHost[Agents.AppHost]
    end
    
    subgraph "Infrastructure Containers"
        Redis[Redis Container<br/>Port 6379]
        SQL[SQL Server Container<br/>Port 1433]
        Ollama[Ollama Container<br/>Port 11434]
    end
    
    subgraph "API Services with Dapr"
        N1[notification-api<br/>Port 7268]
        N1D[Dapr Sidecar<br/>Port 51268]
        D1[devops-api<br/>Port 7108]
        D1D[Dapr Sidecar<br/>Port 51271]
        T1[testplanning-api<br/>Port 7010]
        T1D[Dapr Sidecar<br/>Port 51274]
        I1[implementation-api<br/>Port 5253]
        I1D[Dapr Sidecar<br/>Port 51277]
        S1[servicedesk-api<br/>Port 7145]
        S1D[Dapr Sidecar<br/>Port 51280]
        B1[bimclassification-api<br/>Port 7220]
        B1D[Dapr Sidecar<br/>Port 51283]
    end
    
    subgraph "Developer Experience"
        Dashboard[Aspire Dashboard<br/>Port 17120]
        Browser[Web Browser]
    end
    
    AppHost-->Redis
    AppHost-->SQL
    AppHost-->Ollama
    AppHost-->N1
    AppHost-->D1
    AppHost-->T1
    AppHost-->I1
    AppHost-->S1
    AppHost-->B1
    AppHost-->N1D
    AppHost-->D1D
    AppHost-->T1D
    AppHost-->I1D
    AppHost-->S1D
    AppHost-->B1D
    
    N1<-->N1D
    D1<-->D1D
    T1<-->T1D
    I1<-->I1D
    S1<-->S1D
    B1<-->B1D
    
    N1D-->Redis
    D1D-->Redis
    T1D-->Redis
    I1D-->Redis
    S1D-->Redis
    B1D-->Redis
    
    N1-->SQL
    N1-->Ollama
    D1-->Ollama
    T1-->Ollama
    I1-->Ollama
    S1-->Ollama
    B1-->SQL
    B1-->Ollama
```
    AppHost-->Dashboard
    Browser-->Dashboard
    Browser-->N1
    Browser-->D1
    Browser-->T1
    Browser-->I1
    Browser-->S1
```

**Key Benefits:**
- **One Command Start**: `dotnet run --project src/AppHost/Agents.AppHost/Agents.AppHost.csproj`
- **Integrated Observability**: Real-time logs, traces, and metrics in Aspire Dashboard
- **Service Discovery**: Automatic endpoint configuration
- **Dapr Sidecar Management**: Aspire handles Dapr lifecycle
- **Health Monitoring**: Visual service health status

## Agent Architecture

### Base Agent Structure

All agents inherit from a base agent class:

```csharp
public abstract class BaseAgent
{
    protected readonly ILLMProvider _llmProvider;
    protected readonly IPromptLoader _promptLoader;
    protected readonly IEventPublisher _eventPublisher;
    protected readonly ILogger _logger;

    public abstract Task<AgentResult> ExecuteAsync(AgentRequest request, CancellationToken ct);
    
    protected async Task<string> InvokeKernelAsync(string promptName, object data)
    {
        var prompt = await _promptLoader.LoadPromptAsync(promptName);
        return await _llmProvider.CompleteAsync(prompt, data);
    }
}
```

### Agent Lifecycle

1. **Receive Request**: API endpoint receives HTTP request
2. **Load Prompt**: Agent loads versioned prompt from repository
3. **Invoke LLM**: Semantic Kernel processes prompt with LLM provider
4. **Process Result**: Agent validates and transforms LLM response
5. **Publish Event**: Domain event published to Event Grid
6. **Return Response**: HTTP response returned to caller

## Data Flow

### Request Flow

```
HTTP Request → API Controller → Agent → Prompt Loader → LLM Provider → Agent → Event Publisher → HTTP Response
```

### Event Flow

```
Agent → Event Publisher → Event Grid → Event Subscriber → Agent Event Handler → Domain Logic
```

## Design Patterns

### 1. Repository Pattern
Abstracts data access logic from business logic
```csharp
public interface IAgentRepository
{
    Task<Agent> GetByIdAsync(AgentId id);
    Task SaveAsync(Agent agent);
}
```

### 2. CQRS (Command Query Responsibility Segregation)
Separates read and write operations
```csharp
// Command
public record SendNotificationCommand(string Channel, string Message);

// Query
public record GetNotificationStatusQuery(Guid NotificationId);
```

### 3. Event Sourcing
Events as the source of truth for state changes
```csharp
public class NotificationAggregate
{
    public void Apply(NotificationSentEvent @event)
    {
        Status = NotificationStatus.Sent;
        SentAt = @event.Timestamp;
    }
}
```

### 4. Strategy Pattern
Swappable LLM providers
```csharp
public class LLMProviderFactory
{
    public ILLMProvider Create(string providerType) => providerType switch
    {
        "Ollama" => new OllamaProvider(),
        "AzureOpenAI" => new AzureOpenAIProvider(),
        _ => throw new ArgumentException("Unknown provider")
    };
}
```

### 5. Dependency Injection
All dependencies injected through constructors
```csharp
public class NotificationAgent : BaseAgent
{
    public NotificationAgent(
        ILLMProvider llmProvider,
        IPromptLoader promptLoader,
        IEventPublisher eventPublisher,
        ILogger<NotificationAgent> logger)
        : base(llmProvider, promptLoader, eventPublisher, logger)
    {
    }
}
```

## Technology Decisions

### Why .NET 9?
- Latest C# features and performance improvements
- Native support for OpenTelemetry and observability
- Excellent Azure integration
- Strong typing and compile-time safety

### Why Semantic Kernel?
- First-party Microsoft LLM orchestration framework
- Built-in support for Azure OpenAI
- Pluggable architecture for different LLM providers
- Memory and planning capabilities

### Why Event-Driven Architecture?
- Loose coupling between agents
- Asynchronous processing for better scalability
- Resilience through message queuing
- Easy to add new agents without modifying existing ones

### Why Clean Architecture?
- Testability through dependency inversion
- Technology independence
- Clear separation of concerns
- Maintainability for large codebases

### Why Kubernetes?
- Horizontal scaling of individual agents
- Self-healing and rolling updates
- Service discovery and load balancing
- Cloud provider portability

## Deployment Architecture

### Development Environment
```
Developer Machine
├── Ollama (local LLM)
├── Azurite (storage emulator)
├── Docker Compose
│   ├── Event Grid emulator
│   ├── Cosmos DB emulator
│   └── SQL Server
└── Visual Studio / VS Code
```

### Production Environment
```
Azure Kubernetes Service (AKS)
├── Agent Pods (5 deployments)
├── NGINX Ingress Controller
├── Horizontal Pod Autoscalers
└── Pod Disruption Budgets

Azure Services
├── Azure OpenAI
├── Azure Event Grid
├── Azure Event Hubs
├── Azure Service Bus
├── Azure Cosmos DB
├── Azure SQL Database
├── Azure Key Vault
├── Azure Monitor
└── Application Insights
```

## Security Architecture

### Authentication & Authorization
- Azure AD integration for user authentication
- Managed identities for service-to-service authentication
- RBAC for Kubernetes resources
- API keys stored in Azure Key Vault

### Network Security
- Private endpoints for Azure services
- Network policies in AKS
- HTTPS/TLS everywhere
- Web Application Firewall (WAF)

### Data Security
- Encryption at rest (Azure Storage Service Encryption)
- Encryption in transit (TLS 1.2+)
- Customer-managed keys in Key Vault
- Data residency compliance

## Performance Considerations

### Horizontal Scaling
- Each agent scales independently based on CPU/memory
- Event-driven architecture allows parallel processing
- Stateless agents for easy scaling

### Caching Strategy
- Prompt caching to reduce I/O
- LLM response caching for repeated queries
- Redis cache for frequently accessed data

### Optimization Techniques
- Connection pooling for database access
- Batch processing for high-volume events
- Async/await throughout for non-blocking I/O
- Circuit breakers for failing dependencies

## Disaster Recovery

### High Availability
- Multi-zone AKS deployment
- Geo-replicated storage
- Active-passive Azure OpenAI deployments
- Event Grid availability zones

### Backup & Restore
- Automated Cosmos DB backups
- Point-in-time restore for Azure SQL
- Infrastructure state in source control
- GitOps for Kubernetes manifests

### Business Continuity
- RTO (Recovery Time Objective): < 1 hour
- RPO (Recovery Point Objective): < 15 minutes
- Automated failover procedures
- Regular disaster recovery drills

## Further Reading

- [Agent Development Guide](agent-development.md)
- [Prompt Authoring Guide](prompt-authoring.md)
- [Deployment Guide](deployment.md)
- [Operations Runbook](operations.md)
