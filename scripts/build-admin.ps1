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

$outDir = Join-Path $admin 'dist/admin/browser'
$webConfigSrc = Join-Path $admin 'public/web.config'
$webConfigDst = Join-Path $outDir 'web.config'
if (-not (Test-Path $webConfigSrc)) {
    throw "Missing $webConfigSrc — IIS deep links will 404."
}
Copy-Item $webConfigSrc $webConfigDst -Force
Write-Host "Admin build succeeded ($Configuration)." -ForegroundColor Green
Write-Host "Output: $outDir" -ForegroundColor Gray
Write-Host "Deploy ALL files in that folder to the IIS site root, including web.config." -ForegroundColor Yellow
