# Infrastructure as Code - AI Agents Framework

This directory contains Azure infrastructure definitions using Bicep for deploying the AI Agents multi-agent framework.

## 📁 Directory Structure

```
infrastructure/
├── bicep/
│   ├── main.bicep                 # Main orchestration template
│   ├── modules/                   # Modular Bicep templates
│   │   ├── aks-cluster.bicep     # Azure Kubernetes Service
│   │   ├── container-registry.bicep # Azure Container Registry
│   │   ├── vnet.bicep            # Virtual Network
│   │   ├── cosmos-db.bicep       # Cosmos DB
│   │   ├── sql-database.bicep    # Azure SQL Database
│   │   ├── event-hub.bicep       # Event Hub namespace
│   │   ├── service-bus.bicep     # Service Bus namespace
│   │   └── monitoring.bicep      # Key Vault, App Insights, Log Analytics
│   └── parameters/                # Environment-specific parameters
│       ├── dev.parameters.json   # Development environment
│       ├── staging.parameters.json # Staging environment (optional)
│       └── prod.parameters.json  # Production environment
└── scripts/
    └── Deploy-Infrastructure.ps1  # Deployment automation script
```

## 🎯 Infrastructure Components

| Component | Purpose | SKU (Dev) | SKU (Prod) |
|-----------|---------|-----------|------------|
| **AKS Cluster** | Container orchestration | 3 nodes, D2s_v3 | 5 nodes, D4s_v3 |
| **Azure Container Registry** | Docker image storage | Standard | Premium |
| **Virtual Network** | Network isolation | 10.1.0.0/16 | 10.1.0.0/16 |
| **Cosmos DB** | Event sourcing | Serverless | Serverless |
| **Azure SQL Database** | Relational data | Basic (5 DTU) | Standard S1 (20 DTU) |
| **Event Hub** | Event streaming | Standard | Standard |
| **Service Bus** | Message queuing | Standard | Standard |
| **Key Vault** | Secrets management | Standard | Standard |
| **Application Insights** | Monitoring | Standard | Standard |
| **Log Analytics** | Logging | PerGB2018 | PerGB2018 |

## 🚀 Prerequisites

### Required Tools

1. **Azure CLI** (version 2.50.0 or later)
   ```powershell
   winget install Microsoft.AzureCLI
   ```

2. **Bicep CLI** (installed via Azure CLI)
   ```powershell
   az bicep install
   ```

3. **PowerShell 7+**
   ```powershell
   winget install Microsoft.PowerShell
   ```

### Azure Permissions

- **Subscription Contributor** role or higher
- **User Access Administrator** for RBAC assignments
- **Key Vault Administrator** for managing secrets

## 📝 Configuration

### 1. Update Parameter Files

Edit the environment-specific parameter files in `bicep/parameters/`:

**dev.parameters.json:**
```json
{
  "parameters": {
    "resourceGroupName": {
      "value": "rg-agents-dev"
    },
    "location": {
      "value": "eastus"
    },
    "environment": {
      "value": "dev"
    },
    "sqlAdministratorLogin": {
      "value": "sqladmin"
    },
    "sqlAdministratorPassword": {
      "reference": {
        "keyVault": {
          "id": "/subscriptions/{subscription-id}/resourceGroups/rg-agents-shared/providers/Microsoft.KeyVault/vaults/agents-shared-kv"
        },
        "secretName": "sql-admin-password"
      }
    }
  }
}
```

### 2. Store Secrets in Key Vault

Before deployment, store sensitive values in Azure Key Vault:

```powershell
# Create a shared Key Vault for deployment secrets
az keyvault create `
  --name agents-shared-kv `
  --resource-group rg-agents-shared `
  --location eastus

# Store SQL admin password
az keyvault secret set `
  --vault-name agents-shared-kv `
  --name sql-admin-password `
  --value 'YourSecurePassword123!'
```

## 🎬 Deployment

### Quick Start

Deploy to development environment:

```powershell
cd infrastructure/scripts

./Deploy-Infrastructure.ps1 `
  -Environment dev `
  -SubscriptionId "your-subscription-id"
```

### Deployment Options

**1. Validation Only (What-If)**

Preview changes without deploying:

```powershell
./Deploy-Infrastructure.ps1 `
  -Environment dev `
  -SubscriptionId "your-subscription-id" `
  -WhatIf
```

**2. Custom Location**

Deploy to a specific Azure region:

```powershell
./Deploy-Infrastructure.ps1 `
  -Environment prod `
  -Location westus2 `
  -SubscriptionId "your-subscription-id"
```

**3. Manual Deployment**

Using Azure CLI directly:

```powershell
az deployment sub create `
  --name agents-deployment `
  --location eastus `
  --template-file bicep/main.bicep `
  --parameters @bicep/parameters/dev.parameters.json
```

## 🔍 Post-Deployment

### Verify Deployment

```powershell
# Get deployment outputs
az deployment sub show `
  --name agents-deployment `
  --query properties.outputs

# List all resources in resource group
az resource list `
  --resource-group rg-agents-dev `
  --output table
```

### Connect to AKS

```powershell
# Get AKS credentials
az aks get-credentials `
  --resource-group rg-agents-dev `
  --name agents-aks-dev

# Verify connection
kubectl get nodes
```

### Access Outputs

Key outputs from the deployment:

| Output | Description |
|--------|-------------|
| `resourceGroupName` | Created resource group name |
| `acrLoginServer` | Container registry login server |
| `aksClusterName` | AKS cluster name |
| `aksClusterFqdn` | AKS cluster FQDN |
| `cosmosEndpoint` | Cosmos DB endpoint |
| `sqlServerFqdn` | SQL Server FQDN |
| `keyVaultName` | Key Vault name |
| `appInsightsConnectionString` | Application Insights connection string |

## 🛠️ Troubleshooting

### Common Issues

**1. Bicep Build Errors**

```powershell
# Validate Bicep syntax
az bicep build --file bicep/main.bicep

# Lint Bicep files
az bicep lint --file bicep/main.bicep
```

**2. Deployment Validation Failures**

```powershell
# Run validation step separately
az deployment sub validate `
  --name test-validation `
  --location eastus `
  --template-file bicep/main.bicep `
  --parameters @bicep/parameters/dev.parameters.json
```

**3. Resource Name Conflicts**

Resource names must be globally unique. The templates use `uniqueString()` to generate unique suffixes.

**4. Quota Limits**

Check Azure subscription quotas:

```powershell
az vm list-usage --location eastus --output table
```

### Logs and Diagnostics

View deployment operation details:

```powershell
# List recent deployments
az deployment sub list --output table

# Get detailed deployment info
az deployment sub show `
  --name agents-deployment `
  --query properties.error
```

## 🔄 Updates and Changes

### Update Infrastructure

Modify Bicep files and redeploy:

```powershell
# Run What-If to preview changes
./Deploy-Infrastructure.ps1 -Environment dev -WhatIf

# Apply changes
./Deploy-Infrastructure.ps1 -Environment dev
```

### Rollback

Azure deployments are idempotent. To rollback:

1. Revert Bicep file changes
2. Redeploy with previous configuration

```powershell
git checkout HEAD~1 infrastructure/bicep/
./Deploy-Infrastructure.ps1 -Environment dev
```

## 🧹 Cleanup

### Delete Environment

Remove all resources for an environment:

```powershell
# Delete resource group (removes all resources)
az group delete `
  --name rg-agents-dev `
  --yes --no-wait

# Verify deletion
az group show --name rg-agents-dev
```

### Cost Considerations

- **Development**: ~$150-200/month
- **Production**: ~$500-800/month (depends on usage)

To minimize costs:
- Stop AKS cluster when not in use
- Use serverless Cosmos DB
- Scale down to Basic SKUs for testing

## 📚 Additional Resources

- [Azure Bicep Documentation](https://learn.microsoft.com/azure/azure-resource-manager/bicep/)
- [AKS Best Practices](https://learn.microsoft.com/azure/aks/best-practices)
- [Cosmos DB Best Practices](https://learn.microsoft.com/azure/cosmos-db/best-practices)
- [Azure SQL Database](https://learn.microsoft.com/azure/azure-sql/)

## 🤝 Support

For infrastructure issues:
1. Check the [troubleshooting section](#troubleshooting)
2. Review Azure deployment logs
3. Open an issue in the repository

---

**Last Updated**: 2025-11-11
**Maintained By**: Infrastructure Team
