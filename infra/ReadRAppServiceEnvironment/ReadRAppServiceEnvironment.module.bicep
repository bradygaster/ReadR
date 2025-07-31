@description('The location for the resource(s) to be deployed.')
param location string = resourceGroup().location

param userPrincipalId string

param tags object = { }

resource ReadRAppServiceEnvironment_mi 'Microsoft.ManagedIdentity/userAssignedIdentities@2024-11-30' = {
  name: take('ReadRAppServiceEnvironment_mi-${uniqueString(resourceGroup().id)}', 128)
  location: location
  tags: tags
}

resource ReadRAppServiceEnvironment_acr 'Microsoft.ContainerRegistry/registries@2025-04-01' = {
  name: take('ReadRAppServiceEnvironmentacr${uniqueString(resourceGroup().id)}', 50)
  location: location
  sku: {
    name: 'Basic'
  }
  tags: tags
}

resource ReadRAppServiceEnvironment_acr_ReadRAppServiceEnvironment_mi_AcrPull 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(ReadRAppServiceEnvironment_acr.id, ReadRAppServiceEnvironment_mi.id, subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '7f951dda-4ed3-4680-a7ca-43fe172d538d'))
  properties: {
    principalId: ReadRAppServiceEnvironment_mi.properties.principalId
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '7f951dda-4ed3-4680-a7ca-43fe172d538d')
    principalType: 'ServicePrincipal'
  }
  scope: ReadRAppServiceEnvironment_acr
}

resource ReadRAppServiceEnvironment_asplan 'Microsoft.Web/serverfarms@2024-11-01' = {
  name: take('ReadRAppServiceEnvironmentasplan-${uniqueString(resourceGroup().id)}', 60)
  location: location
  properties: {
    reserved: true
  }
  kind: 'Linux'
  sku: {
    name: 'P0V3'
    tier: 'Premium'
  }
}

output name string = ReadRAppServiceEnvironment_asplan.name

output planId string = ReadRAppServiceEnvironment_asplan.id

output AZURE_CONTAINER_REGISTRY_NAME string = ReadRAppServiceEnvironment_acr.name

output AZURE_CONTAINER_REGISTRY_ENDPOINT string = ReadRAppServiceEnvironment_acr.properties.loginServer

output AZURE_CONTAINER_REGISTRY_MANAGED_IDENTITY_ID string = ReadRAppServiceEnvironment_mi.id

output AZURE_CONTAINER_REGISTRY_MANAGED_IDENTITY_CLIENT_ID string = ReadRAppServiceEnvironment_mi.properties.clientId