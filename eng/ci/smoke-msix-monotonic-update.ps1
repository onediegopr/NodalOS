param(
    [Parameter(Mandatory = $true)]
    [string]$PreviousCommit,
    [Parameter(Mandatory = $true)]
    [ValidatePattern('^\d+\.\d+\.\d+(?:\.\d+)?$')]
    [string]$PreviousVersion,
    [Parameter(Mandatory = $true)]
    [ValidatePattern('^\d+\.\d+\.\d+(?:\.\d+)?$')]
    [string]$CandidateVersion,
    [string]$RunnerTemp = $env:RUNNER_TEMP,
    [string]$PackageName = "NODALOS.PrivateBeta",
    [string]$BaseUrlPrevious = "http://127.0.0.1:5136",
    [string]$BaseUrlCandidate = "http://127.0.0.1:5137"
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

if ([string]::IsNullOrWhiteSpace($RunnerTemp)) {
    $RunnerTemp = Join-Path ([System.IO.Path]::GetTempPath()) "nodal-os-msix-monotonic-update"
}

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot "../..")).Path
$runId = [Guid]::NewGuid().ToString("N")
$runRoot = Join-Path $RunnerTemp "monotonic-$runId"
$previousWorktree = Join-Path $runRoot "previous-worktree"
$previousOutput = Join-Path $runRoot "previous-package"
$candidateOutput = Join-Path $runRoot "candidate-package"
$stateRoot = Join-Path $runRoot "state"
$workspaceRoot = Join-Path $runRoot "workspace"
$pfxPath = Join-Path $runRoot "monotonic-external-signing.pfx"
$pfxPassword = [Guid]::NewGuid().ToString("N") + [Guid]::NewGuid().ToString("N")
$externalCertificateThumbprint = $null
$externalCertificateTrusted = $false
$process = $null
$previousInstalled = $false
$candidateInstalled = $false
$testSignedCurrentThumbprint = $null
$testSignedOldThumbprint = $null

function Assert-Administrator {
    $identity = [Security.Principal.WindowsIdentity]::GetCurrent()
    try {
        $principal = [Security.Principal.WindowsPrincipal]::new($identity)
        if (-not $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) {
            throw "MSIX monotonic update smoke requires elevated PowerShell for local package trust/install cleanup."
        }
    }
    finally {
        $identity.Dispose()
    }
}

function Normalize-Version([string]$Value) {
    $parts = $Value.Split('.')
    if ($parts.Count -eq 3) { $parts += "0" }
    if ($parts.Count -ne 4) { throw "Version must have three or four components: $Value" }
    return ($parts -join '.')
}

function Stop-Product {
    if ($script:process -and -not $script:process.HasExited) {
        Stop-Process -Id $script:process.Id -Force -ErrorAction SilentlyContinue
        $script:process.WaitForExit(10000) | Out-Null
    }
    $script:process = $null
}

function Remove-ExistingPackage {
    Get-AppxPackage -Name $PackageName -ErrorAction SilentlyContinue |
        ForEach-Object { Remove-AppxPackage -Package $_.PackageFullName -ErrorAction SilentlyContinue }
}

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

function Wait-Html([string]$Uri, [int]$Attempts = 80) {
    for ($attempt = 0; $attempt -lt $Attempts; $attempt++) {
        try {
            return Invoke-WebRequest -Uri $Uri -TimeoutSec 3 -SessionVariable script:webSession
        }
        catch {
            Start-Sleep -Milliseconds 250
        }
    }
    throw "Timed out waiting for $Uri"
}

function Get-HiddenToken([string]$Html, [string]$FieldName) {
    $pattern = 'name="' + [regex]::Escape($FieldName) + '" value="([^"]+)"'
    $match = [regex]::Match($Html, $pattern)
    if (-not $match.Success) { throw "Could not find form token $FieldName." }
    return [System.Net.WebUtility]::HtmlDecode($match.Groups[1].Value)
}

function Invoke-LocalFormPost([string]$Uri, [hashtable]$Body, [Microsoft.PowerShell.Commands.WebRequestSession]$Session) {
    Invoke-WebRequest `
        -Uri $Uri `
        -Method Post `
        -WebSession $Session `
        -Headers @{ Origin = ([Uri]$Uri).GetLeftPart([UriPartial]::Authority) } `
        -Body $Body `
        -ContentType "application/x-www-form-urlencoded" `
        -MaximumRedirection 0 `
        -SkipHttpErrorCheck `
        -TimeoutSec 10 | Out-Null
}

function Start-InstalledProduct([string]$BaseUrl) {
    Stop-Product
    $installed = Get-AppxPackage -Name $PackageName -ErrorAction Stop
    $executable = Join-Path $installed.InstallLocation "OneBrain.Pilot.exe"
    if (-not (Test-Path -LiteralPath $executable -PathType Leaf)) {
        throw "Installed NODAL OS executable was not found."
    }

    $env:NODAL_OS_WORKSPACE_SELECTION_METADATA_PATH = Join-Path $stateRoot "workspaces/selection.v1.json"
    $env:NODAL_OS_WORKSPACE_SELECTION_SECRET_ROOT = Join-Path $stateRoot "secrets/workspace-roots"
    $env:NODAL_OS_WORKSPACE_MISSION_METADATA_PATH = Join-Path $stateRoot "missions/active.v1.json"
    New-Item -ItemType Directory -Path $stateRoot -Force | Out-Null

    $script:process = Start-Process `
        -FilePath $executable `
        -ArgumentList @("--urls", $BaseUrl, "--no-open-browser") `
        -WindowStyle Hidden `
        -PassThru
    Wait-Json "$BaseUrl/api/mission-control" | Out-Null
}

function Create-AppOwnedState([string]$BaseUrl) {
    New-Item -ItemType Directory -Path $workspaceRoot -Force | Out-Null
    Set-Content -Path (Join-Path $workspaceRoot "README.md") -Value "# NODAL OS monotonic update smoke" -Encoding utf8NoBOM

    $workspacePage = Invoke-WebRequest -Uri "$BaseUrl/workspace/select" -TimeoutSec 10 -SessionVariable session
    $workspaceToken = Get-HiddenToken $workspacePage.Content "workspaceSelectionToken"
    Invoke-LocalFormPost "$BaseUrl/workspace/select" @{
        workspaceSelectionToken = $workspaceToken
        rootPath = $workspaceRoot
        displayName = "Monotonic Update Smoke"
    } $session

    $workspace = Wait-Json "$BaseUrl/api/workspace/selection"
    if (-not $workspace.accepted -or -not $workspace.persisted -or $workspace.workspaceFilesystemMutated) {
        throw "Workspace selection was not persisted through the installed product."
    }

    $missionPage = Invoke-WebRequest -Uri "$BaseUrl/mission/new" -TimeoutSec 10 -WebSession $session
    $missionToken = Get-HiddenToken $missionPage.Content "missionDraftToken"
    Invoke-LocalFormPost "$BaseUrl/mission/new" @{
        missionDraftToken = $missionToken
        goal = "Prepare a local private-beta handoff after monotonic package update validation."
    } $session

    $mission = Wait-Json "$BaseUrl/api/mission/draft"
    if (-not $mission.accepted -or -not $mission.persisted -or $mission.workspaceFilesystemMutated -or $mission.productAuthorityGranted) {
        throw "Mission draft was not persisted through the installed product."
    }
}

function Assert-AppOwnedStatePreserved([string]$BaseUrl) {
    $workspace = Wait-Json "$BaseUrl/api/workspace/selection"
    $mission = Wait-Json "$BaseUrl/api/mission/draft"
    $missionControl = Wait-Json "$BaseUrl/api/mission-control"
    if (-not $workspace.accepted -or -not $workspace.persisted) {
        throw "Workspace state was not preserved after update."
    }
    if (-not $mission.accepted -or -not $mission.persisted) {
        throw "Mission draft state was not preserved after update."
    }
    if (-not $missionControl.workspaceSelected -or -not $missionControl.realMissionDraft -or $missionControl.productAuthorityGranted) {
        throw "Mission Control did not rehydrate preserved local state after update."
    }
}

function Assert-TeachNodalAvailable([string]$BaseUrl) {
    $teach = Wait-Json "$BaseUrl/api/teach"
    if (-not $teach.localOnly -or
        $teach.replayEnabled -or
        $teach.executionAuthorityGranted -or
        $teach.productAuthorityGranted -or
        $teach.videoStored -or
        $teach.audioStored -or
        $teach.rawInputStored) {
        throw "Teach NODAL API violated local review-only authority."
    }
    $teachPage = Invoke-WebRequest -Uri "$BaseUrl/teach" -TimeoutSec 10
    if ($teachPage.StatusCode -ne 200 -or
        $teachPage.Content -notmatch 'data-nodal-os="teach-nodal-product-surface"' -or
        $teachPage.Content -notmatch 'data-replay-enabled="false"' -or
        $teachPage.Content -notmatch 'data-execution-authority="false"' -or
        $teachPage.Content -notmatch 'data-product-authority="false"') {
        throw "Teach NODAL HTML surface was not available after update."
    }
}

function Get-BundlePaths([string]$OutputDirectory) {
    $manifest = Get-Content (Join-Path $OutputDirectory "nodal-os-update-manifest.json") -Raw | ConvertFrom-Json
    [pscustomobject]@{
        Output = $OutputDirectory
        Manifest = Join-Path $OutputDirectory "nodal-os-update-manifest.json"
        Install = Join-Path $OutputDirectory "Install-NodalOS.ps1"
        Uninstall = Join-Path $OutputDirectory "Uninstall-NodalOS.ps1"
        Msix = Join-Path $OutputDirectory $manifest.packageFile
        Certificate = if ($manifest.testCertificateFile) { Join-Path $OutputDirectory $manifest.testCertificateFile } else { $null }
        Version = [string]$manifest.version
        Publisher = [string]$manifest.publisher
        SigningMode = [string]$manifest.signingMode
    }
}

function Build-Package([string]$Root, [string]$Version, [string]$OutputDirectory, [string]$SigningPfxPath, [string]$SigningPfxPassword) {
    & (Join-Path $Root "eng/packaging/build-msix.ps1") `
        -Version $Version `
        -OutputDirectory $OutputDirectory `
        -SigningPfxPath $SigningPfxPath `
        -SigningPfxPassword $SigningPfxPassword
    if ($LASTEXITCODE -ne 0) { throw "Package build failed for $Root $Version." }
    return Get-BundlePaths $OutputDirectory
}

function Build-TestSignedPackage([string]$Root, [string]$Version, [string]$OutputDirectory) {
    & (Join-Path $Root "eng/packaging/build-msix.ps1") `
        -Version $Version `
        -OutputDirectory $OutputDirectory
    if ($LASTEXITCODE -ne 0) { throw "Test-signed package build failed for $Root $Version." }
    return Get-BundlePaths $OutputDirectory
}

function Trust-Certificate([string]$CertificatePath) {
    $cert = Import-Certificate -FilePath $CertificatePath -CertStoreLocation "Cert:\LocalMachine\TrustedPeople"
    return $cert.Thumbprint
}

function Read-CertificateThumbprint([string]$CertificatePath) {
    $cert = [System.Security.Cryptography.X509Certificates.X509Certificate2]::new($CertificatePath)
    try { return $cert.Thumbprint }
    finally { $cert.Dispose() }
}

try {
    if (-not [OperatingSystem]::IsWindows()) {
        throw "MSIX monotonic update smoke can run only on Windows."
    }
    Assert-Administrator
    $PreviousVersion = Normalize-Version $PreviousVersion
    $CandidateVersion = Normalize-Version $CandidateVersion

    New-Item -ItemType Directory -Path $runRoot -Force | Out-Null
    New-Item -ItemType Directory -Path $workspaceRoot -Force | Out-Null
    Remove-ExistingPackage

    $securePassword = ConvertTo-SecureString $pfxPassword -AsPlainText -Force
    $externalCert = New-SelfSignedCertificate `
        -Type CodeSigningCert `
        -Subject "CN=NODAL OS Private Beta" `
        -FriendlyName "NODAL OS Private Beta Monotonic Smoke External" `
        -CertStoreLocation "Cert:\CurrentUser\My" `
        -KeyAlgorithm RSA `
        -KeyLength 3072 `
        -HashAlgorithm SHA256 `
        -KeyExportPolicy Exportable `
        -NotAfter (Get-Date).AddDays(7)
    try {
        Export-PfxCertificate -Cert $externalCert -FilePath $pfxPath -Password $securePassword | Out-Null
        $publicCertPath = Join-Path $runRoot "monotonic-external-signing.cer"
        Export-Certificate -Cert $externalCert -FilePath $publicCertPath -Type CERT | Out-Null
        $externalCertificateThumbprint = $externalCert.Thumbprint
    }
    finally {
        Remove-Item "Cert:\CurrentUser\My\$($externalCert.Thumbprint)" -Force -ErrorAction SilentlyContinue
    }
    Trust-Certificate $publicCertPath | Out-Null
    $externalCertificateTrusted = $true

    git -C $repoRoot worktree add --detach $previousWorktree $PreviousCommit
    if ($LASTEXITCODE -ne 0) { throw "Could not create previous package worktree." }

    $previous = Build-Package $previousWorktree $PreviousVersion $previousOutput $pfxPath $pfxPassword
    $candidate = Build-Package $repoRoot $CandidateVersion $candidateOutput $pfxPath $pfxPassword

    & $previous.Install
    if ($LASTEXITCODE -ne 0) { throw "Previous package install failed." }
    $previousInstalled = $true
    $installed = Get-AppxPackage -Name $PackageName -ErrorAction Stop
    if ($installed.Version.ToString() -ne $PreviousVersion) {
        throw "Previous package installed version mismatch."
    }

    Start-InstalledProduct $BaseUrlPrevious
    Create-AppOwnedState $BaseUrlPrevious
    Stop-Product

    & $candidate.Install
    if ($LASTEXITCODE -ne 0) { throw "Candidate package update failed." }
    $candidateInstalled = $true
    $previousInstalled = $false
    $installed = Get-AppxPackage -Name $PackageName -ErrorAction Stop
    if ($installed.Version.ToString() -ne $CandidateVersion) {
        throw "Candidate package installed version mismatch."
    }

    Start-InstalledProduct $BaseUrlCandidate
    Assert-AppOwnedStatePreserved $BaseUrlCandidate
    Assert-TeachNodalAvailable $BaseUrlCandidate
    Stop-Product

    $downgradeRejected = $false
    try {
        Add-AppxPackage -Path $previous.Msix
    }
    catch {
        $downgradeRejected = $true
    }
    if (-not $downgradeRejected) { throw "Downgrade package install was not rejected." }
    $installed = Get-AppxPackage -Name $PackageName -ErrorAction Stop
    if ($installed.Version.ToString() -ne $CandidateVersion) {
        throw "Downgrade rejection mutated the installed package."
    }

    $sameVersionRejected = $false
    try {
        & $candidate.Install
    }
    catch {
        if ($_.Exception.Message -notmatch "Same-version reinstall is blocked") { throw }
        $sameVersionRejected = $true
    }
    if (-not $sameVersionRejected) { throw "Same-version candidate reinstall was not rejected." }
    $installed = Get-AppxPackage -Name $PackageName -ErrorAction Stop
    if ($installed.Version.ToString() -ne $CandidateVersion) {
        throw "Same-version rejection mutated the installed package."
    }

    & $candidate.Uninstall -RemoveUserData
    $candidateInstalled = $false
    if (Get-AppxPackage -Name $PackageName -ErrorAction SilentlyContinue) {
        throw "Candidate uninstall left package installed."
    }

    $testOldOutput = Join-Path $runRoot "previous-test-signed"
    $testCurrentOutput = Join-Path $runRoot "candidate-test-signed"
    $testOld = Build-TestSignedPackage $previousWorktree $PreviousVersion $testOldOutput
    $testCurrent = Build-TestSignedPackage $repoRoot $CandidateVersion $testCurrentOutput
    $testSignedOldThumbprint = Read-CertificateThumbprint $testOld.Certificate
    $testSignedCurrentThumbprint = Read-CertificateThumbprint $testCurrent.Certificate

    & $testCurrent.Install -TrustTestCertificate
    $candidateInstalled = $true
    Start-InstalledProduct $BaseUrlCandidate
    Assert-TeachNodalAvailable $BaseUrlCandidate
    Stop-Product
    & $testCurrent.Uninstall -RemoveUserData
    $candidateInstalled = $false
    if (Test-Path "Cert:\LocalMachine\TrustedPeople\$testSignedCurrentThumbprint") {
        throw "Current test-signed clean uninstall left its exact certificate trusted."
    }

    & $testOld.Install -TrustTestCertificate
    $previousInstalled = $true
    if (-not (Test-Path "Cert:\LocalMachine\TrustedPeople\$testSignedOldThumbprint")) {
        throw "Previous test-signed install did not trust its certificate."
    }
    $testSignedUpdateRejected = $false
    try {
        & $testCurrent.Install -TrustTestCertificate
    }
    catch {
        if ($_.Exception.Message -notmatch "Test-signed private-beta packages require clean uninstall before installing another signed revision") { throw }
        $testSignedUpdateRejected = $true
    }
    if (-not $testSignedUpdateRejected) {
        throw "Test-signed in-place update was not rejected."
    }
    $installed = Get-AppxPackage -Name $PackageName -ErrorAction Stop
    if ($installed.Version.ToString() -ne $PreviousVersion) {
        throw "Rejected test-signed in-place update mutated installed version."
    }
    if (Test-Path "Cert:\LocalMachine\TrustedPeople\$testSignedCurrentThumbprint") {
        throw "Rejected test-signed in-place update imported the new certificate."
    }
    if (-not (Test-Path "Cert:\LocalMachine\TrustedPeople\$testSignedOldThumbprint")) {
        throw "Rejected test-signed in-place update altered the old certificate trust."
    }

    & $testOld.Uninstall -RemoveUserData
    $previousInstalled = $false
    if (Test-Path "Cert:\LocalMachine\TrustedPeople\$testSignedOldThumbprint") {
        throw "Previous test-signed uninstall left old certificate trust."
    }

    & $testCurrent.Install -TrustTestCertificate
    $candidateInstalled = $true
    $installed = Get-AppxPackage -Name $PackageName -ErrorAction Stop
    if ($installed.Version.ToString() -ne $CandidateVersion) {
        throw "Current test-signed clean install version mismatch."
    }
    & $testCurrent.Uninstall -RemoveUserData
    $candidateInstalled = $false

    Write-Host "NODAL_OS_MSIX_MONOTONIC_UPDATE_SMOKE=PASS"
    Write-Host "NODAL_OS_MSIX_MONOTONIC_PREVIOUS=$PreviousVersion"
    Write-Host "NODAL_OS_MSIX_MONOTONIC_CANDIDATE=$CandidateVersion"
    Write-Host "NODAL_OS_MSIX_MONOTONIC_PREVIOUS_MSIX=$($previous.Msix)"
    Write-Host "NODAL_OS_MSIX_MONOTONIC_CANDIDATE_MSIX=$($candidate.Msix)"
}
finally {
    Stop-Product
    if ($candidateInstalled -or $previousInstalled) {
        Remove-ExistingPackage
    }
    foreach ($thumbprint in @($testSignedCurrentThumbprint, $testSignedOldThumbprint)) {
        if ($thumbprint -and (Test-Path "Cert:\LocalMachine\TrustedPeople\$thumbprint")) {
            Remove-Item "Cert:\LocalMachine\TrustedPeople\$thumbprint" -Force -ErrorAction SilentlyContinue
        }
    }
    if ($externalCertificateTrusted -and $externalCertificateThumbprint -and (Test-Path "Cert:\LocalMachine\TrustedPeople\$externalCertificateThumbprint")) {
        Remove-Item "Cert:\LocalMachine\TrustedPeople\$externalCertificateThumbprint" -Force -ErrorAction SilentlyContinue
    }
    if (Test-Path $previousWorktree) {
        git -C $repoRoot worktree remove --force $previousWorktree 2>$null
    }
    git -C $repoRoot worktree prune 2>$null
    if (Test-Path $runRoot) {
        Remove-Item $runRoot -Recurse -Force -ErrorAction SilentlyContinue
    }
    Remove-Item Env:\NODAL_OS_WORKSPACE_SELECTION_METADATA_PATH -ErrorAction SilentlyContinue
    Remove-Item Env:\NODAL_OS_WORKSPACE_SELECTION_SECRET_ROOT -ErrorAction SilentlyContinue
    Remove-Item Env:\NODAL_OS_WORKSPACE_MISSION_METADATA_PATH -ErrorAction SilentlyContinue
}
