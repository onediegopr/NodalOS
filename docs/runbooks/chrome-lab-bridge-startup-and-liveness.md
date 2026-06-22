# Runbook — NODAL OS Chrome Lab Bridge: Startup & Liveness

**Owner:** Chrome Lab extension QA
**Applies to:** `src/OneBrain.ChromeLab.Bridge`, extension `browser-extension/onebrain-chrome-lab`
**Created:** M637G (2026-06-22)

## Why this runbook exists

The extension connects to a **local** bridge over `ws://127.0.0.1:8787/ws/extension`. If that bridge process is **not running**, Chrome reports `net::ERR_CONNECTION_REFUSED` and the service worker enters its reconnect backoff loop — exactly the symptom seen in **M637F** (≈1001 `ERR_CONNECTION_REFUSED` errors, **no** `invalid_token`, **no** close `1008`, **no** CSP violations).

`ERR_CONNECTION_REFUSED` with no `invalid_token`/`1008`/CSP means **there is no listener on the port**, not a bug in the extension or the bridge protocol. The bridge is a separate process that must be started explicitly; the extension does not and cannot launch it.

## Defaults (must match)

| Setting | Value | Source |
|---|---|---|
| Bridge host | `127.0.0.1` | `ChromeLabOptions.Host` default |
| Bridge port | `8787` | `ChromeLabOptions.Port` default |
| Extension host | `127.0.0.1` | `DEFAULT_CONFIG.host` in `service_worker.js` |
| Extension port | `8787` | `DEFAULT_CONFIG.port` in `service_worker.js` |
| WS endpoint | `/ws/extension` | `app.Map("/ws/extension", ...)` in `Program.cs` |
| Protocol version | `chrome-lab-v1` | `ChromeLabProtocol.Version` |

The bridge binds with `builder.WebHost.UseUrls($"http://{options.Host}:{options.Port}")` and enables WebSockets with `app.UseWebSockets()`. Default binding is loopback-only (`127.0.0.1`); LAN binding requires the explicit `--allow-lan` flag.

## Start the bridge

```pwsh
dotnet run --project src/OneBrain.ChromeLab.Bridge/OneBrain.ChromeLab.Bridge.csproj
```

On success the console prints:

```
onebrain-chrome-lab-bridge listening on http://127.0.0.1:8787
...
Now listening on: http://127.0.0.1:8787
Application started. Press Ctrl+C to shut down.
```

The bridge generates / loads an extension token in `config/chrome-lab.local.json`. The extension auto-pairs over loopback via `GET /pairing/local-token`, so you normally do not paste a token manually.

Keep this terminal open. Closing it (or Ctrl+C) stops the bridge and the extension will immediately start showing `ERR_CONNECTION_REFUSED`.

## Verify liveness (read-only)

```pwsh
pwsh -File tools/scripts/bridge-liveness-check.ps1
```

Expected when the bridge is up (all PASS):

```
[PASS] tcp 127.0.0.1:8787           listening
[PASS] listener process             PID=<pid> name=OneBrain.ChromeLab.Bridge addr=127.0.0.1
[PASS] http /health                 HTTP 200
[PASS] http /runtime                HTTP 200
[PASS] http /config/public          HTTP 200
[PASS] http /debug                  HTTP 200
[PASS] ws /ws/extension upgrade     accepted (101 Switching Protocols)
```

The script is **strictly read-only**: it never starts, stops, or kills a process. If port 8787 is held by a different process it only reports the PID/name so you can decide.

Manual one-liners if you prefer:

```pwsh
Test-NetConnection 127.0.0.1 -Port 8787          # TcpTestSucceeded must be True
Get-NetTCPConnection -LocalPort 8787 -State Listen | Select LocalAddress,OwningProcess
curl http://127.0.0.1:8787/runtime               # expect 200 ok=true
```

## Reload the extension

1. Confirm the liveness check is all PASS **first**.
2. `chrome://extensions` → reload **NODAL OS**.
3. Open the side panel → Runtime tab.
4. Expect: Health ok, Clients ≥ 1, Heartbeat OK, WebSocket **not** stuck in `reconnecting`.

## Troubleshooting

| Symptom | Cause | Action |
|---|---|---|
| `ERR_CONNECTION_REFUSED`, no `invalid_token`/`1008` | Bridge not running | Start the bridge; re-run liveness check |
| `tcp` PASS but `OwningProcess` is not `OneBrain.ChromeLab.Bridge` | Port 8787 taken by another app | Stop that app or change its port; the bridge default stays 8787 |
| `invalid_token` / close `1008` | Token/protocol mismatch (NOT a liveness issue) | See M637C/M637E; clear saved token and re-pair over loopback |
| Reconnect loop persists with bridge confirmed live | Client-side issue | Re-open M637-series diagnostics (already fixed the reload race in M637E) |

## Scope note

This is an **operational** prerequisite. The bridge being up is required for any extension QA. The extension cannot auto-start the bridge by design (it is a local server the operator runs).
