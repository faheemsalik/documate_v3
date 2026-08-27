<#
.SYNOPSIS
  Hot-reload the Documate API with `dotnet watch`.
.PARAMETER Profile
  http (default) or https.
#>
param(
    [ValidateSet('http', 'https')]
    [string]$Profile = 'http'
)

. "$PSScriptRoot\_common.ps1"
$root = Get-RepoRoot
Set-Location $root

Write-Host "Watching API -> http://localhost:5172 (profile: $Profile)" -ForegroundColor Green
Invoke-DotNet -Args @(
    'watch', 'run',
    '--project', 'apps/api/Documate.Api.csproj',
    '--launch-profile', $Profile
)
