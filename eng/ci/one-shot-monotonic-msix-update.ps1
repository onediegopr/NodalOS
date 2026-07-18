param(
    [string]$RunnerTemp = $env:RUNNER_TEMP
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

if ([string]::IsNullOrWhiteSpace($RunnerTemp)) {
    $RunnerTemp = Join-Path ([System.IO.Path]::GetTempPath()) "nodal-os-monotonic-update-audit"
}

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot "../..")).Path
$auditRoot = Join-Path $RunnerTemp "nodal-monotonic-update-audit"
$evidenceRoot = Join-Path $RunnerTemp "nodal-monotonic-update-evidence"
$oldOutput = Join-Path $auditRoot "0.1.0.4"
$newOutput = Join-Path $auditRoot "0.1.0.5"
$pfxPath = Join-Path $auditRoot "monotonic-update-audit.pfx"
$cerPath = Join-Path $auditRoot "monotonic-update-audit.cer"
$packageName = "NODALOS.PrivateBeta"
$certificateSubject = "CN=NODAL OS Monotonic Update Audit"
$dataRoot = Join-Path ([Environment]::GetFolderPath([Environment+SpecialFolder]::LocalApplicationData)) "NodalOS"
$sentinelPath = Join-Path $dataRoot "ci-monotonic-update-preserve.txt"
$certificate = $null
$trustedPeopleThumbprint = $null
$password = $null
$completed = $false

function Remove-ExistingPackage {
    Get-AppxPackage -Name $packageName -ErrorAction SilentlyContinue |
        ForEach-Object { Remove-AppxPackage -Package $_.PackageFullName -ErrorAction Stop }
}

function Assert-InstalledVersion([string]$ExpectedVersion) {
    $installed = Get-AppxPackage -Name $packageName -ErrorAction Stop | Select-Object -First 1
    if (-not $installed -or $installed.Version.ToString() -ne $ExpectedVersion) {
        $actual = if ($installed) { $installed.Version.ToString() } else { "not installed" }
        throw "Expected installed NODAL OS version $ExpectedVersion, found $actual."
    }
    return $installed
}

function Assert-InstallRefused([string]$ScriptPath, [string]$ExpectedText) {
    $refused = $false
    try {
        & $ScriptPath
    }
    catch {
        if ($_.Exception.Message -notmatch [regex]::Escape($ExpectedText)) {
            throw
        }
        $refused = $true
    }
    if (-not $refused) {
        throw "Installer did not refuse the prohibited version transition."
    }
}

try {
    Remove-ExistingPackage
    if (Test-Path $auditRoot) { Remove-Item $auditRoot -Recurse -Force }
    if (Test-Path $evidenceRoot) { Remove-Item $evidenceRoot -Recurse -Force }
    if (Test-Path $dataRoot) { Remove-Item $dataRoot -Recurse -Force }
    New-Item -ItemType Directory -Path $auditRoot -Force | Out-Null
    New-Item -ItemType Directory -Path $evidenceRoot -Force | Out-Null

    $certificate = New-SelfSignedCertificate `
        -Type CodeSigningCert `
        -Subject $certificateSubject `
        -FriendlyName "NODAL OS Monotonic Update Audit" `
        -CertStoreLocation "Cert:\CurrentUser\My" `
        -KeyAlgorithm RSA `
        -KeyLength 3072 `
        -HashAlgorithm SHA256 `
        -KeyExportPolicy Exportable `
        -NotAfter (Get-Date).AddDays(2)

    $password = [Guid]::NewGuid().ToString("N") + [Guid]::NewGuid().ToString("N")
    $securePassword = ConvertTo-SecureString $password -AsPlainText -Force
    Export-PfxCertificate -Cert $certificate -FilePath $pfxPath -Password $securePassword | Out-Null
    Export-Certificate -Cert $certificate -FilePath $cerPath -Type CERT | Out-Null
    $trusted = Import-Certificate -FilePath $cerPath -CertStoreLocation "Cert:\LocalMachine\TrustedPeople" | Select-Object -First 1
    if (-not $trusted) { throw "Audit signing certificate could not be trusted temporarily." }
    $trustedPeopleThumbprint = $trusted.Thumbprint

    & (Join-Path $repoRoot "eng/packaging/build-msix.ps1") `
        -Version "0.1.0.4" `
        -OutputDirectory $oldOutput `
        -SigningPfxPath $pfxPath `
        -SigningPfxPassword $password

    & (Join-Path $repoRoot "eng/packaging/build-msix.ps1") `
        -Version "0.1.0.5" `
        -OutputDirectory $newOutput `
        -SigningPfxPath $pfxPath `
        -SigningPfxPassword $password

    $oldManifest = Get-Content (Join-Path $oldOutput "nodal-os-update-manifest.json") -Raw | ConvertFrom-Json
    $newManifest = Get-Content (Join-Path $newOutput "nodal-os-update-manifest.json") -Raw | ConvertFrom-Json
    foreach ($manifest in @($oldManifest, $newManifest)) {
        if ($manifest.signingMode -ne "external" -or
            $manifest.publisher -ne $certificateSubject -or
            $manifest.testCertificateFile -or
            $manifest.productAuthorityGranted -or
            $manifest.thirdPartyLegalApprovalGranted -or
            $manifest.updatePolicy -notmatch "strictly greater four-part version") {
            throw "Generated update manifest does not preserve the monotonic external-signing contract."
        }
    }

    $oldInstall = Join-Path $oldOutput "Install-NodalOS.ps1"
    $newInstall = Join-Path $newOutput "Install-NodalOS.ps1"
    $newUninstall = Join-Path $newOutput "Uninstall-NodalOS.ps1"

    & $oldInstall
    $oldInstalled = Assert-InstalledVersion "0.1.0.4"

    New-Item -ItemType Directory -Path $dataRoot -Force | Out-Null
    Set-Content -Path $sentinelPath -Value "preserve-across-monotonic-update" -Encoding utf8NoBOM

    & $newInstall
    $newInstalled = Assert-InstalledVersion "0.1.0.5"
    if (-not (Test-Path $sentinelPath -PathType Leaf)) {
        throw "The 0.1.0.4 to 0.1.0.5 update removed local NODAL OS data."
    }

    Assert-InstallRefused $oldInstall "not newer than the installed version"
    Assert-InstalledVersion "0.1.0.5" | Out-Null
    if (-not (Test-Path $sentinelPath -PathType Leaf)) {
        throw "Downgrade refusal changed local NODAL OS data."
    }

    Assert-InstallRefused $newInstall "not newer than the installed version"
    Assert-InstalledVersion "0.1.0.5" | Out-Null
    if (-not (Test-Path $sentinelPath -PathType Leaf)) {
        throw "Same-version refusal changed local NODAL OS data."
    }

    $evidence = [ordered]@{
        schemaVersion = 1
        product = "NODAL OS"
        packageName = $packageName
        publisher = $certificateSubject
        fromVersion = "0.1.0.4"
        toVersion = "0.1.0.5"
        signingMode = "external-ephemeral-audit"
        updateSucceeded = $true
        localDataPreserved = $true
        downgradeBlocked = $true
        sameVersionReinstallBlocked = $true
        productAuthorityGranted = $false
        publicDistributionAuthorized = $false
    }
    $evidence | ConvertTo-Json -Depth 4 | Set-Content (Join-Path $evidenceRoot "monotonic-update-evidence.json") -Encoding utf8NoBOM

    & $newUninstall -RemoveUserData
    if (Get-AppxPackage -Name $packageName -ErrorAction SilentlyContinue) {
        throw "Audit package remained installed after cleanup."
    }
    if (Test-Path $dataRoot) {
        throw "Audit local data remained after explicit cleanup."
    }

    $completed = $true
}
finally {
    Remove-ExistingPackage
    if ($trustedPeopleThumbprint) {
        $trustedPath = "Cert:\LocalMachine\TrustedPeople\$trustedPeopleThumbprint"
        if (Test-Path $trustedPath) { Remove-Item $trustedPath -Force }
    }
    if ($certificate) {
        $currentUserPath = "Cert:\CurrentUser\My\$($certificate.Thumbprint)"
        if (Test-Path $currentUserPath) { Remove-Item $currentUserPath -Force }
    }
    $password = $null
    if (Test-Path $auditRoot) { Remove-Item $auditRoot -Recurse -Force }
    if (Test-Path $dataRoot) { Remove-Item $dataRoot -Recurse -Force }
}

if (-not $completed) { throw "Monotonic MSIX update audit did not complete." }
Write-Host "NODAL_OS_MONOTONIC_MSIX_UPDATE_AUDIT=PASS"
