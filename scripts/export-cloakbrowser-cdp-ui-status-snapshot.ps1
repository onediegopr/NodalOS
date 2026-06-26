$ErrorActionPreference = "Stop"

$RepoRoot = Resolve-Path (Join-Path $PSScriptRoot "..")
$ArtifactsRoot = Join-Path $RepoRoot "artifacts\local-verification"
$GeneratedRoot = Join-Path $RepoRoot "browser-extension\onebrain-chrome-lab\generated"
$SnapshotPath = Join-Path $GeneratedRoot "cdp-status.snapshot.json"
$LockfilePath = Join-Path $RepoRoot "browser-runtime.lock.json"
$Now = [DateTimeOffset]::UtcNow
$FreshWithin = [TimeSpan]::FromHours(24)

function Get-PropertyValue($Object, [string] $Name) {
    if ($null -eq $Object) {
        return $null
    }

    $property = $Object.PSObject.Properties[$Name]
    if ($null -eq $property) {
        return $null
    }

    return $property.Value
}

function Get-BoolValue($Object, [string[]] $Names, [bool] $Default = $false) {
    foreach ($name in $Names) {
        $value = Get-PropertyValue $Object $name
        if ($null -ne $value) {
            return [bool] $value
        }
    }

    return $Default
}

function Get-CountValue($Object, [string[]] $Names) {
    foreach ($name in $Names) {
        $value = Get-PropertyValue $Object $name
        if ($null -eq $value) {
            continue
        }

        if ($value -is [int] -or $value -is [long] -or $value -is [double]) {
            return [int] $value
        }

        $countProperty = $value.PSObject.Properties["Count"]
        if ($null -ne $countProperty) {
            return [int] $countProperty.Value
        }

        $entriesProperty = $value.PSObject.Properties["EntriesCount"]
        if ($null -ne $entriesProperty) {
            return [int] $entriesProperty.Value
        }
    }

    $bridge = Get-PropertyValue $Object "uiBridgeModel"
    $summary = Get-PropertyValue $bridge "Summary"
    if ($null -ne $summary) {
        $candidate = $null
        if ($Names -contains "interactiveElements" -or $Names -contains "interactiveElementCount") {
            $candidate = "ElementCount"
        } elseif ($Names -contains "frictionSignals") {
            $candidate = "FrictionCount"
        } elseif ($Names -contains "actionMap" -or $Names -contains "actionMapEntries") {
            $candidate = "ActionMapCount"
        }

        if ($null -ne $candidate) {
            $summaryValue = Get-PropertyValue $summary $candidate
            if ($null -ne $summaryValue) {
                return [int] $summaryValue
            }
        }
    }

    return 0
}

function Get-Timestamp($Object, [System.IO.FileInfo] $File) {
    $value = Get-PropertyValue $Object "timestamp"
    if ([string]::IsNullOrWhiteSpace($value)) {
        $value = Get-PropertyValue $Object "createdAt"
    }

    $parsed = [DateTimeOffset]::MinValue
    if (-not [string]::IsNullOrWhiteSpace($value) -and [DateTimeOffset]::TryParse([string] $value, [ref] $parsed)) {
        return $parsed.ToUniversalTime()
    }

    return [DateTimeOffset]::new($File.LastWriteTimeUtc)
}

function New-MissingEvidence([string] $Kind) {
    return [pscustomobject]@{
        Kind = $Kind
        Status = "missing"
        Decision = "missing"
        Timestamp = $null
        Freshness = "Missing"
        LastEvidenceName = "sin-evidencia-cdp"
        EvidenceAvailable = $false
        CaptureOk = $false
        ScreenshotCaptured = $false
        InteractiveElements = 0
        FrictionSignals = 0
        ActionMapEntries = 0
        RuntimeShutdown = $false
        ProcessExited = $false
        OrphanProcessDetected = $false
        ExtensionUsed = $false
        SystemBrowserUsed = $false
        ExternalNavigationBlocked = $true
        ProductFilesModified = $false
        ErrorCode = $null
    }
}

function New-ErrorEvidence([string] $Kind, [string] $FileName, [string] $Message) {
    return [pscustomobject]@{
        Kind = $Kind
        Status = "error"
        Decision = "error"
        Timestamp = $null
        Freshness = "Error"
        LastEvidenceName = $FileName
        EvidenceAvailable = $false
        CaptureOk = $false
        ScreenshotCaptured = $false
        InteractiveElements = 0
        FrictionSignals = 0
        ActionMapEntries = 0
        RuntimeShutdown = $false
        ProcessExited = $false
        OrphanProcessDetected = $false
        ExtensionUsed = $false
        SystemBrowserUsed = $false
        ExternalNavigationBlocked = $true
        ProductFilesModified = $false
        ErrorCode = "EVIDENCE_READ_ERROR"
        ErrorMessage = $Message
    }
}

function Find-LatestEvidence([string] $Kind, [string] $Pattern, [scriptblock] $Filter = { param($file) $true }) {
    if (-not (Test-Path $ArtifactsRoot)) {
        return New-MissingEvidence $Kind
    }

    $latest = Get-ChildItem -LiteralPath $ArtifactsRoot -Filter $Pattern -File |
        Where-Object { $_.Name.EndsWith(".redacted.json", [System.StringComparison]::OrdinalIgnoreCase) } |
        Where-Object { & $Filter $_ } |
        Sort-Object LastWriteTimeUtc -Descending |
        Select-Object -First 1

    if ($null -eq $latest) {
        return New-MissingEvidence $Kind
    }

    try {
        $json = Get-Content -LiteralPath $latest.FullName -Raw | ConvertFrom-Json
        $timestamp = Get-Timestamp $json $latest
        $freshness = if (($Now - $timestamp) -le $FreshWithin) { "Fresh" } else { "Stale" }
        $statusValue = Get-PropertyValue $json "status"
        if ([string]::IsNullOrWhiteSpace($statusValue)) {
            $statusValue = "available"
        }

        $decisionValue = Get-PropertyValue $json "decision"
        if ([string]::IsNullOrWhiteSpace($decisionValue)) {
            $decisionValue = "available"
        }

        return [pscustomobject]@{
            Kind = $Kind
            Status = [string] $statusValue
            Decision = [string] $decisionValue
            Timestamp = $timestamp
            Freshness = $freshness
            LastEvidenceName = $latest.Name
            EvidenceAvailable = $true
            CaptureOk = Get-BoolValue $json @("captureOk") ($Kind -eq "healthcheck")
            ScreenshotCaptured = Get-BoolValue $json @("screenshotCaptured") $false
            InteractiveElements = Get-CountValue $json @("interactiveElements", "interactiveElementCount")
            FrictionSignals = Get-CountValue $json @("frictionSignals")
            ActionMapEntries = Get-CountValue $json @("actionMap", "actionMapEntries")
            RuntimeShutdown = Get-BoolValue $json @("runtimeShutdown", "shutdownOk") $false
            ProcessExited = Get-BoolValue $json @("processExited") $false
            OrphanProcessDetected = Get-BoolValue $json @("orphanProcessDetected") $false
            ExtensionUsed = Get-BoolValue $json @("extensionUsed") $false
            SystemBrowserUsed = Get-BoolValue $json @("systemBrowserUsed") $false
            ExternalNavigationBlocked = Get-BoolValue $json @("externalNavigationBlocked") $true
            ProductFilesModified = Get-BoolValue $json @("productFilesModified", "filesModified") $false
            ErrorCode = $null
        }
    } catch {
        return New-ErrorEvidence $Kind $latest.Name $_.Exception.Message
    }
}

function Merge-Freshness($Healthcheck, $Session) {
    if ($Healthcheck.Freshness -eq "Error" -or $Session.Freshness -eq "Error") {
        return "Error"
    }

    if ($Healthcheck.Freshness -eq "Missing" -and $Session.Freshness -eq "Missing") {
        return "Missing"
    }

    if ($Healthcheck.Freshness -eq "Fresh" -and $Session.Freshness -eq "Fresh") {
        return "Fresh"
    }

    return "Stale"
}

function Select-LatestEvidence($Healthcheck, $BrowserSkills, $Session) {
    if ($Session.EvidenceAvailable) {
        return $Session
    }

    if ($BrowserSkills.EvidenceAvailable) {
        return $BrowserSkills
    }

    return $Healthcheck
}

$lock = Get-Content -LiteralPath $LockfilePath -Raw | ConvertFrom-Json
$healthcheck = Find-LatestEvidence "healthcheck" "cloakbrowser-cdp-healthcheck-*.redacted.json"
$browserSkills = Find-LatestEvidence "browser-skills" "cloakbrowser-cdp-browser-skills-*.redacted.json" {
    param($file)
    -not $file.Name.StartsWith("cloakbrowser-cdp-browser-skills-session-", [System.StringComparison]::OrdinalIgnoreCase)
}
$session = Find-LatestEvidence "browser-skills-session" "cloakbrowser-cdp-browser-skills-session-*.redacted.json"
$freshness = Merge-Freshness $healthcheck $session
$statusText = switch ($freshness) {
    "Fresh" { "listo" }
    "Stale" { "revisar" }
    "Missing" { "sin captura reciente" }
    "Error" { "revisar verificación CDP" }
    default { "revisar" }
}
$latest = Select-LatestEvidence $healthcheck $browserSkills $session
$artifactHashVerified = -not [string]::IsNullOrWhiteSpace($lock.binary_sha256) -and $lock.binary_sha256 -ne "pending"
$channelStatus = switch ($freshness) {
    "Missing" { "MISSING_EVIDENCE" }
    "Error" { "ERROR" }
    default { "READY" }
}

$snapshot = [ordered]@{
    schemaVersion = "1.0"
    generatedAt = $Now.ToString("O")
    channel = "safe-local-status-snapshot"
    source = "cloakbrowser-cdp-direct"
    runtimeProvider = "cloakbrowser"
    runtimeStatus = $statusText
    artifactHashVerified = [bool] $artifactHashVerified
    freshness = $freshness
    lastHealthcheckAt = if ($null -ne $healthcheck.Timestamp) { $healthcheck.Timestamp.ToString("O") } else { $null }
    lastSessionAt = if ($null -ne $session.Timestamp) { $session.Timestamp.ToString("O") } else { $null }
    lastEvidenceName = [string] $latest.LastEvidenceName
    evidenceAvailable = [bool] $latest.EvidenceAvailable
    captureOk = [bool] $latest.CaptureOk
    screenshotCaptured = [bool] $latest.ScreenshotCaptured
    interactiveElements = [int] $latest.InteractiveElements
    frictionSignals = [int] $latest.FrictionSignals
    actionMapEntries = [int] $latest.ActionMapEntries
    runtimeShutdown = [bool] ($healthcheck.RuntimeShutdown -or $session.RuntimeShutdown)
    processExited = [bool] ($healthcheck.ProcessExited -or $session.ProcessExited)
    orphanProcessDetected = [bool] ($healthcheck.OrphanProcessDetected -or $session.OrphanProcessDetected)
    extensionUsed = $false
    systemBrowserUsed = $false
    boundaryReadOnly = $true
    runtimeLaunchedFromUi = $false
    cdpLiveExecutedFromUi = $false
    externalNavigationBlocked = [bool] ($session.ExternalNavigationBlocked -or $browserSkills.ExternalNavigationBlocked -or $healthcheck.ExternalNavigationBlocked)
    productFilesModified = $false
}

New-Item -ItemType Directory -Force -Path $GeneratedRoot | Out-Null
$snapshot | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath $SnapshotPath -Encoding UTF8

Write-Output "status=$channelStatus"
Write-Output "snapshotWritten=True"
Write-Output "snapshotName=cdp-status.snapshot.json"
Write-Output "channel=safe-local-status-snapshot"
Write-Output "freshness=$freshness"
Write-Output "evidenceAvailable=$($snapshot['evidenceAvailable'])"
Write-Output "runtimeLaunched=false"
Write-Output "cdpLiveExecuted=false"
Write-Output "extensionUsed=false"
Write-Output "systemBrowserUsed=false"
Write-Output "productFilesModified=false"
Write-Output "snapshot=$SnapshotPath"

if ($channelStatus -eq "ERROR") {
    exit 2
}
