param(
    [string]$RunnerTemp = $env:RUNNER_TEMP,
    [string]$BaseUrl = "http://127.0.0.1:5101"
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

if ([string]::IsNullOrWhiteSpace($RunnerTemp)) {
    $RunnerTemp = Join-Path ([System.IO.Path]::GetTempPath()) "nodal-os-ci"
}
New-Item -ItemType Directory -Path $RunnerTemp -Force | Out-Null

$fixtureRoot = Join-Path $RunnerTemp "nodal-real-workspace-handoff-execution-fixture"
$workspaceRoot = Join-Path $fixtureRoot "workspace"
$selectionMetadataPath = Join-Path $fixtureRoot "config/selection.v1.json"
$missionMetadataPath = Join-Path $fixtureRoot "config/mission.v1.json"
$executionMetadataPath = Join-Path $fixtureRoot "config/execution.v1.json"
$restoreRoot = Join-Path $fixtureRoot "restore"
$secretRoot = Join-Path $fixtureRoot "secrets"
$targetPath = Join-Path $workspaceRoot "NODAL_HANDOFF.md"
$sensitiveFixtureValue = "handoff-execution-sensitive-fixture-value"
$goal = "Prepare a verified product handoff, approve the exact reviewed scope, execute it, and retain guarded rollback evidence."

if (Test-Path $fixtureRoot) { Remove-Item $fixtureRoot -Recurse -Force }
New-Item -ItemType Directory -Path (Join-Path $workspaceRoot "src") -Force | Out-Null
[System.IO.File]::WriteAllText((Join-Path $workspaceRoot "README.md"), "# Real workspace handoff execution smoke")
$keyName = "api" + "_key"
[System.IO.File]::WriteAllText(
    (Join-Path $workspaceRoot "src/Program.cs"),
    ('var {0} = "{1}";{2}Console.WriteLine("fixture");' -f $keyName, $sensitiveFixtureValue, [Environment]::NewLine)
)

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
        throw "Workspace file count did not return to the baseline after rollback."
    }
    foreach ($key in $Expected.Keys) {
        if (-not $actual.ContainsKey($key) -or $actual[$key] -ne $Expected[$key]) {
            throw "Workspace file did not return to its baseline after rollback: $key"
        }
    }
}

function Assert-NoLeak([string]$Text, [string]$Surface) {
    if ($Text.IndexOf($workspaceRoot, [StringComparison]::OrdinalIgnoreCase) -ge 0 -or
        $Text.IndexOf($sensitiveFixtureValue, [StringComparison]::Ordinal) -ge 0) {
        throw "$Surface leaked the workspace root or sensitive fixture content."
    }
}

function Start-Pilot([string]$Suffix) {
    $stdout = Join-Path $RunnerTemp "pilot-real-workspace-handoff-$Suffix.stdout.log"
    $stderr = Join-Path $RunnerTemp "pilot-real-workspace-handoff-$Suffix.stderr.log"
    $process = Start-Process dotnet -ArgumentList @(
        "run",
        "--project", "src/OneBrain.Pilot/OneBrain.Pilot.csproj",
        "--configuration", "Release",
        "--no-build",
        "--",
        "--urls", $BaseUrl
    ) -PassThru -RedirectStandardOutput $stdout -RedirectStandardError $stderr

    for ($attempt = 0; $attempt -lt 30; $attempt++) {
        if ($process.HasExited) { throw "Pilot exited before the real workspace handoff execution surface became ready." }
        try {
            Invoke-RestMethod -Uri "$BaseUrl/api/workspace/selection" -TimeoutSec 2 | Out-Null
            return [pscustomobject]@{ Process = $process; Stdout = $stdout; Stderr = $stderr }
        }
        catch {
            Start-Sleep -Seconds 1
        }
    }
    throw "Real workspace handoff execution surface did not become ready."
}

function Extract-Token([string]$Html, [string]$FieldName) {
    $pattern = 'name="' + [regex]::Escape($FieldName) + '" value="(?<token>[0-9a-f]+)"'
    $match = [regex]::Match($Html, $pattern)
    if (-not $match.Success) { throw "Request token $FieldName was not rendered." }
    return $match.Groups["token"].Value
}

$baseline = Get-WorkspaceHashes
$env:ASPNETCORE_ENVIRONMENT = "Development"
$env:NODAL_OS_WORKSPACE_SELECTION_METADATA_PATH = $selectionMetadataPath
$env:NODAL_OS_WORKSPACE_SELECTION_SECRET_ROOT = $secretRoot
$env:NODAL_OS_WORKSPACE_MISSION_METADATA_PATH = $missionMetadataPath
$env:NODAL_OS_WORKSPACE_HANDOFF_EXECUTION_METADATA_PATH = $executionMetadataPath
$env:NODAL_OS_WORKSPACE_HANDOFF_RESTORE_ROOT = $restoreRoot
$first = $null
$second = $null

try {
    $first = Start-Pilot "first"
    $session = [Microsoft.PowerShell.Commands.WebRequestSession]::new()

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
            displayName = "CI Handoff Execution Workspace"
        } `
        -TimeoutSec 30
    if ($workspacePage.StatusCode -ne 200 -or
        $workspacePage.Content -notmatch 'data-selection-state="Ready"' -or
        $workspacePage.Content -notmatch 'data-persisted="true"') {
        throw "Workspace selection did not reach the persisted ready state."
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
        $missionPage.Content -notmatch 'data-execution-enabled="false"' -or
        $missionPage.Content -notmatch 'NODAL_HANDOFF.md') {
        throw "Mission draft did not reach its reviewed state before execution."
    }
    if (Test-Path $targetPath) { throw "Mission review mutated the workspace before approval." }

    $approvalPage = Invoke-WebRequest -Uri "$BaseUrl/mission/execution" -WebSession $session -TimeoutSec 15
    $executionToken = Extract-Token $approvalPage.Content "handoffExecutionToken"
    if ($approvalPage.StatusCode -ne 200 -or
        $approvalPage.Content -notmatch 'data-state="ReadyForApproval"' -or
        $approvalPage.Content -notmatch 'data-executed="false"' -or
        $approvalPage.Content -notmatch 'Aprobar alcance y ejecutar') {
        throw "Handoff execution surface did not reach the approval boundary."
    }
    Assert-NoLeak $approvalPage.Content "Approval surface"

    $executionPage = Invoke-WebRequest `
        -Uri "$BaseUrl/mission/execution" `
        -Method Post `
        -WebSession $session `
        -Headers @{ Origin = $BaseUrl } `
        -Body @{ handoffExecutionToken = $executionToken } `
        -TimeoutSec 30
    if ($executionPage.StatusCode -ne 200 -or
        $executionPage.Content -notmatch 'data-state="Completed"' -or
        $executionPage.Content -notmatch 'data-executed="true"' -or
        $executionPage.Content -notmatch 'data-verified="true"' -or
        $executionPage.Content -notmatch 'data-rollback-available="true"') {
        throw "Approved handoff action did not complete with verification and rollback readiness."
    }
    Assert-NoLeak $executionPage.Content "Execution surface"
    if (-not (Test-Path $targetPath)) { throw "Approved create-only handoff target was not created." }

    $execution = Invoke-RestMethod -Uri "$BaseUrl/api/mission/execution" -TimeoutSec 20
    if (-not $execution.accepted -or
        $execution.state -ne 3 -or
        -not $execution.executed -or
        -not $execution.verified -or
        -not $execution.rollbackAvailable -or
        $execution.networkUsed -or
        $execution.externalProcessUsed -or
        $execution.productAuthorityGranted -or
        $execution.relativeTargetPath -ne "NODAL_HANDOFF.md" -or
        $execution.actionKind -ne "CreateTextFile") {
        throw "Execution snapshot boundary flags are invalid."
    }
    $resultHash = (Get-FileHash $targetPath -Algorithm SHA256).Hash.ToLowerInvariant()
    if ($execution.resultSha256 -ne $resultHash) { throw "Execution result SHA-256 does not match the created target." }

    $missionControl = Invoke-RestMethod -Uri "$BaseUrl/api/mission-control" -TimeoutSec 20
    if (-not $missionControl.realMissionDraft -or
        $missionControl.actionExecutionState -ne "Completed" -or
        -not $missionControl.actionExecuted -or
        -not $missionControl.actionVerified -or
        -not $missionControl.actionRollbackAvailable -or
        $missionControl.actionApprovalAvailable -or
        $missionControl.productAuthorityGranted -or
        $missionControl.progressPercent -ne 100) {
        throw "Mission Control did not project the verified real handoff execution safely."
    }

    $executionMetadata = [System.IO.File]::ReadAllText($executionMetadataPath)
    Assert-NoLeak $executionMetadata "Execution metadata"
    if ($executionMetadata -notmatch 'filesystem.write.safe' -or
        $executionMetadata -notmatch 'NODAL_HANDOFF.md' -or
        $executionMetadata -notmatch $resultHash) {
        throw "Execution metadata is incomplete."
    }

    if (-not $first.Process.HasExited) { Stop-Process -Id $first.Process.Id -Force }
    $first = $null
    Start-Sleep -Seconds 1

    $second = Start-Pilot "restart"
    $rehydrated = Invoke-RestMethod -Uri "$BaseUrl/api/mission/execution" -TimeoutSec 20
    if (-not $rehydrated.accepted -or
        -not $rehydrated.rehydrated -or
        $rehydrated.state -ne 3 -or
        -not $rehydrated.executed -or
        -not $rehydrated.verified -or
        -not $rehydrated.rollbackAvailable -or
        $rehydrated.resultSha256 -ne $resultHash -or
        $rehydrated.productAuthorityGranted) {
        throw "Verified handoff execution did not rehydrate after process restart."
    }

    $restartSession = [Microsoft.PowerShell.Commands.WebRequestSession]::new()
    $rollbackForm = Invoke-WebRequest -Uri "$BaseUrl/mission/execution" -WebSession $restartSession -TimeoutSec 15
    $rollbackToken = Extract-Token $rollbackForm.Content "handoffExecutionToken"
    $rollbackPage = Invoke-WebRequest `
        -Uri "$BaseUrl/mission/rollback" `
        -Method Post `
        -WebSession $restartSession `
        -Headers @{ Origin = $BaseUrl } `
        -Body @{ handoffExecutionToken = $rollbackToken } `
        -TimeoutSec 30
    if ($rollbackPage.StatusCode -ne 200 -or
        $rollbackPage.Content -notmatch 'data-state="RolledBack"' -or
        $rollbackPage.Content -notmatch 'data-rolled-back="true"' -or
        $rollbackPage.Content -notmatch 'data-rollback-available="false"') {
        throw "Guarded rollback did not reach the verified rolled-back state."
    }
    Assert-NoLeak $rollbackPage.Content "Rollback surface"
    if (Test-Path $targetPath) { throw "Create-only rollback did not remove the exact created target." }

    $rolledBack = Invoke-RestMethod -Uri "$BaseUrl/api/mission/execution" -TimeoutSec 20
    if (-not $rolledBack.accepted -or
        $rolledBack.state -ne 4 -or
        -not $rolledBack.rolledBack -or
        $rolledBack.rollbackAvailable -or
        $rolledBack.productAuthorityGranted) {
        throw "Rollback snapshot boundary flags are invalid."
    }
    Assert-WorkspaceMatches $baseline
    if (Test-Path $restoreRoot) {
        $remainingSnapshots = @(Get-ChildItem $restoreRoot -File -Recurse -ErrorAction SilentlyContinue)
        if ($remainingSnapshots.Count -ne 0) { throw "Rollback left an app-local restore snapshot behind." }
    }
}
finally {
    foreach ($entry in @($first, $second)) {
        if ($null -ne $entry -and -not $entry.Process.HasExited) {
            Stop-Process -Id $entry.Process.Id -Force
        }
        if ($null -ne $entry) {
            Get-Content $entry.Stdout -ErrorAction SilentlyContinue
            Get-Content $entry.Stderr -ErrorAction SilentlyContinue
        }
    }
    Remove-Item Env:NODAL_OS_WORKSPACE_SELECTION_METADATA_PATH -ErrorAction SilentlyContinue
    Remove-Item Env:NODAL_OS_WORKSPACE_SELECTION_SECRET_ROOT -ErrorAction SilentlyContinue
    Remove-Item Env:NODAL_OS_WORKSPACE_MISSION_METADATA_PATH -ErrorAction SilentlyContinue
    Remove-Item Env:NODAL_OS_WORKSPACE_HANDOFF_EXECUTION_METADATA_PATH -ErrorAction SilentlyContinue
    Remove-Item Env:NODAL_OS_WORKSPACE_HANDOFF_RESTORE_ROOT -ErrorAction SilentlyContinue
    if (Test-Path $fixtureRoot) {
        Get-ChildItem $fixtureRoot -File -Recurse -Force -ErrorAction SilentlyContinue | ForEach-Object {
            $_.Attributes = [System.IO.FileAttributes]::Normal
        }
        Remove-Item $fixtureRoot -Recurse -Force
    }
}
