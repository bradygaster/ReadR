@description('The location for the resource(s) to be deployed.')
param location string = resourceGroup().location

resource AzureWebJobsStorage 'Microsoft.Storage/storageAccounts@2024-01-01' = {
  name: take('azurewebjobsstorage${uniqueString(resourceGroup().id)}', 24)
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
    'aspire-resource-name': 'AzureWebJobsStorage'
  }
}

output blobEndpoint string = AzureWebJobsStorage.properties.primaryEndpoints.blob

output queueEndpoint string = AzureWebJobsStorage.properties.primaryEndpoints.queue

output tableEndpoint string = AzureWebJobsStorage.properties.primaryEndpoints.table

output name string = AzureWebJobsStorage.name