# GitHub Actions Workflows

This directory contains CI/CD workflows for building, testing, and deploying the AI Orchestration Multi-Agent Framework.

## Workflows

### 1. `docker-build.yml` - Build and Push Container Images

Builds Docker images for all agent services and pushes them to Azure Container Registry.

**Triggers:**
- Push to `main` or `develop` branches
- Pull requests to `main` or `develop` branches
- Version tags (`v*`)
- Manual workflow dispatch

**Features:**
- ✅ Works without Azure setup (builds locally only)
- ✅ Automatically detects if Azure is configured
- ✅ Builds all 5 agent services in parallel
- ✅ Pushes to ACR when Azure is configured
- ✅ Security scanning with Trivy
- ✅ Multi-platform support

### 2. `deploy-aks.yml` - Deploy to Azure Kubernetes Service

Deploys agent services to AKS using Helm and Kustomize.

**Triggers:**
- Manual workflow dispatch only

**Features:**
- Environment selection (dev/staging/prod)
- Image tag specification
- Automated health checks
- Slack notifications

### 3. `ci.yml` - Continuous Integration

Runs tests and code quality checks.

### 4. `security-scan.yml` - Security Scanning

Performs security scans on dependencies and code.

## Required Secrets

### For Azure Container Registry (Optional for Development)

The workflow will work without these secrets - it will just build images locally without pushing to ACR.

| Secret Name | Description | How to Get |
|-------------|-------------|------------|
| `AZURE_CREDENTIALS` | Azure service principal credentials | See [Azure Setup](#azure-setup) below |
| `ACR_NAME` | Azure Container Registry name (without .azurecr.io) | e.g., `acragentsdev` |
| `ACR_LOGIN_SERVER` | Full ACR URL | e.g., `acragentsdev.azurecr.io` |

### For AKS Deployment (Required for deploy-aks.yml)

| Secret Name | Description |
|-------------|-------------|
| `AZURE_SUBSCRIPTION_ID` | Azure subscription ID |
| `AZURE_RESOURCE_GROUP` | Resource group name containing AKS cluster |
| `AKS_CLUSTER_NAME` | AKS cluster name |
| `SQL_CONNECTION_STRING` | Azure SQL connection string |
| `COSMOS_CONNECTION_STRING` | Cosmos DB connection string |
| `AZURE_OPENAI_ENDPOINT` | Azure OpenAI endpoint URL |
| `AZURE_OPENAI_API_KEY` | Azure OpenAI API key |
| `AZURE_OPENAI_DEPLOYMENT` | Azure OpenAI deployment name |
| `EVENT_HUB_CONNECTION_STRING` | Event Hub connection string |
| `SERVICE_BUS_CONNECTION_STRING` | Service Bus connection string |
| `APP_INSIGHTS_CONNECTION_STRING` | Application Insights connection string |
| `SLACK_WEBHOOK` | Slack webhook URL for notifications (optional) |

## Azure Setup

### Step 1: Create Azure Service Principal

```bash
# Login to Azure
az login

# Create service principal with Contributor role
az ad sp create-for-rbac \
  --name "github-actions-agents" \
  --role Contributor \
  --scopes /subscriptions/{subscription-id}/resourceGroups/{resource-group} \
  --sdk-auth

# Output will be JSON credentials - save this entire output
```

### Step 2: Create Azure Container Registry

```bash
# Create resource group
az group create --name rg-agents-dev --location eastus

# Create ACR
az acr create \
  --resource-group rg-agents-dev \
  --name acragentsdev \
  --sku Standard

# Get ACR login server
az acr show --name acragentsdev --query loginServer --output tsv
```

### Step 3: Add Secrets to GitHub

1. Go to your GitHub repository
2. Navigate to **Settings** → **Secrets and variables** → **Actions**
3. Click **New repository secret**
4. Add each secret:

**AZURE_CREDENTIALS:**
```json
{
  "clientId": "...",
  "clientSecret": "...",
  "subscriptionId": "...",
  "tenantId": "...",
  "activeDirectoryEndpointUrl": "https://login.microsoftonline.com",
  "resourceManagerEndpointUrl": "https://management.azure.com/",
  "activeDirectoryGraphResourceId": "https://graph.windows.net/",
  "sqlManagementEndpointUrl": "https://management.core.windows.net:8443/",
  "galleryEndpointUrl": "https://gallery.azure.com/",
  "managementEndpointUrl": "https://management.core.windows.net/"
}
```

**ACR_NAME:**
```
acragentsdev
```

**ACR_LOGIN_SERVER:**
```
acragentsdev.azurecr.io
```

## Local Development Workflow

Without Azure configured, the workflow will:

1. ✅ Build all Docker images locally
2. ✅ Run security scans on local images
3. ✅ Upload scan results to GitHub Security
4. ⚠️ Show warning that Azure is not configured
5. ⏭️ Skip pushing to ACR

To test locally:

```bash
# Build image manually
docker build -t agents-api-notification:test \
  -f src/Presentation/Agents.API.Notification/Dockerfile .

# Run locally
docker run -p 5001:8080 agents-api-notification:test
```

## Production Workflow

Once Azure is configured, the workflow will:

1. ✅ Build all Docker images
2. ✅ Push images to Azure Container Registry
3. ✅ Run security scans on ACR images
4. ✅ Upload scan results to GitHub Security
5. ✅ Tag images with:
   - Git SHA (e.g., `sha-abc123`)
   - Branch name (e.g., `main`, `develop`)
   - `latest` tag for default branch
   - Semantic version tags (if version tag pushed)

## Triggering Deployments

### Manual Deployment

1. Go to **Actions** tab in GitHub
2. Select **Deploy to AKS** workflow
3. Click **Run workflow**
4. Select:
   - **Environment**: dev, staging, or prod
   - **Image tag**: Git SHA or tag to deploy (e.g., `sha-abc123`)
5. Click **Run workflow**

### Automated Deployment (Future)

Can be configured to auto-deploy on successful builds:

```yaml
# Add to docker-build.yml
- name: Trigger deployment
  if: github.ref == 'refs/heads/main' && steps.azure-check.outputs.configured == 'true'
  uses: peter-evans/repository-dispatch@v2
  with:
    event-type: deploy
    client-payload: '{"environment": "dev", "image_tag": "${{ github.sha }}"}'
```

## Troubleshooting

### Build fails with "Azure credentials not configured"

This is just a warning. The build will continue and create local images only. To fix:
1. Set up Azure as described above
2. Add the required secrets to GitHub

### Build fails with "Error: buildx failed"

Check that the Dockerfile paths are correct in the matrix configuration.

### Security scan fails

This is allowed to fail (`continue-on-error: true`). Check the security tab for details.

### Deployment fails with "cluster not found"

Ensure:
1. AKS cluster exists and is running
2. Service principal has access to the cluster
3. Resource group and cluster names are correct in secrets

## Monitoring

### View Build Logs

1. Go to **Actions** tab
2. Click on workflow run
3. Click on specific job (e.g., `notification`)
4. View logs for each step

### View Security Scan Results

1. Go to **Security** tab
2. Click **Code scanning**
3. View Trivy scan results

### View Deployed Resources

```bash
# Get AKS credentials
az aks get-credentials \
  --resource-group rg-agents-dev \
  --name aks-agents-dev

# Check pods
kubectl get pods -n agents-dev

# Check deployments
kubectl get deployments -n agents-dev

# View logs
kubectl logs -n agents-dev deployment/notification-agent
```

## Best Practices

1. **Always test in dev environment first** before deploying to staging/prod
2. **Use semantic versioning tags** for production releases
3. **Monitor security scan results** and address critical/high vulnerabilities
4. **Set up Slack notifications** for deployment results
5. **Use branch protection rules** to require PR reviews before merging to main
6. **Keep secrets secure** - never commit them to code
7. **Rotate service principal credentials** regularly (every 90 days recommended)

## Support

For issues with workflows:
- Check workflow logs in the Actions tab
- Review [Deployment Guide](../../docs/deployment.md)
- Review [Operations Runbook](../../docs/operations.md)
- Open an issue with the `ci/cd` label
