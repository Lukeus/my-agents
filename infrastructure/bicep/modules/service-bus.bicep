@description('The name of the Service Bus namespace')
param serviceBusNamespaceName string

@description('Location for Service Bus')
param location string = resourceGroup().location

@description('The SKU for the Service Bus namespace')
@allowed([
  'Basic'
  'Standard'
  'Premium'
])
param sku string = 'Standard'

@description('Tags for the resource')
param tags object = {}

resource serviceBusNamespace 'Microsoft.ServiceBus/namespaces@2022-10-01-preview' = {
  name: serviceBusNamespaceName
  location: location
  tags: tags
  sku: {
    name: sku
    tier: sku
  }
  properties: {
    minimumTlsVersion: '1.2'
    publicNetworkAccess: 'Enabled'
    zoneRedundant: false
  }
}

resource notificationTopic 'Microsoft.ServiceBus/namespaces/topics@2022-10-01-preview' = {
  parent: serviceBusNamespace
  name: 'notification-events'
  properties: {
    maxSizeInMegabytes: 1024
    requiresDuplicateDetection: false
    enablePartitioning: false
  }
}

resource devOpsTopic 'Microsoft.ServiceBus/namespaces/topics@2022-10-01-preview' = {
  parent: serviceBusNamespace
  name: 'devops-events'
  properties: {
    maxSizeInMegabytes: 1024
  }
}

@description('The resource ID of the Service Bus namespace')
output serviceBusNamespaceId string = serviceBusNamespace.id

@description('The name of the Service Bus namespace')
output serviceBusNamespaceName string = serviceBusNamespace.name

@description('The connection string for the Service Bus namespace')
@secure()
output serviceBusConnectionString string = listKeys('${serviceBusNamespace.id}/authorizationRules/RootManageSharedAccessKey', serviceBusNamespace.apiVersion).primaryConnectionString
