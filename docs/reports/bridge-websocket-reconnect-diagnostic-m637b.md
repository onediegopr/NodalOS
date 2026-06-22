# M637B - Bridge WebSocket Reconnect Diagnostic After CSP Patch

## Decision

M637B CERRADO / BRIDGE_WEBSOCKET_RECONNECT_DIAGNOSTIC_READY

## Evidence Analyzed

M637A reported:

- Health: `ok`
- Clientes: `1`
- Heartbeat: `OK`
- Estado: `reconnecting`
- WebSocket: `reconnecting`
- Logs: `Bridge connection closed` and `Bridge WebSocket error`
- Sidepanel rendered.
- Operar and Runtime rendered.
- No DevTools Console was provided.
- No literal `Test-NetConnection` result was provided.

## Current CSP

`script-src 'self'; object-src 'self'; connect-src 'self' http://127.0.0.1:* ws://127.0.0.1:* http://localhost:* ws://localhost:*;`

The CSP includes:

- `http://127.0.0.1:*`
- `ws://127.0.0.1:*`
- `http://localhost:*`
- `ws://localhost:*`

The CSP does not include:

- `http://*:*`
- `ws://*:*`
- `::1`

## WebSocket URL Construction

The service worker constructs the WebSocket URL as:

`ws://${config.host || DEFAULT_CONFIG.host}:${config.port || DEFAULT_CONFIG.port}/ws/extension`

Default config:

- Host: `127.0.0.1`
- Port: `8787`

Under the M637A evidence, the expected WebSocket URL is:

`ws://127.0.0.1:8787/ws/extension`

That URL matches the M637 CSP `ws://127.0.0.1:*` allowance.

## Host / Port / Token Findings

- Host and port are loaded from `chrome.storage.local` with defaults `127.0.0.1` and `8787`.
- Sidepanel shows and edits host, port, and token.
- Local pairing only runs for loopback hosts.
- Code accepts `127.0.0.1`, `localhost`, and `::1` as loopback hosts.
- M637 CSP only allows `127.0.0.1` and `localhost`, not `::1`.
- M637A evidence shows `127.0.0.1`, not `::1`.
- Token/auth mismatch remains possible, but M637A did not show `tokenError`.

## Runtime Tab Interpretation

The Runtime tab can show HTTP bridge health and debug data while WebSocket remains disconnected or reconnecting.

The M637A combination of Health `ok`, Clients `1`, Heartbeat `OK`, and WebSocket `reconnecting` means HTTP/debug endpoints are reachable, but the extension WebSocket is not stable.

## Hypothesis Summary

- CSP blocked WebSocket: possible, but not most likely without DevTools evidence because the observed `127.0.0.1:8787` WebSocket URL is allowed by CSP.
- Host mismatch: unlikely because the observed host is `127.0.0.1`.
- IPv6 `::1` mismatch: unlikely for this evidence because the observed host is not `::1`.
- Bridge accepts HTTP but rejects WS: likely.
- Token/auth mismatch: possible.
- Stale extension state/session reconnect: possible.
- UI state stale despite bridge connected: possible.
- CSP patch unrelated: possible.

## Most Likely Cause

Most likely: bridge WebSocket endpoint, token/session, or stale reconnect state issue after reload.

The current evidence is insufficient to justify immediate CSP adjustment or rollback.

## Required Next Evidence

- DevTools Console capture from the service worker during reconnect.
- Confirm whether a CSP violation appears.
- Confirm whether `ERR_CONNECTION_REFUSED` appears.
- Confirm WebSocket close/error details if visible.
- Confirm saved host/port/token values.
- Check bridge logs for `/ws/extension` accept/reject and token/protocol errors.

## Product Boundary

No product files were modified in M637B.

## Go / No-Go

- CSP adjustment: NO-GO.
- CSP rollback: NO-GO.
- Release evidence gate: NO-GO.
- Release public: NO-GO.
- JS changes: NO-GO.
- Runtime changes: NO-GO.

Recommended next milestone: M637C Manual DevTools CSP/WebSocket Evidence Capture.
