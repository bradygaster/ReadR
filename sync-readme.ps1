#!/usr/bin/env pwsh

<#
.SYNOPSIS
    Syncs README.md from the main branch to all other branches in the repository.

.DESCRIPTION
    This script saves the current README.md content from the main branch and updates
    all other branches to have the same README.md content. It handles both local 
    and remote branches, ensuring consistency across the entire repository.

.PARAMETER ExcludeBranches
    Optional array of branch names to exclude from the sync operation.

.PARAMETER DryRun
    If specified, shows what would be done without making actual changes.

.EXAMPLE
    .\sync-readme.ps1
    
.EXAMPLE
    .\sync-readme.ps1 -ExcludeBranches @("experimental", "temp-branch")
    
.EXAMPLE
    .\sync-readme.ps1 -DryRun
#>

param(
    [string[]]$ExcludeBranches = @(),
    [switch]$DryRun
)

# Ensure we're in a git repository
if (-not (Test-Path ".git")) {
    Write-Error "This script must be run from the root of a git repository."
    exit 1
}

# Ensure we're on the main branch
$currentBranch = git rev-parse --abbrev-ref HEAD
if ($currentBranch -ne "main") {
    Write-Warning "You are not on the 'main' branch. Switching to main..."
    if (-not $DryRun) {
        git checkout main
        if ($LASTEXITCODE -ne 0) {
            Write-Error "Failed to switch to main branch."
            exit 1
        }
    }
}

# Check if there are uncommitted changes
$status = git status --porcelain
if ($status) {
    Write-Warning "You have uncommitted changes. Please commit or stash them before running this script."
    Write-Host "Uncommitted files:"
    $status | ForEach-Object { Write-Host "  $_" }
    exit 1
}

# Save the current README.md content from main branch
$readmePath = "README.md"
if (-not (Test-Path $readmePath)) {
    Write-Error "README.md not found in the main branch."
    exit 1
}

Write-Host "📖 Reading README.md content from main branch..." -ForegroundColor Green
$readmeContent = Get-Content $readmePath -Raw

# Get all branches (local and remote)
Write-Host "🔍 Discovering all branches..." -ForegroundColor Green
$allBranches = git branch -a | ForEach-Object { $_.Trim() } | Where-Object { 
    $_ -and 
    $_ -notmatch '^\*' -and 
    $_ -ne 'main' -and
    $_ -notmatch '^remotes/origin/HEAD' -and
    $_ -notmatch '^remotes/origin/main$'
}

# Clean up branch names and remove duplicates
$branches = @()
foreach ($branch in $allBranches) {
    $cleanBranch = $branch -replace '^remotes/origin/', ''
    $cleanBranch = $cleanBranch -replace '^\* ', ''
    
    # Skip if branch is in exclude list
    if ($ExcludeBranches -contains $cleanBranch) {
        Write-Host "⏭️  Skipping excluded branch: $cleanBranch" -ForegroundColor Yellow
        continue
    }
    
    if ($branches -notcontains $cleanBranch) {
        $branches += $cleanBranch
    }
}

if ($branches.Count -eq 0) {
    Write-Host "ℹ️  No other branches found to sync." -ForegroundColor Blue
    exit 0
}

Write-Host "📋 Found $($branches.Count) branches to sync:" -ForegroundColor Green
$branches | ForEach-Object { Write-Host "  • $_" -ForegroundColor Gray }

if ($DryRun) {
    Write-Host "`n🔍 DRY RUN MODE - No changes will be made" -ForegroundColor Magenta
    Write-Host "The following operations would be performed:"
    $branches | ForEach-Object {
        Write-Host "  - Checkout branch: $_" -ForegroundColor Gray
        Write-Host "  - Update README.md content" -ForegroundColor Gray
        Write-Host "  - Commit changes (if any)" -ForegroundColor Gray
    }
    exit 0
}

# Counter for tracking updates
$updatedBranches = 0
$skippedBranches = 0

# Process each branch
foreach ($branch in $branches) {
    Write-Host "`n🔄 Processing branch: $branch" -ForegroundColor Cyan
    
    # Checkout the branch
    Write-Host "  📂 Checking out branch..." -ForegroundColor Gray
    git checkout $branch 2>$null
    if ($LASTEXITCODE -ne 0) {
        Write-Warning "  ⚠️  Failed to checkout branch '$branch'. Skipping..."
        $skippedBranches++
        continue
    }
    
    # Check if README.md exists in this branch
    if (-not (Test-Path $readmePath)) {
        Write-Host "  ➕ README.md doesn't exist in this branch. Creating..." -ForegroundColor Yellow
        $readmeContent | Out-File -FilePath $readmePath -Encoding UTF8 -NoNewline
    } else {
        # Compare content
        $currentContent = Get-Content $readmePath -Raw -ErrorAction SilentlyContinue
        if ($currentContent -eq $readmeContent) {
            Write-Host "  ✅ README.md is already up to date" -ForegroundColor Green
            continue
        }
        
        Write-Host "  📝 Updating README.md content..." -ForegroundColor Yellow
        $readmeContent | Out-File -FilePath $readmePath -Encoding UTF8 -NoNewline
    }
    
    # Check if there are changes to commit
    $changes = git status --porcelain README.md
    if ($changes) {
        Write-Host "  💾 Committing changes..." -ForegroundColor Yellow
        git add README.md
        git commit -m "Sync README.md from main branch"
        
        if ($LASTEXITCODE -eq 0) {
            Write-Host "  ✅ Successfully updated README.md in branch '$branch'" -ForegroundColor Green
            $updatedBranches++
        } else {
            Write-Warning "  ⚠️  Failed to commit changes in branch '$branch'"
            $skippedBranches++
        }
    } else {
        Write-Host "  ℹ️  No changes detected after update" -ForegroundColor Blue
    }
}

# Return to main branch
Write-Host "`n🏠 Returning to main branch..." -ForegroundColor Green
git checkout main

# Summary
Write-Host "`n📊 Sync Summary:" -ForegroundColor Green
Write-Host "  • Branches updated: $updatedBranches" -ForegroundColor Green
Write-Host "  • Branches skipped: $skippedBranches" -ForegroundColor $(if ($skippedBranches -gt 0) { "Yellow" } else { "Green" })
Write-Host "  • Total branches processed: $($branches.Count)" -ForegroundColor Blue

if ($updatedBranches -gt 0) {
    Write-Host "`n🚀 Don't forget to push the changes to remote branches if needed:" -ForegroundColor Cyan
    Write-Host "    git push --all origin" -ForegroundColor Gray
}

Write-Host "`n✨ README.md sync completed!" -ForegroundColor Green
