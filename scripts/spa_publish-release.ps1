#Requires -Version 7.0
# Builds a customer/admin SPA zip for IIS. The zip always includes web.config
# (required so /login, /home, /dashboard work when opened directly).

[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [ValidateSet('web', 'admin')]
    [string]$App,

    [string]$RepoRoot = '',
    [string]$ArtifactsRoot = ''
)

. "$PSScriptRoot\_common.ps1"

if ([string]::IsNullOrWhiteSpace($RepoRoot)) {
    $RepoRoot = Get-RepoRoot
}
if ([string]::IsNullOrWhiteSpace($ArtifactsRoot)) {
    $ArtifactsRoot = 'D:\deploy-artifacts\documate'
}

$RepoRoot = [IO.Path]::GetFullPath($RepoRoot)
$ArtifactsRoot = [IO.Path]::GetFullPath($ArtifactsRoot)
New-Item -ItemType Directory -Force -Path $ArtifactsRoot | Out-Null

if ($App -eq 'web') {
    & (Join-Path $PSScriptRoot 'build-web.ps1') -Configuration production
    $outDir = Join-Path $RepoRoot 'apps/web/dist/web/browser'
    $zipPath = Join-Path $ArtifactsRoot 'Documate-Web-Windows-Release.zip'
}
else {
    & (Join-Path $PSScriptRoot 'build-admin.ps1') -Configuration production
    $outDir = Join-Path $RepoRoot 'apps/admin/dist/admin/browser'
    $zipPath = Join-Path $ArtifactsRoot 'Documate-Admin-Windows-Release.zip'
}

$webConfig = Join-Path $outDir 'web.config'
$indexHtml = Join-Path $outDir 'index.html'
if (-not (Test-Path $indexHtml)) {
    throw "Build output missing index.html at $outDir"
}
if (-not (Test-Path $webConfig)) {
    throw "Build output missing web.config at $outDir — aborting so IIS deep links are not deployed broken."
}

if (Test-Path $zipPath) {
    Remove-Item $zipPath -Force
}

Compress-Archive -Path (Join-Path $outDir '*') -DestinationPath $zipPath -Force
$size = [math]::Round((Get-Item $zipPath).Length / 1MB, 2)
Write-Host ""
Write-Host "Created: $zipPath ($size MB)" -ForegroundColor Green
Write-Host "Unzip to the IIS site root (the folder that already has index.html)." -ForegroundColor Yellow
Write-Host "web.config must sit next to index.html. Do not skip .config files." -ForegroundColor Yellow
