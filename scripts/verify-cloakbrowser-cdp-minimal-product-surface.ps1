param(
    [string] $RepositoryRoot = (Resolve-Path ".").Path
)

$ErrorActionPreference = "Stop"

$DecisionReady = "NODAL_OS_CLOAKBROWSER_CDP_MINIMAL_NO_EXTENSION_PRODUCT_SURFACE_READY"
$DecisionBlocked = "NODAL_OS_CLOAKBROWSER_CDP_MINIMAL_NO_EXTENSION_PRODUCT_SURFACE_BLOCKED_WITH_CAUSE"
$SnapshotPath = Join-Path $RepositoryRoot "browser-extension/onebrain-chrome-lab/generated/cdp-status.snapshot.json"
$ArtifactsRoot = Join-Path $RepositoryRoot "artifacts/local-verification"

function New-BlockedEvidence {
    param(
        [string] $Reason,
        [string] $ErrorCode
    )

    return [ordered]@{
        schemaVersion = "1.0"
        status = "BLOCKED"
        decision = $DecisionBlocked
        reason = $Reason
        errorCode = $ErrorCode
        generatedAt = (Get-Date).ToUniversalTime().ToString("O")
        productSurface = "minimal-no-extension-runtime-bridge"
        runtimeProvider = "cloakbrowser"
        source = "cloakbrowser-cdp-direct"
        snapshotRead = $false
        metadataOnly = $true
        readOnly = $true
        extensionRequired = $false
        extensionOpened = $false
        installedSidepanelHarnessUsed = $false
        extensionUsed = $false
        systemBrowserUsed = $false
        runtimeLaunchedFromSurface = $false
        cdpLiveExecutedFromSurface = $false
        bridgeWebSocketUsed = $false
        externalNavigationBlocked = $true
        productFilesModified = $false
        fallbackUsed = $false
        rawDomStored = $false
        rawHtmlStored = $false
        inputValuesStored = $false
        cookiesOrStorageStored = $false
        secretsStored = $false
    }
}

function Get-RequiredProperty {
    param(
        [object] $Object,
        [string] $Name
    )

    $property = $Object.PSObject.Properties[$Name]
    if ($null -eq $property) {
        return $null
    }

    return $property.Value
}

function Write-SurfaceEvidence {
    param([object] $Evidence)

    New-Item -ItemType Directory -Force -Path $ArtifactsRoot | Out-Null
    $timestamp = (Get-Date).ToUniversalTime().ToString("yyyy-MM-ddTHH-mm-ss-fffZ")
    $path = Join-Path $ArtifactsRoot "cloakbrowser-cdp-minimal-product-surface-$timestamp.redacted.json"
    $Evidence | ConvertTo-Json -Depth 10 | Set-Content -LiteralPath $path -Encoding UTF8

    "status=$($Evidence.status)"
    "decision=$($Evidence.decision)"
    "reason=$($Evidence.reason)"
    "productSurface=$($Evidence.productSurface)"
    "runtimeProvider=$($Evidence.runtimeProvider)"
    "source=$($Evidence.source)"
    "snapshotRead=$($Evidence.snapshotRead)"
    "evidenceAvailable=$($Evidence.evidenceAvailable)"
    "interactiveElements=$($Evidence.interactiveElements)"
    "frictionSignals=$($Evidence.frictionSignals)"
    "actionMapEntries=$($Evidence.actionMapEntries)"
    "extensionRequired=$($Evidence.extensionRequired)"
    "extensionOpened=$($Evidence.extensionOpened)"
    "installedSidepanelHarnessUsed=$($Evidence.installedSidepanelHarnessUsed)"
    "extensionUsed=$($Evidence.extensionUsed)"
    "systemBrowserUsed=$($Evidence.systemBrowserUsed)"
    "runtimeLaunchedFromSurface=$($Evidence.runtimeLaunchedFromSurface)"
    "cdpLiveExecutedFromSurface=$($Evidence.cdpLiveExecutedFromSurface)"
    "bridgeWebSocketUsed=$($Evidence.bridgeWebSocketUsed)"
    "fallbackUsed=$($Evidence.fallbackUsed)"
    "metadataOnly=$($Evidence.metadataOnly)"
    "evidence=$path"
}

if (-not (Test-Path -LiteralPath $SnapshotPath -PathType Leaf)) {
    $blocked = New-BlockedEvidence `
        -Reason "Safe local CDP status snapshot was not found." `
        -ErrorCode "SNAPSHOT_MISSING"
    Write-SurfaceEvidence -Evidence $blocked
    exit 1
}

try {
    $snapshot = Get-Content -LiteralPath $SnapshotPath -Raw | ConvertFrom-Json
} catch {
    $blocked = New-BlockedEvidence `
        -Reason "Safe local CDP status snapshot could not be parsed." `
        -ErrorCode "SNAPSHOT_INVALID_JSON"
    Write-SurfaceEvidence -Evidence $blocked
    exit 1
}

$schemaVersion = [string] (Get-RequiredProperty $snapshot "schemaVersion")
$channel = [string] (Get-RequiredProperty $snapshot "channel")
$source = [string] (Get-RequiredProperty $snapshot "source")
$runtimeProvider = [string] (Get-RequiredProperty $snapshot "runtimeProvider")

$unsafe = [bool] (Get-RequiredProperty $snapshot "extensionUsed") `
    -or [bool] (Get-RequiredProperty $snapshot "systemBrowserUsed") `
    -or [bool] (Get-RequiredProperty $snapshot "runtimeLaunchedFromUi") `
    -or [bool] (Get-RequiredProperty $snapshot "cdpLiveExecutedFromUi") `
    -or [bool] (Get-RequiredProperty $snapshot "productFilesModified")

if ($schemaVersion -ne "1.0" -or $channel -ne "safe-local-status-snapshot" -or $source -ne "cloakbrowser-cdp-direct" -or $runtimeProvider -ne "cloakbrowser" -or $unsafe) {
    $blocked = New-BlockedEvidence `
        -Reason "Safe local CDP status snapshot failed product surface validation." `
        -ErrorCode "SNAPSHOT_UNSAFE_OR_UNSUPPORTED"
    Write-SurfaceEvidence -Evidence $blocked
    exit 1
}

$surface = [ordered]@{
    schemaVersion = "1.0"
    status = if ([bool] $snapshot.evidenceAvailable) { "PASS" } else { "MISSING_EVIDENCE" }
    decision = if ([bool] $snapshot.evidenceAvailable) { $DecisionReady } else { $DecisionBlocked }
    reason = if ([bool] $snapshot.evidenceAvailable) {
        "Minimal no-extension product surface consumed safe local CDP status snapshot."
    } else {
        "Minimal no-extension product surface loaded snapshot, but no CDP evidence was available."
    }
    generatedAt = (Get-Date).ToUniversalTime().ToString("O")
    productSurface = "minimal-no-extension-runtime-bridge"
    title = "Browser Skills CDP"
    runtimeLabel = "CloakBrowser CDP"
    runtimeProvider = "cloakbrowser"
    source = "cloakbrowser-cdp-direct"
    snapshotChannel = "safe-local-status-snapshot"
    snapshotName = "cdp-status.snapshot.json"
    snapshotGeneratedAt = [string] $snapshot.generatedAt
    snapshotFreshness = [string] $snapshot.freshness
    runtimeStatus = [string] $snapshot.runtimeStatus
    artifactHashVerified = [bool] $snapshot.artifactHashVerified
    lastHealthcheckAt = [string] $snapshot.lastHealthcheckAt
    lastSessionAt = [string] $snapshot.lastSessionAt
    lastEvidenceName = [System.IO.Path]::GetFileName([string] $snapshot.lastEvidenceName)
    evidenceAvailable = [bool] $snapshot.evidenceAvailable
    captureOk = [bool] $snapshot.captureOk
    screenshotCaptured = [bool] $snapshot.screenshotCaptured
    interactiveElements = [int] $snapshot.interactiveElements
    frictionSignals = [int] $snapshot.frictionSignals
    actionMapEntries = [int] $snapshot.actionMapEntries
    runtimeShutdown = [bool] $snapshot.runtimeShutdown
    processExited = [bool] $snapshot.processExited
    orphanProcessDetected = [bool] $snapshot.orphanProcessDetected
    snapshotRead = $true
    metadataOnly = $true
    readOnly = $true
    extensionRequired = $false
    extensionOpened = $false
    installedSidepanelHarnessUsed = $false
    extensionUsed = $false
    systemBrowserUsed = $false
    runtimeLaunchedFromSurface = $false
    cdpLiveExecutedFromSurface = $false
    bridgeWebSocketUsed = $false
    externalNavigationBlocked = $true
    productFilesModified = $false
    fallbackUsed = $false
    rawDomStored = $false
    rawHtmlStored = $false
    inputValuesStored = $false
    cookiesOrStorageStored = $false
    secretsStored = $false
}

Write-SurfaceEvidence -Evidence $surface

if ($surface.status -ne "PASS") {
    exit 1
}
