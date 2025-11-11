@description('The name of the Azure Container Registry')
param acrName string

@description('Location for the ACR')
param location string = resourceGroup().location

@description('SKU for the ACR')
@allowed([
  'Basic'
  'Standard'
  'Premium'
])
param sku string = 'Standard'

@description('Enable admin user')
param adminUserEnabled bool = false

@description('Tags for the resource')
param tags object = {}

resource acr 'Microsoft.ContainerRegistry/registries@2023-07-01' = {
  name: acrName
  location: location
  tags: tags
  sku: {
    name: sku
  }
  properties: {
    adminUserEnabled: adminUserEnabled
    publicNetworkAccess: 'Enabled'
    networkRuleBypassOptions: 'AzureServices'
    zoneRedundancy: sku == 'Premium' ? 'Enabled' : 'Disabled'
  }
}

@description('The resource ID of the ACR')
output acrId string = acr.id

@description('The login server of the ACR')
output acrLoginServer string = acr.properties.loginServer

@description('The name of the ACR')
output acrName string = acr.name
