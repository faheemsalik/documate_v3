<#
.SYNOPSIS
  Run the Documate API (apps/api) with the given launch profile.
.PARAMETER Profile
  http (default) or https — see apps/api/Properties/launchSettings.json
.PARAMETER NoBuild
  Skip build before run.
#>
param(
    [ValidateSet('http', 'https')]
    [string]$Profile = 'http',
    [switch]$NoBuild
)

. "$PSScriptRoot\_common.ps1"
$root = Get-RepoRoot
Set-Location $root

$args = @(
    'run',
    '--project', 'apps/api/Documate.Api.csproj',
    '--launch-profile', $Profile
)
if ($NoBuild) { $args += '--no-build' }

Write-Host "API -> http://localhost:5172 (profile: $Profile)" -ForegroundColor Green
Invoke-DotNet -Args $args
