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
    param([string] $Path)

    if ([string]::IsNullOrWhiteSpace($Path)) {
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

if (Test-SystemBrowserPath -Path $runtimePathInfo.path) {
    Write-Evidence -Status "BLOCKED" -Decision "NODAL_OS_CLOAKBROWSER_CDP_LIVE_STILL_BLOCKED_RUNTIME_ARTIFACT_REQUIRED" -Reason "Configured runtime path is a forbidden system browser." -RuntimePathInfo $runtimePathInfo -Lock $lock
    exit 3
}

Write-Evidence -Status "BLOCKED" -Decision "NODAL_OS_CLOAKBROWSER_CDP_LIVE_STILL_BLOCKED_RUNTIME_ARTIFACT_REQUIRED" -Reason "Runtime artifact discovered but live launch is intentionally not executed until lock metadata is pinned with version, commit, and sha256." -RuntimePathInfo $runtimePathInfo -Lock $lock
