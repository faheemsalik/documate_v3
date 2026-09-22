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

$outDir = Join-Path $web 'dist/web/browser'
$webConfigSrc = Join-Path $web 'public/web.config'
$webConfigDst = Join-Path $outDir 'web.config'
if (-not (Test-Path $webConfigSrc)) {
    throw "Missing $webConfigSrc — IIS deep links will 404."
}
Copy-Item $webConfigSrc $webConfigDst -Force
Write-Host "Web build succeeded ($Configuration)." -ForegroundColor Green
Write-Host "Output: $outDir" -ForegroundColor Gray
Write-Host "Deploy ALL files in that folder to the IIS site root, including web.config." -ForegroundColor Yellow
