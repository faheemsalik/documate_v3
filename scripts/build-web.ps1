<#
.SYNOPSIS
  Build the Angular web app (apps/web).
.PARAMETER Configuration
  development (default) or production.
#>
param(
    [ValidateSet('development', 'production')]
    [string]$Configuration = 'development'
)

. "$PSScriptRoot\_common.ps1"
$root = Get-RepoRoot
$web = Join-Path $root 'apps/web'

if (-not (Test-Path (Join-Path $web 'node_modules'))) {
    Invoke-Npm -WorkingDirectory $web -Args @('install')
}

Invoke-Npm -WorkingDirectory $web -Args @('run', 'build', '--', "--configuration=$Configuration")
Write-Host "Web build succeeded ($Configuration)." -ForegroundColor Green
