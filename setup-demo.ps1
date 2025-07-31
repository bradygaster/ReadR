# ReadR Demo Setup Script (PowerShell)
# This script prepares the demo environment for presenters by:
# 1. Cloning all phase branches to separate directories
# 2. Clearing user secrets from all projects
# 3. Setting up a clean demo environment

param(
    [string]$DemoPath = ".\demo-phases"
)

Write-Host "🚀 ReadR Demo Setup Script" -ForegroundColor Cyan
Write-Host "=========================" -ForegroundColor Cyan

# Get the current directory
$CurrentDir = Get-Location
$DemoDir = Join-Path $CurrentDir $DemoPath

# Create demo directory if it doesn't exist
if (-not (Test-Path $DemoDir)) {
    New-Item -ItemType Directory -Path $DemoDir -Force | Out-Null
    Write-Host "✅ Created demo directory: $DemoDir" -ForegroundColor Green
} else {
    Write-Host "📁 Demo directory exists: $DemoDir" -ForegroundColor Yellow
}

# Array of phase branches
$Phases = @(
    "phase1-webapp-only",
    "phase2-storage", 
    "phase3-function-blob-trigger",
    "phase4-adding-aspire",
    "phase5-deploying-with-aspire"
)

Write-Host ""
Write-Host "📋 Setting up phase directories..." -ForegroundColor Cyan

# Clone each phase to a separate directory
foreach ($phase in $Phases) {
    $PhaseDir = Join-Path $DemoDir $phase
    
    if (Test-Path $PhaseDir) {
        Write-Host "🔄 Updating existing $phase directory..." -ForegroundColor Yellow
        Set-Location $PhaseDir
        git fetch origin
        git reset --hard "origin/$phase"
        git clean -fd
    } else {
        Write-Host "📥 Cloning $phase to new directory..." -ForegroundColor Blue
        git clone -b $phase . $PhaseDir
    }
    
    Write-Host "✅ $phase ready at $PhaseDir" -ForegroundColor Green
}

Write-Host ""
Write-Host "🧹 Clearing user secrets from all projects..." -ForegroundColor Cyan

# Function to clear secrets for a project if it exists
function Clear-SecretsIfExists {
    param(
        [string]$ProjectPath
    )
    
    $ProjectName = [System.IO.Path]::GetFileNameWithoutExtension($ProjectPath)
    
    if (Test-Path $ProjectPath) {
        Write-Host "🔑 Clearing secrets for $ProjectName..." -ForegroundColor Blue
        try {
            dotnet user-secrets clear --project $ProjectPath 2>$null
        } catch {
            Write-Host "⚠️  No secrets to clear for $ProjectName" -ForegroundColor Yellow
        }
    }
}

# Clear secrets for each phase
foreach ($phase in $Phases) {
    $PhaseDir = Join-Path $DemoDir $phase
    Set-Location $PhaseDir
    
    Write-Host ""
    Write-Host "📂 Processing $phase..." -ForegroundColor Cyan
    
    # Clear secrets for projects that might exist in this phase
    Clear-SecretsIfExists "ReadR.Frontend\ReadR.Frontend.csproj"
    Clear-SecretsIfExists "ReadR.AppHost\ReadR.AppHost.csproj"
    Clear-SecretsIfExists "ReadR.Serverless\ReadR.Serverless.csproj"
    Clear-SecretsIfExists "ReadR.Functions\ReadR.Functions.csproj"
}

# Return to original directory
Set-Location $CurrentDir

Write-Host ""
Write-Host "🎯 Demo Environment Ready!" -ForegroundColor Green
Write-Host "========================" -ForegroundColor Green
Write-Host ""
Write-Host "📁 Phase directories created in: $DemoDir" -ForegroundColor Cyan
Write-Host ""
Write-Host "Available phases:" -ForegroundColor Cyan
foreach ($phase in $Phases) {
    $PhaseDir = Join-Path $DemoDir $phase
    Write-Host "  • $PhaseDir" -ForegroundColor White
}

Write-Host ""
Write-Host "🎬 Presenter Notes:" -ForegroundColor Magenta
Write-Host "=================" -ForegroundColor Magenta
Write-Host "1. Each phase is in a separate directory for easy switching" -ForegroundColor White
Write-Host "2. All user secrets have been cleared for clean demos" -ForegroundColor White
Write-Host "3. Use 'git status' in each directory to verify clean state" -ForegroundColor White
Write-Host "4. Start with phase1-webapp-only for the beginning of your demo" -ForegroundColor White
Write-Host "5. Progress through phases in order to show evolution" -ForegroundColor White
Write-Host ""
Write-Host "💡 Quick Start:" -ForegroundColor Yellow
$StartDir = Join-Path $DemoDir "phase1-webapp-only"
Write-Host "  cd `"$StartDir`"" -ForegroundColor White
Write-Host "  code ." -ForegroundColor White
Write-Host ""
Write-Host "✨ Happy presenting! ✨" -ForegroundColor Magenta