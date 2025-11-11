# AI Orchestration Multi-Agent Framework

Enterprise-grade event-driven microservice framework using C# and Microsoft Semantic Kernel for AI agent orchestration.

## Overview

This framework enables multiple specialized AI agents to collaborate on complex workflows through event-driven communication. All agent behavior is driven by versioned prompt files, supporting both local development (Ollama) and production deployment (Azure OpenAI) on Azure Kubernetes Service.

## Architecture

- **Clean Architecture**: Domain, Application, Infrastructure, Presentation layers
- **Event-Driven**: Azure Event Grid, Event Hubs, Service Bus
- **Prompt-Driven**: All agent logic defined in versioned prompt files
- **Cloud Native**: Designed for AKS deployment with full IaC support

## Agents

1. **Notification Agent**: Email, SMS, Teams, Slack notifications
2. **DevOps Agent**: GitHub Projects, workflow automation, sprint analytics
3. **Test Planning Agent**: Test specification and strategy generation
4. **Implementation Agent**: Code generation from specs and tests
5. **Prompt Registry Agent**: Centralized prompt management and versioning
6. **Service Desk Agent**: Ticket triage, solution suggestions, SLA tracking

## Technology Stack

- .NET 9+
- Microsoft Semantic Kernel
- Azure OpenAI / Ollama
- Azure Event Grid, Event Hubs, Service Bus
- Azure Kubernetes Service (AKS)
- Cosmos DB / Azure SQL
- Bicep (IaC)

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

## Contributing

See [CONTRIBUTING.md](CONTRIBUTING.md) for guidelines.

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
