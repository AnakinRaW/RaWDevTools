param(
    [string]$InstalledVersion = "0.0.1-local",
    [string]$ServerVersion    = "99.99.99-local",
    [switch]$DualPublish,
    [string]$CompatibilityUpdater,

    # Make the server's updater byte-different from the installed app's embedded copy (same version,
    # still runnable) so the cycle exercises the launch-time integrity check. See block below.
    [switch]$PerturbServerUpdater
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

    if ($PerturbServerUpdater) {
        # Make the server's updater differ byte-for-byte from the installed app's embedded copy.
        # Deterministic builds otherwise produce identical updaters, so the cycle never exercises an
        # updater whose bytes changed. Appended trailing bytes change the SHA-256 but are ignored by
        # the PE loader (the exe still runs) and leave the version untouched — i.e. a same-version
        # rebuild, which must be trusted from the signed manifest at launch.
        $serverUpdater = Join-Path $serverBuildDir "AnakinRaW.ExternalUpdater.exe"
        if (-not (Test-Path $serverUpdater)) { throw "Server external updater not found at '$serverUpdater'." }
        Write-Host "--- Perturbing server external updater (trailing bytes) to force a hash mismatch ---" -ForegroundColor Cyan
        $marker = [Text.Encoding]::ASCII.GetBytes("/*INTEGRITY-REGRESSION*/")
        $fs = [IO.File]::Open($serverUpdater, [IO.FileMode]::Append, [IO.FileAccess]::Write)
        try { $fs.Write($marker, 0, $marker.Length) } finally { $fs.Dispose() }
    }

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