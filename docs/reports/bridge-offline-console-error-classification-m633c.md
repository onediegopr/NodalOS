# M633C - Bridge Offline Console Error Classification

Decision target: `BRIDGE_OFFLINE_CONSOLE_ERRORS_CLASSIFIED`

## Evidence Received

- User-provided screenshots of the installed sidepanel.
- User-provided screenshot of the DevTools Console for the service worker.
- Visual confirmation that `NODAL OS` is visible.
- Visual confirmation that the sidepanel loaded with `Operar`, `Runtime`, and `STOP` visible.
- Visual confirmation that `NEXA` does not appear as the primary visible naming in the UI.
- Console screenshot showing repeated red errors and approximately 928 errors with 2 warnings.

## Primary Error

- `WebSocket connection to 'ws://127.0.0.1:8787/ws/extension' failed: Error in connection establishment: net::ERR_CONNECTION_REFUSED`
- Source location: `service_worker.js:481`

## Classification

- The error pattern is classified as `bridge-offline-or-refused`.
- The error points to a local runtime bridge that is not running or is refusing connections on `127.0.0.1:8787`.
- This does not look like a sidepanel render failure.
- The runtime connection is impacted.
- No CSP violation was observed in the screenshot provided.

## What This Blocks

- Release public: blocked.
- JS changes: blocked.
- Runtime changes: blocked.
- HTML microcopy patch: not approved from this evidence alone.
- Service worker visible strings cleanup: not approved from this evidence alone.
- CSP tightening: not approved from this evidence alone.

## What This Does Not Prove

- It does not prove the sidepanel rendering is broken.
- It does not prove the visible naming cleanup failed.
- It does not prove the bridge code is incorrect.
- It does not prove a CSP regression.

## Decision

M633C is closed as:

`M633C CERRADO / BRIDGE_OFFLINE_CONSOLE_ERRORS_CLASSIFIED`

## Recommendation

- If the bridge was not running, the next milestone should be `M633D Bridge Running Retest`.
- If the bridge was running, a dedicated `M633D Bridge Connection Diagnostic` would be required instead.
- No JS or runtime changes should proceed from this evidence alone.
