# M637E — Bridge WebSocket Reconnect Follow-up Fix

**Decision:** BRIDGE_WEBSOCKET_RECONNECT_FIX_READY
**Branch:** chrome-lab-001-extension-local-ai-bridge
**Prior:** M637C CLOSED (token fix) → M637D BLOCKED (reload QA still failing)

## Why M637C Was Insufficient

M637C fixed a real bug: `extension.hello` was sent with an empty `config.token` instead of the paired `effectiveToken`. But that path only executes **after** a single socket reaches `socket.open`. The M637D reload loop is driven **before** any hello is sent, so the token fix could not address it.

Decisive evidence from the M637D logs: the state cycles through `connecting → error → reconnecting → connecting` and **never** reaches `tokenError` or `protocolError`. The bridge only ever closes a socket for `invalid_token` or `protocol_version_mismatch`, and **always** sends a `protocol.error` message first (which the service worker maps to `tokenError`/`protocolError` and which **stops** the reconnect loop). Since neither appeared, the close did **not** come from the bridge auth/protocol path.

## Root Cause (M637E)

**Competing-socket race in `connectWebSocket()`.**

The only re-entry guard is `if (connectingPromise) return connectingPromise`, but `connectingPromise` is assigned **after** `await validateConnectionConfig(config)`:

```js
if (connectingPromise) { return connectingPromise; }   // guard
...
setConnectionState('connecting', ...);
const effectiveToken = await validateConnectionConfig(config);  // <-- await: connectingPromise still null
connectingPromise = new Promise(...) { socket = new WebSocket(url); ... };  // assigned here
```

During that await window, `connectingPromise` is `null`, so concurrent calls pass the guard. On **extension reload** the service worker runs `initializeRuntimeLifecycle()` more than once:

- `chrome.runtime.onInstalled` (fires on reload/update) → `initializeRuntimeLifecycle()`
- top-level immediate call (line ~95) → `initializeRuntimeLifecycle()`
- (`chrome.runtime.onStartup`, scheduled reconnects, and `keepAliveTick` can also re-enter)

Each reaches `connectWebSocket(config)` and races through the await, then runs `socket = new WebSocket(url)`, overwriting the global `socket` and **orphaning** the previous one. The orphaned socket's `error`/`close` listeners still fire, calling `scheduleReconnect()` — producing exactly the observed reconnect loop right after reload.

## Fix (minimal, service_worker.js only)

1. **`connectInFlight` guard** — a boolean set **synchronously** before the `validateConnectionConfig` await and cleared when the socket opens/closes/errors (and in `disconnect()` / `blockReconnect()`). Concurrent re-entries during the await window now short-circuit instead of creating a second socket.

2. **Active-socket guard** — each socket's `open`/`message`/`close`/`error` listener captures its own `activeSocket` and checks `if (socket !== activeSocket) return;`. Any orphaned socket is neutralized so only the current socket drives connection state and reconnect scheduling. This also stops the intentional `closeSocketOnly('reconnect')` of a replaced socket from re-scheduling reconnects.

```js
if (connectInFlight) { return connectingPromise || Promise.resolve(); }
connectInFlight = true;
...
let effectiveToken;
try { effectiveToken = await validateConnectionConfig(config); }
catch (error) { connectInFlight = false; throw error; }
...
connectingPromise = new Promise((resolve, reject) => {
  const activeSocket = new WebSocket(url);
  socket = activeSocket;
  activeSocket.addEventListener('open',  () => { if (socket !== activeSocket) return; connectInFlight = false; ... });
  activeSocket.addEventListener('close', () => { if (socket !== activeSocket) return; connectInFlight = false; ... });
  activeSocket.addEventListener('error', () => { if (socket !== activeSocket) return; connectInFlight = false; ... });
});
```

## Diagnostic Answers

| Question | Answer |
|---|---|
| service worker uses effectiveToken in all routes? | Yes (M637C; unchanged) |
| validateConnectionConfig token preserved to socket.open? | Yes |
| More than one connectWebSocket with old config? | Yes — concurrent calls at reload; now guarded |
| hydrateRuntimeState restores stale state and reconnects early? | Not the cause |
| repairLocalTokenAfterAuthError competes with first connect? | Not the cause (no invalid_token observed) |
| Bridge closes by invalid_token? | No — no `tokenError` state appeared |
| Bridge closes by protocolVersion/clientId/capabilities? | No — no `protocolError` state appeared |
| Bridge requires an ack the SW waits for? | No |
| Heartbeat/pong mismatch? | No — bridge pong handling is correct |
| lastWsCloseCode/Reason updated and published? | Yes (already in runtime snapshot) |
| Two simultaneous sockets competing? | **Yes — this is the root cause** |
| CSP still discarded? | Yes — manifest CSP unchanged, no CSP error in state machine |

## What Was NOT Changed

- `manifest.json` (CSP unchanged), `sidepanel.html/css/js`, `content_script.js`, `recipe_core.js` — unchanged
- Bridge server code — unchanged
- Permissions, host_permissions, CSP — unchanged
- Storage keys (`nexaRecipes`, `nexaLearningDraft`, `nexaRuntimeState`) — unchanged
- Protocol version (`chrome-lab-v1`), port (`onebrain-sidepanel`), alarm (`nexa.keepalive`) — unchanged
- Runtime architecture, capabilities, `serviceWorkerRunOwner: false`, `LEGACY_RUNNER_ENABLED = false` — unchanged

## Hash Baseline Update

service_worker.js baseline updated across all tests:
`546AAF381024B5F784F28A94A57F05948AECA92F6BFF174F577D22B4120A655F` →
`E42D5247C0A9CCAC250EB51300E6F6C1B701CADBA3DBD4B86A62126CC7A1933D`

## Next Milestone

**M637F — Manual Reload QA After Bridge WebSocket Follow-up Fix.** Run with the bridge **confirmed running** and DevTools Console + Network open. If the loop persists, capture the WebSocket close code (1006 transport vs 1008 policy) and any `ERR_CONNECTION_REFUSED` / CSP lines — that distinguishes a remaining client bug from "bridge not running".
