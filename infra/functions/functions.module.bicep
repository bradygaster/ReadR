@description('The location for the resource(s) to be deployed.')
param location string = resourceGroup().location

param readrappserviceenvironment_outputs_azure_container_registry_endpoint string

param readrappserviceenvironment_outputs_planid string

param readrappserviceenvironment_outputs_azure_container_registry_managed_identity_id string

param readrappserviceenvironment_outputs_azure_container_registry_managed_identity_client_id string

param functions_containerimage string

param azurewebjobsstorage_outputs_blobendpoint string

param azurewebjobsstorage_outputs_queueendpoint string

param azurewebjobsstorage_outputs_tableendpoint string

param readrstorage_outputs_queueendpoint string

param readrstorage_outputs_blobendpoint string

param functions_identity_outputs_id string

param functions_identity_outputs_clientid string

resource mainContainer 'Microsoft.Web/sites/sitecontainers@2024-11-01' = {
  name: 'main'
  properties: {
    authType: 'UserAssigned'
    image: functions_containerimage
    isMain: true
    userManagedIdentityClientId: readrappserviceenvironment_outputs_azure_container_registry_managed_identity_client_id
  }
  parent: webapp
}

resource webapp 'Microsoft.Web/sites@2024-11-01' = {
  name: take('${toLower('functions')}-${uniqueString(resourceGroup().id)}', 60)
  location: location
  kind: 'functionapp,linux'
  properties: {
    serverFarmId: readrappserviceenvironment_outputs_planid
    keyVaultReferenceIdentity: functions_identity_outputs_id
    siteConfig: {
      linuxFxVersion: 'SITECONTAINERS'
      acrUseManagedIdentityCreds: true
      acrUserManagedIdentityID: readrappserviceenvironment_outputs_azure_container_registry_managed_identity_client_id
      appSettings: [
        {
          name: 'OTEL_DOTNET_EXPERIMENTAL_OTLP_EMIT_EXCEPTION_LOG_ATTRIBUTES'
          value: 'true'
        }
        {
          name: 'OTEL_DOTNET_EXPERIMENTAL_OTLP_EMIT_EVENT_LOG_ATTRIBUTES'
          value: 'true'
        }
        {
          name: 'OTEL_DOTNET_EXPERIMENTAL_OTLP_RETRY'
          value: 'in_memory'
        }
        {
          name: 'ASPNETCORE_FORWARDEDHEADERS_ENABLED'
          value: 'true'
        }
        {
          name: 'FUNCTIONS_WORKER_RUNTIME'
          value: 'dotnet-isolated'
        }
        {
          name: 'AzureFunctionsJobHost__telemetryMode'
          value: 'OpenTelemetry'
        }
        {
          name: 'ASPNETCORE_URLS'
          value: 'http://+:8080'
        }
        {
          name: 'AzureWebJobsStorage__blobServiceUri'
          value: azurewebjobsstorage_outputs_blobendpoint
        }
        {
          name: 'AzureWebJobsStorage__queueServiceUri'
          value: azurewebjobsstorage_outputs_queueendpoint
        }
        {
          name: 'AzureWebJobsStorage__tableServiceUri'
          value: azurewebjobsstorage_outputs_tableendpoint
        }
        {
          name: 'Aspire__Azure__Storage__Blobs__AzureWebJobsStorage__ServiceUri'
          value: azurewebjobsstorage_outputs_blobendpoint
        }
        {
          name: 'Aspire__Azure__Storage__Queues__AzureWebJobsStorage__ServiceUri'
          value: azurewebjobsstorage_outputs_queueendpoint
        }
        {
          name: 'Aspire__Azure__Data__Tables__AzureWebJobsStorage__ServiceUri'
          value: azurewebjobsstorage_outputs_tableendpoint
        }
        {
          name: 'queues__queueServiceUri'
          value: readrstorage_outputs_queueendpoint
        }
        {
          name: 'Aspire__Azure__Storage__Queues__queues__ServiceUri'
          value: readrstorage_outputs_queueendpoint
        }
        {
          name: 'blobs__blobServiceUri'
          value: readrstorage_outputs_blobendpoint
        }
        {
          name: 'blobs__queueServiceUri'
          value: readrstorage_outputs_queueendpoint
        }
        {
          name: 'Aspire__Azure__Storage__Blobs__blobs__ServiceUri'
          value: readrstorage_outputs_blobendpoint
        }
        {
          name: 'AZURE_CLIENT_ID'
          value: functions_identity_outputs_clientid
        }
      ]
    }
  }
  identity: {
    type: 'UserAssigned'
    userAssignedIdentities: {
      '${readrappserviceenvironment_outputs_azure_container_registry_managed_identity_id}': { }
      '${functions_identity_outputs_id}': { }
    }
  }
}
