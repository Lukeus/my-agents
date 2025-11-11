targetScope = 'subscription'

@description('The Azure region for all resources')
param location string = 'eastus'

@description('Environment name (dev, staging, production)')
@allowed([
  'dev'
  'staging'
  'production'
])
param environment string

@description('Base name for all resources')
param baseName string = 'agents'

@description('Tags to apply to all resources')
param tags object = {
  Environment: environment
  Project: 'AI-Agents'
  ManagedBy: 'Bicep'
}

// Resource Group
resource rg 'Microsoft.Resources/resourceGroups@2021-04-01' = {
  name: '${baseName}-${environment}-rg'
  location: location
  tags: tags
}

// AKS Cluster
module aks 'modules/aks.bicep' = {
  name: 'aks-deployment'
  scope: rg
  params: {
    location: location
    environment: environment
    baseName: baseName
    tags: tags
  }
}

// Event Grid Namespace
module eventGrid 'modules/event-grid.bicep' = {
  name: 'event-grid-deployment'
  scope: rg
  params: {
    location: location
    environment: environment
    baseName: baseName
    tags: tags
  }
}

// Event Hubs Namespace
module eventHub 'modules/event-hub.bicep' = {
  name: 'event-hub-deployment'
  scope: rg
  params: {
    location: location
    environment: environment
    baseName: baseName
    tags: tags
  }
}

// Service Bus Namespace
module serviceBus 'modules/service-bus.bicep' = {
  name: 'service-bus-deployment'
  scope: rg
  params: {
    location: location
    environment: environment
    baseName: baseName
    tags: tags
  }
}

// Azure OpenAI
module openAI 'modules/openai.bicep' = {
  name: 'openai-deployment'
  scope: rg
  params: {
    location: location
    environment: environment
    baseName: baseName
    tags: tags
  }
}

// Cosmos DB
module cosmosDb 'modules/cosmos-db.bicep' = {
  name: 'cosmos-db-deployment'
  scope: rg
  params: {
    location: location
    environment: environment
    baseName: baseName
    tags: tags
  }
}

// Azure SQL Server
module sqlServer 'modules/sql-server.bicep' = {
  name: 'sql-server-deployment'
  scope: rg
  params: {
    location: location
    environment: environment
    baseName: baseName
    tags: tags
  }
}

// Key Vault
module keyVault 'modules/key-vault.bicep' = {
  name: 'key-vault-deployment'
  scope: rg
  params: {
    location: location
    environment: environment
    baseName: baseName
    tags: tags
  }
}

// Application Insights and Monitoring
module monitoring 'modules/monitoring.bicep' = {
  name: 'monitoring-deployment'
  scope: rg
  params: {
    location: location
    environment: environment
    baseName: baseName
    tags: tags
  }
}

// Container Registry
module acr 'modules/acr.bicep' = {
  name: 'acr-deployment'
  scope: rg
  params: {
    location: location
    environment: environment
    baseName: baseName
    tags: tags
  }
}

// Outputs
output resourceGroupName string = rg.name
output aksClusterName string = aks.outputs.clusterName
output acrLoginServer string = acr.outputs.loginServer
output keyVaultUri string = keyVault.outputs.vaultUri
output eventGridEndpoint string = eventGrid.outputs.endpoint
output eventHubNamespace string = eventHub.outputs.namespaceName
output serviceBusNamespace string = serviceBus.outputs.namespaceName
