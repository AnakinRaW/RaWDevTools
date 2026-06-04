# =========================================================================================
# Regression scenario for the external-updater launch-time integrity check.
#
# Deploys via deploy-local.ps1 -PerturbServerUpdater (server ships an updater byte-different
# from the installed app's embedded copy, same version) and runs the shared end-to-end cycle.
# The updater installs eagerly, so it must be trusted from the signed manifest at launch.
# Pre-fix this threw "match none of the trusted hashes" at step [1/5]. Windows-only.
# =========================================================================================

#Requires -Version 7.0

[CmdletBinding()]
param(
    [string]$InstalledVersion = '0.0.1-local',
    [string]$ServerVersion    = '99.99.99-local',
    [string]$Branch           = 'beta'
)

$ErrorActionPreference = 'Stop'

$root = $PSScriptRoot
if ([string]::IsNullOrEmpty($root)) { $root = Get-Location }

& (Join-Path $root 'deploy-local.ps1') `
    -InstalledVersion $InstalledVersion `
    -ServerVersion $ServerVersion `
    -PerturbServerUpdater
if ($LASTEXITCODE -ne 0) { throw "deploy-local.ps1 -PerturbServerUpdater failed (exit $LASTEXITCODE)." }

$serverDir = Join-Path $root '.local_deploy\server'
$serverUri = "file:///$(((Resolve-Path $serverDir).Path -replace '\\','/'))"

& (Join-Path $root 'ModdingToolBase\scripts\Test-LocalUpdateCycle.ps1') `
    -AppExePath         (Join-Path $root '.local_deploy\install\RaW-DevLauncher.exe') `
    -ServerUri          $serverUri `
    -Branch             $Branch `
    -NoUpdateMessage    'No update available.' `
    -ExpectedNewVersion $ServerVersion

exit $LASTEXITCODE
