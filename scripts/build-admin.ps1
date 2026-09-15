<#
.SYNOPSIS
  Build the Angular admin app (apps/admin).
.PARAMETER Configuration
  development (default) or production.
#>
param(
    [ValidateSet('development', 'production')]
    [string]$Configuration = 'development'
)

. "$PSScriptRoot\_common.ps1"
$root = Get-RepoRoot
$admin = Join-Path $root 'apps/admin'

if (-not (Test-Path (Join-Path $admin 'node_modules'))) {
    Invoke-Npm -WorkingDirectory $admin -Args @('install')
}

Invoke-Npm -WorkingDirectory $admin -Args @('run', 'build', '--', "--configuration=$Configuration")
Write-Host "Admin build succeeded ($Configuration)." -ForegroundColor Green
Write-Host "Output: apps/admin/dist/admin" -ForegroundColor Gray
