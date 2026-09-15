<#
.SYNOPSIS
  Serve the Angular admin app (apps/admin) on http://localhost:4203
#>
. "$PSScriptRoot\_common.ps1"
$root = Get-RepoRoot
$admin = Join-Path $root 'apps/admin'

if (-not (Test-Path (Join-Path $admin 'node_modules'))) {
    Invoke-Npm -WorkingDirectory $admin -Args @('install')
}

Write-Host "Admin -> http://localhost:4203" -ForegroundColor Green
Invoke-Npm -WorkingDirectory $admin -Args @('start', '--', '--host', 'localhost', '--port', '4203')
