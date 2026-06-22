# M637F - Manual Reload QA After Bridge WebSocket Follow-up Fix

## Decision

M637F BLOQUEADO / BRIDGE_WEBSOCKET_FOLLOWUP_RELOAD_QA_FAILED

## Evidence Type

User-provided manual reload QA with DevTools screenshot.

## Manual Evidence Received

The user reported that the issue persists after M637E:

```text
sigue igual esta mierda
```

Visible local runtime logs provided:

```text
04:23:50 reconnecting: Bridge connection closed
04:23:50 error: Bridge WebSocket error
04:23:46 connecting: Connecting to bridge
04:23:41 reconnecting: Bridge connection closed
04:23:41 error: Bridge WebSocket error
04:23:34 connecting: Connecting to bridge
```

DevTools Console screenshot evidence:

```text
WebSocket connection to 'ws://127.0.0.1:8787/ws/extension' failed:
Error in connection establishment:
net::ERR_CONNECTION_REFUSED
service_worker.js:481
```

The screenshot also showed approximately 1001 errors and 1 warning in DevTools.

## QA Result

| Check | Result |
| --- | --- |
| Bridge running during test | unknown |
| Extension reloaded | pass |
| Sidepanel opened | pass |
| Runtime tab rendered | pass |
| Health ok | unknown |
| Clients observed | unknown |
| Heartbeat ok | unknown |
| WebSocket stuck reconnecting | yes |
| Bridge WebSocket error repeated | yes |
| invalid_token observed | no |
| close 1008 observed | no |
| ERR_CONNECTION_REFUSED observed | yes |
| CSP violations observed | no |
| Screenshots provided | yes |
| DevTools Console provided | yes |
| Product files modified | no |

## Interpretation

The M637E race-condition fix did not close the manual reload QA loop. The visible failure is now again a refused WebSocket connection to the loopback bridge endpoint:

```text
ws://127.0.0.1:8787/ws/extension
```

This means the release evidence gate must remain blocked. The evidence does not justify JS/runtime changes in this evidence-only milestone.

## Go / No-Go

| Area | Decision |
| --- | --- |
| Release evidence gate | NO-GO |
| Release public | NO-GO |
| JS changes | NO-GO |
| Runtime changes | NO-GO |
| Provider/cloud | NO-GO |
| Filesystem | NO-GO |

## Product Boundary

No product files were modified in this milestone:

- `browser-extension/onebrain-chrome-lab/manifest.json`
- `browser-extension/onebrain-chrome-lab/sidepanel.html`
- `browser-extension/onebrain-chrome-lab/sidepanel.css`
- `browser-extension/onebrain-chrome-lab/sidepanel.js`
- `browser-extension/onebrain-chrome-lab/service_worker.js`
- `browser-extension/onebrain-chrome-lab/content_script.js`
- `browser-extension/onebrain-chrome-lab/recipe_core.js`

## Recommended Next Milestone

M637G Bridge WebSocket Follow-up Diagnostic.
