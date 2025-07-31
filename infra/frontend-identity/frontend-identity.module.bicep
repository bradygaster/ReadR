@description('The location for the resource(s) to be deployed.')
param location string = resourceGroup().location

resource frontend_identity 'Microsoft.ManagedIdentity/userAssignedIdentities@2024-11-30' = {
  name: take('frontend_identity-${uniqueString(resourceGroup().id)}', 128)
  location: location
}

output id string = frontend_identity.id

output clientId string = frontend_identity.properties.clientId

output principalId string = frontend_identity.properties.principalId

output principalName string = frontend_identity.name

output name string = frontend_identity.name