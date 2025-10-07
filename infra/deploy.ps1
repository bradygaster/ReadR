param(
    [Parameter(Mandatory=$true)]
    [string]$ResourceGroupName,
    
    [Parameter(Mandatory=$false)]
    [string]$Location = "eastus",
    
    [Parameter(Mandatory=$false)]
    [string]$NamePrefix = "readr",
    
    [Parameter(Mandatory=$false)]
    [string]$Environment = "dev"
)

Write-Host "Starting Azure OpenAI deployment..." -ForegroundColor Green

# Get the current user's principal ID
Write-Host "Getting current user principal ID..." -ForegroundColor Yellow
$principalId = az ad signed-in-user show --query id -o tsv

if (-not $principalId) {
    Write-Error "Failed to get current user principal ID. Make sure you're logged in to Azure CLI."
    exit 1
}

Write-Host "Principal ID: $principalId" -ForegroundColor Cyan

# Check if resource group exists
Write-Host "Checking if resource group '$ResourceGroupName' exists..." -ForegroundColor Yellow
$rgExists = az group exists --name $ResourceGroupName

if ($rgExists -eq "false") {
    Write-Host "Creating resource group '$ResourceGroupName' in location '$Location'..." -ForegroundColor Yellow
    az group create --name $ResourceGroupName --location $Location
    
    if ($LASTEXITCODE -ne 0) {
        Write-Error "Failed to create resource group."
        exit 1
    }
}

# Validate the deployment
Write-Host "Validating Bicep template..." -ForegroundColor Yellow
az deployment group validate `
    --resource-group $ResourceGroupName `
    --template-file "main.bicep" `
    --parameters location=$Location namePrefix=$NamePrefix environment=$Environment principalId=$principalId

if ($LASTEXITCODE -ne 0) {
    Write-Error "Template validation failed."
    exit 1
}

# Preview the deployment
Write-Host "Previewing deployment changes..." -ForegroundColor Yellow
az deployment group what-if `
    --resource-group $ResourceGroupName `
    --template-file "main.bicep" `
    --parameters location=$Location namePrefix=$NamePrefix environment=$Environment principalId=$principalId

# Ask for confirmation
$confirmation = Read-Host "Do you want to proceed with the deployment? (y/N)"
if ($confirmation -ne 'y' -and $confirmation -ne 'Y') {
    Write-Host "Deployment cancelled." -ForegroundColor Yellow
    exit 0
}

# Deploy the template
Write-Host "Deploying Azure OpenAI resources..." -ForegroundColor Yellow
$deploymentResult = az deployment group create `
    --resource-group $ResourceGroupName `
    --template-file "main.bicep" `
    --parameters location=$Location namePrefix=$NamePrefix environment=$Environment principalId=$principalId `
    --output json | ConvertFrom-Json

if ($LASTEXITCODE -ne 0) {
    Write-Error "Deployment failed."
    exit 1
}

# Extract outputs
$endpoint = $deploymentResult.properties.outputs.openAiEndpoint.value
$openAiName = $deploymentResult.properties.outputs.openAiName.value
$gpt4oMiniDeployment = $deploymentResult.properties.outputs.gpt4oMiniDeploymentName.value

Write-Host "`nDeployment completed successfully!" -ForegroundColor Green
Write-Host "Azure OpenAI Details:" -ForegroundColor Cyan
Write-Host "  Service Name: $openAiName" -ForegroundColor White
Write-Host "  Endpoint: $endpoint" -ForegroundColor White
Write-Host "  GPT-4o-mini Deployment: $gpt4oMiniDeployment" -ForegroundColor White

Write-Host "`nNext steps:" -ForegroundColor Yellow
Write-Host "1. Configure user secrets for your application:" -ForegroundColor White
Write-Host "   dotnet user-secrets set `"AZURE_OPENAI_ENDPOINT`" `"$endpoint`"" -ForegroundColor Gray
Write-Host "   dotnet user-secrets set `"AZURE_OPENAI_MODEL_NAME`" `"$gpt4oMiniDeployment`"" -ForegroundColor Gray
Write-Host "`n2. Your application should now be able to connect using managed identity authentication." -ForegroundColor White
