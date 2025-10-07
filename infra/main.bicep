@description('Location for all resources')
param location string = resourceGroup().location

@description('Name prefix for all resources')
param namePrefix string = 'readr'

@description('Environment suffix (dev, test, prod)')
param environment string = 'dev'

@description('Principal ID of the user or managed identity that will access Azure OpenAI')
param principalId string

@description('Principal type (User, ServicePrincipal, Group)')
@allowed(['User', 'ServicePrincipal', 'Group'])
param principalType string = 'User'

// Variables
var resourceName = '${namePrefix}-openai-${environment}'
var cognitiveServicesUserRoleId = 'a97b65f3-24c7-4388-baec-2e87135dc908' // Cognitive Services User role

// Azure OpenAI Service
resource openAiService 'Microsoft.CognitiveServices/accounts@2025-06-01' = {
  name: resourceName
  location: location
  kind: 'OpenAI'
  properties: {
    customSubDomainName: resourceName
    publicNetworkAccess: 'Enabled'
    disableLocalAuth: true // Disable key-based authentication to enforce managed identity
    restrictOutboundNetworkAccess: false
    networkAcls: {
      defaultAction: 'Allow'
    }
  }
  sku: {
    name: 'S0'
  }
  identity: {
    type: 'SystemAssigned'
  }
}

// GPT-4o-mini deployment
resource gpt4oMiniDeployment 'Microsoft.CognitiveServices/accounts/deployments@2025-06-01' = {
  parent: openAiService
  name: 'gpt-4o-mini'
  properties: {
    model: {
      format: 'OpenAI'
      name: 'gpt-4o-mini'
      version: '2024-07-18'
    }
    versionUpgradeOption: 'OnceNewDefaultVersionAvailable'
    raiPolicyName: 'Microsoft.Default'
  }
  sku: {
    name: 'Standard'
    capacity: 20
  }
}

// Role assignment for the principal to access Azure OpenAI
resource roleAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(openAiService.id, principalId, cognitiveServicesUserRoleId)
  scope: openAiService
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', cognitiveServicesUserRoleId)
    principalId: principalId
    principalType: principalType
  }
}

// Outputs
output openAiEndpoint string = openAiService.properties.endpoint
output openAiName string = openAiService.name
output openAiResourceId string = openAiService.id
output gpt4oMiniDeploymentName string = gpt4oMiniDeployment.name
