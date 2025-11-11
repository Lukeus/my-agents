@description('The name of the AKS cluster')
param clusterName string

@description('Location for the AKS cluster')
param location string = resourceGroup().location

@description('Kubernetes version')
param kubernetesVersion string = '1.28.3'

@description('DNS prefix')
param dnsPrefix string = clusterName

@description('Node count for the default node pool')
@minValue(1)
@maxValue(10)
param agentCount int = 3

@description('VM size for the default node pool')
param agentVMSize string = 'Standard_D2s_v3'

@description('Enable auto-scaling')
param enableAutoScaling bool = true

@description('Minimum node count for auto-scaling')
param minCount int = 1

@description('Maximum node count for auto-scaling')
param maxCount int = 5

@description('ACR resource ID for integration')
param acrId string

@description('Tags for the resource')
param tags object = {}

resource aks 'Microsoft.ContainerService/managedClusters@2023-10-01' = {
  name: clusterName
  location: location
  tags: tags
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    kubernetesVersion: kubernetesVersion
    dnsPrefix: dnsPrefix
    enableRBAC: true
    agentPoolProfiles: [
      {
        name: 'agentpool'
        count: agentCount
        vmSize: agentVMSize
        osType: 'Linux'
        mode: 'System'
        enableAutoScaling: enableAutoScaling
        minCount: enableAutoScaling ? minCount : null
        maxCount: enableAutoScaling ? maxCount : null
        type: 'VirtualMachineScaleSets'
        availabilityZones: [
          '1'
          '2'
          '3'
        ]
      }
    ]
    networkProfile: {
      networkPlugin: 'azure'
      networkPolicy: 'azure'
      serviceCidr: '10.0.0.0/16'
      dnsServiceIP: '10.0.0.10'
      loadBalancerSku: 'standard'
    }
    addonProfiles: {
      azureKeyvaultSecretsProvider: {
        enabled: true
      }
      omsagent: {
        enabled: false // Will be enabled with Log Analytics workspace
      }
    }
  }
}

// Grant AKS pull access to ACR
resource aksAcrPullRole 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(aks.id, acrId, 'AcrPull')
  scope: resourceGroup()
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '7f951dda-4ed3-4680-a7ca-43fe172d538d') // AcrPull role
    principalId: aks.properties.identityProfile.kubeletidentity.objectId
    principalType: 'ServicePrincipal'
  }
}

@description('The resource ID of the AKS cluster')
output aksId string = aks.id

@description('The name of the AKS cluster')
output aksName string = aks.name

@description('The FQDN of the AKS cluster')
output aksFqdn string = aks.properties.fqdn

@description('The principal ID of the AKS cluster identity')
output aksPrincipalId string = aks.identity.principalId
