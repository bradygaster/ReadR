targetScope = 'subscription'

@minLength(1)
@maxLength(64)
@description('Name of the environment that can be used as part of naming resource convention, the name of the resource group for your application will use this name, prefixed with rg-')
param environmentName string

@minLength(1)
@description('The location used for all deployed resources')
param location string

@description('Id of the user or app to assign application roles')
param principalId string = ''


var tags = {
  'azd-env-name': environmentName
}

resource rg 'Microsoft.Resources/resourceGroups@2022-09-01' = {
  name: environmentName
  location: location
  tags: tags
}

module AzureWebJobsStorage 'AzureWebJobsStorage/AzureWebJobsStorage.module.bicep' = {
  name: 'AzureWebJobsStorage'
  scope: rg
  params: {
    location: location
  }
}
module ReadRAppServiceEnvironment 'ReadRAppServiceEnvironment/ReadRAppServiceEnvironment.module.bicep' = {
  name: 'ReadRAppServiceEnvironment'
  scope: rg
  params: {
    location: location
    userPrincipalId: principalId
  }
}
module frontend_identity 'frontend-identity/frontend-identity.module.bicep' = {
  name: 'frontend-identity'
  scope: rg
  params: {
    location: location
  }
}
module frontend_roles_readrstorage 'frontend-roles-readrstorage/frontend-roles-readrstorage.module.bicep' = {
  name: 'frontend-roles-readrstorage'
  scope: rg
  params: {
    location: location
    principalId: frontend_identity.outputs.principalId
    readrstorage_outputs_name: readrstorage.outputs.name
  }
}
module functions_identity 'functions-identity/functions-identity.module.bicep' = {
  name: 'functions-identity'
  scope: rg
  params: {
    location: location
  }
}
module functions_roles_AzureWebJobsStorage 'functions-roles-AzureWebJobsStorage/functions-roles-AzureWebJobsStorage.module.bicep' = {
  name: 'functions-roles-AzureWebJobsStorage'
  scope: rg
  params: {
    azurewebjobsstorage_outputs_name: AzureWebJobsStorage.outputs.name
    location: location
    principalId: functions_identity.outputs.principalId
  }
}
module functions_roles_readrstorage 'functions-roles-readrstorage/functions-roles-readrstorage.module.bicep' = {
  name: 'functions-roles-readrstorage'
  scope: rg
  params: {
    location: location
    principalId: functions_identity.outputs.principalId
    readrstorage_outputs_name: readrstorage.outputs.name
  }
}
module readrstorage 'readrstorage/readrstorage.module.bicep' = {
  name: 'readrstorage'
  scope: rg
  params: {
    location: location
  }
}
output AZUREWEBJOBSSTORAGE_BLOBENDPOINT string = AzureWebJobsStorage.outputs.blobEndpoint
output AZUREWEBJOBSSTORAGE_QUEUEENDPOINT string = AzureWebJobsStorage.outputs.queueEndpoint
output AZUREWEBJOBSSTORAGE_TABLEENDPOINT string = AzureWebJobsStorage.outputs.tableEndpoint
output AZURE_CONTAINER_REGISTRY_ENDPOINT string = ReadRAppServiceEnvironment.outputs.AZURE_CONTAINER_REGISTRY_ENDPOINT
output FRONTEND_IDENTITY_CLIENTID string = frontend_identity.outputs.clientId
output FRONTEND_IDENTITY_ID string = frontend_identity.outputs.id
output FUNCTIONS_IDENTITY_CLIENTID string = functions_identity.outputs.clientId
output FUNCTIONS_IDENTITY_ID string = functions_identity.outputs.id
output READRAPPSERVICEENVIRONMENT_AZURE_CONTAINER_REGISTRY_ENDPOINT string = ReadRAppServiceEnvironment.outputs.AZURE_CONTAINER_REGISTRY_ENDPOINT
output READRAPPSERVICEENVIRONMENT_AZURE_CONTAINER_REGISTRY_MANAGED_IDENTITY_CLIENT_ID string = ReadRAppServiceEnvironment.outputs.AZURE_CONTAINER_REGISTRY_MANAGED_IDENTITY_CLIENT_ID
output READRAPPSERVICEENVIRONMENT_AZURE_CONTAINER_REGISTRY_MANAGED_IDENTITY_ID string = ReadRAppServiceEnvironment.outputs.AZURE_CONTAINER_REGISTRY_MANAGED_IDENTITY_ID
output READRAPPSERVICEENVIRONMENT_PLANID string = ReadRAppServiceEnvironment.outputs.planId
output READRSTORAGE_BLOBENDPOINT string = readrstorage.outputs.blobEndpoint
output READRSTORAGE_QUEUEENDPOINT string = readrstorage.outputs.queueEndpoint
