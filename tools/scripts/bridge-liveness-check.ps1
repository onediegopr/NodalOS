#requires -Version 7.0
<#
.SYNOPSIS
    Read-only liveness diagnostic for the OneBrain Chrome Lab bridge.

.DESCRIPTION
    Verifies whether the local NODAL OS Chrome Lab bridge is reachable on
    127.0.0.1:8787 and probes the HTTP and WebSocket endpoints the extension
    depends on. This is the diagnostic that distinguishes a real connection
    bug from the operational case where the bridge process is simply not
    running (the cause of the ERR_CONNECTION_REFUSED reconnect loop seen in
    M637F).

    This script is strictly READ-ONLY. It NEVER starts, stops, or kills any
    process. If port 8787 is held by another process it only REPORTS the PID
    and process name so the operator can decide what to do.

.PARAMETER BridgeHost
    Host to probe. Default 127.0.0.1 (the extension default).

.PARAMETER Port
    Port to probe. Default 8787 (the extension default).

.EXAMPLE
    pwsh -File tools/scripts/bridge-liveness-check.ps1

.NOTES
    To START the bridge (separate, explicit operator action), see
    docs/runbooks/chrome-lab-bridge-startup-and-liveness.md
#>
[CmdletBinding()]
param(
    [string]$BridgeHost = '127.0.0.1',
    [int]$Port = 8787
)

$ErrorActionPreference = 'SilentlyContinue'
$base = "http://${BridgeHost}:${Port}"
$results = [ordered]@{}
$anyFail = $false

function Write-Check($name, $ok, $detail) {
    $tag = if ($ok) { 'PASS' } else { 'FAIL' }
    Write-Host ("[{0}] {1,-28} {2}" -f $tag, $name, $detail)
}

Write-Host "NODAL OS Chrome Lab bridge liveness check (read-only)"
Write-Host "Target: $base/ws/extension"
Write-Host ""

# 1. TCP reachability
$tcp = Test-NetConnection -ComputerName $BridgeHost -Port $Port -WarningAction SilentlyContinue
$tcpOk = [bool]$tcp.TcpTestSucceeded
$results['tcp'] = $tcpOk
if (-not $tcpOk) { $anyFail = $true }
Write-Check 'tcp 127.0.0.1:8787' $tcpOk $(if ($tcpOk) { 'listening' } else { 'NO listener (ERR_CONNECTION_REFUSED in the extension)' })

# 2. Who is listening (read-only; never kills)
$listener = Get-NetTCPConnection -LocalPort $Port -State Listen -ErrorAction SilentlyContinue | Select-Object -First 1
if ($listener) {
    $proc = Get-Process -Id $listener.OwningProcess -ErrorAction SilentlyContinue
    $results['observedListeningAddress'] = "$($listener.LocalAddress):$($listener.LocalPort)"
    $results['observedPid'] = "$($listener.OwningProcess)"
    $results['observedProcessName'] = "$($proc.ProcessName)"
    Write-Check 'listener process' $true "PID=$($listener.OwningProcess) name=$($proc.ProcessName) addr=$($listener.LocalAddress)"
    if ($proc.ProcessName -and $proc.ProcessName -ne 'OneBrain.ChromeLab.Bridge') {
        Write-Host "      NOTE: port held by '$($proc.ProcessName)', not the bridge. Possible port conflict (reported only, not killed)."
    }
} else {
    $results['observedListeningAddress'] = ''
    $results['observedPid'] = ''
    $results['observedProcessName'] = ''
    Write-Check 'listener process' $false 'no process listening on 8787'
}

# 3. HTTP endpoints
foreach ($ep in @('health', 'runtime', 'config/public', 'debug')) {
    $code = $null
    try {
        $resp = Invoke-WebRequest -Uri "$base/$ep" -TimeoutSec 4 -SkipHttpErrorCheck
        $code = [int]$resp.StatusCode
    } catch { $code = 0 }
    $ok = ($code -eq 200)
    $results["http_$($ep -replace '/','_')"] = $ok
    if (-not $ok) { $anyFail = $true }
    Write-Check "http /$ep" $ok "HTTP $code"
}

# 4. WebSocket endpoint upgrade (expect HTTP 101 Switching Protocols)
$wsOk = $false
try {
    $cws = [System.Net.WebSockets.ClientWebSocket]::new()
    $cts = [System.Threading.CancellationTokenSource]::new(4000)
    $cws.ConnectAsync([Uri]"ws://${BridgeHost}:${Port}/ws/extension", $cts.Token).GetAwaiter().GetResult()
    $wsOk = ($cws.State -eq [System.Net.WebSockets.WebSocketState]::Open)
    $cws.Dispose()
} catch { $wsOk = $false }
$results['ws_extension_upgrade'] = $wsOk
if (-not $wsOk) { $anyFail = $true }
Write-Check 'ws /ws/extension upgrade' $wsOk $(if ($wsOk) { 'accepted (101 Switching Protocols)' } else { 'upgrade NOT accepted' })

Write-Host ""
if ($anyFail) {
    Write-Host "RESULT: bridge NOT fully live. If tcp failed, START the bridge first:"
    Write-Host "  dotnet run --project src/OneBrain.ChromeLab.Bridge/OneBrain.ChromeLab.Bridge.csproj"
    Write-Host "See docs/runbooks/chrome-lab-bridge-startup-and-liveness.md"
    exit 1
}

Write-Host "RESULT: bridge is LIVE on $base and /ws/extension accepts WebSocket upgrades."
Write-Host "If the extension still shows reconnecting, reload it from chrome://extensions."
exit 0
