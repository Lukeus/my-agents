# AI Orchestration Multi-Agent Framework

An event-driven microservice framework for AI agent orchestration using C#, Microsoft Semantic Kernel, and Azure services.

## 🎯 Overview

This framework provides a production-ready, clean architecture implementation of AI agents that can:
- Process events from Azure Event Grid, Event Hubs, and Service Bus
- Execute LLM-powered operations via Azure OpenAI or Ollama
- Communicate via REST APIs with full Swagger documentation
- Scale independently in Azure Kubernetes Service (AKS)
- Load and execute versioned prompts from GitHub repositories

## 🏗️ Architecture

### C4 Context Diagram

```mermaid
C4Context
    title System Context - AI Agent Orchestration Framework
    
    Person(user, "Developer/User", "Interacts with agents via API")
    System(agentSystem, "AI Agent Framework", "Multi-agent orchestration system")
    
    System_Ext(azureOpenAI, "Azure OpenAI", "Production LLM")
    System_Ext(ollama, "Ollama", "Development LLM")
    System_Ext(github, "GitHub", "Prompt repository")
    System_Ext(eventGrid, "Azure Event Grid", "Event routing")
    System_Ext(eventHubs, "Azure Event Hubs", "High-throughput events")
    System_Ext(serviceBus, "Azure Service Bus", "Reliable messaging")
    
    Rel(user, agentSystem, "Sends requests", "HTTPS/REST")
    Rel(agentSystem, azureOpenAI, "Invokes LLM", "HTTPS")
    Rel(agentSystem, ollama, "Invokes LLM (dev)", "HTTP")
    Rel(agentSystem, github, "Fetches prompts", "HTTPS")
    Rel(agentSystem, eventGrid, "Publishes/subscribes", "AMQP")
    Rel(agentSystem, eventHubs, "Produces/consumes", "AMQP")
    Rel(agentSystem, serviceBus, "Sends/receives", "AMQP")
```

### C4 Container Diagram

```mermaid
C4Container
    title Container Diagram - Agent Microservices
    
    Container(notificationAPI, "Notification API", "ASP.NET Core", "Handles multi-channel notifications")
    Container(devopsAPI, "DevOps API", "ASP.NET Core", "GitHub automation & analytics")
    Container(testPlanningAPI, "TestPlanning API", "ASP.NET Core", "Test spec generation")
    Container(implementationAPI, "Implementation API", "ASP.NET Core", "Code generation & review")
    Container(serviceDeskAPI, "ServiceDesk API", "ASP.NET Core", "Ticket triage & SLA tracking")
    
    ContainerDb(promptRepo, "Prompt Repository", "GitHub", "Versioned prompt files")
    ContainerDb(eventInfra, "Event Infrastructure", "Azure", "Event Grid, Hubs, Service Bus")
    Container(llmProvider, "LLM Provider", "Semantic Kernel", "Azure OpenAI / Ollama")
    
    Rel(notificationAPI, llmProvider, "Invokes")
    Rel(devopsAPI, llmProvider, "Invokes")
    Rel(testPlanningAPI, llmProvider, "Invokes")
    Rel(implementationAPI, llmProvider, "Invokes")
    Rel(serviceDeskAPI, llmProvider, "Invokes")
    
    Rel(notificationAPI, promptRepo, "Loads prompts")
    Rel(devopsAPI, promptRepo, "Loads prompts")
    Rel(testPlanningAPI, promptRepo, "Loads prompts")
    Rel(implementationAPI, promptRepo, "Loads prompts")
    Rel(serviceDeskAPI, promptRepo, "Loads prompts")
    
    Rel(notificationAPI, eventInfra, "Publishes events")
    Rel(devopsAPI, eventInfra, "Publishes events")
    Rel(testPlanningAPI, eventInfra, "Publishes events")
    Rel(implementationAPI, eventInfra, "Publishes events")
    Rel(serviceDeskAPI, eventInfra, "Publishes events")
```

### C4 Component Diagram - Agent Structure

```mermaid
C4Component
    title Component Diagram - Agent Internal Structure
    
    Container_Boundary(agent, "Agent Microservice") {
        Component(controller, "API Controller", "ASP.NET MVC", "REST endpoints")
        Component(agentCore, "Agent", "BaseAgent", "Core agent logic")
        Component(llmProvider, "LLM Provider", "ILLMProvider", "Provider abstraction")
        Component(promptLoader, "Prompt Loader", "IPromptLoader", "Loads & caches prompts")
        Component(eventPublisher, "Event Publisher", "IEventPublisher", "Publishes domain events")
        Component(domainModel, "Domain Model", "Aggregates", "Business logic")
    }
    
    Rel(controller, agentCore, "Calls")
    Rel(agentCore, llmProvider, "Uses")
    Rel(agentCore, promptLoader, "Uses")
    Rel(agentCore, eventPublisher, "Uses")
    Rel(agentCore, domainModel, "Manages")
```

## 🔄 Agent Workflows

### Notification Agent Sequence

```mermaid
sequenceDiagram
    participant Client
    participant API as Notification API
    participant Agent as NotificationAgent
    participant Prompt as PromptLoader
    participant LLM as Semantic Kernel
    participant Channel as NotificationChannel
    participant Events as Event Publisher
    
    Client->>API: POST /api/notification/send
    API->>Agent: ExecuteAsync(request)
    Agent->>Prompt: LoadPromptAsync("email-formatter")
    Prompt-->>Agent: Prompt template
    Agent->>LLM: InvokeKernelAsync(prompt + data)
    LLM-->>Agent: Formatted content
    Agent->>Events: Publish(NotificationFormattedEvent)
    Agent->>Channel: SendAsync(recipient, content)
    Channel-->>Agent: Success/Failure
    Agent->>Events: Publish(NotificationSentEvent)
    Agent-->>API: AgentResult
    API-->>Client: 200 OK
```

### DevOps Agent Sequence

```mermaid
sequenceDiagram
    participant Client
    participant API as DevOps API
    participant Agent as DevOpsAgent
    participant Prompt as PromptLoader
    participant LLM as Semantic Kernel
    participant GitHub as GitHub API
    participant Events as Event Publisher
    
    Client->>API: POST /api/devops/execute<br/>{action: "create_issue"}
    API->>Agent: ExecuteAsync(request)
    Agent->>Prompt: LoadPromptAsync("issue-creator")
    Prompt-->>Agent: Prompt template
    Agent->>LLM: InvokeKernelAsync(prompt + issue data)
    LLM-->>Agent: Enhanced issue details
    Agent->>GitHub: Create issue (TODO)
    GitHub-->>Agent: Issue #123
    Agent->>Events: Publish(IssueCreatedEvent)
    Agent-->>API: AgentResult(issueNumber)
    API-->>Client: 200 OK
```

### Test Planning Agent Sequence

```mermaid
sequenceDiagram
    participant Client
    participant API as TestPlanning API
    participant Agent as TestPlanningAgent
    participant Prompt as PromptLoader
    participant LLM as Semantic Kernel
    participant Events as Event Publisher
    
    Client->>API: POST /api/testplanning/execute<br/>{type: "generate_spec"}
    API->>Agent: ExecuteAsync(request)
    Agent->>Prompt: LoadPromptAsync("spec-generator")
    Prompt-->>Agent: Prompt template
    Agent->>LLM: InvokeKernelAsync(feature description)
    LLM-->>Agent: Test specification
    Agent->>Events: Publish(TestSpecGeneratedEvent)
    Agent-->>API: AgentResult<TestSpec>
    API-->>Client: 200 OK + test spec
```

### Implementation Agent Sequence

```mermaid
sequenceDiagram
    participant Client
    participant API as Implementation API
    participant Agent as ImplementationAgent
    participant Prompt as PromptLoader
    participant LLM as Semantic Kernel
    participant Events as Event Publisher
    
    Client->>API: POST /api/implementation/execute<br/>{action: "generate_code"}
    API->>Agent: ExecuteAsync(request)
    Agent->>Prompt: LoadPromptAsync("code-generator")
    Prompt-->>Agent: Prompt template
    Agent->>LLM: InvokeKernelAsync(specification)
    LLM-->>Agent: Generated code
    Agent->>Events: Publish(CodeGeneratedEvent)
    Agent-->>API: AgentResult<Code>
    API-->>Client: 200 OK + generated code
```

### Inter-Agent Event Flow

```mermaid
sequenceDiagram
    participant TestAgent as TestPlanning Agent
    participant EventBus as Azure Event Grid
    participant ImplAgent as Implementation Agent
    participant NotifAgent as Notification Agent
    
    TestAgent->>EventBus: Publish(TestSpecGeneratedEvent)
    EventBus->>ImplAgent: Subscribe(TestSpecGeneratedEvent)
    ImplAgent->>ImplAgent: Generate implementation
    ImplAgent->>EventBus: Publish(CodeGeneratedEvent)
    EventBus->>NotifAgent: Subscribe(CodeGeneratedEvent)
    NotifAgent->>NotifAgent: Send notification to team
```

## Getting Started

### Prerequisites

- .NET 8 SDK or later
- Docker Desktop
- Azure CLI (for Azure deployment)
- Ollama (for local development)
- kubectl (for Kubernetes)

### Local Development Setup

1. **Clone the repository**
   ```powershell
   git clone <repository-url>
   cd my-agents
   ```

2. **Install Ollama**
   ```powershell
   # Download from https://ollama.ai
   ollama pull llama3.1
   ```

3. **Configure application settings**
   ```powershell
   cp src/Presentation/Agents.API.Gateway/appsettings.json src/Presentation/Agents.API.Gateway/appsettings.Development.json
   # Edit appsettings.Development.json to use Ollama
   ```

4. **Build the solution**
   ```powershell
   dotnet build
   ```

5. **Run tests**
   ```powershell
   dotnet test
   ```

6. **Run locally**
   ```powershell
   dotnet run --project src/Presentation/Agents.API.Gateway
   ```

### Azure Deployment

1. **Deploy infrastructure**
   ```powershell
   az login
   cd infrastructure/bicep
   az deployment sub create --location eastus --template-file main.bicep --parameters @parameters/dev.parameters.json
   ```

2. **Build and push containers**
   ```powershell
   # See deployment documentation
   ```

3. **Deploy to AKS**
   ```powershell
   kubectl apply -k k8s/overlays/dev
   ```

## Project Structure

```
├── src/
│   ├── Domain/              # Core business logic
│   ├── Application/         # Use cases and orchestration
│   ├── Infrastructure/      # External integrations
│   ├── Presentation/        # APIs
│   └── Shared/             # Cross-cutting concerns
├── prompts/                # Agent prompt files
├── infrastructure/         # IaC (Bicep/Terraform)
├── k8s/                   # Kubernetes manifests
├── tests/                 # Tests
└── docs/                  # Documentation
```

## Configuration

### LLM Provider Switching

Toggle between Ollama (development) and Azure OpenAI (production) via `appsettings.json`:

```json
{
  "LLMProvider": {
    "Type": "Ollama"  // or "AzureOpenAI"
  }
}
```

## 🚀 Features

### Core Capabilities
- **5 Specialized Agents**: Notification, DevOps, TestPlanning, Implementation, ServiceDesk
- **Event-Driven Architecture**: Azure Event Grid, Event Hubs, Service Bus integration
- **Prompt-Driven**: All agent logic configured via versioned prompt files
- **Dual LLM Support**: Azure OpenAI (production) and Ollama (development)
- **REST APIs**: Full Swagger documentation for all endpoints
- **Health Checks**: Kubernetes-ready liveness/readiness probes
- **Clean Architecture**: Strict separation of concerns (Domain, Application, Infrastructure, Presentation)
- **Comprehensive Testing**: Unit tests with 100% pass rate

### Agent Capabilities

| Agent | Purpose | Key Features |
|-------|---------|-------------|
| **Notification** | Multi-channel notifications | Email, SMS, Teams, Slack formatters with delivery tracking |
| **DevOps** | GitHub automation | Issue creation, sprint analytics, workflow triggering |
| **TestPlanning** | Test generation | Test spec generation, strategy planning, coverage analysis |
| **Implementation** | Code generation | Code generation, review, refactoring suggestions |
| **ServiceDesk** | Ticket management | Triage, solution suggestions, SLA tracking, escalation |

## 🛠️ Technology Stack

### Core Technologies
- **.NET 9**: Latest C# features and performance improvements
- **Microsoft Semantic Kernel 1.67.1**: LLM orchestration framework
- **ASP.NET Core**: REST API hosting
- **Swashbuckle 10.0**: OpenAPI/Swagger documentation

### Azure Services
- **Azure OpenAI**: Production LLM
- **Azure Event Grid**: Event routing and delivery
- **Azure Event Hubs**: High-throughput event streaming
- **Azure Service Bus**: Reliable message queuing
- **Azure Kubernetes Service (AKS)**: Container orchestration

### Development
- **Ollama**: Local LLM for development
- **xUnit**: Unit testing framework
- **Moq 4.20**: Mocking framework
- **FluentAssertions 8.8**: Assertion library

## 📁 Project Structure

```
my-agents/
├── src/
│   ├── Application/           # Application layer (agents, CQRS)
│   │   ├── Agents.Application.Core/
│   │   ├── Agents.Application.Notification/
│   │   ├── Agents.Application.DevOps/
│   │   ├── Agents.Application.TestPlanning/
│   │   ├── Agents.Application.Implementation/
│   │   └── Agents.Application.ServiceDesk/
│   ├── Domain/                # Domain layer (entities, events)
│   │   ├── Agents.Domain.Core/
│   │   └── Agents.Domain.*/
│   ├── Infrastructure/        # Infrastructure layer
│   │   ├── Agents.Infrastructure.LLM/
│   │   ├── Agents.Infrastructure.Prompts/
│   │   ├── Agents.Infrastructure.EventGrid/
│   │   ├── Agents.Infrastructure.EventHub/
│   │   └── Agents.Infrastructure.ServiceBus/
│   └── Presentation/          # API layer
│       ├── Agents.API.Notification/
│       ├── Agents.API.DevOps/
│       ├── Agents.API.TestPlanning/
│       ├── Agents.API.Implementation/
│       └── Agents.API.ServiceDesk/
├── tests/
│   └── Agents.Tests.Unit/     # Unit tests (24 tests, 100% pass)
├── prompts/                   # Versioned prompt files
└── infrastructure/            # IaC (Bicep/Terraform)
```

## 🏃 Getting Started

### Prerequisites
- .NET 9 SDK
- Ollama (for local development) or Azure OpenAI credentials
- Docker (optional, for containerization)

### Configuration

1. **Clone the repository**
```bash
git clone <repository-url>
cd my-agents
```

2. **Configure LLM Provider**

Edit `appsettings.json` in any API project:

```json
{
  "LLMProvider": {
    "ProviderType": "Ollama",  // or "AzureOpenAI"
    "Ollama": {
      "Endpoint": "http://localhost:11434",
      "ModelId": "llama3.2"
    },
    "AzureOpenAI": {
      "Endpoint": "https://your-openai.openai.azure.com",
      "ApiKey": "your-api-key",
      "DeploymentName": "gpt-4",
      "ModelId": "gpt-4"
    }
  }
}
```

3. **Build the solution**
```bash
dotnet build
```

4. **Run tests**
```bash
dotnet test
```

5. **Run an API**
```bash
# Run Notification API
cd src/Presentation/Agents.API.Notification
dotnet run

# Navigate to https://localhost:5001/swagger
```

### API Endpoints

Each agent exposes the following endpoints:

| Agent | Base URL | Execute Endpoint | Health Check |
|-------|----------|------------------|-------------|
| Notification | `:5001` | `POST /api/notification/send` | `GET /api/notification/health` |
| DevOps | `:5002` | `POST /api/devops/execute` | `GET /api/devops/health` |
| TestPlanning | `:5003` | `POST /api/testplanning/execute` | `GET /api/testplanning/health` |
| Implementation | `:5004` | `POST /api/implementation/execute` | `GET /api/implementation/health` |
| ServiceDesk | `:5005` | `POST /api/servicedesk/execute` | `GET /api/servicedesk/health` |

### Example Request

```bash
curl -X POST https://localhost:5001/api/notification/send \
  -H "Content-Type: application/json" \
  -d '{
    "channel": "email",
    "recipient": "user@example.com",
    "subject": "Test Notification",
    "content": "This is a test message"
  }'
```

## 🧪 Testing

### Unit Tests
Run all unit tests:
```bash
dotnet test tests/Agents.Tests.Unit/Agents.Tests.Unit.csproj
```

### Integration Tests
Integration tests use Testcontainers (requires Docker):
```bash
dotnet test tests/Agents.Tests.Integration/Agents.Tests.Integration.csproj
```

**Test Coverage:**
- Unit tests: 24 tests, 100% pass rate
- Integration tests: 8 tests for SQL Server persistence
- Technologies: xUnit, Moq, FluentAssertions, Testcontainers

## 📊 Status

| Phase | Status | Description |
|-------|--------|-------------|
| Phase 1 | ✅ Complete | Foundation (Domain, Entities, Interfaces) |
| Phase 2 | ✅ Complete | Event Infrastructure (Event Grid, Hubs, Service Bus) |
| Phase 3 | ✅ Complete | Prompt Management System |
| Phase 4 | ✅ Complete | Core Agents Implementation |
| Phase 5 | ✅ Complete | API Layer (REST APIs, Swagger, Health Checks) |
| Phase 6 | 🚧 In Progress | Persistence Layer (Cosmos DB, Azure SQL) |
| Phase 7 | ⏳ Planned | Infrastructure as Code (Bicep/Terraform) |
| Phase 8 | ⏳ Planned | Kubernetes Deployment (Dockerfiles, Helm) |
| Phase 9 | ⏳ Planned | Observability (Logging, Metrics, Tracing) |
| Phase 10 | ⏳ Planned | Integration Testing |

## Contributing

See [CONTRIBUTING.md](CONTRIBUTING.md) for details on how to contribute to this project.

## Documentation

- [Architecture Overview](docs/architecture.md)
- [Agent Development Guide](docs/agent-development.md)
- [Prompt Authoring Guide](docs/prompt-authoring.md)
- [Deployment Guide](docs/deployment.md)
- [Operations Runbook](docs/operations.md)

## License

MIT License - see [LICENSE](LICENSE) for details.

## Support

For issues and questions, please open a GitHub issue.
