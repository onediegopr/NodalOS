# M637A - Manual Reload QA After CSP Loopback Patch

## Decision

M637A BLOQUEADO / CSP_LOOPBACK_RELOAD_QA_FAILED

## Evidence Received

Evidence type: user-reported manual reload QA via pasted Runtime tab text.

The user provided Runtime tab text after M637. The extension UI rendered and the local bridge information was visible.

Observed values:

- Host: `127.0.0.1`
- Port: `8787`
- Estado: `reconnecting`
- Health: `ok`
- Token: `guardado`
- WebSocket: `reconnecting`
- Clientes: `1`
- Heartbeat: `OK`
- Diagnostico: `Conectando`
- Accion recomendada: `Esperando reconexion. Si persiste, verifica token y bridge.`

Technical logs included repeated:

- `reconnecting: Bridge connection closed`
- `error: Bridge WebSocket error`
- `connecting: Connecting to bridge`

## Result

- Bridge running during test: pass, based on Health `ok` and Clients `1`.
- TCP probe `127.0.0.1:8787`: unknown, because a literal `Test-NetConnection` result was not provided.
- Extension reload: pass, based on the loaded sidepanel/Runtime state provided after the patch.
- Sidepanel opened: pass.
- Operar tab rendered: pass.
- Runtime tab rendered: pass.
- NODAL OS visible: pass.
- NEXA visible as primary UI naming: fail, meaning no visible primary NEXA naming was reported.
- Runtime connection observed: fail, because Runtime/WebSocket remained `reconnecting`.
- CSP violations: unknown, because DevTools Console was not provided.
- `ERR_CONNECTION_REFUSED`: unknown, because DevTools Console was not provided and the pasted logs did not include that exact error.
- Screenshots provided: false.

## Product Boundary

No product files were modified in M637A.

Files intentionally unchanged:

- `browser-extension/onebrain-chrome-lab/manifest.json`
- `browser-extension/onebrain-chrome-lab/sidepanel.html`
- `browser-extension/onebrain-chrome-lab/sidepanel.css`
- `browser-extension/onebrain-chrome-lab/sidepanel.js`
- `browser-extension/onebrain-chrome-lab/service_worker.js`
- `browser-extension/onebrain-chrome-lab/content_script.js`
- `browser-extension/onebrain-chrome-lab/recipe_core.js`

## Go / No-Go

- Release evidence gate: NO-GO.
- Public release: NO-GO.
- JS changes: NO-GO.
- Runtime changes: NO-GO.
- Provider/cloud: NO-GO.
- Filesystem: NO-GO.

Recommended next milestone: M637B Bridge WebSocket Reconnect Diagnostic After CSP Patch.
