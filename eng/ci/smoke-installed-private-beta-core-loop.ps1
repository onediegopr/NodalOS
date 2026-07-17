param(
    [Parameter(Mandatory = $true)]
    [string]$ExecutablePath,
    [string]$RunnerTemp = $env:RUNNER_TEMP,
    [string]$BaseUrl = "http://127.0.0.1:5127",
    [int]$ProviderPort = 5528
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

if ([string]::IsNullOrWhiteSpace($RunnerTemp)) {
    $RunnerTemp = Join-Path ([System.IO.Path]::GetTempPath()) "nodal-os-installed-core-loop-ci"
}
$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot "../..")).Path
$ExecutablePath = [System.IO.Path]::GetFullPath($ExecutablePath)
if (-not (Test-Path -LiteralPath $ExecutablePath -PathType Leaf)) {
    throw "Installed NODAL OS executable was not found: $ExecutablePath"
}

$fixtureRoot = Join-Path $RunnerTemp "nodal-installed-private-beta-core-loop"
$workspaceRoot = Join-Path $fixtureRoot "workspace"
$selectionMetadataPath = Join-Path $fixtureRoot "config/selection.v1.json"
$missionMetadataPath = Join-Path $fixtureRoot "config/mission.v1.json"
$executionMetadataPath = Join-Path $fixtureRoot "config/execution.v1.json"
$restoreRoot = Join-Path $fixtureRoot "restore"
$workspaceSecretRoot = Join-Path $fixtureRoot "workspace-secrets"
$modelMetadataPath = Join-Path $fixtureRoot "models/byok.v1.json"
$modelSecretRoot = Join-Path $fixtureRoot "model-secrets"
$targetPath = Join-Path $workspaceRoot "NODAL_HANDOFF.md"
$providerUrl = "http://127.0.0.1:$ProviderPort"
$primaryKey = "installed-primary-smoke-key"
$fallbackKey = "installed-fallback-smoke-key"
$responseContent = "NODAL_BYOK_SMOKE_OK"
$sensitiveFixtureValue = "installed-core-loop-sensitive-fixture-value"
$goal = "Prepare a verified installed-product handoff with approval, evidence, export, and guarded rollback."
$previousAspNetCoreEnvironment = $env:ASPNETCORE_ENVIRONMENT
$provider = $null
$pilot = $null

if (Test-Path $fixtureRoot) { Remove-Item $fixtureRoot -Recurse -Force }
New-Item -ItemType Directory -Path (Join-Path $workspaceRoot "src") -Force | Out-Null
[System.IO.File]::WriteAllText((Join-Path $workspaceRoot "README.md"), "# Installed private-beta core-loop smoke")
$keyName = "api" + "_key"
[System.IO.File]::WriteAllText(
    (Join-Path $workspaceRoot "src/Program.cs"),
    ('var {0} = "{1}";{2}Console.WriteLine("fixture");' -f $keyName, $sensitiveFixtureValue, [Environment]::NewLine)
)

function Extract-Token([string]$Html, [string]$FieldName) {
    $pattern = 'name="' + [regex]::Escape($FieldName) + '" value="(?<token>[0-9a-f]+)"'
    $match = [regex]::Match($Html, $pattern)
    if (-not $match.Success) { throw "Request token $FieldName was not rendered." }
    return $match.Groups["token"].Value
}

function Assert-NoLeak([string]$Text, [string]$Surface) {
    foreach ($forbidden in @($workspaceRoot, $primaryKey, $fallbackKey, $responseContent, $sensitiveFixtureValue)) {
        if ($Text.IndexOf($forbidden, [StringComparison]::OrdinalIgnoreCase) -ge 0) {
            throw "$Surface leaked a workspace path, credential, provider response, or sensitive fixture value."
        }
    }
}

function Get-WorkspaceHashes {
    $result = @{}
    Get-ChildItem $workspaceRoot -File -Recurse | Sort-Object FullName | ForEach-Object {
        $relative = [System.IO.Path]::GetRelativePath($workspaceRoot, $_.FullName)
        $result[$relative] = (Get-FileHash $_.FullName -Algorithm SHA256).Hash.ToLowerInvariant()
    }
    return $result
}

function Assert-WorkspaceMatches([hashtable]$Expected) {
    $actual = Get-WorkspaceHashes
    if ($Expected.Count -ne $actual.Count) {
        throw "Installed core-loop rollback did not restore the workspace file count."
    }
    foreach ($key in $Expected.Keys) {
        if (-not $actual.ContainsKey($key) -or $actual[$key] -ne $Expected[$key]) {
            throw "Installed core-loop rollback did not restore the workspace baseline: $key"
        }
    }
}

function Read-SafeLog([string]$Path, [string]$Surface) {
    if (-not (Test-Path $Path)) { return }
    $text = Get-Content $Path -Raw -ErrorAction SilentlyContinue
    if ([string]::IsNullOrWhiteSpace($text)) { return }
    Assert-NoLeak $text $Surface
    Write-Output $text
}

$baseline = Get-WorkspaceHashes
$env:ASPNETCORE_ENVIRONMENT = "Production"
$env:NODAL_OS_WORKSPACE_SELECTION_METADATA_PATH = $selectionMetadataPath
$env:NODAL_OS_WORKSPACE_SELECTION_SECRET_ROOT = $workspaceSecretRoot
$env:NODAL_OS_WORKSPACE_MISSION_METADATA_PATH = $missionMetadataPath
$env:NODAL_OS_WORKSPACE_HANDOFF_EXECUTION_METADATA_PATH = $executionMetadataPath
$env:NODAL_OS_WORKSPACE_HANDOFF_RESTORE_ROOT = $restoreRoot
$env:NODAL_OS_BYOK_MODEL_METADATA_PATH = $modelMetadataPath
$env:NODAL_OS_BYOK_MODEL_SECRET_ROOT = $modelSecretRoot

try {
    $providerStdout = Join-Path $RunnerTemp "installed-core-loop-provider.stdout.log"
    $providerStderr = Join-Path $RunnerTemp "installed-core-loop-provider.stderr.log"
    $provider = [pscustomobject]@{
        Process = Start-Process python -WorkingDirectory $repoRoot -ArgumentList @(
            (Join-Path $repoRoot "eng/ci/openai_compatible_fixture_server.py"),
            "--port", $ProviderPort
        ) -PassThru -RedirectStandardOutput $providerStdout -RedirectStandardError $providerStderr
        Stdout = $providerStdout
        Stderr = $providerStderr
    }
    for ($attempt = 0; $attempt -lt 40; $attempt++) {
        if ($provider.Process.HasExited) { throw "Installed core-loop provider fixture exited before readiness." }
        try {
            $health = Invoke-RestMethod -Uri "$providerUrl/health" -TimeoutSec 2
            if ($health.status -eq "ready") { break }
        }
        catch {
            Start-Sleep -Milliseconds 500
        }
        if ($attempt -eq 39) { throw "Installed core-loop provider fixture did not become ready." }
    }

    $pilotStdout = Join-Path $RunnerTemp "installed-core-loop-pilot.stdout.log"
    $pilotStderr = Join-Path $RunnerTemp "installed-core-loop-pilot.stderr.log"
    $pilot = [pscustomobject]@{
        Process = Start-Process -FilePath $ExecutablePath -ArgumentList @(
            "--urls", $BaseUrl,
            "--no-open-browser"
        ) -WindowStyle Hidden -PassThru -RedirectStandardOutput $pilotStdout -RedirectStandardError $pilotStderr
        Stdout = $pilotStdout
        Stderr = $pilotStderr
    }
    for ($attempt = 0; $attempt -lt 60; $attempt++) {
        if ($pilot.Process.HasExited) { throw "Installed NODAL OS exited before the core-loop surface became ready." }
        try {
            Invoke-RestMethod -Uri "$BaseUrl/api/mission-control" -TimeoutSec 2 | Out-Null
            break
        }
        catch {
            Start-Sleep -Milliseconds 500
        }
        if ($attempt -eq 59) { throw "Installed NODAL OS core-loop surface did not become ready." }
    }

    $session = [Microsoft.PowerShell.Commands.WebRequestSession]::new()
    $unavailableHandoff = Invoke-WebRequest -Uri "$BaseUrl/mission/handoff.md" -WebSession $session -TimeoutSec 15 -SkipHttpErrorCheck
    if ($unavailableHandoff.StatusCode -ne 409 -or $unavailableHandoff.Content -notmatch "Handoff no disponible") {
        throw "Installed product exposed a handoff before a real mission existed."
    }

    $modelForm = Invoke-WebRequest -Uri "$BaseUrl/models/config" -WebSession $session -TimeoutSec 15
    $modelToken = Extract-Token $modelForm.Content "byokModelToken"
    $configuredPage = Invoke-WebRequest `
        -Uri "$BaseUrl/models/config" `
        -Method Post `
        -WebSession $session `
        -Headers @{ Origin = $BaseUrl } `
        -Body @{
            byokModelToken = $modelToken
            primaryProviderId = "primary-provider"
            primaryDisplayName = "Primary Installed Provider"
            primaryProviderType = "OpenAiCompatibleLocal"
            primaryEndpoint = "$providerUrl/v1"
            primaryModelId = "primary-smoke-model"
            primaryApiKey = $primaryKey
            enableFallback = "on"
            fallbackProviderId = "fallback-provider"
            fallbackDisplayName = "Fallback Installed Provider"
            fallbackProviderType = "OpenAiCompatibleLocal"
            fallbackEndpoint = "$providerUrl/v1"
            fallbackModelId = "fallback-smoke-model"
            fallbackApiKey = $fallbackKey
            maximumTotalCostUsd = "1"
            perAttemptTimeoutSeconds = "5"
            primaryInputCostPerMillion = "1"
            primaryOutputCostPerMillion = "2"
            fallbackInputCostPerMillion = "0.5"
            fallbackOutputCostPerMillion = "1"
        } `
        -TimeoutSec 30
    Assert-NoLeak $configuredPage.Content "Installed BYOK configuration page"
    if ($configuredPage.StatusCode -ne 200 -or $configuredPage.Content -notmatch 'data-configured="true"') {
        throw "Installed BYOK configuration was not persisted."
    }

    $modelToken = Extract-Token $configuredPage.Content "byokModelToken"
    $testedPage = Invoke-WebRequest `
        -Uri "$BaseUrl/models/test" `
        -Method Post `
        -WebSession $session `
        -Headers @{ Origin = $BaseUrl } `
        -Body @{ byokModelToken = $modelToken } `
        -TimeoutSec 30
    Assert-NoLeak $testedPage.Content "Installed BYOK test page"
    if ($testedPage.StatusCode -ne 200 -or
        $testedPage.Content -notmatch 'data-connected="true"' -or
        $testedPage.Content -notmatch 'data-fallback-applied="true"' -or
        $testedPage.Content -notmatch 'data-secrets-excluded="true"') {
        throw "Installed BYOK connection did not complete through the authorized fallback."
    }

    $workspaceForm = Invoke-WebRequest -Uri "$BaseUrl/workspace/select" -WebSession $session -TimeoutSec 15
    $workspaceToken = Extract-Token $workspaceForm.Content "workspaceSelectionToken"
    $workspacePage = Invoke-WebRequest `
        -Uri "$BaseUrl/workspace/select" `
        -Method Post `
        -WebSession $session `
        -Headers @{ Origin = $BaseUrl } `
        -Body @{
            workspaceSelectionToken = $workspaceToken
            rootPath = $workspaceRoot
            displayName = "Installed Core Loop Workspace"
        } `
        -TimeoutSec 30
    if ($workspacePage.StatusCode -ne 200 -or
        $workspacePage.Content -notmatch 'data-selection-state="Ready"' -or
        $workspacePage.Content -notmatch 'data-persisted="true"') {
        throw "Installed workspace selection did not reach the persisted ready state."
    }

    $missionForm = Invoke-WebRequest -Uri "$BaseUrl/mission/new" -WebSession $session -TimeoutSec 15
    $missionToken = Extract-Token $missionForm.Content "missionDraftToken"
    $missionPage = Invoke-WebRequest `
        -Uri "$BaseUrl/mission/new" `
        -Method Post `
        -WebSession $session `
        -Headers @{ Origin = $BaseUrl } `
        -Body @{
            missionDraftToken = $missionToken
            goal = $goal
        } `
        -TimeoutSec 30
    if ($missionPage.StatusCode -ne 200 -or
        $missionPage.Content -notmatch 'data-state="ReadyForReview"' -or
        $missionPage.Content -notmatch "NODAL_HANDOFF.md") {
        throw "Installed mission draft did not reach review state."
    }
    if (Test-Path $targetPath) { throw "Installed mission review mutated the workspace before approval." }

    $approvalPage = Invoke-WebRequest -Uri "$BaseUrl/mission/execution" -WebSession $session -TimeoutSec 15
    $executionToken = Extract-Token $approvalPage.Content "handoffExecutionToken"
    $executionPage = Invoke-WebRequest `
        -Uri "$BaseUrl/mission/execution" `
        -Method Post `
        -WebSession $session `
        -Headers @{ Origin = $BaseUrl } `
        -Body @{ handoffExecutionToken = $executionToken } `
        -TimeoutSec 30
    Assert-NoLeak $executionPage.Content "Installed execution page"
    if ($executionPage.StatusCode -ne 200 -or
        $executionPage.Content -notmatch 'data-state="Completed"' -or
        $executionPage.Content -notmatch 'data-verified="true"' -or
        $executionPage.Content -notmatch 'data-rollback-available="true"') {
        throw "Installed approved action did not complete with verification and rollback readiness."
    }
    if (-not (Test-Path $targetPath)) { throw "Installed approved action did not create NODAL_HANDOFF.md." }

    $missionControl = Invoke-RestMethod -Uri "$BaseUrl/api/mission-control" -TimeoutSec 20
    if (-not $missionControl.realMissionDraft -or
        -not $missionControl.actionExecuted -or
        -not $missionControl.actionVerified -or
        -not $missionControl.actionRollbackAvailable -or
        -not $missionControl.byokConfigured -or
        -not $missionControl.modelConnectionVerified -or
        $missionControl.activeProvider -ne "fallback-provider" -or
        $missionControl.activeModel -ne "fallback-smoke-model" -or
        $missionControl.progressPercent -ne 100 -or
        $missionControl.productAuthorityGranted) {
        throw "Installed Mission Control did not project the complete provider-to-handoff loop safely."
    }

    $handoff = Invoke-WebRequest -Uri "$BaseUrl/mission/handoff.md" -WebSession $session -TimeoutSec 20
    foreach ($required in @(
        "# NODAL OS — Handoff de misión",
        $goal,
        "NODAL_HANDOFF.md",
        "- **Resultado verificado:** Sí",
        "- **Conexión verificada:** Sí",
        "- Autoridad de producto concedida: No"
    )) {
        if ($handoff.Content -notmatch [regex]::Escape($required)) {
            throw "Installed canonical handoff omitted required state: $required"
        }
    }
    if ($handoff.StatusCode -ne 200 -or [string]$handoff.Headers["Cache-Control"] -notmatch "no-store") {
        throw "Installed canonical handoff was not returned as a no-store successful export."
    }
    Assert-NoLeak $handoff.Content "Installed canonical handoff"

    $rollbackForm = Invoke-WebRequest -Uri "$BaseUrl/mission/execution" -WebSession $session -TimeoutSec 15
    $rollbackToken = Extract-Token $rollbackForm.Content "handoffExecutionToken"
    $rollbackPage = Invoke-WebRequest `
        -Uri "$BaseUrl/mission/rollback" `
        -Method Post `
        -WebSession $session `
        -Headers @{ Origin = $BaseUrl } `
        -Body @{ handoffExecutionToken = $rollbackToken } `
        -TimeoutSec 30
    Assert-NoLeak $rollbackPage.Content "Installed rollback page"
    if ($rollbackPage.StatusCode -ne 200 -or
        $rollbackPage.Content -notmatch 'data-state="RolledBack"' -or
        $rollbackPage.Content -notmatch 'data-rolled-back="true"') {
        throw "Installed guarded rollback did not complete."
    }
    if (Test-Path $targetPath) { throw "Installed guarded rollback did not remove the exact created target." }
    Assert-WorkspaceMatches $baseline
}
finally {
    foreach ($entry in @($pilot, $provider)) {
        if ($null -ne $entry -and -not $entry.Process.HasExited) {
            Stop-Process -Id $entry.Process.Id -Force -ErrorAction SilentlyContinue
            $entry.Process.WaitForExit(10000) | Out-Null
        }
    }
    if ($null -ne $pilot) {
        Read-SafeLog $pilot.Stdout "Installed Pilot stdout"
        Read-SafeLog $pilot.Stderr "Installed Pilot stderr"
    }
    if ($null -ne $provider) {
        Read-SafeLog $provider.Stdout "Installed provider stdout"
        Read-SafeLog $provider.Stderr "Installed provider stderr"
    }

    if ([string]::IsNullOrWhiteSpace($previousAspNetCoreEnvironment)) {
        Remove-Item Env:ASPNETCORE_ENVIRONMENT -ErrorAction SilentlyContinue
    }
    else {
        $env:ASPNETCORE_ENVIRONMENT = $previousAspNetCoreEnvironment
    }
    Remove-Item Env:NODAL_OS_WORKSPACE_SELECTION_METADATA_PATH -ErrorAction SilentlyContinue
    Remove-Item Env:NODAL_OS_WORKSPACE_SELECTION_SECRET_ROOT -ErrorAction SilentlyContinue
    Remove-Item Env:NODAL_OS_WORKSPACE_MISSION_METADATA_PATH -ErrorAction SilentlyContinue
    Remove-Item Env:NODAL_OS_WORKSPACE_HANDOFF_EXECUTION_METADATA_PATH -ErrorAction SilentlyContinue
    Remove-Item Env:NODAL_OS_WORKSPACE_HANDOFF_RESTORE_ROOT -ErrorAction SilentlyContinue
    Remove-Item Env:NODAL_OS_BYOK_MODEL_METADATA_PATH -ErrorAction SilentlyContinue
    Remove-Item Env:NODAL_OS_BYOK_MODEL_SECRET_ROOT -ErrorAction SilentlyContinue
    if (Test-Path $fixtureRoot) {
        Get-ChildItem $fixtureRoot -File -Recurse -Force -ErrorAction SilentlyContinue | ForEach-Object {
            $_.Attributes = [System.IO.FileAttributes]::Normal
        }
        Remove-Item $fixtureRoot -Recurse -Force
    }
}

Write-Host "NODAL_OS_INSTALLED_PRIVATE_BETA_CORE_LOOP=PASS"
