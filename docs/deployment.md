# Deployment Guide

This guide covers deploying the AI Orchestration Multi-Agent Framework in various environments: local development, Azure, and Kubernetes.

## Table of Contents

- [Overview](#overview)
- [Prerequisites](#prerequisites)
- [Local Development Deployment](#local-development-deployment)
- [Azure Infrastructure Deployment](#azure-infrastructure-deployment)
- [Kubernetes Deployment](#kubernetes-deployment)
- [Configuration Management](#configuration-management)
- [Deployment Validation](#deployment-validation)
- [Rollback Procedures](#rollback-procedures)
- [Troubleshooting](#troubleshooting)

## Overview

The framework supports three deployment targets:

1. **Local Development**: Docker Compose with Ollama for testing
2. **Azure Services**: Managed Azure services for production
3. **Kubernetes**: AKS for containerized microservices

## Prerequisites

### Required Tools

**For All Deployments:**
- **.NET 9 SDK**: [Download](https://dotnet.microsoft.com/download)
- **Git**: Version control
- **PowerShell 7+**: Deployment scripts

**For Local Development:**
- **Docker Desktop**: Container runtime
- **Ollama**: Local LLM - [Download](https://ollama.ai)

**For Azure Deployment:**
- **Azure CLI**: `az` command-line tool
- **Azure Subscription**: With appropriate permissions
- **Azure PowerShell Module** (optional): For scripting

**For Kubernetes Deployment:**
- **kubectl**: Kubernetes command-line tool
- **Helm** (optional): Package manager for Kubernetes
- **Docker**: For building container images

### Azure Permissions Required

- **Contributor** role on the subscription or resource group
- **User Access Administrator** for managed identities
- **Key Vault Administrator** for secrets management

## Local Development Deployment

### 🚀 Quick Start with .NET Aspire (Recommended)

**New!** The fastest way to run the entire system locally:

#### Step 1: Clone Repository

```powershell
git clone https://github.com/your-org/my-agents.git
cd my-agents
```

#### Step 2: Ensure Docker is Running

```powershell
# Verify Docker is running
docker ps
```

#### Step 3: Run Aspire AppHost

```powershell
# Run the entire system with one command
dotnet run --project src/AppHost/Agents.AppHost/Agents.AppHost.csproj
```

#### Step 4: Access Aspire Dashboard

Open your browser to: **http://localhost:15000**

**What You Get:**
- ✅ All 5 agent APIs with Dapr sidecars
- ✅ SQL Server, Redis, Ollama containers
- ✅ Unified dashboard with logs, traces, metrics
- ✅ Service discovery and health checks
- ✅ No manual configuration needed

**For detailed testing instructions, see:** [Aspire & Dapr Testing Guide](aspire-dapr-testing-guide.md)

---

### 🎨 UI Development Server

After starting the backend with Aspire, run the frontend apps:

#### Step 1: Enable pnpm

```powershell
corepack enable
corepack prepare pnpm@9.0.0 --activate
```

#### Step 2: Install Dependencies

```powershell
cd ui
pnpm install
```

#### Step 3: Start Development Servers

```powershell
# Start all apps
pnpm dev

# Or start a specific app
pnpm --filter @agents/agents-console dev
```

#### Step 4: Access UI Apps

- **Agents Console**: http://localhost:5173
- **Test Planning Studio**: http://localhost:5174
- **DevOps Explorer**: http://localhost:5175
- **Notification Center**: http://localhost:5176

**What You Get:**
- ✅ Multi-app frontend with AppSwitcher navigation
- ✅ Hot module reloading for instant updates
- ✅ TypeScript type-checking in real-time
- ✅ Shared design system across all apps
- ✅ Connected to backend APIs

**For detailed UI development guide, see:** [UI README](../ui/README.md)

---

### 🔧 Alternative: Manual Setup (Legacy)

If you need to run services individually without Aspire:

#### Step 1: Install Ollama

```powershell
# Download and install Ollama from https://ollama.ai

# Verify installation
ollama --version

# Pull the required model
ollama pull llama3.2
```

#### Step 2: Clone Repository

```powershell
git clone https://github.com/your-org/my-agents.git
cd my-agents
```

### Step 3: Configure Settings

Create development configuration:

```powershell
# Copy template
Copy-Item src/Presentation/Agents.API.Notification/appsettings.json `
          src/Presentation/Agents.API.Notification/appsettings.Development.json

# Edit the file to configure Ollama
```

`appsettings.Development.json`:
```json
{
  "LLMProvider": {
    "ProviderType": "Ollama",
    "Ollama": {
      "Endpoint": "http://localhost:11434",
      "ModelId": "llama3.2"
    }
  },
  "PromptLoader": {
    "Source": "FileSystem",
    "BasePath": "./prompts"
  },
  "EventPublisher": {
    "Type": "InMemory"
  }
}
```

### Step 4: Build Solution

```powershell
# Restore dependencies
dotnet restore

# Build solution
dotnet build

# Run tests
dotnet test
```

### Step 5: Run Agent APIs

**Option A: Run Individual APIs**

```powershell
# Terminal 1 - Notification Agent
cd src/Presentation/Agents.API.Notification
dotnet run

# Terminal 2 - DevOps Agent
cd src/Presentation/Agents.API.DevOps
dotnet run

# Terminal 3 - TestPlanning Agent
cd src/Presentation/Agents.API.TestPlanning
dotnet run
```

**Option B: Use Docker Compose**

```yaml
# docker-compose.dev.yml
version: '3.8'

services:
  notification-api:
    build:
      context: .
      dockerfile: src/Presentation/Agents.API.Notification/Dockerfile
    ports:
      - "5001:8080"
    environment:
      - ASPNETCORE_ENVIRONMENT=Development
      - LLMProvider__ProviderType=Ollama
      - LLMProvider__Ollama__Endpoint=http://host.docker.internal:11434
    
  devops-api:
    build:
      context: .
      dockerfile: src/Presentation/Agents.API.DevOps/Dockerfile
    ports:
      - "5002:8080"
    environment:
      - ASPNETCORE_ENVIRONMENT=Development
      - LLMProvider__ProviderType=Ollama
      - LLMProvider__Ollama__Endpoint=http://host.docker.internal:11434
```

Run with Docker Compose:
```powershell
docker-compose -f docker-compose.dev.yml up --build
```

### Step 6: Verify Deployment

```powershell
# Check Notification API health
curl http://localhost:5001/health

# Open Swagger UI
Start-Process "http://localhost:5001/swagger"
```

## Azure Infrastructure Deployment

### Step 1: Login to Azure

```powershell
# Login
az login

# Select subscription
az account set --subscription "Your Subscription Name"

# Verify
az account show
```

### Step 2: Deploy Infrastructure with Bicep

```powershell
cd infrastructure/bicep

# Create resource group
az group create `
  --name rg-agents-dev `
  --location eastus

# Deploy infrastructure
az deployment group create `
  --resource-group rg-agents-dev `
  --template-file main.bicep `
  --parameters @parameters/dev.parameters.json
```

**main.bicep** (excerpt):
```bicep
param location string = resourceGroup().location
param environment string = 'dev'

module aks 'modules/aks.bicep' = {
  name: 'aks-deployment'
  params: {
    location: location
    clusterName: 'aks-agents-${environment}'
  }
}

module openai 'modules/openai.bicep' = {
  name: 'openai-deployment'
  params: {
    location: location
    accountName: 'oai-agents-${environment}'
  }
}

module eventGrid 'modules/event-grid.bicep' = {
  name: 'eventgrid-deployment'
  params: {
    location: location
    namespaceName: 'eg-agents-${environment}'
  }
}
```

**parameters/dev.parameters.json**:
```json
{
  "$schema": "https://schema.management.azure.com/schemas/2019-04-01/deploymentParameters.json#",
  "contentVersion": "1.0.0.0",
  "parameters": {
    "environment": {
      "value": "dev"
    },
    "aksNodeCount": {
      "value": 2
    },
    "aksNodeSize": {
      "value": "Standard_D2s_v3"
    }
  }
}
```

### Step 3: Configure Secrets in Key Vault

```powershell
$keyVaultName = "kv-agents-dev"

# Add Azure OpenAI API key
az keyvault secret set `
  --vault-name $keyVaultName `
  --name "AzureOpenAI--ApiKey" `
  --value "your-api-key-here"

# Add Event Grid access key
az keyvault secret set `
  --vault-name $keyVaultName `
  --name "EventGrid--AccessKey" `
  --value "your-access-key-here"
```

### Step 4: Create Azure Container Registry

```powershell
$acrName = "acragentsdev"

az acr create `
  --resource-group rg-agents-dev `
  --name $acrName `
  --sku Standard

# Login to ACR
az acr login --name $acrName
```

### Step 5: Build and Push Images

```powershell
# Tag format: <acr-name>.azurecr.io/<image-name>:<version>
$acrLoginServer = az acr show --name $acrName --query loginServer --output tsv
$version = "1.0.0"

# Build each agent image
$agents = @("notification", "devops", "testplanning", "implementation", "servicedesk")

foreach ($agent in $agents) {
    $imageName = "agents-api-$agent"
    $tag = "${acrLoginServer}/${imageName}:${version}"
    
    Write-Host "Building $imageName..."
    docker build `
        -t $tag `
        -f "src/Presentation/Agents.API.$(Get-Culture).TextInfo.ToTitleCase($agent)/Dockerfile" `
        .
    
    Write-Host "Pushing $imageName..."
    docker push $tag
}
```

### Step 5a: Build and Deploy UI Apps

For production deployment, build the UI apps as static assets:

```powershell
# Navigate to UI directory
cd ui

# Install dependencies
pnpm install

# Build all apps for production
pnpm build

# Output will be in:
# - ui/apps/agents-console/dist
# - ui/apps/test-planning-studio/dist
# - ui/apps/devops-agent-explorer/dist
# - ui/apps/notification-center/dist
```

**Deployment Options:**

1. **Azure Static Web Apps** (Recommended)
   ```powershell
   # Deploy agents-console
   az staticwebapp create `
     --name swa-agents-console `
     --resource-group rg-agents-dev `
     --location eastus `
     --source ui/apps/agents-console/dist `
     --branch main
   ```

2. **Azure Blob Storage + CDN**
   ```powershell
   # Upload to blob storage
   az storage blob upload-batch `
     --account-name stagentsui `
     --destination '$web' `
     --source ui/apps/agents-console/dist
   ```

3. **Docker/Nginx** (for self-hosted)
   ```dockerfile
   FROM nginx:alpine
   COPY ui/apps/agents-console/dist /usr/share/nginx/html
   EXPOSE 80
   ```

**Environment Configuration:**

Update `ui/apps/*/src/config.ts` with production API endpoints before building.

### Step 6: Verify Azure Resources

```powershell
# List all resources in resource group
az resource list --resource-group rg-agents-dev --output table

# Check AKS status
az aks show --resource-group rg-agents-dev --name aks-agents-dev --query provisioningState

# Check Azure OpenAI deployment
az cognitiveservices account show `
  --resource-group rg-agents-dev `
  --name oai-agents-dev `
  --query properties.provisioningState
```

## Kubernetes Deployment

### Step 1: Connect to AKS Cluster

```powershell
# Get AKS credentials
az aks get-credentials `
  --resource-group rg-agents-dev `
  --name aks-agents-dev

# Verify connection
kubectl cluster-info
kubectl get nodes
```

### Step 2: Create Namespace

```powershell
# Create namespace
kubectl create namespace agents-dev

# Set as default
kubectl config set-context --current --namespace=agents-dev
```

### Step 3: Create Secrets

```powershell
# Create secret for ACR
kubectl create secret docker-registry acr-secret `
  --docker-server=$acrLoginServer `
  --docker-username=$acrName `
  --docker-password=$(az acr credential show --name $acrName --query passwords[0].value -o tsv) `
  --namespace=agents-dev

# Create secret for Azure OpenAI (from Key Vault)
$openAiKey = az keyvault secret show `
  --vault-name kv-agents-dev `
  --name "AzureOpenAI--ApiKey" `
  --query value -o tsv

kubectl create secret generic agent-secrets `
  --from-literal=AZUREOPENAI_APIKEY=$openAiKey `
  --namespace=agents-dev
```

### Step 4: Deploy with Kustomize

**Base Configuration** (`k8s/base/deployment.yaml`):
```yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: notification-agent
spec:
  replicas: 2
  selector:
    matchLabels:
      app: notification-agent
  template:
    metadata:
      labels:
        app: notification-agent
    spec:
      imagePullSecrets:
        - name: acr-secret
      containers:
        - name: notification-agent
          image: acragentsdev.azurecr.io/agents-api-notification:1.0.0
          ports:
            - containerPort: 8080
          env:
            - name: ASPNETCORE_ENVIRONMENT
              value: "Production"
            - name: LLMProvider__ProviderType
              value: "AzureOpenAI"
            - name: LLMProvider__AzureOpenAI__ApiKey
              valueFrom:
                secretKeyRef:
                  name: agent-secrets
                  key: AZUREOPENAI_APIKEY
          resources:
            requests:
              memory: "256Mi"
              cpu: "250m"
            limits:
              memory: "512Mi"
              cpu: "500m"
          livenessProbe:
            httpGet:
              path: /health
              port: 8080
            initialDelaySeconds: 30
            periodSeconds: 10
          readinessProbe:
            httpGet:
              path: /health
              port: 8080
            initialDelaySeconds: 10
            periodSeconds: 5
---
apiVersion: v1
kind: Service
metadata:
  name: notification-agent
spec:
  type: ClusterIP
  ports:
    - port: 80
      targetPort: 8080
  selector:
    app: notification-agent
```

**Environment Overlay** (`k8s/overlays/dev/kustomization.yaml`):
```yaml
apiVersion: kustomize.config.k8s.io/v1beta1
kind: Kustomization

namespace: agents-dev

resources:
  - ../../base

images:
  - name: agents-api-notification
    newName: acragentsdev.azurecr.io/agents-api-notification
    newTag: "1.0.0"

replicas:
  - name: notification-agent
    count: 2

configMapGenerator:
  - name: agent-config
    literals:
      - ENVIRONMENT=dev
      - LOG_LEVEL=Information
```

**Deploy**:
```powershell
# Apply base + overlay
kubectl apply -k k8s/overlays/dev

# Watch deployment progress
kubectl rollout status deployment/notification-agent
```

### Step 5: Configure Ingress

```yaml
# k8s/base/ingress.yaml
apiVersion: networking.k8s.io/v1
kind: Ingress
metadata:
  name: agents-ingress
  annotations:
    kubernetes.io/ingress.class: nginx
    cert-manager.io/cluster-issuer: letsencrypt-prod
spec:
  tls:
    - hosts:
        - agents-dev.yourdomain.com
      secretName: agents-tls
  rules:
    - host: agents-dev.yourdomain.com
      http:
        paths:
          - path: /api/notification
            pathType: Prefix
            backend:
              service:
                name: notification-agent
                port:
                  number: 80
          - path: /api/devops
            pathType: Prefix
            backend:
              service:
                name: devops-agent
                port:
                  number: 80
```

Apply ingress:
```powershell
kubectl apply -f k8s/base/ingress.yaml
```

### Step 6: Configure Horizontal Pod Autoscaler

```yaml
# k8s/base/hpa.yaml
apiVersion: autoscaling/v2
kind: HorizontalPodAutoscaler
metadata:
  name: notification-agent-hpa
spec:
  scaleTargetRef:
    apiVersion: apps/v1
    kind: Deployment
    name: notification-agent
  minReplicas: 2
  maxReplicas: 10
  metrics:
    - type: Resource
      resource:
        name: cpu
        target:
          type: Utilization
          averageUtilization: 70
    - type: Resource
      resource:
        name: memory
        target:
          type: Utilization
          averageUtilization: 80
```

Apply HPA:
```powershell
kubectl apply -f k8s/base/hpa.yaml
```

## Configuration Management

### Environment-Specific Configuration

Use ConfigMaps for non-sensitive configuration:

```yaml
apiVersion: v1
kind: ConfigMap
metadata:
  name: agent-config
  namespace: agents-dev
data:
  appsettings.Production.json: |
    {
      "LLMProvider": {
        "ProviderType": "AzureOpenAI",
        "AzureOpenAI": {
          "Endpoint": "https://oai-agents-dev.openai.azure.com/",
          "DeploymentName": "gpt-4",
          "ModelId": "gpt-4"
        }
      },
      "EventGrid": {
        "TopicEndpoint": "https://eg-agents-dev.eastus-1.eventgrid.azure.net/api/events"
      },
      "Logging": {
        "LogLevel": {
          "Default": "Information",
          "Microsoft": "Warning"
        }
      }
    }
```

Mount as volume:
```yaml
volumes:
  - name: config
    configMap:
      name: agent-config
volumeMounts:
  - name: config
    mountPath: /app/appsettings.Production.json
    subPath: appsettings.Production.json
```

### Secrets Management

Store secrets in Azure Key Vault and sync to Kubernetes:

```powershell
# Install Azure Key Vault Provider for Secrets Store CSI Driver
kubectl apply -f https://raw.githubusercontent.com/Azure/secrets-store-csi-driver-provider-azure/master/deployment/provider-azure-installer.yaml

# Create SecretProviderClass
kubectl apply -f k8s/base/secret-provider.yaml
```

## Deployment Validation

### Health Checks

```powershell
# Check all pods
kubectl get pods -n agents-dev

# Check pod logs
kubectl logs -n agents-dev deployment/notification-agent

# Check events
kubectl get events -n agents-dev --sort-by='.lastTimestamp'
```

### API Testing

```powershell
# Port forward to test locally
kubectl port-forward -n agents-dev svc/notification-agent 8080:80

# Test health endpoint
curl http://localhost:8080/health

# Test API endpoint
curl -X POST http://localhost:8080/api/notification/execute `
  -H "Content-Type: application/json" `
  -d '{"channel":"email","recipient":"test@example.com","message":"Test"}'
```

### Load Testing

```powershell
# Install k6
choco install k6

# Run load test
k6 run tests/load/notification-agent.js
```

## Rollback Procedures

### Kubernetes Rollback

```powershell
# View deployment history
kubectl rollout history deployment/notification-agent -n agents-dev

# Rollback to previous version
kubectl rollout undo deployment/notification-agent -n agents-dev

# Rollback to specific revision
kubectl rollout undo deployment/notification-agent -n agents-dev --to-revision=2

# Check rollback status
kubectl rollout status deployment/notification-agent -n agents-dev
```

### Azure Infrastructure Rollback

```powershell
# List deployments
az deployment group list --resource-group rg-agents-dev --output table

# Redeploy previous version
az deployment group create `
  --resource-group rg-agents-dev `
  --template-file main.bicep `
  --parameters @parameters/dev.parameters.previous.json
```

## Troubleshooting

### Common Issues

**Issue: Pods stuck in ImagePullBackOff**

```powershell
# Check pod details
kubectl describe pod <pod-name> -n agents-dev

# Solution: Verify ACR credentials
kubectl get secret acr-secret -n agents-dev -o yaml
```

**Issue: Pods crashing (CrashLoopBackOff)**

```powershell
# Check logs
kubectl logs <pod-name> -n agents-dev --previous

# Common causes:
# - Missing environment variables
# - Configuration errors
# - Insufficient resources
```

**Issue: Service not reachable**

```powershell
# Check service endpoints
kubectl get endpoints -n agents-dev

# Test service internally
kubectl run test-pod --image=curlimages/curl -it --rm -- sh
curl http://notification-agent.agents-dev.svc.cluster.local/health
```

**Issue: High latency or timeouts**

```powershell
# Check HPA status
kubectl get hpa -n agents-dev

# Check resource usage
kubectl top pods -n agents-dev
kubectl top nodes

# Scale manually if needed
kubectl scale deployment notification-agent --replicas=5 -n agents-dev
```

## CI/CD Integration

### GitHub Actions Workflow

```yaml
# .github/workflows/deploy.yml
name: Deploy to AKS

on:
  push:
    branches: [main]

env:
  AZURE_RESOURCE_GROUP: rg-agents-dev
  AKS_CLUSTER: aks-agents-dev
  ACR_NAME: acragentsdev

jobs:
  build-and-deploy:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v3
      
      - name: Azure Login
        uses: azure/login@v1
        with:
          creds: ${{ secrets.AZURE_CREDENTIALS }}
      
      - name: Build and push images
        run: |
          az acr build -t agents-api-notification:${{ github.sha }} \
            -r $ACR_NAME \
            -f src/Presentation/Agents.API.Notification/Dockerfile .
      
      - name: Get AKS credentials
        run: |
          az aks get-credentials \
            --resource-group $AZURE_RESOURCE_GROUP \
            --name $AKS_CLUSTER
      
      - name: Deploy to AKS
        run: |
          kubectl set image deployment/notification-agent \
            notification-agent=$ACR_NAME.azurecr.io/agents-api-notification:${{ github.sha }} \
            -n agents-dev
          kubectl rollout status deployment/notification-agent -n agents-dev
```

## Further Reading

- [Architecture Overview](architecture.md)
- [Agent Development Guide](agent-development.md)
- [Prompt Authoring Guide](prompt-authoring.md)
- [Operations Runbook](operations.md)
