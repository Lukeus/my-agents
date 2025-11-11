@description('The name of the Virtual Network')
param vnetName string

@description('Location for the VNet')
param location string = resourceGroup().location

@description('Address prefix for the VNet')
param addressPrefix string = '10.1.0.0/16'

@description('Tags for the resource')
param tags object = {}

resource vnet 'Microsoft.Network/virtualNetworks@2023-05-01' = {
  name: vnetName
  location: location
  tags: tags
  properties: {
    addressSpace: {
      addressPrefixes: [
        addressPrefix
      ]
    }
    subnets: [
      {
        name: 'aks-subnet'
        properties: {
          addressPrefix: '10.1.0.0/20'
          serviceEndpoints: [
            {
              service: 'Microsoft.Sql'
            }
            {
              service: 'Microsoft.AzureCosmosDB'
            }
            {
              service: 'Microsoft.EventHub'
            }
            {
              service: 'Microsoft.ServiceBus'
            }
          ]
        }
      }
      {
        name: 'data-subnet'
        properties: {
          addressPrefix: '10.1.16.0/24'
          serviceEndpoints: [
            {
              service: 'Microsoft.Sql'
            }
            {
              service: 'Microsoft.AzureCosmosDB'
            }
          ]
        }
      }
      {
        name: 'integration-subnet'
        properties: {
          addressPrefix: '10.1.17.0/24'
          serviceEndpoints: [
            {
              service: 'Microsoft.EventHub'
            }
            {
              service: 'Microsoft.ServiceBus'
            }
          ]
        }
      }
    ]
  }
}

@description('The resource ID of the VNet')
output vnetId string = vnet.id

@description('The name of the VNet')
output vnetName string = vnet.name

@description('The resource ID of the AKS subnet')
output aksSubnetId string = vnet.properties.subnets[0].id

@description('The resource ID of the data subnet')
output dataSubnetId string = vnet.properties.subnets[1].id

@description('The resource ID of the integration subnet')
output integrationSubnetId string = vnet.properties.subnets[2].id
