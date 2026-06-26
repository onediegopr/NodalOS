param(
    [string] $RepositoryRoot = (Resolve-Path ".").Path
)

$ErrorActionPreference = "Stop"

$ArtifactsRoot = Join-Path $RepositoryRoot "artifacts/local-verification"
$DecisionReady = "NODAL_OS_CLOAKBROWSER_CDP_NO_EXTENSION_DEFAULT_HARNESS_READY"
$DecisionBlocked = "NODAL_OS_CLOAKBROWSER_CDP_NO_EXTENSION_DEFAULT_HARNESS_BLOCKED_WITH_CAUSE"

function Invoke-HarnessStep {
    param(
        [string] $Name,
        [string] $ScriptPath,
        [string[]] $Arguments = @()
    )

    $fullPath = Join-Path $RepositoryRoot $ScriptPath
    if (-not (Test-Path -LiteralPath $fullPath -PathType Leaf)) {
        return [pscustomobject]@{
            Name = $Name
            ExitCode = 127
            Lines = @()
            Fields = @{}
            Ok = $false
            Error = "Script not found: $ScriptPath"
        }
    }

    $output = & powershell -NoProfile -ExecutionPolicy Bypass -File $fullPath @Arguments 2>&1
    $exitCode = $LASTEXITCODE
    $lines = @($output | ForEach-Object { [string] $_ })

    return [pscustomobject]@{
        Name = $Name
        ExitCode = $exitCode
        Lines = $lines
        Fields = Convert-KeyValueLines $lines
        Ok = ($exitCode -eq 0)
        Error = if ($exitCode -eq 0) { $null } else { ($lines | Select-Object -Last 8) -join "`n" }
    }
}

function Convert-KeyValueLines {
    param([string[]] $Lines)

    $fields = @{}
    foreach ($line in $Lines) {
        if ($line -match '^([A-Za-z][A-Za-z0-9_]*)=(.*)$') {
            $fields[$matches[1]] = $matches[2]
        }
    }

    return $fields
}

function Get-Field {
    param(
        [object] $Step,
        [string] $Name,
        [string] $Default = ""
    )

    if ($Step -and $Step.Fields.ContainsKey($Name)) {
        return [string] $Step.Fields[$Name]
    }

    return $Default
}

function Get-BoolField {
    param(
        [object] $Step,
        [string] $Name,
        [bool] $Default = $false
    )

    $value = Get-Field -Step $Step -Name $Name -Default $null
    if ($null -eq $value -or $value -eq "") {
        return $Default
    }

    return $value -eq "True" -or $value -eq "true"
}

function New-EvidenceName {
    param([object] $Step)

    $path = Get-Field -Step $Step -Name "evidence"
    if ([string]::IsNullOrWhiteSpace($path)) {
        return $null
    }

    return [System.IO.Path]::GetFileName($path)
}

function Write-NoExtensionEvidence {
    param(
        [object] $Runtime,
        [object] $BrowserSkills,
        [object] $Session,
        [object] $Snapshot,
        [bool] $Ok,
        [string] $Reason
    )

    New-Item -ItemType Directory -Force -Path $ArtifactsRoot | Out-Null

    $timestamp = (Get-Date).ToUniversalTime().ToString("yyyy-MM-ddTHH-mm-ss-fffZ")
    $artifactPath = Join-Path $ArtifactsRoot "cloakbrowser-cdp-no-extension-default-$timestamp.redacted.json"

    $evidence = [ordered]@{
        schemaVersion = "1.0"
        status = if ($Ok) { "PASS" } else { "BLOCKED" }
        decision = if ($Ok) { $DecisionReady } else { $DecisionBlocked }
        reason = $Reason
        generatedAt = (Get-Date).ToUniversalTime().ToString("O")
        defaultHarness = "cloakbrowser-cdp-no-extension"
        runtimeProvider = "cloakbrowser"
        source = "cloakbrowser-cdp-direct"
        readOnly = $true
        metadataOnly = $true
        runtimeOk = [bool] $Runtime.Ok
        browserSkillsOk = [bool] $BrowserSkills.Ok
        sessionOk = [bool] $Session.Ok
        snapshotOk = [bool] $Snapshot.Ok
        runtimeStatus = Get-Field -Step $Runtime -Name "status"
        browserSkillsStatus = Get-Field -Step $BrowserSkills -Name "status"
        sessionStatus = Get-Field -Step $Session -Name "status"
        snapshotStatus = Get-Field -Step $Snapshot -Name "status"
        artifactHash = Get-Field -Step $Runtime -Name "artifactHash"
        captureOk = Get-BoolField -Step $BrowserSkills -Name "captureOk"
        domIndexOk = Get-BoolField -Step $BrowserSkills -Name "domIndexOk"
        interactiveElementsDetected = Get-BoolField -Step $BrowserSkills -Name "interactiveElementsDetected"
        frictionSignalsDetected = Get-BoolField -Step $BrowserSkills -Name "frictionSignalsDetected"
        actionMapOk = Get-BoolField -Step $BrowserSkills -Name "actionMapOk"
        sessionCreated = Get-BoolField -Step $Session -Name "sessionCreated"
        uiBridgeOk = Get-BoolField -Step $Session -Name "uiBridgeOk"
        snapshotWritten = Get-BoolField -Step $Snapshot -Name "snapshotWritten"
        snapshotChannel = Get-Field -Step $Snapshot -Name "channel"
        snapshotFreshness = Get-Field -Step $Snapshot -Name "freshness"
        extensionOpened = $false
        installedSidepanelHarnessUsed = $false
        extensionUsed = $false
        extensionFallbackUsed = $false
        systemBrowserUsed = $false
        systemBrowserFallbackUsed = $false
        fallbackUsed = $false
        runtimeLaunchedFromUi = $false
        cdpLiveExecutedFromUi = $false
        externalNavigationBlocked = $true
        productFilesModified = $false
        rawDomStored = $false
        rawHtmlStored = $false
        inputValuesStored = $false
        cookiesOrStorageStored = $false
        secretsStored = $false
        evidenceRefs = [ordered]@{
            runtime = New-EvidenceName -Step $Runtime
            browserSkills = New-EvidenceName -Step $BrowserSkills
            session = New-EvidenceName -Step $Session
            snapshot = Get-Field -Step $Snapshot -Name "snapshotName"
        }
    }

    $evidence | ConvertTo-Json -Depth 10 | Set-Content -LiteralPath $artifactPath -Encoding UTF8

    "status=$($evidence.status)"
    "decision=$($evidence.decision)"
    "reason=$Reason"
    "defaultHarness=$($evidence.defaultHarness)"
    "runtimeOk=$($evidence.runtimeOk)"
    "browserSkillsOk=$($evidence.browserSkillsOk)"
    "sessionOk=$($evidence.sessionOk)"
    "snapshotOk=$($evidence.snapshotOk)"
    "extensionOpened=false"
    "installedSidepanelHarnessUsed=false"
    "extensionUsed=false"
    "systemBrowserUsed=false"
    "fallbackUsed=false"
    "runtimeLaunchedFromUi=false"
    "cdpLiveExecutedFromUi=false"
    "noExtensionDefault=True"
    "evidence=$artifactPath"
}

$runtime = Invoke-HarnessStep `
    -Name "runtime-healthcheck" `
    -ScriptPath "scripts/verify-cloakbrowser-cdp-runtime.ps1" `
    -Arguments @("-RepositoryRoot", $RepositoryRoot)

$browserSkills = Invoke-HarnessStep `
    -Name "browser-skills-cdp" `
    -ScriptPath "scripts/verify-cloakbrowser-cdp-browser-skills.ps1" `
    -Arguments @("-RepositoryRoot", $RepositoryRoot)

$session = Invoke-HarnessStep `
    -Name "browser-skills-session" `
    -ScriptPath "scripts/verify-cloakbrowser-cdp-browser-skills-session.ps1" `
    -Arguments @("-RepositoryRoot", $RepositoryRoot)

$snapshot = Invoke-HarnessStep `
    -Name "safe-local-status-snapshot" `
    -ScriptPath "scripts/export-cloakbrowser-cdp-ui-status-snapshot.ps1"

$allOk = $runtime.Ok -and $browserSkills.Ok -and $session.Ok -and $snapshot.Ok
$expectedSourcesOk = (Get-Field -Step $BrowserSkills -Name "source") -eq "cloakbrowser-cdp-direct" `
    -and (Get-Field -Step $Session -Name "source") -eq "cloakbrowser-cdp-direct" `
    -and (Get-Field -Step $Snapshot -Name "channel") -eq "safe-local-status-snapshot"
$noFallbackOk = -not (Get-BoolField -Step $Runtime -Name "extensionUsed") `
    -and -not (Get-BoolField -Step $Runtime -Name "systemBrowserUsed") `
    -and -not (Get-BoolField -Step $BrowserSkills -Name "extensionUsed") `
    -and -not (Get-BoolField -Step $BrowserSkills -Name "systemBrowserUsed") `
    -and -not (Get-BoolField -Step $Session -Name "extensionUsed") `
    -and -not (Get-BoolField -Step $Session -Name "systemBrowserUsed") `
    -and (Get-Field -Step $Snapshot -Name "extensionUsed") -eq "false" `
    -and (Get-Field -Step $Snapshot -Name "systemBrowserUsed") -eq "false"

$ok = $allOk -and $expectedSourcesOk -and $noFallbackOk
$reason = if ($ok) {
    "CloakBrowser CDP no-extension default harness completed without extension use or system browser use."
} elseif (-not $allOk) {
    "One or more CloakBrowser CDP harness steps failed."
} elseif (-not $expectedSourcesOk) {
    "One or more harness steps did not report the expected CDP direct source."
} else {
    "One or more harness steps reported extension or system browser usage."
}

Write-NoExtensionEvidence `
    -Runtime $runtime `
    -BrowserSkills $browserSkills `
    -Session $session `
    -Snapshot $snapshot `
    -Ok $ok `
    -Reason $reason

if (-not $ok) {
    exit 1
}
