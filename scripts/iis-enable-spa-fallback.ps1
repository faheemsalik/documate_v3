#Requires -Version 5.1
<#
.SYNOPSIS
  Installs the Angular SPA fallback web.config onto an IIS site (run ON the IIS host).

.DESCRIPTION
  Direct URLs like /login 404 until IIS serves index.html for missing paths.
  This copies web.config next to index.html and unlocks httpErrors if needed.

.PARAMETER SitePhysicalPath
  IIS site folder that already contains index.html
  (e.g. C:\inetpub\app.documate.ai).

.PARAMETER WebConfigPath
  Optional path to web.config. Defaults to the matching file in this repo.
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [string]$SitePhysicalPath,

    [string]$WebConfigPath = ''
)

$ErrorActionPreference = 'Stop'

$SitePhysicalPath = [IO.Path]::GetFullPath($SitePhysicalPath)
$indexHtml = Join-Path $SitePhysicalPath 'index.html'
if (-not (Test-Path -LiteralPath $indexHtml)) {
    throw "index.html not found in $SitePhysicalPath — this is not the Angular site root."
}

if ([string]::IsNullOrWhiteSpace($WebConfigPath)) {
    $repoRoot = Split-Path -Parent $PSScriptRoot
    $candidates = @(
        (Join-Path $repoRoot 'apps/web/public/web.config'),
        (Join-Path $repoRoot 'apps/admin/public/web.config')
    )
    $WebConfigPath = $candidates | Where-Object { Test-Path $_ } | Select-Object -First 1
}

if (-not (Test-Path -LiteralPath $WebConfigPath)) {
    throw "web.config not found. Pass -WebConfigPath."
}

$dest = Join-Path $SitePhysicalPath 'web.config'
Copy-Item -LiteralPath $WebConfigPath -Destination $dest -Force
Write-Host "Wrote $dest" -ForegroundColor Green

$appcmd = Join-Path $env:windir 'system32\inetsrv\appcmd.exe'
if (Test-Path $appcmd) {
    & $appcmd unlock config /section:system.webServer/httpErrors
    Write-Host "Unlocked system.webServer/httpErrors (ignore errors if already unlocked)." -ForegroundColor Gray
}

Write-Host "Done. Test https://<host>/login — it should show the Angular app, not IIS 404." -ForegroundColor Green
