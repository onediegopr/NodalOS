param(
    [string] $RepositoryRoot = (Resolve-Path ".").Path
)

$ErrorActionPreference = "Stop"

$DecisionReady = "NODAL_OS_CLOAKBROWSER_CDP_EXTENSION_DEPRECATION_HARDENED_FINAL"
$DecisionBlocked = "NODAL_OS_CLOAKBROWSER_CDP_EXTENSION_DEPRECATION_HARDENING_BLOCKED_WITH_CAUSE"
$ArtifactsRoot = Join-Path $RepositoryRoot "artifacts/local-verification"
$LockfilePath = Join-Path $RepositoryRoot "browser-runtime.lock.json"
$InstalledSidepanelHarnessPath = Join-Path $RepositoryRoot "scripts/verify-installed-sidepanel.mjs"

function Find-LatestRedactedEvidence {
    param([string] $Pattern)

    if (-not (Test-Path -LiteralPath $ArtifactsRoot -PathType Container)) {
        return $null
    }

    return Get-ChildItem -LiteralPath $ArtifactsRoot -Filter $Pattern -File |
        Where-Object { $_.Name.EndsWith(".redacted.json", [System.StringComparison]::OrdinalIgnoreCase) } |
        Sort-Object LastWriteTimeUtc -Descending |
        Select-Object -First 1
}

function Read-JsonOrNull {
    param([System.IO.FileInfo] $File)

    if ($null -eq $File) {
        return $null
    }

    return Get-Content -LiteralPath $File.FullName -Raw | ConvertFrom-Json
}

function Get-Bool {
    param(
        [object] $Object,
        [string] $Name,
        [bool] $Default = $false
    )

    if ($null -eq $Object) {
        return $Default
    }

    $property = $Object.PSObject.Properties[$Name]
    if ($null -eq $property) {
        return $Default
    }

    return [bool] $property.Value
}

function Get-String {
    param(
        [object] $Object,
        [string] $Name,
        [string] $Default = ""
    )

    if ($null -eq $Object) {
        return $Default
    }

    $property = $Object.PSObject.Properties[$Name]
    if ($null -eq $property -or $null -eq $property.Value) {
        return $Default
    }

    return [string] $property.Value
}

function Add-Error {
    param(
        [System.Collections.Generic.List[string]] $Errors,
        [string] $Code,
        [bool] $Condition
    )

    if (-not $Condition) {
        $Errors.Add($Code)
    }
}

function New-EvidenceName {
    param([System.IO.FileInfo] $File)

    if ($null -eq $File) {
        return $null
    }

    return $File.Name
}

if (-not (Test-Path -LiteralPath $LockfilePath -PathType Leaf)) {
    throw "browser-runtime.lock.json not found."
}

$lock = Get-Content -LiteralPath $LockfilePath -Raw | ConvertFrom-Json
$sidepanelHarness = if (Test-Path -LiteralPath $InstalledSidepanelHarnessPath -PathType Leaf) {
    Get-Content -LiteralPath $InstalledSidepanelHarnessPath -Raw
} else {
    ""
}

$defaultEvidenceFile = Find-LatestRedactedEvidence "cloakbrowser-cdp-no-extension-default-*.redacted.json"
$surfaceEvidenceFile = Find-LatestRedactedEvidence "cloakbrowser-cdp-minimal-product-surface-*.redacted.json"
$defaultEvidence = Read-JsonOrNull $defaultEvidenceFile
$surfaceEvidence = Read-JsonOrNull $surfaceEvidenceFile

$errors = [System.Collections.Generic.List[string]]::new()

Add-Error $errors "LOCK_PROVIDER_NOT_CLOAKBROWSER" $([string] $lock.provider -eq "cloakbrowser")
Add-Error $errors "LOCK_MODE_NOT_CDP_DIRECT" $([string] $lock.mode -eq "cdp-direct")
Add-Error $errors "LOCK_EXTENSION_ENABLED_NOT_FALSE" $(-not [bool] $lock.extension_enabled)
Add-Error $errors "LOCK_SYSTEM_BROWSER_ALLOWED_NOT_FALSE" $(-not [bool] $lock.system_browser_allowed)
Add-Error $errors "SIDEPANEL_NOT_MARKED_LEGACY_COMPAT" $($sidepanelHarness.Contains("legacy-installed-sidepanel-compat-only"))
Add-Error $errors "SIDEPANEL_MARKED_DEFAULT" $($sidepanelHarness.Contains("installedSidepanelHarnessDefault: false"))
Add-Error $errors "EXTENSION_RUNTIME_DEFAULT_NOT_FALSE" $($sidepanelHarness.Contains("extensionRuntimeDefault: false"))
Add-Error $errors "DEFAULT_EVIDENCE_MISSING" $($null -ne $defaultEvidence)
Add-Error $errors "SURFACE_EVIDENCE_MISSING" $($null -ne $surfaceEvidence)

if ($null -ne $defaultEvidence) {
    Add-Error $errors "DEFAULT_HARNESS_NOT_NO_EXTENSION" $((Get-String $defaultEvidence "defaultHarness") -eq "cloakbrowser-cdp-no-extension")
    Add-Error $errors "DEFAULT_HARNESS_OPENED_EXTENSION" $(-not (Get-Bool $defaultEvidence "extensionOpened"))
    Add-Error $errors "DEFAULT_HARNESS_USED_INSTALLED_SIDEPANEL" $(-not (Get-Bool $defaultEvidence "installedSidepanelHarnessUsed"))
    Add-Error $errors "DEFAULT_HARNESS_USED_EXTENSION" $(-not (Get-Bool $defaultEvidence "extensionUsed"))
    Add-Error $errors "DEFAULT_HARNESS_USED_SYSTEM_BROWSER" $(-not (Get-Bool $defaultEvidence "systemBrowserUsed"))
    Add-Error $errors "DEFAULT_HARNESS_USED_FALLBACK" $(-not (Get-Bool $defaultEvidence "fallbackUsed"))
}

if ($null -ne $surfaceEvidence) {
    Add-Error $errors "SURFACE_NOT_NO_EXTENSION" $((Get-String $surfaceEvidence "productSurface") -eq "minimal-no-extension-runtime-bridge")
    Add-Error $errors "SURFACE_REQUIRES_EXTENSION" $(-not (Get-Bool $surfaceEvidence "extensionRequired"))
    Add-Error $errors "SURFACE_OPENED_EXTENSION" $(-not (Get-Bool $surfaceEvidence "extensionOpened"))
    Add-Error $errors "SURFACE_USED_INSTALLED_SIDEPANEL" $(-not (Get-Bool $surfaceEvidence "installedSidepanelHarnessUsed"))
    Add-Error $errors "SURFACE_USED_EXTENSION" $(-not (Get-Bool $surfaceEvidence "extensionUsed"))
    Add-Error $errors "SURFACE_USED_SYSTEM_BROWSER" $(-not (Get-Bool $surfaceEvidence "systemBrowserUsed"))
    Add-Error $errors "SURFACE_LAUNCHED_RUNTIME" $(-not (Get-Bool $surfaceEvidence "runtimeLaunchedFromSurface"))
    Add-Error $errors "SURFACE_EXECUTED_CDP_LIVE" $(-not (Get-Bool $surfaceEvidence "cdpLiveExecutedFromSurface"))
    Add-Error $errors "SURFACE_USED_BRIDGE_WEBSOCKET" $(-not (Get-Bool $surfaceEvidence "bridgeWebSocketUsed"))
    Add-Error $errors "SURFACE_USED_FALLBACK" $(-not (Get-Bool $surfaceEvidence "fallbackUsed"))
    Add-Error $errors "SURFACE_NOT_METADATA_ONLY" $(Get-Bool $surfaceEvidence "metadataOnly")
}

$ok = $errors.Count -eq 0
New-Item -ItemType Directory -Force -Path $ArtifactsRoot | Out-Null
$timestamp = (Get-Date).ToUniversalTime().ToString("yyyy-MM-ddTHH-mm-ss-fffZ")
$evidencePath = Join-Path $ArtifactsRoot "cloakbrowser-cdp-extension-deprecation-hardening-$timestamp.redacted.json"

$evidence = [ordered]@{
    schemaVersion = "1.0"
    status = if ($ok) { "PASS" } else { "BLOCKED" }
    decision = if ($ok) { $DecisionReady } else { $DecisionBlocked }
    reason = if ($ok) {
        "Chrome Extension is legacy/no-default and CloakBrowser CDP no-extension remains the default runtime proof."
    } else {
        "Extension deprecation hardening checks failed."
    }
    generatedAt = (Get-Date).ToUniversalTime().ToString("O")
    defaultRuntime = "cloakbrowser-cdp-direct"
    defaultHarness = "cloakbrowser-cdp-no-extension"
    minimalProductSurface = "minimal-no-extension-runtime-bridge"
    extensionMode = "legacy/no-default"
    installedSidepanelHarnessMode = "legacy-installed-sidepanel-compat-only"
    extensionDefaultRuntime = $false
    installedSidepanelHarnessDefault = $false
    extensionFallbackAllowed = $false
    systemBrowserFallbackAllowed = $false
    extensionRequired = $false
    extensionOpened = $false
    extensionUsed = $false
    systemBrowserUsed = $false
    fallbackUsed = $false
    bridgeWebSocketUsed = $false
    runtimeLaunchedFromUi = $false
    cdpLiveExecutedFromUi = $false
    productFilesModified = $false
    metadataOnly = $true
    lockProvider = [string] $lock.provider
    lockMode = [string] $lock.mode
    lockExtensionEnabled = [bool] $lock.extension_enabled
    lockSystemBrowserAllowed = [bool] $lock.system_browser_allowed
    defaultEvidenceName = New-EvidenceName $defaultEvidenceFile
    surfaceEvidenceName = New-EvidenceName $surfaceEvidenceFile
    errors = @($errors)
}

$evidence | ConvertTo-Json -Depth 10 | Set-Content -LiteralPath $evidencePath -Encoding UTF8

"status=$($evidence.status)"
"decision=$($evidence.decision)"
"reason=$($evidence.reason)"
"defaultRuntime=$($evidence.defaultRuntime)"
"defaultHarness=$($evidence.defaultHarness)"
"minimalProductSurface=$($evidence.minimalProductSurface)"
"extensionMode=$($evidence.extensionMode)"
"installedSidepanelHarnessMode=$($evidence.installedSidepanelHarnessMode)"
"extensionDefaultRuntime=$($evidence.extensionDefaultRuntime)"
"installedSidepanelHarnessDefault=$($evidence.installedSidepanelHarnessDefault)"
"extensionFallbackAllowed=$($evidence.extensionFallbackAllowed)"
"systemBrowserFallbackAllowed=$($evidence.systemBrowserFallbackAllowed)"
"extensionRequired=$($evidence.extensionRequired)"
"extensionOpened=$($evidence.extensionOpened)"
"extensionUsed=$($evidence.extensionUsed)"
"systemBrowserUsed=$($evidence.systemBrowserUsed)"
"fallbackUsed=$($evidence.fallbackUsed)"
"bridgeWebSocketUsed=$($evidence.bridgeWebSocketUsed)"
"runtimeLaunchedFromUi=$($evidence.runtimeLaunchedFromUi)"
"cdpLiveExecutedFromUi=$($evidence.cdpLiveExecutedFromUi)"
"metadataOnly=$($evidence.metadataOnly)"
"evidence=$evidencePath"

if (-not $ok) {
    exit 1
}
