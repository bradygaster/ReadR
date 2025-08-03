# Clone All Branches Script
# This script clones each branch of the current repository into separate directories
# Run this from within your main repository directory (e.g., c:\src\readr)

param(
    [string]$RemoteUrl = "",
    [string]$BaseDirectory = ""
)

# Get the current directory if BaseDirectory is not provided
if ([string]::IsNullOrEmpty($BaseDirectory)) {
    $BaseDirectory = Split-Path -Parent (Get-Location)
}

# Get the remote URL if not provided
if ([string]::IsNullOrEmpty($RemoteUrl)) {
    try {
        $RemoteUrl = git config --get remote.origin.url
        if ([string]::IsNullOrEmpty($RemoteUrl)) {
            Write-Error "Could not determine remote URL. Please ensure you're in a git repository or provide the -RemoteUrl parameter."
            exit 1
        }
    }
    catch {
        Write-Error "Could not determine remote URL. Please ensure you're in a git repository or provide the -RemoteUrl parameter."
        exit 1
    }
}

# Get the repository name from the current directory
$RepoName = Split-Path -Leaf (Get-Location)

Write-Host "Repository: $RepoName" -ForegroundColor Green
Write-Host "Remote URL: $RemoteUrl" -ForegroundColor Green
Write-Host "Base Directory: $BaseDirectory" -ForegroundColor Green
Write-Host ""

# Get all remote branches
Write-Host "Fetching remote branches..." -ForegroundColor Yellow
git fetch --all

# Get list of all remote branches (excluding HEAD)
$RemoteBranches = git branch -r | Where-Object { $_ -notmatch "HEAD" } | ForEach-Object { $_.Trim() -replace "origin/", "" }

Write-Host "Found branches:" -ForegroundColor Yellow
$RemoteBranches | ForEach-Object { Write-Host "  - $_" -ForegroundColor Cyan }
Write-Host ""

# Clone each branch into its own directory
foreach ($Branch in $RemoteBranches) {
    $BranchDirName = "$RepoName-$Branch"
    $BranchPath = Join-Path $BaseDirectory $BranchDirName
    
    Write-Host "Cloning branch '$Branch' into '$BranchDirName'..." -ForegroundColor Yellow
    
    # Check if directory already exists
    if (Test-Path $BranchPath) {
        Write-Host "  Directory '$BranchDirName' already exists. Skipping..." -ForegroundColor Red
        continue
    }
    
    try {
        # Clone the repository and checkout the specific branch
        git clone -b $Branch $RemoteUrl $BranchPath
        
        if ($LASTEXITCODE -eq 0) {
            Write-Host "  Successfully cloned branch '$Branch'" -ForegroundColor Green
        } else {
            Write-Host "  Failed to clone branch '$Branch'" -ForegroundColor Red
        }
    }
    catch {
        Write-Host "  Error cloning branch '$Branch': $($_.Exception.Message)" -ForegroundColor Red
    }
    
    Write-Host ""
}

Write-Host "Branch cloning completed!" -ForegroundColor Green
Write-Host ""
Write-Host "Directory structure:" -ForegroundColor Yellow
Get-ChildItem $BaseDirectory -Directory | Where-Object { $_.Name -like "$RepoName*" } | ForEach-Object {
    Write-Host "  $($_.FullName)" -ForegroundColor Cyan
}
