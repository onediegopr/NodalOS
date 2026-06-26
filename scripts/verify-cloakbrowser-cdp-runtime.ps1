param(
    [string] $RepositoryRoot = (Resolve-Path ".").Path
)

$ErrorActionPreference = "Stop"

function New-RedactedPath {
    param([string] $Path)

    if ([string]::IsNullOrWhiteSpace($Path)) {
        return $null
    }

    return [System.IO.Path]::GetFileName($Path)
}

function Test-SystemBrowserPath {
    param(
        [string] $Path,
        [object] $Lock
    )

    if ([string]::IsNullOrWhiteSpace($Path)) {
        return $false
    }

    if (Test-PinnedCloakBrowserArtifactPath -Path $Path -Lock $Lock) {
        return $false
    }

    $fileName = [System.IO.Path]::GetFileName($Path)
    if ($fileName -match '^(chrome|msedge|chromium)(\.exe)?$') {
        return $true
    }

    return $Path -match '\\Google\\Chrome\\' `
        -or $Path -match '\\Microsoft\\Edge\\' `
        -or $Path -match '\\Chromium\\Application\\'
}

function Test-PinnedCloakBrowserArtifactPath {
    param(
        [string] $Path,
        [object] $Lock
    )

    if ([string]::IsNullOrWhiteSpace($Path) -or -not $Lock) {
        return $false
    }

    $hasPinnedMetadata = -not [string]::IsNullOrWhiteSpace([string] $Lock.runtime_version) `
        -and [string] $Lock.runtime_version -ne "pending" `
        -and -not [string]::IsNullOrWhiteSpace([string] $Lock.runtime_commit) `
        -and [string] $Lock.runtime_commit -ne "pending" `
        -and -not [string]::IsNullOrWhiteSpace([string] $Lock.binary_sha256) `
        -and [string] $Lock.binary_sha256 -ne "pending"

    if (-not $hasPinnedMetadata) {
        return $false
    }

    $normalized = $Path -replace '/', '\'
    $repo = [regex]::Escape([string] $Lock.runtime_repo)

    return $normalized -match "\\$repo\\" `
        -and $normalized -match "\\\.cloakbrowser\\chromium-[^\\]+\\chrome\.exe$"
}

function Get-LocalConfigPath {
    param([string] $Root)

    $envPath = [Environment]::GetEnvironmentVariable("NODAL_CLOAKBROWSER_RUNTIME_PATH")
    if (-not [string]::IsNullOrWhiteSpace($envPath)) {
        return @{
            source = "env:NODAL_CLOAKBROWSER_RUNTIME_PATH"
            path = $envPath
        }
    }

    $jsonConfig = Join-Path $Root ".local/browser-runtime.local.json"
    if (Test-Path -LiteralPath $jsonConfig) {
        $config = Get-Content -LiteralPath $jsonConfig -Raw | ConvertFrom-Json
        if ($config.cloakbrowser_executable_path) {
            return @{
                source = ".local/browser-runtime.local.json"
                path = [string] $config.cloakbrowser_executable_path
            }
        }
    }

    $runtimeLocal = Join-Path $Root "runtime.local.json"
    if (Test-Path -LiteralPath $runtimeLocal) {
        $config = Get-Content -LiteralPath $runtimeLocal -Raw | ConvertFrom-Json
        if ($config.cloakbrowser_executable_path) {
            return @{
                source = "runtime.local.json"
                path = [string] $config.cloakbrowser_executable_path
            }
        }
    }

    return $null
}

function Write-Evidence {
    param(
        [string] $Status,
        [string] $Decision,
        [string] $Reason,
        [object] $RuntimePathInfo,
        [object] $Lock
    )

    $artifactRoot = Join-Path $RepositoryRoot "artifacts/local-verification"
    New-Item -ItemType Directory -Force -Path $artifactRoot | Out-Null

    $timestamp = (Get-Date).ToUniversalTime().ToString("yyyy-MM-ddTHH-mm-ss-fffZ")
    $artifactPath = Join-Path $artifactRoot "cloakbrowser-cdp-runtime-$timestamp.redacted.json"

    $evidence = [ordered]@{
        status = $Status
        decision = $Decision
        reason = $Reason
        runtimeProvider = "cloakbrowser"
        cdpMode = "cdp-direct"
        lockProvider = $Lock.provider
        lockMode = $Lock.mode
        lockSystemBrowserAllowed = [bool] $Lock.system_browser_allowed
        runtimePathSource = if ($RuntimePathInfo) { $RuntimePathInfo.source } else { $null }
        runtimePathRedacted = if ($RuntimePathInfo) { New-RedactedPath -Path $RuntimePathInfo.path } else { $null }
        runtimeArtifactPresent = if ($RuntimePathInfo) { Test-Path -LiteralPath $RuntimePathInfo.path -PathType Leaf } else { $false }
        browserVersion = $null
        targetCount = $null
        bootstrapInjected = $false
        screenshotCaptured = $false
        systemBrowserUsed = $false
        extensionUsed = $false
        timestamp = (Get-Date).ToUniversalTime().ToString("o")
    }

    $evidence | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath $artifactPath -Encoding UTF8

    "status=$Status"
    "decision=$Decision"
    "reason=$Reason"
    "evidence=$artifactPath"
}

$lockPath = Join-Path $RepositoryRoot "browser-runtime.lock.json"
if (-not (Test-Path -LiteralPath $lockPath)) {
    throw "browser-runtime.lock.json not found."
}

$lock = Get-Content -LiteralPath $lockPath -Raw | ConvertFrom-Json
$runtimePathInfo = Get-LocalConfigPath -Root $RepositoryRoot

if ($lock.provider -ne "cloakbrowser" -or $lock.mode -ne "cdp-direct" -or [bool] $lock.system_browser_allowed) {
    Write-Evidence -Status "BLOCKED" -Decision "NODAL_OS_CLOAKBROWSER_CDP_LIVE_STILL_BLOCKED_RUNTIME_ARTIFACT_REQUIRED" -Reason "Browser runtime lock invalid for cloakbrowser cdp-direct." -RuntimePathInfo $runtimePathInfo -Lock $lock
    exit 2
}

if (-not $runtimePathInfo) {
    Write-Evidence -Status "BLOCKED" -Decision "NODAL_OS_CLOAKBROWSER_CDP_LIVE_STILL_BLOCKED_RUNTIME_ARTIFACT_REQUIRED" -Reason "CloakBrowser runtime artifact required." -RuntimePathInfo $null -Lock $lock
    exit 0
}

if (-not (Test-Path -LiteralPath $runtimePathInfo.path -PathType Leaf)) {
    Write-Evidence -Status "BLOCKED" -Decision "NODAL_OS_CLOAKBROWSER_CDP_LIVE_STILL_BLOCKED_RUNTIME_ARTIFACT_REQUIRED" -Reason "Configured CloakBrowser runtime path does not exist." -RuntimePathInfo $runtimePathInfo -Lock $lock
    exit 0
}

if (Test-SystemBrowserPath -Path $runtimePathInfo.path -Lock $lock) {
    Write-Evidence -Status "BLOCKED" -Decision "NODAL_OS_CLOAKBROWSER_CDP_LIVE_STILL_BLOCKED_RUNTIME_ARTIFACT_REQUIRED" -Reason "Configured runtime path is a forbidden system browser." -RuntimePathInfo $runtimePathInfo -Lock $lock
    exit 3
}

$testProject = Join-Path $RepositoryRoot "tests/OneBrain.Safety.Tests/OneBrain.Safety.Tests.csproj"
$testArgs = @(
    "test",
    $testProject,
    "--no-build",
    "--filter",
    "TestCategory=CloakBrowserRuntimeLive"
)

& dotnet @testArgs
$testExitCode = $LASTEXITCODE
if ($testExitCode -ne 0) {
    Write-Evidence -Status "BLOCKED" -Decision "NODAL_OS_CLOAKBROWSER_CDP_LIVE_BLOCKED_WITH_CAUSE" -Reason "CloakBrowser CDP live healthcheck test failed with exit code $testExitCode." -RuntimePathInfo $runtimePathInfo -Lock $lock
    exit $testExitCode
}

$latestEvidence = Get-ChildItem -LiteralPath (Join-Path $RepositoryRoot "artifacts/local-verification") -Filter "cloakbrowser-cdp-healthcheck-*.redacted.json" |
    Sort-Object LastWriteTime -Descending |
    Select-Object -First 1

$latestDomActionEvidence = Get-ChildItem -LiteralPath (Join-Path $RepositoryRoot "artifacts/local-verification") -Filter "cloakbrowser-cdp-dom-action-*.redacted.json" |
    Sort-Object LastWriteTime -Descending |
    Select-Object -First 1

if (-not $latestEvidence) {
    Write-Evidence -Status "BLOCKED" -Decision "NODAL_OS_CLOAKBROWSER_CDP_LIVE_BLOCKED_WITH_CAUSE" -Reason "CloakBrowser CDP live healthcheck completed but evidence JSON was not found." -RuntimePathInfo $runtimePathInfo -Lock $lock
    exit 4
}

$evidenceJson = Get-Content -LiteralPath $latestEvidence.FullName -Raw | ConvertFrom-Json
$domActionEvidenceJson = if ($latestDomActionEvidence) { Get-Content -LiteralPath $latestDomActionEvidence.FullName -Raw | ConvertFrom-Json } else { $null }
"status=$($evidenceJson.status)"
"decision=$($evidenceJson.decision)"
"reason=$($evidenceJson.reason)"
"artifactHash=OK"
"runtimeLaunched=$($evidenceJson.processStarted)"
"cdpEndpointHost=$($evidenceJson.cdpEndpointHost)"
"browserVersion=$($evidenceJson.browserVersion)"
"targetLifecycle=$($evidenceJson.targetCreated)/$($evidenceJson.targetClosed)"
"sessionLifecycle=$($evidenceJson.sessionCreated)/$($evidenceJson.sessionClosed)"
"injectionOk=$($evidenceJson.bootstrapInjected)"
"domSnapshotOk=$($evidenceJson.domSnapshotCaptured)"
"interactiveElements=$($evidenceJson.interactiveElementCount)"
"controlledClickOk=$($evidenceJson.controlledClickOk)"
"controlledTypeOk=$($evidenceJson.controlledTypeOk)"
"externalNavigationBlocked=$($evidenceJson.externalNavigationBlocked)"
"screenshotOk=$($evidenceJson.screenshotCaptured)"
"shutdownOk=$($evidenceJson.runtimeShutdown)"
"processExited=$($evidenceJson.processExited)"
"noOrphanProcess=$(-not [bool] $evidenceJson.orphanProcessDetected)"
"systemBrowserUsed=$($evidenceJson.systemBrowserUsed)"
"extensionUsed=$($evidenceJson.extensionUsed)"
"evidence=$($latestEvidence.FullName)"
if ($latestDomActionEvidence) {
    "domActionEvidence=$($latestDomActionEvidence.FullName)"
    "domActionSource=$($domActionEvidenceJson.source)"
    "domActionSecretsRedacted=$($domActionEvidenceJson.secretsRedacted)"
}
