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

$env:ASPNETCORE_ENVIRONMENT = "Development"
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
        -not $snapshot.fixtureBacked -or
        -not $snapshot.secretsExcluded) {
        throw "Mission Control snapshot boundary flags are invalid."
    }
    if ($snapshot.missionStatus -ne "Completed" -or $snapshot.progressPercent -ne 100) {
        throw "Mission Control did not project the verified fixture mission."
    }
    if ($snapshot.externalIoUsed -or $snapshot.networkUsed -or $snapshot.productAuthorityGranted) {
        throw "Mission Control crossed a forbidden boundary."
    }
    if ($snapshot.timeline.Count -lt 8 -or $snapshot.context.Count -lt 6 -or $snapshot.evidenceRefs.Count -lt 2) {
        throw "Mission Control projection is incomplete."
    }

    $response = Invoke-WebRequest -Uri "$BaseUrl/" -TimeoutSec 10
    if ($response.StatusCode -ne 200) { throw "Mission Control root did not return 200." }
    if ($response.Headers["Cache-Control"] -notmatch "no-store") { throw "Mission Control root must be no-store." }
    if ($response.Headers["Content-Security-Policy"] -notmatch "default-src 'none'") { throw "Mission Control CSP is missing." }
    if ($response.Content -notmatch 'data-nodal-os="mission-control-product-shell"' -or
        $response.Content -notmatch 'data-section-id="timeline"' -or
        $response.Content -notmatch 'data-section-id="context"' -or
        $response.Content -notmatch 'data-section-id="evidence"' -or
        $response.Content -notmatch 'data-section-id="diagnostics"') {
        throw "Mission Control root is missing canonical product sections."
    }
    if ($response.Content -notmatch '#0D1117' -or
        $response.Content -notmatch '#161B22' -or
        $response.Content -notmatch '#1C2128' -or
        $response.Content -notmatch '#4F7CFF') {
        throw "Mission Control dark-first tokens are missing."
    }
    if ($response.Content -match '<script|<form|onclick=|https?://' -or
        $response.Content -match 'Probar ahora') {
        throw "Mission Control root exposed legacy, executable or external content."
    }

    $legacy = Invoke-WebRequest -Uri "$BaseUrl/pilot/legacy" -TimeoutSec 10
    if ($legacy.StatusCode -ne 200 -or
        $legacy.Content -notmatch 'ONE BRAIN Pilot' -or
        $legacy.Content -notmatch 'Probar ahora') {
        throw "Legacy Pilot was not preserved under its explicit lab route."
    }
}
finally {
    if (-not $process.HasExited) { Stop-Process -Id $process.Id -Force }
    Get-Content $stdout -ErrorAction SilentlyContinue
    Get-Content $stderr -ErrorAction SilentlyContinue
}
