@description('The location for the resource(s) to be deployed.')
param location string = resourceGroup().location

resource readrstorage 'Microsoft.Storage/storageAccounts@2024-01-01' = {
  name: take('readrstorage${uniqueString(resourceGroup().id)}', 24)
  kind: 'StorageV2'
  location: location
  sku: {
    name: 'Standard_GRS'
  }
  properties: {
    accessTier: 'Hot'
    allowSharedKeyAccess: false
    minimumTlsVersion: 'TLS1_2'
    networkAcls: {
      defaultAction: 'Allow'
    }
  }
  tags: {
    'aspire-resource-name': 'readrstorage'
  }
}

output blobEndpoint string = readrstorage.properties.primaryEndpoints.blob

output queueEndpoint string = readrstorage.properties.primaryEndpoints.queue

output tableEndpoint string = readrstorage.properties.primaryEndpoints.table

output name string = readrstorage.name