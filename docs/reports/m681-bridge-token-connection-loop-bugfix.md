# M681 Bridge Token Connection Loop Bugfix

Date: 2026-06-23

## Fix Applied

The fix is limited to the Chrome extension service worker and sidepanel.

`service_worker.js` now gates startup, keepalive, manual connect, and scheduled reconnect on a non-empty storage-backed token before opening a WebSocket. Missing token is classified as `token_required`.

The service worker now publishes `hasToken` and `tokenStatus` in runtime snapshots. The sidepanel consumes those fields so the visible token status can become `guardado` after reload without exposing the token value in runtime status text.

WebSocket errors with a saved token are classified as `bridge_unreachable`. Policy violation close code or invalid-token close reason is classified as `invalid_token`.

Reconnect attempts now have a circuit breaker after five scheduled attempts. The existing single-flight guard remains in place to prevent simultaneous sockets.

## Files Modified

- `browser-extension/onebrain-chrome-lab/service_worker.js`
- `browser-extension/onebrain-chrome-lab/sidepanel.js`

## Not Modified

- `manifest.json`
- `manifest.public.json`
- bridge source
- CSP

## Behavior After Fix

No token: no WebSocket attempt; diagnostic is `token_required`.

Token stored: token-present state is emitted by the service worker as a boolean; the runtime tab can show token present after reload.

Bridge off with token: diagnostic is `bridge_unreachable`, with capped reconnect attempts.

Invalid token: diagnostic is `invalid_token`, reconnect blocked until token replacement.

Valid token and live bridge: connection path can proceed to WebSocket and engine hello.

Public release and Chrome Web Store submission remain NO-GO.
