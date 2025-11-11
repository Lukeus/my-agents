@description('The name of the Event Hub namespace')
param eventHubNamespaceName string

@description('Location for Event Hub')
param location string = resourceGroup().location

@description('The SKU for the Event Hub namespace')
@allowed([
  'Basic'
  'Standard'
  'Premium'
])
param sku string = 'Standard'

@description('Tags for the resource')
param tags object = {}

resource eventHubNamespace 'Microsoft.EventHub/namespaces@2023-01-01-preview' = {
  name: eventHubNamespaceName
  location: location
  tags: tags
  sku: {
    name: sku
    tier: sku
    capacity: 1
  }
  properties: {
    minimumTlsVersion: '1.2'
    publicNetworkAccess: 'Enabled'
    zoneRedundant: false
  }
}

resource agentEventsHub 'Microsoft.EventHub/namespaces/eventhubs@2023-01-01-preview' = {
  parent: eventHubNamespace
  name: 'agent-events'
  properties: {
    messageRetentionInDays: 7
    partitionCount: 4
  }
}

@description('The resource ID of the Event Hub namespace')
output eventHubNamespaceId string = eventHubNamespace.id

@description('The name of the Event Hub namespace')
output eventHubNamespaceName string = eventHubNamespace.name

@description('The connection string for the Event Hub namespace')
@secure()
output eventHubConnectionString string = listKeys('${eventHubNamespace.id}/authorizationRules/RootManageSharedAccessKey', eventHubNamespace.apiVersion).primaryConnectionString
