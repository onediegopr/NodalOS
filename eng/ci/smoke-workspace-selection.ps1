param(
    [string]$RunnerTemp = $env:RUNNER_TEMP,
    [string]$BaseUrl = "http://127.0.0.1:5099"
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

if ([string]::IsNullOrWhiteSpace($RunnerTemp)) {
    $RunnerTemp = Join-Path ([System.IO.Path]::GetTempPath()) "nodal-os-ci"
}
New-Item -ItemType Directory -Path $RunnerTemp -Force | Out-Null

$fixtureRoot = Join-Path $RunnerTemp "nodal-workspace-selection-fixture"
$workspaceRoot = Join-Path $fixtureRoot "workspace"
$metadataPath = Join-Path $fixtureRoot "config/selection.v1.json"
$secretRoot = Join-Path $fixtureRoot "secrets"
$fakeSecret = "s" + "k-workspace-selection-smoke-secret-value-123456789"
if (Test-Path $fixtureRoot) { Remove-Item $fixtureRoot -Recurse -Force }
New-Item -ItemType Directory -Path (Join-Path $workspaceRoot "src") -Force | Out-Null
[System.IO.File]::WriteAllText((Join-Path $workspaceRoot "README.md"), "# Workspace selection smoke")
[System.IO.File]::WriteAllText(
    (Join-Path $workspaceRoot "src/Program.cs"),
    ('var api_key = "{0}";{1}Console.WriteLine("fixture");' -f $fakeSecret, [Environment]::NewLine)
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
    if ($Before.Count -ne $after.Count) { throw "Workspace file count changed during selection." }
    foreach ($key in $Before.Keys) {
        if (-not $after.ContainsKey($key) -or $after[$key] -ne $Before[$key]) {
            throw "Workspace file changed during selection: $key"
        }
    }
}

function Start-Pilot([string]$Suffix) {
    $stdout = Join-Path $RunnerTemp "pilot-workspace-selection-$Suffix.stdout.log"
    $stderr = Join-Path $RunnerTemp "pilot-workspace-selection-$Suffix.stderr.log"
    $process = Start-Process dotnet -ArgumentList @(
        "run",
        "--project", "src/OneBrain.Pilot/OneBrain.Pilot.csproj",
        "--configuration", "Release",
        "--no-build",
        "--",
        "--urls", $BaseUrl
    ) -PassThru -RedirectStandardOutput $stdout -RedirectStandardError $stderr

    for ($attempt = 0; $attempt -lt 30; $attempt++) {
        if ($process.HasExited) { throw "Pilot exited before workspace selection became ready." }
        try {
            Invoke-RestMethod -Uri "$BaseUrl/api/workspace/selection" -TimeoutSec 2 | Out-Null
            return [pscustomobject]@{ Process = $process; Stdout = $stdout; Stderr = $stderr }
        }
        catch {
            Start-Sleep -Seconds 1
        }
    }
    throw "Workspace selection surface did not become ready."
}

$before = Get-WorkspaceHashes
$env:ASPNETCORE_ENVIRONMENT = "Development"
$env:NODAL_OS_WORKSPACE_SELECTION_METADATA_PATH = $metadataPath
$env:NODAL_OS_WORKSPACE_SELECTION_SECRET_ROOT = $secretRoot
$first = $null
$second = $null

try {
    $first = Start-Pilot "first"
    $session = [Microsoft.PowerShell.Commands.WebRequestSession]::new()
    $formPage = Invoke-WebRequest -Uri "$BaseUrl/workspace/select" -WebSession $session -TimeoutSec 10
    $tokenMatch = [regex]::Match($formPage.Content, 'name="workspaceSelectionToken" value="(?<token>[0-9a-f]+)"')
    if (-not $tokenMatch.Success) { throw "Workspace selection CSRF token was not rendered." }
    $token = $tokenMatch.Groups["token"].Value

    $selectedPage = Invoke-WebRequest `
        -Uri "$BaseUrl/workspace/select" `
        -Method Post `
        -WebSession $session `
        -Headers @{ Origin = $BaseUrl } `
        -Body @{
            workspaceSelectionToken = $token
            rootPath = $workspaceRoot
            displayName = "CI Real Workspace"
        } `
        -TimeoutSec 30

    if ($selectedPage.StatusCode -ne 200 -or
        $selectedPage.Content -notmatch 'data-selection-state="Ready"' -or
        $selectedPage.Content -notmatch 'data-persisted="true"' -or
        $selectedPage.Content -notmatch 'CI Real Workspace') {
        throw "Workspace selection surface did not reach the persisted ready state."
    }
    if ($selectedPage.Content.IndexOf($workspaceRoot, [StringComparison]::OrdinalIgnoreCase) -ge 0 -or
        $selectedPage.Content.IndexOf($fakeSecret, [StringComparison]::OrdinalIgnoreCase) -ge 0) {
        throw "Workspace selection surface leaked the root or a secret."
    }

    $snapshot = Invoke-RestMethod -Uri "$BaseUrl/api/workspace/selection" -TimeoutSec 15
    if (-not $snapshot.accepted -or
        -not $snapshot.persisted -or
        -not $snapshot.rehydrated -or
        -not $snapshot.realFilesystemRead -or
        $snapshot.workspaceFilesystemMutated -or
        $snapshot.networkUsed -or
        -not $snapshot.secretsExcluded -or
        $snapshot.productAuthorityGranted -or
        $snapshot.filesRead -lt 2) {
        throw "Workspace selection snapshot boundary flags are invalid."
    }

    $mission = Invoke-RestMethod -Uri "$BaseUrl/api/mission-control" -TimeoutSec 15
    if (-not $mission.workspaceSelected -or
        -not $mission.workspacePersisted -or
        $mission.workspaceFilesRead -lt 2 -or
        $mission.productAuthorityGranted) {
        throw "Mission Control did not project the selected workspace safely."
    }
    $missionPage = Invoke-WebRequest -Uri "$BaseUrl/" -TimeoutSec 15
    if ($missionPage.Content -notmatch 'data-workspace-selected="true"' -or
        $missionPage.Content -notmatch 'data-workspace-persisted="true"' -or
        $missionPage.Content -notmatch 'CI Real Workspace') {
        throw "Mission Control selected-workspace anchors are missing."
    }
    if ($missionPage.Content.IndexOf($workspaceRoot, [StringComparison]::OrdinalIgnoreCase) -ge 0 -or
        $missionPage.Content.IndexOf($fakeSecret, [StringComparison]::OrdinalIgnoreCase) -ge 0) {
        throw "Mission Control leaked the selected root or a secret."
    }

    if (-not (Test-Path $metadataPath)) { throw "Workspace selection metadata was not persisted." }
    $metadata = [System.IO.File]::ReadAllText($metadataPath)
    if ($metadata.IndexOf($workspaceRoot, [StringComparison]::OrdinalIgnoreCase) -ge 0 -or
        $metadata.IndexOf($fakeSecret, [StringComparison]::OrdinalIgnoreCase) -ge 0 -or
        $metadata -notmatch 'windows-dpapi') {
        throw "Workspace selection metadata is unsafe or incomplete."
    }
    $protectedFiles = @(Get-ChildItem $secretRoot -File -Filter '*.bin' -Force)
    if ($protectedFiles.Count -ne 1) {
        throw "Protected workspace root reference was not persisted exactly once."
    }
    Assert-WorkspaceUnchanged $before

    if (-not $first.Process.HasExited) { Stop-Process -Id $first.Process.Id -Force }
    $first = $null
    Start-Sleep -Seconds 1

    $second = Start-Pilot "restart"
    $rehydrated = Invoke-RestMethod -Uri "$BaseUrl/api/workspace/selection" -TimeoutSec 15
    if (-not $rehydrated.accepted -or
        -not $rehydrated.persisted -or
        -not $rehydrated.rehydrated -or
        $rehydrated.displayNameRedacted -ne "CI Real Workspace" -or
        $rehydrated.workspaceFilesystemMutated -or
        $rehydrated.productAuthorityGranted) {
        throw "Workspace selection did not rehydrate after process restart."
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
    if (Test-Path $fixtureRoot) {
        Get-ChildItem $fixtureRoot -File -Recurse -Force -ErrorAction SilentlyContinue | ForEach-Object {
            $_.Attributes = [System.IO.FileAttributes]::Normal
        }
        Remove-Item $fixtureRoot -Recurse -Force
    }
}
