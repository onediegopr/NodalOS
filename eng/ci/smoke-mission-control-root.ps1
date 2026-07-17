param(
    [string]$RunnerTemp = $env:RUNNER_TEMP,
    [string]$BaseUrl = "http://127.0.0.1:5098"
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

if ([string]::IsNullOrWhiteSpace($RunnerTemp)) {
    $RunnerTemp = Join-Path ([System.IO.Path]::GetTempPath()) "nodal-os-ci"
}
New-Item -ItemType Directory -Path $RunnerTemp -Force | Out-Null

$stateRoot = Join-Path $RunnerTemp "mission-control-clean-state"
if (Test-Path $stateRoot) { Remove-Item $stateRoot -Recurse -Force }
New-Item -ItemType Directory -Path $stateRoot -Force | Out-Null

$env:ASPNETCORE_ENVIRONMENT = "Development"
$env:NODAL_OS_WORKSPACE_SELECTION_METADATA_PATH = Join-Path $stateRoot "workspaces/selection.v1.json"
$env:NODAL_OS_WORKSPACE_SELECTION_SECRET_ROOT = Join-Path $stateRoot "secrets/workspace-roots"
$env:NODAL_OS_WORKSPACE_MISSION_METADATA_PATH = Join-Path $stateRoot "missions/active.v1.json"
$env:NODAL_OS_WORKSPACE_HANDOFF_EXECUTION_METADATA_PATH = Join-Path $stateRoot "missions/active-handoff-execution.v1.json"
$env:NODAL_OS_WORKSPACE_HANDOFF_RESTORE_ROOT = Join-Path $stateRoot "missions/restore"
$env:NODAL_OS_BYOK_MODEL_METADATA_PATH = Join-Path $stateRoot "models/byok.v1.json"
$env:NODAL_OS_BYOK_MODEL_SECRET_ROOT = Join-Path $stateRoot "secrets/model-credentials"

$stdout = Join-Path $RunnerTemp "pilot-mission-control.stdout.log"
$stderr = Join-Path $RunnerTemp "pilot-mission-control.stderr.log"
$process = Start-Process dotnet -ArgumentList @(
    "run",
    "--project", "src/OneBrain.Pilot/OneBrain.Pilot.csproj",
    "--configuration", "Release",
    "--no-build",
    "--",
    "--urls", $BaseUrl
) -PassThru -RedirectStandardOutput $stdout -RedirectStandardError $stderr

try {
    $ready = $false
    for ($attempt = 0; $attempt -lt 30; $attempt++) {
        if ($process.HasExited) { throw "Pilot exited before Mission Control became ready." }
        try {
            $snapshot = Invoke-RestMethod -Uri "$BaseUrl/api/mission-control" -TimeoutSec 2
            $ready = $true
            break
        }
        catch {
            Start-Sleep -Seconds 1
        }
    }
    if (-not $ready) { throw "Mission Control did not become ready." }

    if ($snapshot.decision -ne "GO_MISSION_CONTROL_PRODUCT_SHELL_V1_READY" -or
        -not $snapshot.accepted -or
        -not $snapshot.localOnly -or
        -not $snapshot.readOnly -or
        $snapshot.fixtureBacked -or
        -not $snapshot.secretsExcluded) {
        throw "Mission Control snapshot boundary flags are invalid."
    }
    if ($snapshot.missionStatus -ne "NotStarted" -or $snapshot.progressPercent -ne 0) {
        throw "Mission Control should start from a real empty product state."
    }
    if ($snapshot.activeModel -ne "not configured" -or $snapshot.recentFallback) {
        throw "Mission Control exposed a synthetic model or fallback in product mode."
    }
    if ($snapshot.externalIoUsed -or $snapshot.networkUsed -or $snapshot.productAuthorityGranted) {
        throw "Mission Control crossed a forbidden boundary."
    }
    if ($snapshot.timeline.Count -ne 0 -or $snapshot.context.Count -lt 8 -or $snapshot.evidenceRefs.Count -ne 0) {
        throw "Mission Control clean-state projection is inconsistent."
    }

    $response = Invoke-WebRequest -Uri "$BaseUrl/" -TimeoutSec 10
    if ($response.StatusCode -ne 200) { throw "Mission Control root did not return 200." }
    if ($response.Headers["Cache-Control"] -notmatch "no-store") { throw "Mission Control root must be no-store." }
    if ($response.Headers["Content-Security-Policy"] -notmatch "default-src 'none'") { throw "Mission Control CSP is missing." }
    if ($response.Content -notmatch 'data-nodal-os="mission-control-product-shell"' -or
        $response.Content -notmatch 'data-fixture-backed="false"' -or
        $response.Content -notmatch 'data-onboarding-complete="false"' -or
        $response.Content -notmatch 'data-section-id="timeline"' -or
        $response.Content -notmatch 'data-section-id="context"' -or
        $response.Content -notmatch 'data-section-id="evidence"' -or
        $response.Content -notmatch 'data-section-id="diagnostics"' -or
        $response.Content -notmatch 'Ruta rápida' -or
        $response.Content -notmatch 'Seleccionar workspace local') {
        throw "Mission Control root is missing canonical product sections."
    }
    if ($response.Content -notmatch '#0D1117' -or
        $response.Content -notmatch '#161B22' -or
        $response.Content -notmatch '#1C2128' -or
        $response.Content -notmatch '#4F7CFF') {
        throw "Mission Control dark-first tokens are missing."
    }
    if ($response.Content -match '<script|<form|onclick=|https?://' -or
        $response.Content -match 'Probar ahora|fixture-fallback-chat|Primary fixture model|Fixture mission|Abrir laboratorio Pilot legado') {
        throw "Mission Control root exposed legacy, synthetic, executable or external content."
    }

    $legacy = Invoke-WebRequest -Uri "$BaseUrl/pilot/legacy" -TimeoutSec 10
    if ($legacy.StatusCode -ne 200 -or
        $legacy.Content -notmatch 'ONE BRAIN Pilot' -or
        $legacy.Content -notmatch 'Probar ahora') {
        throw "Legacy Pilot was not preserved under its explicit development route."
    }
}
finally {
    if (-not $process.HasExited) { Stop-Process -Id $process.Id -Force }
    Get-Content $stdout -ErrorAction SilentlyContinue
    Get-Content $stderr -ErrorAction SilentlyContinue
    if (Test-Path $stateRoot) { Remove-Item $stateRoot -Recurse -Force }
    @(
        "NODAL_OS_WORKSPACE_SELECTION_METADATA_PATH",
        "NODAL_OS_WORKSPACE_SELECTION_SECRET_ROOT",
        "NODAL_OS_WORKSPACE_MISSION_METADATA_PATH",
        "NODAL_OS_WORKSPACE_HANDOFF_EXECUTION_METADATA_PATH",
        "NODAL_OS_WORKSPACE_HANDOFF_RESTORE_ROOT",
        "NODAL_OS_BYOK_MODEL_METADATA_PATH",
        "NODAL_OS_BYOK_MODEL_SECRET_ROOT"
    ) | ForEach-Object { Remove-Item "Env:$_" -ErrorAction SilentlyContinue }
}
