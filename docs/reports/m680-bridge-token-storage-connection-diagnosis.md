# M680 Bridge Token Storage Connection Diagnosis

Date: 2026-06-23

## Summary

The public variant has the required local permissions and CSP for storage, HTTP health checks, and local WebSocket connections. The root cause is in the extension connection flow: startup and keepalive paths could attempt WebSocket connection with an empty token, and the sidepanel runtime status inferred token presence from local UI state instead of a service-worker-owned storage signal.

## Flow Map

Token capture happens in `sidepanel.html` through password inputs handled by `sidepanel.js`. The sidepanel posts `saveTokenAndConnect` or `connect` messages to `service_worker.js`.

Token persistence is canonical in `chrome.storage.local` under key `token`, with `host` and `port` stored beside it. The service worker reads it through `chrome.storage.local.get(DEFAULT_CONFIG)`.

Before the fix, `initializeRuntimeLifecycle()` connected whenever host and port existed, even if token was empty. `validateConnectionConfig()` could return an empty token when the bridge config endpoint was unreachable, allowing a WebSocket attempt with no token. That made bridge-off and token-missing states collapse into a reconnect loop.

## Hypotheses

H1 PARTIAL: token save path exists, but UI state could still report missing after reload because runtime snapshots did not carry token-present status.
H2 RULED_OUT: key is `token` in sidepanel and service worker.
H3 RULED_OUT: service worker reads the same `token` key.
H4 RULED_OUT: no localStorage token path is used for the bridge token.
H5 RULED_OUT: clear runtime state does not clear token; clear token explicitly does.
H6 CONFIRMED: runtime UI could show missing when token existed because runtime snapshot had no `hasToken`.
H7 CONFIRMED: startup could connect before proving a token existed.
H8 CONFIRMED: duplicate lifecycle calls and reconnects had an existing race guard, but missing-token gating still happened too late.
H9 RULED_OUT: public manifest includes `storage`.
H10 RULED_OUT: public manifest allows local HTTP hosts and CSP allows local WebSocket.
H11 PARTIAL: invalid token remains possible and is now classified as `invalid_token`.
H12 PARTIAL: not reproducible here as live bridge/Chrome evidence; bridge-off path is handled as `bridge_unreachable`.
H13 RULED_OUT static: both localhost and 127.0.0.1 are permitted.
H14 RULED_OUT static: default extension UI and bridge config use 127.0.0.1; localhost remains permitted.
H15 RULED_OUT: CSP already allows local WebSocket.
H16 PARTIAL: service worker lifecycle can lose in-memory UI hints; fixed by publishing storage-backed `hasToken`.
H17 CONFIRMED: reconnect loop needed a circuit breaker.
H18 PARTIAL: no live Chrome load evidence in this pass.
H19 PARTIAL: no live Chrome load evidence in this pass.
H20 RULED_OUT static: public variant omits content scripts, but bridge/token flow is service-worker and sidepanel based.

## Diagnosis

If token is absent, the correct diagnostic is `token_required` and no WebSocket should open.

If token is invalid, the bridge sends an auth error and closes with policy violation; the extension must classify this as `invalid_token` and stop reconnecting until the user replaces the token.

If bridge is off or unreachable while a token exists, the correct diagnostic is `bridge_unreachable`, not token missing.

If bridge is alive and token matches, the WebSocket should connect and `engine.hello` should keep state connected.

No bridge source, CSP, or manifest change is required.
