# M682 Bridge Token Connection QA

Date: 2026-06-23

## Static QA Result

Static verification confirms that the extension no longer opens a WebSocket without a token, exposes token-present status as a boolean in runtime snapshots, distinguishes invalid token from unreachable bridge, and caps reconnect attempts.

## Cases

Case A, no token: PASS static. `token_required` is set and WebSocket creation is skipped.

Case B, token saved: PASS static. `chrome.storage.local` key `token` remains canonical; runtime snapshot publishes `hasToken` and `tokenStatus` without publishing the token in runtime status fields.

Case C, bridge off: PASS static. WebSocket error path reports `bridge_unreachable`; circuit breaker stops aggressive reconnect.

Case D, invalid token: PASS static. Auth rejection close is classified as `invalid_token` and reconnect is blocked.

Case E, live bridge plus matching token: CONDITIONAL. The code path is corrected, but live Chrome extension evidence was not captured in this environment during this pass.

Case F, public variant: PASS static. Public manifest remains valid JSON, has no wildcard host permissions, includes `storage`, and has no wildcard content scripts.

## Manual QA Unblock

Manual QA is conditionally unblocked. The next milestone should reload the public variant in Chrome, paste a valid local token, confirm token present, then capture runtime tab, service worker DevTools, health, WebSocket, and liveness evidence.

Public release and Chrome Web Store submission remain NO-GO.
