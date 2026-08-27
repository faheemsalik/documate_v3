<#
.SYNOPSIS
  Serve the Angular web app (apps/web) on http://localhost:4200
#>
. "$PSScriptRoot\_common.ps1"
$root = Get-RepoRoot
$web = Join-Path $root 'apps/web'

if (-not (Test-Path (Join-Path $web 'node_modules'))) {
    Invoke-Npm -WorkingDirectory $web -Args @('install')
}

Write-Host "Web -> http://localhost:4200" -ForegroundColor Green
Invoke-Npm -WorkingDirectory $web -Args @('start', '--', '--host', 'localhost', '--port', '4200')
