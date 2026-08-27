<#
.SYNOPSIS
  Run API unit tests (tests/api).
.PARAMETER Filter
  Optional xUnit/VSTest filter, e.g. FullyQualifiedName~Webhook
.PARAMETER NoBuild
  Skip build before test.
#>
param(
    [string]$Filter,
    [switch]$NoBuild
)

. "$PSScriptRoot\_common.ps1"
$root = Get-RepoRoot
Set-Location $root

$args = @(
    'test', 'tests/api/Documate.Api.Tests.csproj',
    '--nologo',
    '-v', 'minimal'
)
if ($NoBuild) { $args += '--no-build' }
if ($Filter) { $args += @('--filter', $Filter) }

Invoke-DotNet -Args $args
Write-Host "Tests finished." -ForegroundColor Green
