param(
    [string]$InstalledVersion = "0.0.1-local",
    [string]$ServerVersion    = "99.99.99-local",
    [switch]$DualPublish,
    [string]$CompatibilityUpdater
)

$ErrorActionPreference = "Stop"

$root = $PSScriptRoot
if ([string]::IsNullOrEmpty($root)) { $root = Get-Location }

. (Join-Path $root "ModdingToolBase\scripts\NbgvVersion.ps1")

$deployRoot      = Join-Path $root ".local_deploy"
$installBuildDir = Join-Path $deployRoot "bin\install"
$serverBuildDir  = Join-Path $deployRoot "bin\tool"

$toolProj   = Join-Path $root "src\DevLauncher\DevLauncher.csproj"
$baseScript = Join-Path $root "ModdingToolBase\scripts\Publish-LocalRelease.ps1"

if (Test-Path $deployRoot) { Remove-Item -Recurse -Force $deployRoot }
New-Item -ItemType Directory -Path $deployRoot | Out-Null

$nbgv = Backup-NbgvVersion -RepoRoot $root
try {
    Write-Host "--- Building DevLauncher (net481) @ installed v$InstalledVersion ---" -ForegroundColor Cyan
    Set-NbgvVersion -Snapshot $nbgv -Version $InstalledVersion
    dotnet build $toolProj --configuration Release -f net481 --output $installBuildDir /p:DebugType=None /p:DebugSymbols=false /p:LocalDeploy=true

    Write-Host "--- Building DevLauncher (net481) @ server v$ServerVersion ---" -ForegroundColor Cyan
    Set-NbgvVersion -Snapshot $nbgv -Version $ServerVersion
    dotnet build $toolProj --configuration Release -f net481 --output $serverBuildDir /p:DebugType=None /p:DebugSymbols=false /p:LocalDeploy=true

    $publishParams = @{
        AppExePath      = Join-Path $serverBuildDir "RaW-DevLauncher.exe"
        UpdaterExePath  = Join-Path $serverBuildDir "AnakinRaW.ExternalUpdater.exe"
        DeployRoot      = $deployRoot
        InstallBuildDir = $installBuildDir
        Branch          = "beta"
    }
    if ($DualPublish)          { $publishParams.DualPublish          = $true }
    if ($CompatibilityUpdater) { $publishParams.CompatibilityUpdater = $CompatibilityUpdater }

    & $baseScript @publishParams
}
finally {
    Restore-NbgvVersion -Snapshot $nbgv
}