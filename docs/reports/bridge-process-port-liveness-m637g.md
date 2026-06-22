# M637G — Bridge Process / Port Liveness Diagnostic & Fix

**Decision:** BRIDGE_PROCESS_PORT_LIVENESS_FIX_READY
**Branch:** chrome-lab-001-extension-local-ai-bridge
**Prior:** M637E CLOSED (race fix) → M637F BLOCKED (`ERR_CONNECTION_REFUSED`)

## Root Cause

**Operational, not code.** The M637F DevTools evidence — `WebSocket connection to 'ws://127.0.0.1:8787/ws/extension' failed: net::ERR_CONNECTION_REFUSED` (≈1001 errors), **no** `invalid_token`, **no** close `1008`, **no** CSP violations — is the textbook signature of **no TCP listener on the port**. The bridge process was not running (or had exited) during the test, so the WebSocket upgrade never reached any server.

This is exactly the branch flagged at the end of M637E: "If the bridge was simply not running, the race fix improves robustness but the user must start the bridge."

## Live Proof (the bridge works when started)

The bridge was started locally and every endpoint probed with real requests:

```
dotnet run --project src/OneBrain.ChromeLab.Bridge/OneBrain.ChromeLab.Bridge.csproj
→ onebrain-chrome-lab-bridge listening on http://127.0.0.1:8787
```

| Probe | Result |
|---|---|
| `Test-NetConnection 127.0.0.1:8787` | TcpTestSucceeded = **True** |
| Listening socket | `127.0.0.1:8787`, **PID 6620**, process `OneBrain.ChromeLab.Bridge` |
| `GET /health` | **200** `{"ok":true,...}` |
| `GET /runtime` | **200** `{"ok":true,...,"host":"127.0.0.1","port":8787}` |
| `GET /debug` | **200** |
| `GET /config/public` | **200** `{"protocolVersion":"chrome-lab-v1","requiresToken":true}` |
| `GET /ws/extension` **with** upgrade headers | **101 Switching Protocols** |
| `GET /ws/extension` **without** upgrade headers | **400** (correct guard) |
| `/debug` connectedCount while live | **1** (events: `ws.accepted`, `extension.hello`, …) |

A real extension client completed `extension.hello` against the running bridge (`connectedCount: 1`), confirming the full end-to-end path works once the listener exists — no reconnect loop, no `invalid_token`. The test process was then stopped and port 8787 confirmed freed.

## Diagnostic Answers

| # | Question | Answer |
|---|---|---|
| 1 | Correct run command? | `dotnet run --project src/OneBrain.ChromeLab.Bridge/OneBrain.ChromeLab.Bridge.csproj` |
| 2 | Listens on 127.0.0.1:8787? | Yes |
| 3 | `Test-NetConnection 8787` true? | Yes |
| 4 | `/runtime` 200? | Yes |
| 5 | `/debug` 200? | Yes |
| 6 | `/ws/extension` accepts upgrade? | Yes (101) |
| 7 | Bridge crashes after start? | No |
| 8 | Other process on 8787? | No (only the bridge) |
| 9 | Binds IPv6/0.0.0.0 instead of 127.0.0.1? | No — binds `127.0.0.1` (loopback-only by default) |
| 10 | Extension host/port mismatch? | No — extension default `127.0.0.1:8787` matches bridge default |
| 11 | Reproduces when bridge confirmed listening? | No — connects fine |
| 12 | Operational, not code? | **Yes** |
| 13 | Need a diagnostic helper? | Yes — added (see below) |

## Fix (operational — no product/bridge code changed)

1. **Read-only liveness diagnostic:** `tools/scripts/bridge-liveness-check.ps1`
   Probes TCP, `/health`, `/runtime`, `/config/public`, `/debug`, and the `/ws/extension` upgrade; reports the listening PID/process. **Never** starts, stops, or kills any process — a port conflict is only reported. With the bridge down it prints exactly the M637F diagnosis (`NO listener (ERR_CONNECTION_REFUSED)`), distinguishing "bridge not running" from a real bug in one command.

2. **Runbook:** `docs/runbooks/chrome-lab-bridge-startup-and-liveness.md`
   Start command, expected console output, liveness verification, extension reload order, and a troubleshooting table mapping `ERR_CONNECTION_REFUSED` / `invalid_token` / `1008` to causes.

## What Was NOT Changed

- Bridge code (`src/OneBrain.ChromeLab.Bridge/**`) — unchanged (proven correct as-is)
- `service_worker.js`, `manifest.json`, CSP, sidepanel.*, content_script.js, recipe_core.js — unchanged
- Permissions, host_permissions, storage keys, protocol version, port/alarm names, runtime architecture — unchanged

## Next Milestone

**M637H — Manual Reload QA After Bridge Liveness Fix.** Precondition: operator starts the bridge and confirms `bridge-liveness-check.ps1` reports all PASS **before** reloading the extension. If, with the bridge confirmed live, the extension still loops, that is a separate genuine client bug.
