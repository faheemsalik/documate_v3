# Shared helpers for Documate v3 scripts. Dot-source from other scripts.
$ErrorActionPreference = 'Stop'

function Get-RepoRoot {
    Split-Path -Parent $PSScriptRoot
}

function Invoke-DotNet {
    param(
        [Parameter(Mandatory)]
        [string[]]$Args
    )
    Write-Host "-> dotnet $($Args -join ' ')" -ForegroundColor Cyan
    & dotnet @Args
    if ($LASTEXITCODE -ne 0) {
        throw "dotnet exited with code $LASTEXITCODE"
    }
}

function Invoke-Npm {
    param(
        [Parameter(Mandatory)]
        [string[]]$Args,
        [string]$WorkingDirectory
    )
    Push-Location $WorkingDirectory
    try {
        Write-Host "-> npm $($Args -join ' ')  (in $WorkingDirectory)" -ForegroundColor Cyan
        & npm @Args
        if ($LASTEXITCODE -ne 0) {
            throw "npm exited with code $LASTEXITCODE"
        }
    }
    finally {
        Pop-Location
    }
}
