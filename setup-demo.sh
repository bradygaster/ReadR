#!/bin/bash

# ReadR Demo Setup Script
# This script prepares the demo environment for presenters by:
# 1. Cloning all phase branches to separate directories
# 2. Clearing user secrets from all projects
# 3. Setting up a clean demo environment

set -e

echo "🚀 ReadR Demo Setup Script"
echo "========================="

# Get the current directory
CURRENT_DIR=$(pwd)
DEMO_DIR="$CURRENT_DIR/demo-phases"

# Create demo directory if it doesn't exist
if [ ! -d "$DEMO_DIR" ]; then
    mkdir -p "$DEMO_DIR"
    echo "✅ Created demo directory: $DEMO_DIR"
else
    echo "📁 Demo directory exists: $DEMO_DIR"
fi

# Array of phase branches
PHASES=(
    "phase1-webapp-only"
    "phase2-storage" 
    "phase3-function-blob-trigger"
    "phase4-adding-aspire"
    "phase5-deploying-with-aspire"
)

echo ""
echo "📋 Setting up phase directories..."

# Clone each phase to a separate directory
for phase in "${PHASES[@]}"; do
    PHASE_DIR="$DEMO_DIR/$phase"
    
    if [ -d "$PHASE_DIR" ]; then
        echo "🔄 Updating existing $phase directory..."
        cd "$PHASE_DIR"
        git fetch origin
        git reset --hard "origin/$phase"
        git clean -fd
    else
        echo "📥 Cloning $phase to new directory..."
        git clone -b "$phase" . "$PHASE_DIR"
    fi
    
    echo "✅ $phase ready at $PHASE_DIR"
done

echo ""
echo "🧹 Clearing user secrets from all projects..."

# Function to clear secrets for a project if it exists
clear_secrets_if_exists() {
    local project_path="$1"
    local project_name=$(basename "$project_path" .csproj)
    
    if [ -f "$project_path" ]; then
        echo "🔑 Clearing secrets for $project_name..."
        dotnet user-secrets clear --project "$project_path" 2>/dev/null || echo "⚠️  No secrets to clear for $project_name"
    fi
}

# Clear secrets for each phase
for phase in "${PHASES[@]}"; do
    PHASE_DIR="$DEMO_DIR/$phase"
    cd "$PHASE_DIR"
    
    echo ""
    echo "📂 Processing $phase..."
    
    # Clear secrets for projects that might exist in this phase
    clear_secrets_if_exists "ReadR.Frontend/ReadR.Frontend.csproj"
    clear_secrets_if_exists "ReadR.AppHost/ReadR.AppHost.csproj"
    clear_secrets_if_exists "ReadR.Serverless/ReadR.Serverless.csproj"
    clear_secrets_if_exists "ReadR.Functions/ReadR.Functions.csproj"
done

# Return to original directory
cd "$CURRENT_DIR"

echo ""
echo "☁️ Setting up Azure environment for phase4 demo..."
echo "================================================="

# Setup Azure environment for phase4
PHASE4_DIR="$DEMO_DIR/phase4-adding-aspire"
if [ -d "$PHASE4_DIR" ]; then
    cd "$PHASE4_DIR"
    echo "🔧 Preparing Azure environment in phase4..."
    
    # Check if azd is available
    if command -v azd &> /dev/null; then
        echo "📋 Creating new azd environment..."
        azd env new readr-demo --subscription-prompt || echo "⚠️  azd env new failed or environment already exists"
        
        echo "🚀 Deploying Azure resources..."
        azd up || echo "⚠️  azd up failed - you may need to configure authentication"
        
        echo "✅ Phase4 Azure environment ready for 'azd deploy [service]' demos"
    else
        echo "⚠️  Azure Developer CLI (azd) not found. Please install azd to prepare phase4 environment."
        echo "    Visit: https://learn.microsoft.com/en-us/azure/developer/azure-developer-cli/install-azd"
    fi
    
    cd "$CURRENT_DIR"
else
    echo "⚠️  Phase4 directory not found. Skipping Azure environment setup."
fi

echo ""
echo "🎯 Demo Environment Ready!"
echo "========================"
echo ""
echo "📁 Phase directories created in: $DEMO_DIR"
echo ""
echo "Available phases:"
for phase in "${PHASES[@]}"; do
    echo "  • $DEMO_DIR/$phase"
done

echo ""
echo "🎬 Presenter Notes:"
echo "=================="
echo "1. Each phase is in a separate directory for easy switching"
echo "2. All user secrets have been cleared for clean demos"
echo "3. Azure environment has been prepared for phase4 'azd deploy [service]' demos"
echo "4. Use 'git status' in each directory to verify clean state"
echo "5. Start with phase1-webapp-only for the beginning of your demo"
echo "6. Progress through phases in order to show evolution"
echo ""
echo "💡 Quick Start:"
echo "  cd $DEMO_DIR/phase1-webapp-only"
echo "  code ."
echo ""
echo "✨ Happy presenting! ✨"