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
$installScriptPath = Join-Path $outputRoot "Install-NodalOS.ps1"
$uninstallScriptPath = Join-Path $outputRoot "Uninstall-NodalOS.ps1"
$thirdPartyRoot = Join-Path $outputRoot "ThirdParty"
$thirdPartyNoticesPath = Join-Path $thirdPartyRoot "THIRD_PARTY_NOTICES.txt"
$thirdPartyInventoryPath = Join-Path $thirdPartyRoot "third-party-components.json"
$dataRoot = Join-Path ([Environment]::GetFolderPath([Environment+SpecialFolder]::LocalApplicationData)) "NodalOS"
$preserveSentinelPath = Join-Path $dataRoot "ci-default-uninstall-preserve.txt"
$trustedPeopleThumbprint = $null
$operatorUninstallCompleted = $false
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

    foreach ($requiredPath in @(
        $installScriptPath,
        $uninstallScriptPath,
        (Join-Path $outputRoot "README-INSTALL.txt"),
        $thirdPartyNoticesPath,
        $thirdPartyInventoryPath
    )) {
        if (-not (Test-Path $requiredPath)) {
            throw "Desktop package bundle did not emit required operator file: $requiredPath"
        }
    }

    $installScriptText = Get-Content $installScriptPath -Raw
    $uninstallScriptText = Get-Content $uninstallScriptPath -Raw
    $installReadmeText = Get-Content (Join-Path $outputRoot "README-INSTALL.txt") -Raw
    if ($installScriptText -match 'ForceUpdateFromAnyVersion' -or
        $installScriptText -notmatch 'Same-version reinstall and downgrade are blocked') {
        throw "Private-beta installer does not enforce monotonic package versions."
    }
    if ($installScriptText -notmatch 'requires an elevated PowerShell' -or
        $installScriptText -notmatch 'SHA-256' -or
        $uninstallScriptText -notmatch 'LocalMachine\\TrustedPeople' -or
        $installReadmeText -notmatch 'exact included test-certificate trust') {
        throw "Private-beta operator scripts do not verify integrity or clean up their machine-wide certificate trust."
    }

    $updateManifestPath = Join-Path $outputRoot "nodal-os-update-manifest.json"
    $updateManifest = Get-Content $updateManifestPath -Raw | ConvertFrom-Json
    if ($updateManifest.version -ne $Version -or
        $updateManifest.packageName -ne $packageName -or
        $updateManifest.channel -ne "private-beta-manual" -or
        $updateManifest.productAuthorityGranted -or
        $updateManifest.thirdPartyLegalApprovalGranted -or
        $updateManifest.thirdPartyNoticesFile -ne "ThirdParty/THIRD_PARTY_NOTICES.txt" -or
        $updateManifest.thirdPartyComponentsFile -ne "ThirdParty/third-party-components.json") {
        throw "Desktop update manifest does not match the private-beta package contract."
    }
    if ((Get-FileHash $thirdPartyNoticesPath -Algorithm SHA256).Hash.ToLowerInvariant() -ne $updateManifest.thirdPartyNoticesSha256 -or
        (Get-FileHash $thirdPartyInventoryPath -Algorithm SHA256).Hash.ToLowerInvariant() -ne $updateManifest.thirdPartyComponentsSha256) {
        throw "Third-party notice hashes do not match the update manifest."
    }
    $thirdPartyInventory = Get-Content $thirdPartyInventoryPath -Raw | ConvertFrom-Json
    if ($thirdPartyInventory.legalApprovalGranted -or
        $thirdPartyInventory.publicDistributionAuthorized -or
        -not $thirdPartyInventory.legalReviewRequired -or
        $thirdPartyInventory.componentCount -lt 1 -or
        $thirdPartyInventory.noticeFileCount -lt $thirdPartyInventory.componentCount) {
        throw "Third-party inventory overclaims legal authority or lacks component notice coverage."
    }
    foreach ($component in $thirdPartyInventory.components) {
        if (-not $component.id -or -not $component.version -or $component.noticeFiles.Count -lt 1) {
            throw "Third-party inventory contains an incomplete component."
        }
        foreach ($noticeFile in $component.noticeFiles) {
            $noticeMaterialPath = Join-Path $thirdPartyRoot $noticeFile.bundlePath
            if (-not (Test-Path -LiteralPath $noticeMaterialPath -PathType Leaf) -or
                (Get-FileHash $noticeMaterialPath -Algorithm SHA256).Hash.ToLowerInvariant() -ne $noticeFile.sha256) {
                throw "Third-party source notice is missing or changed: $($noticeFile.bundlePath)"
            }
        }
    }
    $bundlePaths = @(Get-ChildItem $outputRoot -Filter "*-private-beta.zip" -File)
    if ($bundlePaths.Count -ne 1) {
        throw "Expected exactly one private-beta bundle, found $($bundlePaths.Count)."
    }
    $bundlePath = $bundlePaths[0]
    Add-Type -AssemblyName System.IO.Compression.FileSystem
    $bundleArchive = [System.IO.Compression.ZipFile]::OpenRead($bundlePath.FullName)
    try {
        $bundleEntries = @($bundleArchive.Entries | ForEach-Object { $_.FullName.Replace("\", "/") })
        foreach ($requiredEntry in @("ThirdParty/THIRD_PARTY_NOTICES.txt", "ThirdParty/third-party-components.json")) {
            if ($bundleEntries -notcontains $requiredEntry) { throw "Private-beta bundle is missing $requiredEntry." }
        }
    }
    finally {
        $bundleArchive.Dispose()
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
    $certificate = [System.Security.Cryptography.X509Certificates.X509Certificate2]::new($certificatePath)
    try {
        $trustedPeopleThumbprint = $certificate.Thumbprint
    }
    finally {
        $certificate.Dispose()
    }

    $trustRefusalObserved = $false
    try {
        & $installScriptPath
    }
    catch {
        if ($_.Exception.Message -notmatch [regex]::Escape("-TrustTestCertificate")) {
            throw
        }
        $trustRefusalObserved = $true
    }
    if (-not $trustRefusalObserved) {
        throw "Bundled installer did not refuse a test-signed package without explicit certificate trust."
    }

    & $installScriptPath -TrustTestCertificate
    $trustedPeoplePath = "Cert:\LocalMachine\TrustedPeople\$trustedPeopleThumbprint"
    if (-not (Test-Path $trustedPeoplePath)) {
        throw "Bundled installer did not trust the expected test certificate."
    }

    $signature = Get-AuthenticodeSignature $msixPath
    if ($signature.Status -ne [System.Management.Automation.SignatureStatus]::Valid) {
        throw "MSIX signature validation failed: $($signature.Status) $($signature.StatusMessage)"
    }

    $installed = Get-AppxPackage -Name $packageName -ErrorAction Stop
    if (-not $installed -or $installed.Version.ToString() -ne $Version) {
        throw "Installed package identity or version is incorrect."
    }

    $sameVersionRefusalObserved = $false
    try {
        & $installScriptPath -TrustTestCertificate
    }
    catch {
        if ($_.Exception.Message -notmatch "not newer than the installed version") { throw }
        $sameVersionRefusalObserved = $true
    }
    if (-not $sameVersionRefusalObserved) {
        throw "Bundled installer did not block a same-version reinstall."
    }
    $installedAfterRefusal = Get-AppxPackage -Name $packageName -ErrorAction Stop
    if (-not $installedAfterRefusal -or $installedAfterRefusal.Version.ToString() -ne $Version) {
        throw "Same-version refusal changed the installed package."
    }

    $buildInfoPath = Join-Path $installed.InstallLocation "nodal-build-info.json"
    $buildInfo = Get-Content $buildInfoPath -Raw | ConvertFrom-Json
    if ($buildInfo.version -ne $Version -or
        $buildInfo.channel -ne "private-beta" -or
        $buildInfo.productAuthorityGranted) {
        throw "Installed build metadata is invalid."
    }
    $installedThirdPartyRoot = Join-Path $installed.InstallLocation "ThirdParty"
    $installedNoticesPath = Join-Path $installedThirdPartyRoot "THIRD_PARTY_NOTICES.txt"
    $installedInventoryPath = Join-Path $installedThirdPartyRoot "third-party-components.json"
    if ((Get-FileHash $installedNoticesPath -Algorithm SHA256).Hash.ToLowerInvariant() -ne $updateManifest.thirdPartyNoticesSha256 -or
        (Get-FileHash $installedInventoryPath -Algorithm SHA256).Hash.ToLowerInvariant() -ne $updateManifest.thirdPartyComponentsSha256) {
        throw "Installed third-party notices do not match the package manifest."
    }
    $installedDeps = Get-Content (Join-Path $installed.InstallLocation "OneBrain.Pilot.deps.json") -Raw | ConvertFrom-Json
    $expectedComponents = @(
        $installedDeps.libraries.PSObject.Properties |
            Where-Object { [string]$_.Value.type -in @("package", "runtimepack") } |
            ForEach-Object {
                $separator = $_.Name.LastIndexOf("/")
                $id = $_.Name.Substring(0, $separator)
                if ([string]$_.Value.type -eq "runtimepack" -and $id.StartsWith("runtimepack.", [StringComparison]::OrdinalIgnoreCase)) {
                    $id = $id.Substring("runtimepack.".Length)
                }
                "$id/$($_.Name.Substring($separator + 1))"
            } | Sort-Object -Unique
    )
    $actualComponents = @(
        $thirdPartyInventory.components | ForEach-Object { "$($_.id)/$($_.version)" } | Sort-Object -Unique
    )
    if (Compare-Object $expectedComponents $actualComponents) {
        throw "Third-party inventory does not match the installed dependency manifest."
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

    if ($process -and -not $process.HasExited) {
        Stop-Process -Id $process.Id -Force -ErrorAction Stop
        $process.WaitForExit(10000) | Out-Null
        $process = $null
    }

    & (Join-Path $repoRoot "eng/ci/smoke-installed-private-beta-core-loop.ps1") `
        -ExecutablePath $executable `
        -RunnerTemp $RunnerTemp `
        -BaseUrl "http://127.0.0.1:5127" `
        -ProviderPort 5528

    New-Item -ItemType Directory -Path $dataRoot -Force | Out-Null
    Set-Content -Path $preserveSentinelPath -Value "preserve-on-default-uninstall" -Encoding utf8NoBOM
    & $uninstallScriptPath
    $operatorUninstallCompleted = $true

    if (Get-AppxPackage -Name $packageName -ErrorAction SilentlyContinue) {
        throw "Bundled uninstaller left the package registered."
    }
    $trustedPeoplePath = "Cert:\LocalMachine\TrustedPeople\$trustedPeopleThumbprint"
    if (Test-Path $trustedPeoplePath) {
        throw "Bundled uninstaller left the private-beta test certificate trusted."
    }
    if (-not (Test-Path $preserveSentinelPath)) {
        throw "Bundled default uninstall removed local NODAL OS user data."
    }

    & $uninstallScriptPath -RemoveUserData
    if (Test-Path $dataRoot) {
        throw "Bundled uninstaller did not remove local NODAL OS user data when explicitly requested."
    }
    $trustedPeopleThumbprint = $null
}
finally {
    if ($process -and -not $process.HasExited) {
        Stop-Process -Id $process.Id -Force -ErrorAction SilentlyContinue
        $process.WaitForExit(10000) | Out-Null
    }

    if (-not $operatorUninstallCompleted) {
        Remove-ExistingPackage
    }
    if (Get-AppxPackage -Name $packageName -ErrorAction SilentlyContinue) {
        throw "Desktop package remained installed after cleanup."
    }
    if ($trustedPeopleThumbprint) {
        $trustedPeoplePath = "Cert:\LocalMachine\TrustedPeople\$trustedPeopleThumbprint"
        if (Test-Path $trustedPeoplePath) { Remove-Item $trustedPeoplePath -Force }
    }
    if (Test-Path $dataRoot) { Remove-Item $dataRoot -Recurse -Force }
}

Write-Host "NODAL_OS_DESKTOP_PACKAGE_SMOKE=PASS"
Write-Host "NODAL_OS_DESKTOP_PACKAGE_OUTPUT=$outputRoot"
