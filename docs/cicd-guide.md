# CI/CD Pipeline Guide

## Overview

This document describes the CI/CD pipelines for the AI Orchestration Multi-Agent Framework with .NET Aspire and Dapr integration.

## GitHub Actions Workflows

### 1. CI - Build and Test (`ci.yml`)

**Triggers:**
- Push to `main` or `develop` branches
- Pull requests to `main` or `develop`

**Jobs:**

#### build-and-test
- **Setup**: .NET 9.0, Aspire workload, Dapr CLI
- **Dependencies**: Restores NuGet packages
- **Build**: Compiles solution in Release mode
- **Unit Tests**: Runs tests with `Category=Unit` filter
- **Integration Tests**: Runs tests with `Category=Integration` filter
  - Starts Redis and SQL Server containers
  - Sets connection strings via environment variables
- **Artifacts**: Uploads test results and code coverage

#### analyze
- **Code Analysis**: Builds with warnings as errors
- **Formatting**: Verifies code formatting with `dotnet format`

**Key Changes for Dapr/Aspire:**
```yaml
- name: Install .NET Aspire workload
  run: dotnet workload install aspire

- name: Setup Dapr CLI
  uses: dapr/setup-dapr@v1
  with:
    version: '1.14.0'

- name: Initialize Dapr
  run: dapr init
```

### 2. Docker Build and Push (`docker-build.yml`)

**Triggers:**
- Push to `main` or `develop`
- Tags matching `v*`
- Pull requests
- Manual workflow dispatch

**Services Built:**
- `agents-notification-api`
- `agents-devops-api`
- `agents-testplanning-api`
- `agents-implementation-api`
- `agents-servicedesk-api`

**Steps:**
1. Builds Docker images for all API services
2. Pushes to Azure Container Registry (ACR)
3. Runs Trivy security scans
4. Uploads security results to GitHub Security tab

**Docker Image Tags:**
- Branch name (e.g., `main`, `develop`)
- PR number (e.g., `pr-123`)
- Git SHA (e.g., `sha-abc123`)
- Semantic version (e.g., `v1.2.3`, `1.2`)
- `latest` (for default branch only)

**Dapr Considerations:**
- Dockerfile should include Dapr SDK packages
- No need to include daprd binary (managed by Kubernetes)
- Images are compatible with Dapr sidecar injection

### 3. Deploy to AKS (`deploy-aks.yml`)

**Trigger:** Manual workflow dispatch

**Inputs:**
- `environment`: Environment to deploy to (dev/staging/prod)
- `image_tag`: Docker image tag to deploy

**Prerequisites:**
1. AKS cluster with Dapr enabled
2. Azure Container Registry configured
3. GitHub secrets configured (see below)

**Deployment Steps:**

1. **Setup**
   - Azure login
   - Set AKS context
   - Setup kubectl, Helm, Kustomize
   - Install Dapr CLI

2. **Initialize Dapr on AKS**
   ```bash
   dapr init --kubernetes --wait
   dapr status -k
   ```

3. **Create Namespace**
   ```bash
   kubectl create namespace agents-<environment>
   ```

4. **Deploy Dapr Components**
   - **Pub/Sub**: Azure Service Bus Topics
   - **State Store**: Azure Cosmos DB

5. **Deploy Secrets**
   - Database connection strings
   - LLM provider credentials
   - Azure service connection strings
   - Application Insights connection string

6. **Deploy with Helm**
   - Updates image tags
   - Applies environment-specific values
   - Waits for deployment to complete (10m timeout)

7. **Verify Deployment**
   - Checks rollout status
   - Lists all pods
   - Runs smoke tests on health endpoints

8. **Notify**
   - Sends Slack notification with deployment result

## Required GitHub Secrets

### Azure Resources
- `AZURE_SUBSCRIPTION_ID`: Azure subscription ID
- `AZURE_RESOURCE_GROUP`: Resource group name
- `AZURE_CREDENTIALS`: Service principal credentials (JSON)
- `AKS_CLUSTER_NAME`: AKS cluster name

### Container Registry
- `ACR_NAME`: Azure Container Registry name
- `ACR_LOGIN_SERVER`: ACR login server URL
- `ACR_USERNAME`: ACR username (usually same as ACR_NAME)
- `ACR_PASSWORD`: ACR password/token

### Database
- `SQL_CONNECTION_STRING`: Azure SQL Server connection string
- `COSMOS_CONNECTION_STRING`: Cosmos DB connection string
- `COSMOS_ENDPOINT`: Cosmos DB endpoint URL

### LLM Provider
- `AZURE_OPENAI_ENDPOINT`: Azure OpenAI endpoint
- `AZURE_OPENAI_API_KEY`: Azure OpenAI API key
- `AZURE_OPENAI_DEPLOYMENT`: Deployment/model name

### Messaging
- `EVENT_HUB_CONNECTION_STRING`: Azure Event Hub connection string
- `SERVICE_BUS_CONNECTION_STRING`: Azure Service Bus connection string

### Observability
- `APP_INSIGHTS_CONNECTION_STRING`: Application Insights connection string

### Notifications
- `SLACK_WEBHOOK`: Slack webhook URL for deployment notifications

## Dapr Component Configuration

### Pub/Sub (Azure Service Bus)

```yaml
apiVersion: dapr.io/v1alpha1
kind: Component
metadata:
  name: agents-pubsub
  namespace: agents-<environment>
spec:
  type: pubsub.azure.servicebus.topics
  version: v1
  metadata:
  - name: connectionString
    secretKeyRef:
      name: agents-secrets
      key: ServiceBus__ConnectionString
```

### State Store (Azure Cosmos DB)

```yaml
apiVersion: dapr.io/v1alpha1
kind: Component
metadata:
  name: agents-statestore
  namespace: agents-<environment>
spec:
  type: state.azure.cosmosdb
  version: v1
  metadata:
  - name: url
    value: "<cosmos-endpoint>"
  - name: masterKey
    secretKeyRef:
      name: agents-secrets
      key: ConnectionStrings__CosmosDb
  - name: database
    value: "agents-db"
  - name: collection
    value: "agents-state"
```

## Kubernetes Deployment with Dapr

### Dapr Sidecar Annotations

Add these annotations to your Kubernetes deployment manifests:

```yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: notification-api
spec:
  template:
    metadata:
      annotations:
        dapr.io/enabled: "true"
        dapr.io/app-id: "notification-api"
        dapr.io/app-port: "8080"
        dapr.io/enable-api-logging: "true"
    spec:
      containers:
      - name: notification-api
        image: <acr-server>/agents-notification-api:<tag>
        env:
        - name: Dapr__Enabled
          value: "true"
```

**Key Annotations:**
- `dapr.io/enabled`: Enable Dapr sidecar injection
- `dapr.io/app-id`: Unique application ID for Dapr
- `dapr.io/app-port`: Port the API listens on
- `dapr.io/enable-api-logging`: Enable Dapr API logging

## Local Development vs. Production

### Local Development (Aspire)
- **Orchestration**: .NET Aspire AppHost
- **Dapr**: Sidecars managed by Aspire
- **Components**: Redis for both pub/sub and state store
- **Start Command**: `dotnet run --project src/AppHost/Agents.AppHost/Agents.AppHost.csproj`
- **Dashboard**: `https://localhost:17120`

### Production (AKS)
- **Orchestration**: Kubernetes + Dapr
- **Dapr**: Sidecars injected by Dapr control plane
- **Components**: Azure Service Bus (pub/sub), Cosmos DB (state store)
- **Deployment**: Helm charts via GitHub Actions
- **Monitoring**: Azure Monitor + Application Insights

## Environment-Specific Configuration

### Development
```yaml
Dapr__Enabled: false  # Use mock services
LLMProvider__ProviderType: Ollama
ConnectionStrings__SqlServer: <local-sql-server>
```

### Staging/Production
```yaml
Dapr__Enabled: true  # Use Dapr services
LLMProvider__ProviderType: AzureOpenAI
LLMProvider__AzureOpenAI__Endpoint: <azure-openai-endpoint>
ConnectionStrings__SqlServer: <azure-sql-server>
```

## Monitoring and Observability

### Aspire Dashboard (Local)
- **URL**: https://localhost:17120
- **Features**:
  - Service health monitoring
  - Structured logs
  - Distributed tracing
  - Metrics and performance graphs
  - Real-time resource view

### Application Insights (Production)
- **Metrics**:
  - Request rates and response times
  - Dependency calls (SQL, Dapr)
  - Exception tracking
  - Custom events
- **Logs**:
  - Structured application logs
  - Dapr sidecar logs
  - Kubernetes pod logs
- **Distributed Tracing**:
  - End-to-end request tracking
  - Service-to-service calls via Dapr
  - Performance bottleneck identification

## Troubleshooting CI/CD

### Common Issues

#### 1. Dapr initialization fails in CI
**Symptom**: `dapr init` times out or fails
**Solution**:
```yaml
- name: Initialize Dapr with retry
  run: |
    for i in {1..3}; do
      dapr init && break || sleep 10
    done
```

#### 2. Integration tests fail with "cannot connect to database"
**Symptom**: SQL Server container not ready
**Solution**: Add wait logic
```yaml
- name: Wait for SQL Server
  run: |
    until docker exec $(docker ps -qf "ancestor=mcr.microsoft.com/mssql/server:2022-latest") /opt/mssql-tools/bin/sqlcmd -S localhost -U sa -P 'YourStrong@Passw0rd' -Q "SELECT 1"; do
      echo "Waiting for SQL Server..."
      sleep 5
    done
```

#### 3. Kubernetes deployment fails with "ImagePullBackOff"
**Symptom**: Cannot pull Docker image from ACR
**Solution**: Verify ACR credentials
```bash
kubectl get secret acr-secret -n agents-<env> -o yaml
az acr check-health --name <acr-name>
```

#### 4. Dapr sidecar not injecting
**Symptom**: Pod only has 1 container instead of 2
**Solution**: Check annotations
```bash
kubectl describe pod <pod-name> -n agents-<env>
# Verify dapr.io/enabled annotation is present
```

## Security Scan Integration

### Trivy Scans
- Runs on every Docker image build
- Scans for HIGH and CRITICAL vulnerabilities
- Uploads results to GitHub Security tab
- Fails build if vulnerabilities exceed threshold

### Code Analysis
- Runs on every PR
- Enforces code formatting
- Treats warnings as errors in Release build

## Deployment Best Practices

1. **Always deploy to dev first**: Test changes in dev environment
2. **Use semantic versioning**: Tag releases with `v1.2.3` format
3. **Run smoke tests**: Verify health endpoints after deployment
4. **Monitor dashboards**: Check Application Insights after deployment
5. **Rollback plan**: Keep previous image tags for quick rollback
6. **Secret rotation**: Regularly update Azure secrets in GitHub
7. **Review security scans**: Address Trivy findings before production

## Next Steps

1. **Setup Pre-Production**: Add UAT/pre-prod environment
2. **Implement Blue-Green Deployments**: Zero-downtime releases
3. **Add Performance Tests**: Load testing in CI pipeline
4. **Setup GitOps**: Use Flux or ArgoCD for continuous deployment
5. **Implement Canary Releases**: Gradual rollout with monitoring

## References

- [.NET Aspire Documentation](https://learn.microsoft.com/en-us/dotnet/aspire/)
- [Dapr Documentation](https://docs.dapr.io/)
- [GitHub Actions Documentation](https://docs.github.com/en/actions)
- [Azure Kubernetes Service (AKS)](https://learn.microsoft.com/en-us/azure/aks/)
- [Helm Charts](https://helm.sh/docs/)
