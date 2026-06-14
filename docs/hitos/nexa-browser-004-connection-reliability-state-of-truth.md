# NEXA Browser-004 - Connection Reliability & State-of-Truth

Date: 2026-06-14
Branch: `chrome-lab-001-extension-local-ai-bridge`

## Objective

Browser-004 addresses the Browser-003.5 blocker:

```text
No extension client connected
```

The goal is to make the local .NET bridge and Chrome MV3 extension connection diagnosable, token-bound, reconnectable, and less dependent on volatile service worker globals.

## Decision

Chosen architecture: **Option A - hardened service worker WebSocket**.

Reason:

- The current Recipe Runner and content-tool routing already live in the service worker.
- Moving the interactive WebSocket to the side panel would be a larger redesign.
- Browser-004 is a reliability and state-of-truth hito, not a rewrite.

Known limitation:

- MV3 service workers can still be suspended by Chrome. Browser-004 mitigates this with alarms, session state, reconnect, heartbeat, and debug endpoints.

## Bridge Changes

The bridge now has:

- WebSocket frame accumulation until `EndOfMessage`.
- Support for messages larger than a single 64 KB receive buffer.
- Per-message JSON error handling.
- Protocol error responses without killing the entire process.
- Explicit connected-client diagnostics.
- Protocol event ring buffer.
- Runtime diagnostics.
- Clear `409 Conflict` when `/api/runs` is called without an extension client.
- Token-bound WebSocket handshake.
- Default bind to `127.0.0.1`.
- LAN bind only with explicit `--allow-lan`.

New endpoints:

- `GET /clients`
- `GET /runtime`
- `GET /last-events`
- `GET /debug`

No endpoint exposes:

- API key
- connection token
- password values
- raw page text
- raw input values

## Protocol Changes

Extension handshake:

```json
{
  "type": "extension.hello",
  "clientId": "...",
  "protocolVersion": "chrome-lab-v1",
  "extensionVersion": "0.1.0",
  "browser": "Chrome",
  "token": "...",
  "resumeRunId": null
}
```

Bridge response:

```json
{
  "type": "engine.hello",
  "protocolVersion": "chrome-lab-v1",
  "engineVersion": "0.1.0",
  "serverTime": "...",
  "resync": {
    "run": null,
    "pendingRequest": null
  }
}
```

Heartbeat:

- Extension sends `extension.ping`.
- Bridge replies `engine.pong`.
- Missing pongs mark the connection degraded and trigger reconnect.

## Token

The bridge reads the connection token from:

- environment variable `NEXA_CHROME_BRIDGE_TOKEN`
- local ignored JSON `config/chrome-lab.local.json` property `connectionToken`
- generated runtime token printed masked in console if missing

The extension stores the token only in Chrome local storage after the user enters it in Runtime.

No token is committed to git.

## Extension Changes

The service worker now has:

- `chrome.runtime.onStartup`
- `chrome.runtime.onInstalled`
- reconnect with backoff and jitter
- `chrome.alarms` keepalive/resync
- app-level heartbeat
- `chrome.storage.session` state snapshot
- outgoing message queue with size limit
- runtime debug fetches
- clearer state publishing to side panel

Persisted session state:

- `clientId`
- `currentRunId`
- `currentRequestId`
- `connectionState`
- `lastConnectedAt`
- `lastSeenAt`
- `recipeRunner`
- limited `pendingOutgoingMessages`

Not persisted:

- API keys
- passwords
- credential values
- raw visible text dumps

## Side Panel Changes

Runtime now shows:

- token input
- reconnect button
- refresh debug button
- client count
- diagnostic reason
- client id
- last seen
- protocol version

Example diagnostics:

- `Bridge caido`
- `Bridge OK, extension no conectada`
- `Conectando`
- `Conectado`
- `Error de token`
- `Error de protocolo`

## Anchor Navigation

`clickElement` now treats safe anchor navigation specially:

- If target is `<a href>`
- and verification expects URL change
- and href is `http` or `https`
- then it navigates via `window.location.href`

This avoids relying only on synthetic `element.click()` for real navigation targets.

Blocked:

- `javascript:`
- `data:`
- restricted extension/browser pages

No AFIP/ARCA hardcode was added.

## Smoke Plan

Required after loading the updated extension:

1. Start bridge.
2. Copy the masked/full token from local config or console.
3. Open NEXA side panel.
4. Enter host, port, token.
5. Press Reconnect.
6. Verify `/clients` shows one connected client.
7. Run Operator simple page.
8. Run form smoke.
9. Run Learning smoke.
10. Run Recipe Runner smoke.
11. Run Human Checkpoint smoke.
12. Run AFIP/ARCA initial smoke without credentials.

## Current Limitation

The Codex Chrome automation surface cannot directly inspect the extension side panel as a normal web tab. Full side-panel visual smoke still needs either:

- manual verification,
- a dedicated extension harness,
- or a future test-only Playwright harness.

## Security Confirmations

- No Windows/UIA.
- No CDP/Playwright in product runtime.
- No OCR.
- No desktop automation.
- No hardcoded AFIP/ARCA.
- No credentials.
- No password values.
- No API key in extension.
- No API key in git.
- No captcha bypass.
- No fiscal action.

## Recommended Next Hito

If Browser-004 smoke passes manually:

```text
NEXA Browser-005 - Candidate Repair UI + Persistent Recipe Run Recovery
```

If connection smoke still fails:

```text
NEXA Browser-004.1 - Side Panel WebSocket Ownership Spike
```
