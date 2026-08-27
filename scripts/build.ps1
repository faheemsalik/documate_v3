<#
.SYNOPSIS
  Build the Documate.slnx solution (API + tests).
.PARAMETER Configuration
  Debug (default) or Release.
.PARAMETER Restore
  Run restore before build.
#>
param(
    [ValidateSet('Debug', 'Release')]
    [string]$Configuration = 'Debug',
    [switch]$Restore
)

. "$PSScriptRoot\_common.ps1"
$root = Get-RepoRoot
Set-Location $root

if ($Restore) {
    Invoke-DotNet -Args @('restore', 'Documate.slnx')
}

Invoke-DotNet -Args @(
    'build', 'Documate.slnx',
    '-c', $Configuration,
    '--nologo',
    '-v', 'minimal'
)

Write-Host "Build succeeded ($Configuration)." -ForegroundColor Green
