#Requires -Version 7.0
# Documate API - Release Build Publishing Script
# Publishes a self-contained win-x64 package for IIS (api2.documate.ai).
# Release zip lands under D:\deploy-artifacts\documate (not the product repo).

[CmdletBinding()]
param(
    [string]$RepoRoot = '',
    [string]$ArtifactsRoot = ''
)

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Documate API - Release Build Publisher" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

$scriptDir = $PSScriptRoot
if ([string]::IsNullOrWhiteSpace($RepoRoot)) {
    $RepoRoot = [System.IO.Path]::GetFullPath((Join-Path $scriptDir ".."))
}
if ([string]::IsNullOrWhiteSpace($ArtifactsRoot)) {
    $ArtifactsRoot = 'D:\deploy-artifacts\documate'
}

$RepoRoot = [IO.Path]::GetFullPath($RepoRoot)
$ArtifactsRoot = [IO.Path]::GetFullPath($ArtifactsRoot)

$projectFile = Join-Path $RepoRoot "apps\api\Documate.Api.csproj"
$configuration = "Release"
$outputWin = Join-Path $ArtifactsRoot "publish\Documate.Api"
$zipWin = Join-Path $ArtifactsRoot "Documate-Api-Windows-Release.zip"

if (-not (Test-Path -LiteralPath $projectFile)) {
    Write-Host "ERROR: Project file not found at: $projectFile" -ForegroundColor Red
    exit 1
}

Write-Host "RepoRoot: $RepoRoot" -ForegroundColor Gray
Write-Host "ArtifactsRoot: $ArtifactsRoot" -ForegroundColor Gray
Write-Host "Project: $projectFile" -ForegroundColor Green
Write-Host "Configuration: $configuration" -ForegroundColor Green
Write-Host "Target: win-x64 self-contained" -ForegroundColor Green
Write-Host ""

if (Test-Path -LiteralPath $outputWin) {
    Write-Host "Cleaning previous publish folder..." -ForegroundColor Yellow
    Remove-Item -LiteralPath $outputWin -Recurse -Force
}

New-Item -ItemType Directory -Force -Path $outputWin | Out-Null
New-Item -ItemType Directory -Force -Path $ArtifactsRoot | Out-Null

Write-Host "Publishing..." -ForegroundColor Cyan
dotnet publish $projectFile `
    -c $configuration `
    -o $outputWin `
    -r win-x64 `
    --self-contained true `
    /p:PublishSingleFile=false

if ($LASTEXITCODE -ne 0) {
    Write-Host "ERROR: Windows build failed!" -ForegroundColor Red
    exit 1
}

Write-Host "  SUCCESS: published to $outputWin" -ForegroundColor Green

if (Test-Path -LiteralPath $zipWin) {
    Remove-Item -LiteralPath $zipWin -Force
}

Write-Host "Creating deployment package..." -ForegroundColor Cyan
Compress-Archive -Path (Join-Path $outputWin '*') -DestinationPath $zipWin -Force
$size = [math]::Round((Get-Item $zipWin).Length / 1MB, 2)
Write-Host "  Created: $zipWin ($size MB)" -ForegroundColor Green

Write-Host ""
Write-Host "Release build completed successfully." -ForegroundColor Green
Write-Host "Package is self-contained — .NET 10 Hosting Bundle is not required on the IIS host." -ForegroundColor Gray
