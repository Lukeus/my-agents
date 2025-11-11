# Kubernetes Deployment Guide

This directory contains all Kubernetes deployment configurations for the AI Agents multi-agent microservices framework.

## Directory Structure

```
k8s/
├── base/                    # Base Kubernetes manifests
│   ├── kustomization.yaml
│   └── namespace.yaml
├── overlays/                # Environment-specific overlays
│   ├── dev/                 # Development environment
│   │   ├── kustomization.yaml
│   │   └── patches/
│   ├── staging/             # Staging environment
│   │   ├── kustomization.yaml
│   │   └── patches/
│   └── prod/                # Production environment
│       ├── kustomization.yaml
│       └── patches/
└── README.md
```

## Prerequisites

- kubectl 1.28+
- Helm 3.12+
- Kustomize 5.0+
- Azure CLI 2.50+
- Access to Azure Kubernetes Service (AKS)
- Azure Container Registry credentials

## Quick Start

### 1. Connect to AKS Cluster

```bash
# Login to Azure
az login

# Set subscription
az account set --subscription <SUBSCRIPTION_ID>

# Get AKS credentials
az aks get-credentials --resource-group <RESOURCE_GROUP> --name <AKS_CLUSTER_NAME>

# Verify connection
kubectl cluster-info
```

### 2. Configure Container Registry Access

```bash
# Create namespace
kubectl create namespace agents-dev

# Create image pull secret
kubectl create secret docker-registry acr-secret \
  --namespace=agents-dev \
  --docker-server=<ACR_NAME>.azurecr.io \
  --docker-username=<ACR_USERNAME> \
  --docker-password=<ACR_PASSWORD>
```

### 3. Configure Application Secrets

```bash
# Create secrets from Azure resources
kubectl create secret generic agents-secrets \
  --namespace=agents-dev \
  --from-literal=ConnectionStrings__SqlServer="<SQL_CONNECTION_STRING>" \
  --from-literal=ConnectionStrings__CosmosDb="<COSMOS_CONNECTION_STRING>" \
  --from-literal=LLMProvider__AzureOpenAI__Endpoint="<AZURE_OPENAI_ENDPOINT>" \
  --from-literal=LLMProvider__AzureOpenAI__ApiKey="<AZURE_OPENAI_API_KEY>" \
  --from-literal=LLMProvider__AzureOpenAI__DeploymentName="<DEPLOYMENT_NAME>" \
  --from-literal=EventHub__ConnectionString="<EVENT_HUB_CONNECTION_STRING>" \
  --from-literal=ServiceBus__ConnectionString="<SERVICE_BUS_CONNECTION_STRING>" \
  --from-literal=ApplicationInsights__ConnectionString="<APP_INSIGHTS_CONNECTION_STRING>"
```

### 4. Deploy Using Helm

```bash
# From repository root
cd helm/agents

# Install/upgrade release
helm upgrade --install agents . \
  --namespace agents-dev \
  --values values.yaml \
  --set image.registry=<ACR_NAME>.azurecr.io \
  --set image.tag=latest \
  --wait \
  --timeout 10m
```

### 5. Deploy Using Kustomize

```bash
# From repository root
cd k8s

# Deploy to dev environment
kubectl apply -k overlays/dev

# Deploy to staging environment
kubectl apply -k overlays/staging

# Deploy to production environment
kubectl apply -k overlays/prod
```

## Environment Configuration

### Development (dev)

- **Namespace**: `agents-dev`
- **Replicas**: 1 per service
- **Resources**: Reduced (100m CPU, 128Mi RAM requests)
- **Image Tag**: `dev-latest`
- **Log Level**: Debug

### Staging (staging)

- **Namespace**: `agents-staging`
- **Replicas**: 2 per service
- **Resources**: Standard (250m CPU, 256Mi RAM requests)
- **Image Tag**: `staging-latest`
- **Log Level**: Information

### Production (prod)

- **Namespace**: `agents-prod`
- **Replicas**: 3 per service
- **Resources**: Enhanced (500m CPU, 512Mi RAM requests)
- **Image Tag**: Versioned (e.g., `prod-v1.0.0`)
- **Log Level**: Warning
- **HPA**: Enabled (2-10 replicas based on CPU/Memory)

## Helm Values Customization

### Override Default Values

Create environment-specific values files:

```yaml
# helm/agents/values-dev.yaml
image:
  tag: dev-latest

notificationAgent:
  replicaCount: 1
  autoscaling:
    enabled: false

ingress:
  enabled: false
```

Then deploy with:

```bash
helm upgrade --install agents ./helm/agents \
  --namespace agents-dev \
  --values helm/agents/values.yaml \
  --values helm/agents/values-dev.yaml
```

## Kustomize Overlays

### Customize Image Tags

```bash
cd k8s/overlays/dev

# Update image tags
kustomize edit set image \
  myacr.azurecr.io/agents-notification-api=myacr.azurecr.io/agents-notification-api:v1.2.3 \
  myacr.azurecr.io/agents-devops-api=myacr.azurecr.io/agents-devops-api:v1.2.3

# Apply changes
kubectl apply -k .
```

### Add Custom Patches

Create a new patch file in `overlays/<env>/patches/`:

```yaml
# custom-patch.yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: notification
spec:
  template:
    spec:
      containers:
      - name: notification
        env:
        - name: CUSTOM_SETTING
          value: "custom-value"
```

Add to `kustomization.yaml`:

```yaml
patchesStrategicMerge:
  - patches/custom-patch.yaml
```

## Verification

### Check Deployment Status

```bash
# Get all resources
kubectl get all -n agents-dev

# Check pod status
kubectl get pods -n agents-dev

# Check deployment rollout
kubectl rollout status deployment -n agents-dev

# View logs
kubectl logs -n agents-dev deployment/agents-notification -f

# Describe pod for troubleshooting
kubectl describe pod -n agents-dev <POD_NAME>
```

### Test Service Health

```bash
# Port forward to test locally
kubectl port-forward -n agents-dev svc/agents-notification 8080:80

# Test health endpoint
curl http://localhost:8080/health

# Test Swagger UI
open http://localhost:8080/swagger
```

### Check HPA Status

```bash
# View HPA status
kubectl get hpa -n agents-dev

# Watch HPA in real-time
kubectl get hpa -n agents-dev -w

# Describe HPA for details
kubectl describe hpa -n agents-dev agents-notification
```

## Scaling

### Manual Scaling

```bash
# Scale specific deployment
kubectl scale deployment agents-notification -n agents-dev --replicas=5

# Scale all deployments
kubectl scale deployment -n agents-dev -l app.kubernetes.io/name=agents --replicas=3
```

### Auto-scaling Configuration

Modify HPA settings in Helm values:

```yaml
notificationAgent:
  autoscaling:
    enabled: true
    minReplicas: 2
    maxReplicas: 20
    targetCPUUtilizationPercentage: 70
    targetMemoryUtilizationPercentage: 80
```

## Rollback

### Helm Rollback

```bash
# List releases
helm list -n agents-dev

# View release history
helm history agents -n agents-dev

# Rollback to previous version
helm rollback agents -n agents-dev

# Rollback to specific revision
helm rollback agents 3 -n agents-dev
```

### kubectl Rollback

```bash
# View rollout history
kubectl rollout history deployment/agents-notification -n agents-dev

# Rollback to previous version
kubectl rollout undo deployment/agents-notification -n agents-dev

# Rollback to specific revision
kubectl rollout undo deployment/agents-notification -n agents-dev --to-revision=2
```

## Monitoring

### View Metrics

```bash
# Top pods
kubectl top pods -n agents-dev

# Top nodes
kubectl top nodes

# Get events
kubectl get events -n agents-dev --sort-by='.lastTimestamp'
```

### Access Application Insights

Application logs and metrics are automatically sent to Azure Application Insights when configured.

View in Azure Portal:
1. Navigate to Application Insights resource
2. Go to "Logs" section
3. Query using KQL

Example query:
```kql
requests
| where cloud_RoleName == "agents-notification"
| where timestamp > ago(1h)
| summarize count() by resultCode, bin(timestamp, 5m)
```

## Troubleshooting

### Common Issues

#### Pods in CrashLoopBackOff

```bash
# Check pod logs
kubectl logs -n agents-dev <POD_NAME> --previous

# Check pod events
kubectl describe pod -n agents-dev <POD_NAME>

# Common causes:
# - Missing secrets
# - Database connection issues
# - Invalid configuration
```

#### ImagePullBackOff

```bash
# Check if secret exists
kubectl get secret acr-secret -n agents-dev

# Verify secret data
kubectl get secret acr-secret -n agents-dev -o yaml

# Recreate secret if needed
kubectl delete secret acr-secret -n agents-dev
# Then recreate following step 2 above
```

#### Service Not Accessible

```bash
# Check service endpoints
kubectl get endpoints -n agents-dev

# Verify service selector matches pod labels
kubectl get pods -n agents-dev --show-labels
kubectl get svc agents-notification -n agents-dev -o yaml

# Test from within cluster
kubectl run curl-test --image=curlimages/curl --rm -i --restart=Never \
  --namespace=agents-dev \
  -- curl -v http://agents-notification/health
```

#### High Memory/CPU Usage

```bash
# Check resource usage
kubectl top pods -n agents-dev

# Adjust resource limits in values.yaml
# Or create a patch for specific deployment
```

### Enable Debug Logging

Create a patch to enable debug logging:

```yaml
# debug-logging-patch.yaml
apiVersion: v1
kind: ConfigMap
metadata:
  name: agents-config
data:
  Logging__LogLevel__Default: "Debug"
  Logging__LogLevel__Microsoft: "Debug"
```

Apply:
```bash
kubectl apply -f debug-logging-patch.yaml -n agents-dev
kubectl rollout restart deployment -n agents-dev
```

## Security

### Pod Security Standards

All deployments follow pod security best practices:
- Run as non-root user (UID 1000)
- Drop all capabilities
- Read-only root filesystem where possible
- No privilege escalation

### Network Policies

Network policies are enabled by default to restrict traffic between pods. Review in `helm/agents/templates/networkpolicy.yaml`.

### Secrets Management

For production, consider using:
- **Azure Key Vault Secrets Store CSI Driver**: Sync secrets from Azure Key Vault
- **External Secrets Operator**: Manage secrets from external sources
- **Sealed Secrets**: Encrypt secrets in Git

## CI/CD Integration

Deployments are automated via GitHub Actions. See `.github/workflows/deploy-aks.yml`.

### Manual Deployment from CI/CD

```bash
# Trigger deployment workflow
gh workflow run deploy-aks.yml \
  -f environment=staging \
  -f image_tag=v1.2.3
```

## Cleanup

### Remove Deployment

```bash
# Uninstall Helm release
helm uninstall agents -n agents-dev

# Delete namespace
kubectl delete namespace agents-dev

# Using Kustomize
kubectl delete -k overlays/dev
```

## Additional Resources

- [Kubernetes Documentation](https://kubernetes.io/docs/)
- [Helm Documentation](https://helm.sh/docs/)
- [Kustomize Documentation](https://kustomize.io/)
- [Azure AKS Documentation](https://docs.microsoft.com/en-us/azure/aks/)
- [Project README](../README.md)
