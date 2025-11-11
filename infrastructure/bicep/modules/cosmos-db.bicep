@description('The name of the Cosmos DB account')
param cosmosAccountName string

@description('Location for Cosmos DB')
param location string = resourceGroup().location

@description('The primary region for Cosmos DB')
param primaryRegion string = location

@description('The default consistency level')
@allowed([
  'Eventual'
  'ConsistentPrefix'
  'Session'
  'BoundedStaleness'
  'Strong'
])
param defaultConsistencyLevel string = 'Session'

@description('Tags for the resource')
param tags object = {}

resource cosmosAccount 'Microsoft.DocumentDB/databaseAccounts@2023-04-15' = {
  name: cosmosAccountName
  location: location
  tags: tags
  kind: 'GlobalDocumentDB'
  properties: {
    databaseAccountOfferType: 'Standard'
    consistencyPolicy: {
      defaultConsistencyLevel: defaultConsistencyLevel
    }
    locations: [
      {
        locationName: primaryRegion
        failoverPriority: 0
        isZoneRedundant: false
      }
    ]
    enableAutomaticFailover: false
    enableMultipleWriteLocations: false
    capabilities: [
      {
        name: 'EnableServerless'
      }
    ]
  }
}

resource cosmosDatabase 'Microsoft.DocumentDB/databaseAccounts/sqlDatabases@2023-04-15' = {
  parent: cosmosAccount
  name: 'AgentsDB'
  properties: {
    resource: {
      id: 'AgentsDB'
    }
  }
}

resource eventsContainer 'Microsoft.DocumentDB/databaseAccounts/sqlDatabases/containers@2023-04-15' = {
  parent: cosmosDatabase
  name: 'events'
  properties: {
    resource: {
      id: 'events'
      partitionKey: {
        paths: [
          '/aggregateId'
        ]
        kind: 'Hash'
      }
      indexingPolicy: {
        indexingMode: 'consistent'
        automatic: true
        includedPaths: [
          {
            path: '/*'
          }
        ]
      }
      defaultTtl: -1
    }
  }
}

resource notificationsContainer 'Microsoft.DocumentDB/databaseAccounts/sqlDatabases/containers@2023-04-15' = {
  parent: cosmosDatabase
  name: 'notifications'
  properties: {
    resource: {
      id: 'notifications'
      partitionKey: {
        paths: [
          '/id'
        ]
        kind: 'Hash'
      }
    }
  }
}

resource serviceDeskContainer 'Microsoft.DocumentDB/databaseAccounts/sqlDatabases/containers@2023-04-15' = {
  parent: cosmosDatabase
  name: 'servicedesk-tickets'
  properties: {
    resource: {
      id: 'servicedesk-tickets'
      partitionKey: {
        paths: [
          '/id'
        ]
        kind: 'Hash'
      }
    }
  }
}

resource devOpsContainer 'Microsoft.DocumentDB/databaseAccounts/sqlDatabases/containers@2023-04-15' = {
  parent: cosmosDatabase
  name: 'devops-items'
  properties: {
    resource: {
      id: 'devops-items'
      partitionKey: {
        paths: [
          '/id'
        ]
        kind: 'Hash'
      }
    }
  }
}

@description('The resource ID of the Cosmos DB account')
output cosmosAccountId string = cosmosAccount.id

@description('The name of the Cosmos DB account')
output cosmosAccountName string = cosmosAccount.name

@description('The endpoint of the Cosmos DB account')
output cosmosEndpoint string = cosmosAccount.properties.documentEndpoint

@description('The primary key of the Cosmos DB account')
@secure()
output cosmosPrimaryKey string = cosmosAccount.listKeys().primaryMasterKey
