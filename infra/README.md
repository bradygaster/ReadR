# Azure OpenAI Infrastructure

This directory contains the Bicep templates for deploying Azure OpenAI resources for the ReadR application.

## Files

- `main.bicep` - Main Bicep template that deploys:
  - Azure OpenAI Service with managed identity authentication
  - GPT-4o-mini model deployment
  - RBAC role assignment for accessing the service

- `main.parameters.json` - Parameters file for the Bicep template
- `deploy.ps1` - PowerShell deployment script

## Deployment

### Prerequisites

1. Azure CLI installed and logged in
2. Appropriate permissions to create resources in the target subscription
3. A resource group created for the deployment

### Quick Deployment

```powershell
# Run the deployment script
.\deploy.ps1 -ResourceGroupName "rg-readr-dev" -Location "eastus"
```

### Manual Deployment

```bash
# Get your user principal ID
$principalId = az ad signed-in-user show --query id -o tsv

# Deploy the template
az deployment group create `
  --resource-group "rg-readr-dev" `
  --template-file main.bicep `
  --parameters location="eastus" principalId=$principalId
```

## Security Considerations

- Key-based authentication is disabled (`disableLocalAuth: true`)
- Only managed identity authentication is allowed
- The deploying user is automatically granted the "Cognitive Services User" role
- Public network access is enabled but can be restricted based on requirements

## Model Deployments

The template deploys one OpenAI model:

1. **GPT-4o-mini** - Optimized for speed and cost-effectiveness
