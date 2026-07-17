param(
    [string]$RunnerTemp = $env:RUNNER_TEMP,
    [string]$Version = "0.1.0.0",
    [string]$BaseUrl = "http://127.0.0.1:5126"
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

if ([string]::IsNullOrWhiteSpace($RunnerTemp)) {
    $RunnerTemp = Join-Path ([System.IO.Path]::GetTempPath()) "nodal-os-desktop-ci"
}
$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot "../..")).Path
$outputRoot = Join-Path $RunnerTemp "nodal-desktop-package"
$packageName = "NODALOS.PrivateBeta"
$trustedPeopleThumbprint = $null
$process = $null

function Wait-Json([string]$Uri, [int]$Attempts = 80) {
    for ($attempt = 0; $attempt -lt $Attempts; $attempt++) {
        try {
            return Invoke-RestMethod -Uri $Uri -TimeoutSec 3
        }
        catch {
            Start-Sleep -Milliseconds 250
        }
    }
    throw "Timed out waiting for $Uri"
}

function Remove-ExistingPackage {
    Get-AppxPackage -Name $packageName -ErrorAction SilentlyContinue |
        ForEach-Object { Remove-AppxPackage -Package $_.PackageFullName -ErrorAction Stop }
}

try {
    Remove-ExistingPackage
    if (Test-Path $outputRoot) { Remove-Item $outputRoot -Recurse -Force }

    & (Join-Path $repoRoot "eng/packaging/build-msix.ps1") `
        -Version $Version `
        -OutputDirectory $outputRoot
    if ($LASTEXITCODE -ne 0) { throw "Desktop package build failed with exit code $LASTEXITCODE." }

    $updateManifestPath = Join-Path $outputRoot "nodal-os-update-manifest.json"
    $updateManifest = Get-Content $updateManifestPath -Raw | ConvertFrom-Json
    if ($updateManifest.version -ne $Version -or
        $updateManifest.packageName -ne $packageName -or
        $updateManifest.channel -ne "private-beta-manual" -or
        $updateManifest.productAuthorityGranted) {
        throw "Desktop update manifest does not match the private-beta package contract."
    }

    $msixPath = Join-Path $outputRoot $updateManifest.packageFile
    $actualHash = (Get-FileHash $msixPath -Algorithm SHA256).Hash.ToLowerInvariant()
    if ($actualHash -ne $updateManifest.sha256) {
        throw "Desktop package SHA-256 does not match the update manifest."
    }

    $certificatePath = Join-Path $outputRoot $updateManifest.testCertificateFile
    if (-not (Test-Path $certificatePath)) {
        throw "Private-beta test certificate was not emitted."
    }
    $trustedPeople = Import-Certificate -FilePath $certificatePath -CertStoreLocation "Cert:\LocalMachine\TrustedPeople"
    $trustedPeopleThumbprint = $trustedPeople.Thumbprint

    $signature = Get-AuthenticodeSignature $msixPath
    if ($signature.Status -ne [System.Management.Automation.SignatureStatus]::Valid) {
        throw "MSIX signature validation failed: $($signature.Status) $($signature.StatusMessage)"
    }

    Add-AppxPackage -Path $msixPath -ErrorAction Stop
    $installed = Get-AppxPackage -Name $packageName -ErrorAction Stop
    if (-not $installed -or $installed.Version.ToString() -ne $Version) {
        throw "Installed package identity or version is incorrect."
    }

    $buildInfoPath = Join-Path $installed.InstallLocation "nodal-build-info.json"
    $buildInfo = Get-Content $buildInfoPath -Raw | ConvertFrom-Json
    if ($buildInfo.version -ne $Version -or
        $buildInfo.channel -ne "private-beta" -or
        $buildInfo.productAuthorityGranted) {
        throw "Installed build metadata is invalid."
    }

    $executable = Join-Path $installed.InstallLocation "OneBrain.Pilot.exe"
    if (-not (Test-Path $executable)) { throw "Installed product executable was not found." }
    $process = Start-Process `
        -FilePath $executable `
        -ArgumentList @("--urls", $BaseUrl, "--no-open-browser") `
        -WindowStyle Hidden `
        -PassThru

    $mission = Wait-Json "$BaseUrl/api/mission-control"
    if (-not $mission.accepted -or
        -not $mission.localOnly -or
        -not $mission.readOnly -or
        $mission.fixtureBacked -or
        -not $mission.secretsExcluded -or
        $mission.productAuthorityGranted) {
        throw "Installed Mission Control did not preserve the local private-beta boundary."
    }
    if ($mission.missionStatus -ne "NotStarted" -or
        $mission.progressPercent -ne 0 -or
        $mission.activeModel -ne "not configured" -or
        $mission.timeline.Count -ne 0 -or
        $mission.evidenceRefs.Count -ne 0) {
        throw "Fresh installed Mission Control exposed synthetic work instead of a clean product state."
    }

    $models = Wait-Json "$BaseUrl/api/models/config"
    if ($models.configured -or
        -not $models.secretsExcluded -or
        $models.productAuthorityGranted) {
        throw "Fresh installed model configuration did not start clean and redacted."
    }

    $root = Invoke-WebRequest -Uri "$BaseUrl/" -TimeoutSec 10
    if ($root.StatusCode -ne 200 -or
        $root.Content -notmatch 'data-nodal-os="mission-control-product-shell"' -or
        $root.Content -notmatch 'data-fixture-backed="false"' -or
        $root.Content -match 'fixture-fallback-chat|Primary fixture model|Abrir laboratorio Pilot legado|CloakBrowser|BLOCKED_EXTERNAL_CLOAKBROWSER_BINARY') {
        throw "Installed desktop package did not expose the clean canonical Mission Control root."
    }

    foreach ($blockedRoute in @(
        "/pilot/legacy",
        "/recording/demo",
        "/executor-harness",
        "/runs",
        "/recipes",
        "/guia"
    )) {
        $blocked = Invoke-WebRequest -Uri "$BaseUrl$blockedRoute" -TimeoutSec 10 -SkipHttpErrorCheck
        if ($blocked.StatusCode -ne 404) {
            throw "Packaged runtime exposed non-product route $blockedRoute with status $($blocked.StatusCode)."
        }
    }
}
finally {
    if ($process -and -not $process.HasExited) {
        Stop-Process -Id $process.Id -Force -ErrorAction SilentlyContinue
        $process.WaitForExit(10000) | Out-Null
    }
    Remove-ExistingPackage
    if (Get-AppxPackage -Name $packageName -ErrorAction SilentlyContinue) {
        throw "Desktop package remained installed after uninstall."
    }
    if ($trustedPeopleThumbprint) {
        $trustedPeoplePath = "Cert:\LocalMachine\TrustedPeople\$trustedPeopleThumbprint"
        if (Test-Path $trustedPeoplePath) { Remove-Item $trustedPeoplePath -Force }
    }
    $dataRoot = Join-Path ([Environment]::GetFolderPath([Environment+SpecialFolder]::LocalApplicationData)) "NodalOS"
    if (Test-Path $dataRoot) { Remove-Item $dataRoot -Recurse -Force }
}

Write-Host "NODAL_OS_DESKTOP_PACKAGE_SMOKE=PASS"
Write-Host "NODAL_OS_DESKTOP_PACKAGE_OUTPUT=$outputRoot"
