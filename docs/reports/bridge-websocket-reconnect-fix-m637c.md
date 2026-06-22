# M637C — Bridge WebSocket Reconnect Fix

**Decision:** BRIDGE_WEBSOCKET_RECONNECT_FIX_READY  
**Branch:** chrome-lab-001-extension-local-ai-bridge

## Root Cause

`validateConnectionConfig()` calls `tryLocalPairing()` which saves the bridge token to `chrome.storage.local` and returns it. However, the `config` object captured by the `connectWebSocket()` closure was not updated. When `socket.open` fired, `extension.hello` was sent with `config.token || ''` (always empty string on first connect), because the `config` object reference still held the original empty token.

The bridge validates the token strictly on every `extension.hello` regardless of `requiresToken`. An empty token never matches the generated `ConnectionToken`, so the bridge sent `protocol.error { error: "invalid_token" }` and closed the socket with `WebSocketCloseStatus.PolicyViolation` (close code 1008). This produced the reconnect loop.

The `connectedCount=1` observed in M637B was from the `repairLocalTokenAfterAuthError()` secondary recovery path, which ran asynchronously after the first rejection and connected successfully using the freshly-read config from storage. However, this repair path created a race between the UI state (`reconnecting` from the close event) and the actual second successful connection.

## Diagnostic Answers

| Question | Answer |
|---|---|
| CSP root cause? | No — `ws://127.0.0.1:*` is allowed |
| Token/auth root cause? | Yes — empty token in `extension.hello` due to stale closure |
| Bridge WS endpoint root cause? | No — bridge accepts WS upgrade and handles hello correctly |
| Heartbeat root cause? | No — bridge implements correct ping/pong |
| Stale session state root cause? | No — secondary contributor, not primary |
| UI state display root cause? | No — UI correctly reflects the reconnecting state |
| Close code observed | 1008 PolicyViolation, reason: "invalid token" |

## Fix Applied

**File:** `browser-extension/onebrain-chrome-lab/service_worker.js`  
**Lines changed:** 4  

### Change 1 — `validateConnectionConfig` returns effective token

```js
// Before
async function validateConnectionConfig(config) {
  ...
  if (!response.ok) { return; }
  ...
  if (paired) { return; }
  ...
}

// After
async function validateConnectionConfig(config) {
  ...
  if (!response.ok) { return token; }
  ...
  if (paired) { return paired; }
  ...
  return token;
}
```

### Change 2 — `connectWebSocket` uses the returned token in hello

```js
// Before
await validateConnectionConfig(config);
...
token: config.token || '',

// After
const effectiveToken = await validateConnectionConfig(config);
...
token: effectiveToken || '',
```

## What Was NOT Changed

- `manifest.json` — unchanged
- `sidepanel.html`, `sidepanel.css`, `sidepanel.js` — unchanged
- `content_script.js`, `recipe_core.js` — unchanged
- Bridge server code — unchanged
- Permissions, host_permissions — unchanged
- Storage keys (`nexaRecipes`, `nexaLearningDraft`, `nexaRuntimeState`) — unchanged
- Protocol version (`chrome-lab-v1`) — unchanged
- Port/alarm names (`onebrain-sidepanel`, `nexa.keepalive`) — unchanged
- Runtime architecture — unchanged
- `repairLocalTokenAfterAuthError()` — retained as secondary recovery

## Test Coverage Added

`NodalOsBridgeWebSocketReconnectFixM637CTests.cs` covers:
- All 4 artifact files exist
- Root cause fields are correct
- Fix summary fields are correct
- Go/no-go fields are correct
- service_worker.js uses `effectiveToken` in hello
- `validateConnectionConfig` returns the paired token
- Bridge correctly rejects invalid token (existing ChromeLabBridgeTests)
- Bridge correctly handles ping/pong (existing ChromeLabBridgeTests)
- Manifest, sidepanel, content_script unchanged
- Permissions and storage keys unchanged
- readyForJsChanges=false, readyForRuntimeChanges=false

## Hash Baseline Update

All existing tests with `ServiceWorkerUnchanged` baseline updated from:  
`5C98C0B1481ACEAA4EE957CF38C80E5BADA592DF469F077C277D1EA7658EC444`  
to:  
`546AAF381024B5F784F28A94A57F05948AECA92F6BFF174F577D22B4120A655F`

## Next Milestone

**M637D — Manual Reload QA After Bridge WebSocket Fix**

Load the extension fresh (no saved token), start the bridge, open the side panel, and confirm the connection reaches `connected` on first attempt without cycling through `reconnecting`.
