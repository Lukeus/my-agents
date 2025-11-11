@description('The name of the Key Vault')
param keyVaultName string

@description('The name of the Application Insights instance')
param appInsightsName string

@description('The name of the Log Analytics workspace')
param logAnalyticsWorkspaceName string

@description('Location for resources')
param location string = resourceGroup().location

@description('The tenant ID')
param tenantId string = tenant().tenantId

@description('Tags for the resource')
param tags object = {}

resource logAnalyticsWorkspace 'Microsoft.OperationalInsights/workspaces@2023-09-01' = {
  name: logAnalyticsWorkspaceName
  location: location
  tags: tags
  properties: {
    sku: {
      name: 'PerGB2018'
    }
    retentionInDays: 30
    features: {
      enableLogAccessUsingOnlyResourcePermissions: true
    }
  }
}

resource appInsights 'Microsoft.Insights/components@2020-02-02' = {
  name: appInsightsName
  location: location
  tags: tags
  kind: 'web'
  properties: {
    Application_Type: 'web'
    WorkspaceResourceId: logAnalyticsWorkspace.id
  }
}

resource keyVault 'Microsoft.KeyVault/vaults@2023-07-01' = {
  name: keyVaultName
  location: location
  tags: tags
  properties: {
    tenantId: tenantId
    sku: {
      family: 'A'
      name: 'standard'
    }
    enableRbacAuthorization: true
    enableSoftDelete: true
    softDeleteRetentionInDays: 90
    enablePurgeProtection: true
    networkAcls: {
      defaultAction: 'Allow'
      bypass: 'AzureServices'
    }
  }
}

@description('The resource ID of the Key Vault')
output keyVaultId string = keyVault.id

@description('The name of the Key Vault')
output keyVaultName string = keyVault.name

@description('The URI of the Key Vault')
output keyVaultUri string = keyVault.properties.vaultUri

@description('The resource ID of Application Insights')
output appInsightsId string = appInsights.id

@description('The instrumentation key of Application Insights')
@secure()
output appInsightsInstrumentationKey string = appInsights.properties.InstrumentationKey

@description('The connection string of Application Insights')
@secure()
output appInsightsConnectionString string = appInsights.properties.ConnectionString

@description('The resource ID of Log Analytics workspace')
output logAnalyticsWorkspaceId string = logAnalyticsWorkspace.id
