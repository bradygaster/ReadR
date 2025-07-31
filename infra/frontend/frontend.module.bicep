@description('The location for the resource(s) to be deployed.')
param location string = resourceGroup().location

param readrappserviceenvironment_outputs_azure_container_registry_endpoint string

param readrappserviceenvironment_outputs_planid string

param readrappserviceenvironment_outputs_azure_container_registry_managed_identity_id string

param readrappserviceenvironment_outputs_azure_container_registry_managed_identity_client_id string

param frontend_containerimage string

param frontend_containerport string

param readrstorage_outputs_queueendpoint string

param readrstorage_outputs_blobendpoint string

param frontend_identity_outputs_id string

param frontend_identity_outputs_clientid string

resource mainContainer 'Microsoft.Web/sites/sitecontainers@2024-11-01' = {
  name: 'main'
  properties: {
    authType: 'UserAssigned'
    image: frontend_containerimage
    isMain: true
    userManagedIdentityClientId: readrappserviceenvironment_outputs_azure_container_registry_managed_identity_client_id
  }
  parent: webapp
}

resource webapp 'Microsoft.Web/sites@2024-11-01' = {
  name: take('${toLower('frontend')}-${uniqueString(resourceGroup().id)}', 60)
  location: location
  properties: {
    serverFarmId: readrappserviceenvironment_outputs_planid
    keyVaultReferenceIdentity: frontend_identity_outputs_id
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
          name: 'HTTP_PORTS'
          value: frontend_containerport
        }
        {
          name: 'ConnectionStrings__queues'
          value: readrstorage_outputs_queueendpoint
        }
        {
          name: 'ConnectionStrings__blobs'
          value: readrstorage_outputs_blobendpoint
        }
        {
          name: 'AZURE_CLIENT_ID'
          value: frontend_identity_outputs_clientid
        }
      ]
    }
  }
  identity: {
    type: 'UserAssigned'
    userAssignedIdentities: {
      '${readrappserviceenvironment_outputs_azure_container_registry_managed_identity_id}': { }
      '${frontend_identity_outputs_id}': { }
    }
  }
}