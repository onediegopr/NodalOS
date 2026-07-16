param(
    [string]$RunnerTemp = $env:RUNNER_TEMP,
    [string]$BaseUrl = "http://127.0.0.1:5100"
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

if ([string]::IsNullOrWhiteSpace($RunnerTemp)) {
    $RunnerTemp = Join-Path ([System.IO.Path]::GetTempPath()) "nodal-os-ci"
}
New-Item -ItemType Directory -Path $RunnerTemp -Force | Out-Null

$fixtureRoot = Join-Path $RunnerTemp "nodal-real-workspace-mission-fixture"
$workspaceRoot = Join-Path $fixtureRoot "workspace"
$selectionMetadataPath = Join-Path $fixtureRoot "config/selection.v1.json"
$missionMetadataPath = Join-Path $fixtureRoot "config/mission.v1.json"
$secretRoot = Join-Path $fixtureRoot "secrets"
$targetPath = Join-Path $workspaceRoot "NODAL_HANDOFF.md"
$sensitiveFixtureValue = "mission-draft-sensitive-fixture-value"
$goal = "Prepare a verified product handoff for the selected workspace and make the next safe action explicit."
$missionDraftReadyForReview = 1
$actionKindCreateTextFile = 0
$actionStateReadyForReview = 0

if (Test-Path $fixtureRoot) { Remove-Item $fixtureRoot -Recurse -Force }
New-Item -ItemType Directory -Path (Join-Path $workspaceRoot "src") -Force | Out-Null
[System.IO.File]::WriteAllText((Join-Path $workspaceRoot "README.md"), "# Real workspace mission smoke")
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

function Assert-WorkspaceUnchanged([hashtable]$Before) {
    $after = Get-WorkspaceHashes
    if ($Before.Count -ne $after.Count) { throw "Workspace file count changed during mission draft preparation." }
    foreach ($key in $Before.Keys) {
        if (-not $after.ContainsKey($key) -or $after[$key] -ne $Before[$key]) {
            throw "Workspace file changed during mission draft preparation: $key"
        }
    }
    if (Test-Path $targetPath) { throw "Reviewed mission candidate executed unexpectedly." }
}

function Start-Pilot([string]$Suffix) {
    $stdout = Join-Path $RunnerTemp "pilot-real-workspace-mission-$Suffix.stdout.log"
    $stderr = Join-Path $RunnerTemp "pilot-real-workspace-mission-$Suffix.stderr.log"
    $process = Start-Process dotnet -ArgumentList @(
        "run",
        "--project", "src/OneBrain.Pilot/OneBrain.Pilot.csproj",
        "--configuration", "Release",
        "--no-build",
        "--",
        "--urls", $BaseUrl
    ) -PassThru -RedirectStandardOutput $stdout -RedirectStandardError $stderr

    for ($attempt = 0; $attempt -lt 30; $attempt++) {
        if ($process.HasExited) { throw "Pilot exited before the real workspace mission surface became ready." }
        try {
            Invoke-RestMethod -Uri "$BaseUrl/api/workspace/selection" -TimeoutSec 2 | Out-Null
            return [pscustomobject]@{ Process = $process; Stdout = $stdout; Stderr = $stderr }
        }
        catch {
            Start-Sleep -Seconds 1
        }
    }
    throw "Real workspace mission surface did not become ready."
}

function Extract-Token([string]$Html, [string]$FieldName) {
    $pattern = 'name="' + [regex]::Escape($FieldName) + '" value="(?<token>[0-9a-f]+)"'
    $match = [regex]::Match($Html, $pattern)
    if (-not $match.Success) { throw "Request token $FieldName was not rendered." }
    return $match.Groups["token"].Value
}

$before = Get-WorkspaceHashes
$env:ASPNETCORE_ENVIRONMENT = "Development"
$env:NODAL_OS_WORKSPACE_SELECTION_METADATA_PATH = $selectionMetadataPath
$env:NODAL_OS_WORKSPACE_SELECTION_SECRET_ROOT = $secretRoot
$env:NODAL_OS_WORKSPACE_MISSION_METADATA_PATH = $missionMetadataPath
$first = $null
$second = $null

try {
    $first = Start-Pilot "first"
    $session = [Microsoft.PowerShell.Commands.WebRequestSession]::new()

    $workspaceForm = Invoke-WebRequest -Uri "$BaseUrl/workspace/select" -WebSession $session -TimeoutSec 10
    $workspaceToken = Extract-Token $workspaceForm.Content "workspaceSelectionToken"
    $workspacePage = Invoke-WebRequest `
        -Uri "$BaseUrl/workspace/select" `
        -Method Post `
        -WebSession $session `
        -Headers @{ Origin = $BaseUrl } `
        -Body @{
            workspaceSelectionToken = $workspaceToken
            rootPath = $workspaceRoot
            displayName = "CI Mission Workspace"
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
        $missionPage.Content -notmatch 'data-persisted="true"' -or
        $missionPage.Content -notmatch 'data-rehydrated="true"' -or
        $missionPage.Content -notmatch 'data-candidate-state="ReadyForReview"' -or
        $missionPage.Content -notmatch 'data-execution-enabled="false"' -or
        $missionPage.Content -notmatch 'NODAL_HANDOFF.md') {
        throw "Real workspace mission draft surface did not reach the reviewed persisted state."
    }
    if ($missionPage.Content.IndexOf($workspaceRoot, [StringComparison]::OrdinalIgnoreCase) -ge 0 -or
        $missionPage.Content.IndexOf($sensitiveFixtureValue, [StringComparison]::OrdinalIgnoreCase) -ge 0) {
        throw "Mission draft surface leaked the workspace root or sensitive fixture value."
    }

    $draft = Invoke-RestMethod -Uri "$BaseUrl/api/mission/draft" -TimeoutSec 20
    if (-not $draft.accepted -or
        -not $draft.persisted -or
        -not $draft.rehydrated -or
        $draft.state -ne $missionDraftReadyForReview -or
        $draft.candidate.kind -ne $actionKindCreateTextFile -or
        $draft.candidate.state -ne $actionStateReadyForReview -or
        $draft.candidate.relativeTargetPath -ne "NODAL_HANDOFF.md" -or
        $draft.candidate.executionEnabled -or
        $draft.workspaceFilesystemMutated -or
        $draft.networkUsed -or
        $draft.productAuthorityGranted) {
        throw "Mission draft snapshot boundary flags are invalid."
    }

    $missionControl = Invoke-RestMethod -Uri "$BaseUrl/api/mission-control" -TimeoutSec 20
    if (-not $missionControl.realMissionDraft -or
        -not $missionControl.missionDraftPersisted -or
        $missionControl.goal -ne $goal -or
        $missionControl.actionCandidateTarget -ne "NODAL_HANDOFF.md" -or
        $missionControl.actionExecutionEnabled -or
        $missionControl.productAuthorityGranted) {
        throw "Mission Control did not project the reviewed real workspace mission safely."
    }
    $controlPage = Invoke-WebRequest -Uri "$BaseUrl/" -TimeoutSec 20
    if ($controlPage.Content -notmatch 'data-real-mission-draft="true"' -or
        $controlPage.Content -notmatch 'data-mission-draft-persisted="true"' -or
        $controlPage.Content -notmatch 'data-action-execution-enabled="false"' -or
        $controlPage.Content -notmatch 'NODAL_HANDOFF.md' -or
        $controlPage.Content -notmatch '/mission/new') {
        throw "Mission Control real mission anchors are missing."
    }
    if ($controlPage.Content.IndexOf($workspaceRoot, [StringComparison]::OrdinalIgnoreCase) -ge 0 -or
        $controlPage.Content.IndexOf($sensitiveFixtureValue, [StringComparison]::OrdinalIgnoreCase) -ge 0) {
        throw "Mission Control leaked the workspace root or sensitive fixture value."
    }

    if (-not (Test-Path $missionMetadataPath)) { throw "Mission draft metadata was not persisted." }
    $missionMetadata = [System.IO.File]::ReadAllText($missionMetadataPath)
    if ($missionMetadata.IndexOf($workspaceRoot, [StringComparison]::OrdinalIgnoreCase) -ge 0 -or
        $missionMetadata.IndexOf($sensitiveFixtureValue, [StringComparison]::OrdinalIgnoreCase) -ge 0 -or
        $missionMetadata -notmatch 'NODAL_HANDOFF.md') {
        throw "Mission draft metadata is unsafe or incomplete."
    }
    Assert-WorkspaceUnchanged $before

    if (-not $first.Process.HasExited) { Stop-Process -Id $first.Process.Id -Force }
    $first = $null
    Start-Sleep -Seconds 1

    $second = Start-Pilot "restart"
    $rehydrated = Invoke-RestMethod -Uri "$BaseUrl/api/mission/draft" -TimeoutSec 20
    if (-not $rehydrated.accepted -or
        -not $rehydrated.persisted -or
        -not $rehydrated.rehydrated -or
        $rehydrated.goalRedacted -ne $goal -or
        $rehydrated.candidate.state -ne $actionStateReadyForReview -or
        $rehydrated.candidate.executionEnabled -or
        $rehydrated.workspaceFilesystemMutated -or
        $rehydrated.productAuthorityGranted) {
        throw "Real workspace mission draft did not rehydrate after process restart."
    }
    $rehydratedControl = Invoke-RestMethod -Uri "$BaseUrl/api/mission-control" -TimeoutSec 20
    if (-not $rehydratedControl.realMissionDraft -or
        $rehydratedControl.goal -ne $goal -or
        $rehydratedControl.actionExecutionEnabled -or
        $rehydratedControl.productAuthorityGranted) {
        throw "Mission Control did not restore the real mission projection after restart."
    }
    Assert-WorkspaceUnchanged $before
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
    if (Test-Path $fixtureRoot) {
        Get-ChildItem $fixtureRoot -File -Recurse -Force -ErrorAction SilentlyContinue | ForEach-Object {
            $_.Attributes = [System.IO.FileAttributes]::Normal
        }
        Remove-Item $fixtureRoot -Recurse -Force
    }
}