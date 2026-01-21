#!/usr/bin/env pwsh
# Build script for PS3 Quick Disc Decryptor

param(
    [Parameter(Mandatory=$false)]
    [ValidateSet('Release', 'Debug')]
    [string]$Configuration = 'Release',

    [Parameter(Mandatory=$false)]
    [switch]$Clean,

    [Parameter(Mandatory=$false)]
    [switch]$Publish
)

$ErrorActionPreference = "Stop"

# Project paths
$ProjectPath = "Source/PS3 Quick Disc Decryptor/PS3 Quick Disc Decryptor.vbproj"
$SolutionPath = "Source/PS3 Quick Disc Decryptor.sln"

Write-Host "================================================" -ForegroundColor Cyan
Write-Host "  PS3 Quick Disc Decryptor - Build Script" -ForegroundColor Cyan
Write-Host "================================================" -ForegroundColor Cyan
Write-Host ""

# Check if .NET SDK is installed
Write-Host "Checking .NET SDK..." -ForegroundColor Yellow
try {
    $dotnetVersion = dotnet --version
    Write-Host "✓ .NET SDK found: $dotnetVersion" -ForegroundColor Green
}
catch {
    Write-Host "✗ .NET SDK not found!" -ForegroundColor Red
    Write-Host "Please install .NET 6.0 SDK or later from:" -ForegroundColor Yellow
    Write-Host "https://dotnet.microsoft.com/download/dotnet/6.0" -ForegroundColor Yellow
    exit 1
}

Write-Host ""

# Clean if requested
if ($Clean) {
    Write-Host "Cleaning previous build..." -ForegroundColor Yellow
    dotnet clean $SolutionPath --configuration $Configuration
    if ($LASTEXITCODE -ne 0) {
        Write-Host "✗ Clean failed!" -ForegroundColor Red
        exit 1
    }
    Write-Host "✓ Clean completed" -ForegroundColor Green
    Write-Host ""
}

# Restore dependencies
Write-Host "Restoring NuGet packages..." -ForegroundColor Yellow
dotnet restore $SolutionPath
if ($LASTEXITCODE -ne 0) {
    Write-Host "✗ Restore failed!" -ForegroundColor Red
    exit 1
}
Write-Host "✓ Restore completed" -ForegroundColor Green
Write-Host ""

# Build
Write-Host "Building project ($Configuration)..." -ForegroundColor Yellow
dotnet build $SolutionPath --configuration $Configuration --no-restore
if ($LASTEXITCODE -ne 0) {
    Write-Host "✗ Build failed!" -ForegroundColor Red
    exit 1
}
Write-Host "✓ Build completed successfully!" -ForegroundColor Green
Write-Host ""

# Publish if requested
if ($Publish) {
    Write-Host "Publishing application..." -ForegroundColor Yellow
    $publishPath = "Publish/$Configuration"
    dotnet publish $ProjectPath --configuration $Configuration --output $publishPath --no-build --self-contained false

    if ($LASTEXITCODE -ne 0) {
        Write-Host "✗ Publish failed!" -ForegroundColor Red
        exit 1
    }
    Write-Host "✓ Publish completed!" -ForegroundColor Green
    Write-Host "  Output: $publishPath" -ForegroundColor Cyan
    Write-Host ""
}

# Show output location
$outputPath = "Source/PS3 Quick Disc Decryptor/bin/$Configuration/net6.0-windows"
Write-Host "================================================" -ForegroundColor Cyan
Write-Host "Build Output Location:" -ForegroundColor Cyan
Write-Host "  $outputPath" -ForegroundColor White
Write-Host "================================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "✓ All operations completed successfully!" -ForegroundColor Green
