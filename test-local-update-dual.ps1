#Requires -Version 7.0

[CmdletBinding()]
param(
    [string]$InstalledVersion    = '0.0.1-local',
    [string]$ServerVersion       = '99.99.99-local',
    [string]$Branch              = 'beta',
    [string]$CompatibilityUpdater
)

$ErrorActionPreference = 'Stop'

$root = $PSScriptRoot
if ([string]::IsNullOrEmpty($root)) { $root = Get-Location }

$deployArgs = @{
    InstalledVersion = $InstalledVersion
    ServerVersion    = $ServerVersion
    DualPublish      = $true
}
if ($CompatibilityUpdater) { $deployArgs.CompatibilityUpdater = $CompatibilityUpdater }

& (Join-Path $root 'deploy-local.ps1') @deployArgs
if ($LASTEXITCODE -ne 0) { throw "deploy-local.ps1 -DualPublish failed (exit $LASTEXITCODE)." }

$nextServerDir = Join-Path $root '.local_deploy\server\v2'
if (-not (Test-Path $nextServerDir)) {
    throw "Expected /v2/ server dir at '$nextServerDir' but it does not exist."
}
$nextServerUri = "file:///$(((Resolve-Path $nextServerDir).Path -replace '\\','/'))"

& (Join-Path $root 'ModdingToolBase\scripts\Test-LocalUpdateCycle.ps1') `
    -AppExePath         (Join-Path $root '.local_deploy\install\RaW-DevLauncher.exe') `
    -ServerUri          $nextServerUri `
    -Branch             $Branch `
    -NoUpdateMessage    'No update available.' `
    -ExpectedNewVersion $ServerVersion

exit $LASTEXITCODE