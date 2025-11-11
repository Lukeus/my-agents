# AI Orchestration Multi-Agent Event-Driven Microservice Framework

## Problem Statement

Build an enterprise-grade, event-driven microservice framework using C# and Microsoft Semantic Kernel that enables multiple specialized AI agents to collaborate on complex workflows. The system must support local development with Ollama and production deployment on Azure Kubernetes Service (AKS) with Azure OpenAI, following clean architecture principles with all agent logic driven by prompt files.

## Current State

**Directory**: `C:\Users\lukeu\source\repos\my-agents`
**Status**: 💚 **Phases 1-10 Complete** - Production-ready framework with full observability

### Implementation Progress

**Completed** (✅ Phases 1-10):
- ✅ **Phase 1**: Foundation layer (36 projects, clean architecture)
- ✅ **Phase 2**: Event infrastructure (Event Grid, Event Hubs, Service Bus)
- ✅ **Phase 3**: Prompt management system (YAML parsing, GitHub integration)
- ✅ **Phase 4**: 5 core agents with 24 unit tests (100% passing)
- ✅ **Phase 5**: REST APIs with Swagger documentation
- ✅ **Phase 6**: Persistence layer (Cosmos DB + Azure SQL with EF Core 9.0)
- ✅ **Phase 7**: Persistence integration (7 integration tests, Testcontainers)
- ✅ **Phase 8**: Infrastructure as Code (8 Bicep modules, deployment automation)
- ✅ **Phase 9**: Kubernetes deployment (Docker, Helm, Kustomize, CI/CD pipelines)
- ✅ **Phase 10**: Monitoring & Observability (Prometheus, Grafana, OpenTelemetry, Event Hub/Service Bus)

**Metrics**:
- 40 .NET 9 projects
- 44 unit tests (100% passing)
- 7 integration tests (requires Docker)
- 5 production-ready REST APIs
- 8 Azure Bicep modules
- 5 Dockerfiles with multi-stage builds
- Helm chart with 9 templates
- Kustomize overlays (dev/staging/prod)
- 4 GitHub Actions workflows
- Prometheus + Grafana monitoring stack
- ~12,000+ lines of production code

## Architecture Overview

### Core Principles

1. **Clean Architecture**: Separation of concerns with clear boundaries between Domain, Application, Infrastructure, and Presentation layers
2. **Event-Driven**: Agents communicate asynchronously via Azure Event Grid/Event Hubs
3. **Prompt-Driven**: All agent behavior defined in versioned prompt files
4. **Environment Agnostic**: Ollama (dev) ↔ Azure OpenAI (prod) switchable via configuration
5. **Infrastructure as Code**: Complete Azure environment provisioning via Bicep/Terraform
6. **Cloud Native**: Designed for Kubernetes deployment on AKS

### Technology Stack

- **Framework**: .NET 9+ with C#
- **AI Orchestration**: Microsoft Semantic Kernel
- **LLM Providers**: 
  - Development: Ollama (local)
  - Production: Azure OpenAI
- **Event Infrastructure**:
  - Azure Event Grid (event routing/schema registry)
  - Azure Event Hubs (high-throughput streaming)
  - Azure Service Bus (reliable messaging)
- **Container Orchestration**: Azure Kubernetes Service (AKS)
- **IaC**: Bicep (primary) with Terraform option
- **Storage**: Azure Cosmos DB / Azure SQL
- **Monitoring**: Application Insights, Azure Monitor

## Agent Specifications

### 1. Notification Agent
**Purpose**: Handle all notification services (email, SMS, Teams, Slack)
**Triggers**: Notification request events
**Actions**: Send formatted notifications, track delivery status
**Prompts**: 
- `notification-formatter.prompt` - Format messages for different channels
- `notification-prioritizer.prompt` - Prioritize urgent vs standard notifications

### 2. DevOps Agent
**Purpose**: Manage GitHub Projects, automate workflows, track work items
**Triggers**: Project management events, CI/CD status changes
**Actions**: Create/update issues, manage project boards, analyze sprint progress
**Prompts**:
- `devops-project-manager.prompt` - Manage GitHub Projects
- `devops-workflow-optimizer.prompt` - Suggest workflow improvements
- `devops-sprint-analyzer.prompt` - Sprint metrics and insights

### 3. Test Planning Agent
**Purpose**: Generate test specifications and test-driven development plans
**Triggers**: Feature request events, code change events
**Actions**: Create test specifications, generate test cases, design test strategies
**Prompts**:
- `test-spec-generator.prompt` - Generate comprehensive test specs
- `test-strategy-planner.prompt` - Design testing strategies (unit, integration, e2e)
- `test-coverage-analyzer.prompt` - Analyze and recommend coverage improvements

### 4. Implementation Agent
**Purpose**: Generate code implementations based on specs and tests
**Triggers**: Test specification approved events
**Actions**: Generate code, implement features, refactor existing code
**Prompts**:
- `code-generator.prompt` - Generate implementation from specs
- `code-reviewer.prompt` - Review and improve generated code
- `refactoring-agent.prompt` - Suggest and implement refactorings

### 5. Prompt Registry Agent
**Purpose**: Manage centralized enterprise prompt repository
**Triggers**: Prompt update events, version requests
**Actions**: Version prompts, distribute updates, track prompt performance
**Prompts**:
- `prompt-versioner.prompt` - Manage prompt versions
- `prompt-optimizer.prompt` - Analyze and improve prompt effectiveness
- `prompt-distributor.prompt` - Distribute prompts to agents

### 6. Service Desk Agent
**Purpose**: Handle ticketed service desk operations
**Triggers**: Ticket creation/update events, user queries
**Actions**: Triage tickets, suggest solutions, escalate issues, track SLAs
**Prompts**:
- `ticket-triager.prompt` - Classify and prioritize tickets
- `solution-suggester.prompt` - Recommend solutions from knowledge base
- `escalation-manager.prompt` - Determine when to escalate

## System Architecture

### Layer Structure (Clean Architecture)

```
src/
├── Domain/                          # Enterprise business rules
│   ├── Agents.Domain.Core/         # Shared domain models
│   │   ├── Entities/
│   │   ├── ValueObjects/
│   │   ├── Events/
│   │   └── Interfaces/
│   ├── Agents.Domain.Notification/
│   ├── Agents.Domain.DevOps/
│   ├── Agents.Domain.TestPlanning/
│   ├── Agents.Domain.Implementation/
│   ├── Agents.Domain.PromptRegistry/
│   └── Agents.Domain.ServiceDesk/
│
├── Application/                     # Application business rules
│   ├── Agents.Application.Core/    # Shared application layer
│   │   ├── Interfaces/
│   │   ├── DTOs/
│   │   ├── Behaviors/              # MediatR pipeline behaviors
│   │   └── Exceptions/
│   ├── Agents.Application.Notification/
│   │   ├── Commands/
│   │   ├── Queries/
│   │   ├── EventHandlers/
│   │   └── Validators/
│   ├── Agents.Application.DevOps/
│   ├── Agents.Application.TestPlanning/
│   ├── Agents.Application.Implementation/
│   ├── Agents.Application.PromptRegistry/
│   └── Agents.Application.ServiceDesk/
│
├── Infrastructure/                  # External concerns
│   ├── Agents.Infrastructure.SemanticKernel/
│   │   ├── Configuration/
│   │   ├── Plugins/
│   │   ├── Planners/
│   │   └── Memory/
│   ├── Agents.Infrastructure.AzureOpenAI/
│   │   └── Services/
│   ├── Agents.Infrastructure.Ollama/
│   │   └── Services/
│   ├── Agents.Infrastructure.EventGrid/
│   │   ├── Publishers/
│   │   └── Subscribers/
│   ├── Agents.Infrastructure.EventHub/
│   ├── Agents.Infrastructure.ServiceBus/
│   ├── Agents.Infrastructure.Persistence/
│   │   ├── CosmosDb/
│   │   └── SqlServer/
│   └── Agents.Infrastructure.Monitoring/
│
├── Presentation/                    # API/UI Layer
│   ├── Agents.API.Gateway/         # API Gateway
│   ├── Agents.API.Notification/    # Per-agent APIs
│   ├── Agents.API.DevOps/
│   ├── Agents.API.TestPlanning/
│   ├── Agents.API.Implementation/
│   ├── Agents.API.PromptRegistry/
│   └── Agents.API.ServiceDesk/
│
└── Shared/                          # Cross-cutting concerns
    ├── Agents.Shared.Common/
    ├── Agents.Shared.Logging/
    ├── Agents.Shared.Security/
    └── Agents.Shared.Telemetry/

prompts/                             # Prompt repository
├── notification/
│   ├── notification-formatter.prompt
│   └── notification-prioritizer.prompt
├── devops/
│   ├── devops-project-manager.prompt
│   ├── devops-workflow-optimizer.prompt
│   └── devops-sprint-analyzer.prompt
├── test-planning/
│   ├── test-spec-generator.prompt
│   ├── test-strategy-planner.prompt
│   └── test-coverage-analyzer.prompt
├── implementation/
│   ├── code-generator.prompt
│   ├── code-reviewer.prompt
│   └── refactoring-agent.prompt
├── prompt-registry/
│   ├── prompt-versioner.prompt
│   ├── prompt-optimizer.prompt
│   └── prompt-distributor.prompt
├── service-desk/
│   ├── ticket-triager.prompt
│   ├── solution-suggester.prompt
│   └── escalation-manager.prompt
└── shared/
    ├── common-instructions.prompt
    └── safety-guidelines.prompt

infrastructure/                      # IaC
├── bicep/
│   ├── main.bicep
│   ├── modules/
│   │   ├── aks.bicep
│   │   ├── event-grid.bicep
│   │   ├── event-hub.bicep
│   │   ├── service-bus.bicep
│   │   ├── openai.bicep
│   │   ├── cosmos-db.bicep
│   │   ├── sql-server.bicep
│   │   ├── key-vault.bicep
│   │   └── monitoring.bicep
│   └── parameters/
│       ├── dev.parameters.json
│       ├── staging.parameters.json
│       └── prod.parameters.json
└── terraform/                       # Alternative IaC option
    ├── main.tf
    ├── variables.tf
    └── modules/

tests/
├── unit/
├── integration/
└── e2e/

k8s/                                 # Kubernetes manifests
├── base/
│   ├── namespace.yaml
│   ├── configmap.yaml
│   └── secrets.yaml
├── agents/
│   ├── notification-agent/
│   ├── devops-agent/
│   ├── test-planning-agent/
│   ├── implementation-agent/
│   ├── prompt-registry-agent/
│   └── service-desk-agent/
└── overlays/
    ├── dev/
    ├── staging/
    └── production/
```

### Event Schema Design

#### Base Event Structure
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

#### Event Types by Agent

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

**Test Planning Agent**
- `agents.testplanning.spec.generated`
- `agents.testplanning.strategy.created`
- `agents.testplanning.coverage.analyzed`

**Implementation Agent**
- `agents.implementation.code.generated`
- `agents.implementation.review.completed`
- `agents.implementation.refactoring.suggested`

**Prompt Registry Agent**
- `agents.promptregistry.version.published`
- `agents.promptregistry.prompt.updated`
- `agents.promptregistry.performance.analyzed`

**Service Desk Agent**
- `agents.servicedesk.ticket.created`
- `agents.servicedesk.ticket.triaged`
- `agents.servicedesk.solution.suggested`
- `agents.servicedesk.ticket.escalated`

## Implementation Plan

### Phase 1: Foundation (Weeks 1-2)

#### 1.1 Project Initialization
- [ ] Create solution structure with all projects
- [ ] Set up Git repository with appropriate .gitignore
- [ ] Configure NuGet package references
- [ ] Set up CI/CD pipeline skeleton (GitHub Actions)
- [ ] Create README.md and CONTRIBUTING.md

#### 1.2 Core Domain Layer
- [ ] Define base entity and value object abstractions
- [ ] Create domain events base classes
- [ ] Define shared interfaces (IEventPublisher, IEventHandler, etc.)
- [ ] Implement domain models for each agent

#### 1.3 Semantic Kernel Integration
- [ ] Configure Semantic Kernel with dependency injection
- [ ] Implement provider abstraction (ILLMProvider)
- [ ] Create OllamaProvider implementation
- [ ] Create AzureOpenAIProvider implementation
- [ ] Implement configuration-based provider switching
- [ ] Create base agent class with SK integration

### Phase 2: Event Infrastructure (Weeks 3-4)

#### 2.1 Event Grid Setup
- [ ] Design event schema registry
- [ ] Implement EventGridPublisher service
- [ ] Implement EventGridSubscriber service
- [ ] Create event serialization/deserialization utilities
- [ ] Add dead letter handling

#### 2.2 Event Hubs Integration
- [ ] Implement EventHubProducer for high-throughput scenarios
- [ ] Implement EventHubConsumer with checkpointing
- [ ] Add event batching support

#### 2.3 Service Bus Integration
- [ ] Implement ServiceBusPublisher for reliable messaging
- [ ] Implement ServiceBusSubscriber with retry policies
- [ ] Configure message sessions for ordering guarantees

### Phase 3: Prompt Management System (Weeks 5-6)

#### 3.1 Prompt Repository
- [ ] Design prompt file format and metadata structure
- [ ] Create PromptLoader service to load from filesystem
- [ ] Implement prompt versioning system
- [ ] Create prompt validation framework
- [ ] Build PromptCache for performance

#### 3.2 Prompt Registry Agent
- [ ] Implement PromptRegistryAgent
- [ ] Create prompt CRUD operations
- [ ] Build version control for prompts
- [ ] Add prompt performance tracking
- [ ] Implement prompt distribution mechanism

#### 3.3 GitHub Integration
- [ ] Set up GitHub repository for prompt storage
- [ ] Implement GitHubPromptProvider
- [ ] Add webhook handler for prompt updates
- [ ] Create prompt sync mechanism

### Phase 4: Core Agents Implementation (Weeks 7-10)

#### 4.1 Notification Agent
- [ ] Implement NotificationAgent with SK
- [ ] Create notification formatters (email, SMS, Teams, Slack)
- [ ] Build delivery tracking system
- [ ] Add retry logic for failed notifications
- [ ] Write unit tests

#### 4.2 DevOps Agent
- [ ] Implement DevOpsAgent with SK
- [ ] Integrate with GitHub API (Projects, Issues, Actions)
- [ ] Create workflow automation logic
- [ ] Build sprint analytics features
- [ ] Write unit tests

#### 4.3 Test Planning Agent
- [ ] Implement TestPlanningAgent with SK
- [ ] Create test specification generator
- [ ] Build test strategy planner
- [ ] Add coverage analysis features
- [ ] Write unit tests

#### 4.4 Implementation Agent
- [ ] Implement ImplementationAgent with SK
- [ ] Create code generation pipeline
- [ ] Build code review analyzer
- [ ] Add refactoring suggestions engine
- [ ] Write unit tests

#### 4.5 Service Desk Agent
- [ ] Implement ServiceDeskAgent with SK
- [ ] Create ticket triage system
- [ ] Build solution suggestion engine
- [ ] Add escalation logic
- [ ] Implement SLA tracking
- [ ] Write unit tests

### Phase 5: API Layer (Weeks 11-12)

#### 5.1 API Gateway
- [ ] Set up Ocelot or YARP for API Gateway
- [ ] Configure routing to agent microservices
- [ ] Add authentication/authorization (Azure AD)
- [ ] Implement rate limiting
- [ ] Add API versioning

#### 5.2 Agent APIs
- [ ] Create REST APIs for each agent
- [ ] Implement health checks
- [ ] Add OpenAPI/Swagger documentation
- [ ] Configure CORS policies
- [ ] Add request validation

### Phase 6: Persistence Layer (Weeks 13-14)

#### 6.1 Cosmos DB Integration
- [ ] Configure Cosmos DB containers
- [ ] Implement repositories for event sourcing
- [ ] Add change feed processing
- [ ] Create indexing strategy

#### 6.2 Azure SQL Integration
- [ ] Design relational schema
- [ ] Implement EF Core DbContext
- [ ] Create repositories for structured data
- [ ] Add migration scripts

### Phase 7: Infrastructure as Code (Weeks 15-16)

#### 7.1 Bicep Development
- [ ] Create main.bicep orchestrator
- [ ] Implement AKS cluster module
- [ ] Create Event Grid namespace and topics
- [ ] Configure Event Hubs namespace
- [ ] Set up Service Bus namespace
- [ ] Deploy Azure OpenAI service
- [ ] Configure Cosmos DB
- [ ] Set up Azure SQL
- [ ] Create Key Vault for secrets
- [ ] Configure Application Insights
- [ ] Add Azure Monitor dashboards

#### 7.2 Deployment Pipeline
- [ ] Create GitHub Actions workflows for IaC
- [ ] Implement environment-specific parameters
- [ ] Add infrastructure testing
- [ ] Create rollback procedures

### Phase 8: Kubernetes Deployment (Weeks 17-18)

#### 8.1 Containerization
- [ ] Create Dockerfiles for each agent
- [ ] Set up Azure Container Registry (ACR)
- [ ] Configure multi-stage builds for optimization
- [ ] Implement security scanning

#### 8.2 Kubernetes Manifests
- [ ] Create Deployments for each agent
- [ ] Configure Services (ClusterIP, LoadBalancer)
- [ ] Set up ConfigMaps and Secrets
- [ ] Implement HorizontalPodAutoscaler
- [ ] Add PodDisruptionBudgets
- [ ] Configure Ingress controllers
- [ ] Set up Kustomize overlays

#### 8.3 Helm Charts (Optional)
- [ ] Create Helm charts for easier deployment
- [ ] Define values.yaml for configuration
- [ ] Add templates for all resources

### Phase 9: Observability & Monitoring (Weeks 19-20)

#### 9.1 Logging
- [ ] Configure structured logging (Serilog)
- [ ] Send logs to Application Insights
- [ ] Implement correlation IDs across services
- [ ] Add log aggregation dashboards

#### 9.2 Metrics
- [ ] Implement Prometheus metrics
- [ ] Create Grafana dashboards
- [ ] Track agent performance metrics
- [ ] Monitor event processing latency

#### 9.3 Tracing
- [ ] Implement distributed tracing (OpenTelemetry)
- [ ] Configure Application Insights integration
- [ ] Add service map visualization

#### 9.4 Alerting
- [ ] Set up Azure Monitor alerts
- [ ] Configure PagerDuty/Teams integration
- [ ] Create runbooks for common issues

### Phase 10: Testing & Quality Assurance (Weeks 21-22)

#### 10.1 Unit Testing
- [ ] Achieve >80% code coverage for all agents
- [ ] Test prompt loading and validation
- [ ] Test event publishing/subscribing
- [ ] Mock external dependencies

#### 10.2 Integration Testing
- [ ] Test agent-to-agent communication
- [ ] Verify event flow end-to-end
- [ ] Test database operations
- [ ] Validate prompt registry operations

#### 10.3 End-to-End Testing
- [ ] Create realistic user scenarios
- [ ] Test complete workflows (e.g., ticket → triage → solution)
- [ ] Verify Ollama ↔ Azure OpenAI switching
- [ ] Performance and load testing

#### 10.4 Chaos Engineering
- [ ] Implement chaos experiments (Azure Chaos Studio)
- [ ] Test failure scenarios (network issues, service crashes)
- [ ] Verify circuit breakers and retry policies

### Phase 11: Security & Compliance (Week 23)

#### 11.1 Security Implementation
- [ ] Implement Azure AD authentication
- [ ] Configure RBAC for AKS
- [ ] Set up managed identities
- [ ] Enable Key Vault integration
- [ ] Implement network policies
- [ ] Configure Azure Policy compliance

#### 11.2 Secret Management
- [ ] Store all secrets in Key Vault
- [ ] Rotate secrets automatically
- [ ] Audit secret access

#### 11.3 Security Scanning
- [ ] Add container vulnerability scanning
- [ ] Implement code security analysis (SonarQube)
- [ ] Configure dependency scanning

### Phase 12: Documentation & Launch (Week 24)

#### 12.1 Documentation
- [ ] Complete architecture documentation
- [ ] Write deployment guides
- [ ] Create operations runbooks
- [ ] Document API endpoints
- [ ] Add prompt authoring guidelines
- [ ] Create troubleshooting guides

#### 12.2 Production Readiness
- [ ] Conduct production readiness review
- [ ] Perform disaster recovery drills
- [ ] Validate backup/restore procedures
- [ ] Complete security audit

#### 12.3 Launch
- [ ] Deploy to staging environment
- [ ] Conduct user acceptance testing
- [ ] Deploy to production (gradual rollout)
- [ ] Monitor system health
- [ ] Collect feedback and iterate

## Key Design Decisions

### 1. LLM Provider Abstraction
```csharp
public interface ILLMProvider
{
    Task<string> CompleteAsync(string prompt, CancellationToken ct);
    Task<TResult> CompleteAsync<TResult>(string prompt, CancellationToken ct);
    IAsyncEnumerable<string> StreamCompleteAsync(string prompt, CancellationToken ct);
}
```

**Configuration (appsettings.json)**:
```json
{
  "LLMProvider": {
    "Type": "Ollama", // or "AzureOpenAI"
    "Ollama": {
      "Endpoint": "http://localhost:11434",
      "Model": "llama3.1"
    },
    "AzureOpenAI": {
      "Endpoint": "https://{resource}.openai.azure.com/",
      "DeploymentName": "gpt-4",
      "ApiVersion": "2024-02-01"
    }
  }
}
```

### 2. Prompt File Format
```yaml
# notification-formatter.prompt
---
name: notification-formatter
version: 1.0.0
description: Formats notification messages for various channels
model_requirements:
  min_tokens: 4096
  temperature: 0.7
input_schema:
  - name: message
    type: string
    required: true
  - name: channel
    type: enum
    values: [email, sms, teams, slack]
    required: true
  - name: priority
    type: enum
    values: [low, normal, high, urgent]
    required: false
output_schema:
  type: object
  properties:
    formatted_message: string
    subject: string
    metadata: object
---
You are a notification formatting assistant. Format the provided message for the specified channel.

# Channel-specific guidelines:
- Email: Include subject line, use professional tone, support HTML
- SMS: Keep under 160 characters, be concise
- Teams: Use markdown, support adaptive cards
- Slack: Use Block Kit formatting, support threads

# Priority handling:
- Urgent: Use attention-grabbing language
- High: Clear and direct
- Normal: Professional and informative
- Low: Brief and casual

Message: {{message}}
Channel: {{channel}}
Priority: {{priority}}

Format the message appropriately and return structured JSON.
```

### 3. Event-Driven Communication Pattern

**Publisher (any agent)**:
```csharp
public class NotificationAgent
{
    private readonly IEventPublisher _eventPublisher;
    
    public async Task SendNotificationAsync(NotificationRequest request)
    {
        // Process notification
        var result = await ProcessAsync(request);
        
        // Publish event
        var @event = new NotificationSentEvent
        {
            NotificationId = result.Id,
            Channel = request.Channel,
            Status = result.Status
        };
        
        await _eventPublisher.PublishAsync(@event);
    }
}
```

**Subscriber (another agent)**:
```csharp
public class ServiceDeskAgent : IEventHandler<NotificationSentEvent>
{
    public async Task HandleAsync(NotificationSentEvent @event)
    {
        // React to notification sent
        await UpdateTicketStatusAsync(@event.NotificationId);
    }
}
```

### 4. Clean Architecture Dependency Flow
```
Presentation Layer → Application Layer → Domain Layer
            ↓                ↓
    Infrastructure Layer (implements interfaces from Application/Domain)
```

**Key Rule**: Dependencies always point inward. Infrastructure depends on Domain/Application, never the reverse.

## Configuration Management

### Environment-Specific Settings

**Development (Ollama)**:
```json
{
  "LLMProvider": { "Type": "Ollama" },
  "EventGrid": { "UseEmulator": true },
  "CosmosDb": { "UseEmulator": true }
}
```

**Production (Azure)**:
```json
{
  "LLMProvider": { "Type": "AzureOpenAI" },
  "EventGrid": { "Endpoint": "https://{namespace}.eventgrid.azure.net/" },
  "CosmosDb": { "Endpoint": "https://{account}.documents.azure.com:443/" }
}
```

## Testing Strategy

### Unit Tests
- Test each agent in isolation with mocked dependencies
- Test prompt loading and validation logic
- Validate event serialization/deserialization
- Use xUnit + FluentAssertions + Moq

### Integration Tests
- Use Testcontainers for dependencies (Cosmos DB, Ollama)
- Test event flow between agents
- Verify database operations
- Use WebApplicationFactory for API testing

### E2E Tests
- Deploy to test AKS cluster
- Run realistic scenarios
- Validate monitoring and alerting
- Performance benchmarking

## Monitoring & Metrics

### Key Metrics to Track
- Agent response time (p50, p95, p99)
- Event processing latency
- Prompt execution success rate
- LLM token usage and costs
- API request/response times
- Error rates by agent
- Queue depth for event processing

### Dashboards
1. **System Health**: CPU, memory, pod status
2. **Agent Performance**: Response times, success rates
3. **Event Flow**: Throughput, latency, dead letters
4. **Cost Tracking**: LLM usage, Azure resource costs
5. **Business Metrics**: Tickets resolved, notifications sent, tests generated

## Security Considerations

1. **Authentication**: Azure AD for user authentication
2. **Authorization**: RBAC for agent access control
3. **Secrets**: All secrets in Azure Key Vault, accessed via managed identities
4. **Network**: Private endpoints for Azure services, network policies in AKS
5. **Data**: Encryption at rest and in transit
6. **Compliance**: Audit logging for all operations
7. **API Security**: Rate limiting, input validation, CORS policies

## Cost Optimization

1. **LLM Costs**: Use Ollama for dev/test, cache frequent prompts
2. **Compute**: Right-size AKS nodes, use autoscaling
3. **Storage**: Use appropriate tier for Cosmos DB, implement data retention policies
4. **Networking**: Use private endpoints to avoid egress charges
5. **Monitoring**: Set appropriate log retention periods

## Risks & Mitigations

| Risk | Impact | Mitigation |
|------|--------|------------|
| LLM response inconsistency | High | Implement validation, use temperature settings, version prompts |
| Event delivery failures | High | Implement retry policies, dead letter queues, idempotency |
| Cost overruns | Medium | Set budget alerts, implement usage quotas, cache responses |
| Prompt injection attacks | High | Validate inputs, use system messages, implement content filtering |
| Scaling challenges | Medium | Design for horizontal scaling, use event-driven architecture |
| Complexity overhead | Medium | Follow clean architecture, comprehensive documentation |

## Success Criteria

- [ ] All 6 agents operational and processing events
- [ ] Successfully switch between Ollama and Azure OpenAI via configuration
- [ ] Event flow functioning across all agents
- [ ] Complete IaC deployment to Azure AKS
- [ ] >80% code coverage with passing tests
- [ ] Monitoring and alerting functional
- [ ] Sub-second p95 latency for agent responses
- [ ] Zero secrets in code or configuration files
- [ ] Complete documentation for operations and development

## Next Steps

1. **Review and Approve**: Validate architecture decisions and implementation plan
2. **Initialize Repository**: Set up Git repo with branching strategy
3. **Create Initial Projects**: Bootstrap solution with all projects
4. **Begin Phase 1**: Start with foundation layer implementation
5. **Iterative Development**: Complete phases sequentially with regular reviews

## References

- [Microsoft Semantic Kernel Documentation](https://learn.microsoft.com/en-us/semantic-kernel/)
- [Azure Event Grid](https://learn.microsoft.com/en-us/azure/event-grid/)
- [Clean Architecture by Robert C. Martin](https://blog.cleancoder.com/uncle-bob/2012/08/13/the-clean-architecture.html)
- [Azure Architecture Center - Microservices](https://learn.microsoft.com/en-us/azure/architecture/guide/architecture-styles/microservices)
- [Ollama Documentation](https://ollama.ai/docs)
- [GitHub Copilot Prompts](https://github.com/github/awesome-copilot)

---

**Document Version**: 1.0
**Last Updated**: 2025-11-11
**Status**: Draft - Awaiting Approval