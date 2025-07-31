@description('The location for the resource(s) to be deployed.')
param location string = resourceGroup().location

resource functions_identity 'Microsoft.ManagedIdentity/userAssignedIdentities@2024-11-30' = {
  name: take('functions_identity-${uniqueString(resourceGroup().id)}', 128)
  location: location
}

output id string = functions_identity.id

output clientId string = functions_identity.properties.clientId

output principalId string = functions_identity.properties.principalId

output principalName string = functions_identity.name

output name string = functions_identity.name