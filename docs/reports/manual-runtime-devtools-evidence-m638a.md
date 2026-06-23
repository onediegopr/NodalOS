# M638A-MANUAL Manual Runtime + Service Worker DevTools Evidence Capture

Decision: `M638A-MANUAL BLOQUEADO / MANUAL_RUNTIME_DEVTOOLS_EVIDENCE_NOT_PROVIDED`

## Scope

M638A-MANUAL is manual evidence-only. It does not modify product files, JavaScript, manifest, service worker, CSP, permissions, host permissions, sidepanel, bridge source, runtime, provider/cloud, or filesystem behavior.

## Preflight

- Project path: `C:\DESARROLLO\NodalOS\Codigo-m12-audit`
- Branch: `chrome-lab-001-extension-local-ai-bridge`
- Base commit: `d156540bc28c0cf1f6b0722872c0093e0e2f6b9f`
- Origin branch HEAD at start: `d156540bc28c0cf1f6b0722872c0093e0e2f6b9f`
- NODRIX: frozen / not used

## Bridge Liveness Evidence

The existing read-only liveness script was executed:

`tools/scripts/bridge-liveness-check.ps1`

Observed result:

- TCP `127.0.0.1:8787`: PASS
- Listener process: PASS, `OneBrain.ChromeLab.Bridge`
- `/health`: PASS, HTTP 200
- `/runtime`: PASS, HTTP 200
- `/debug`: PASS, HTTP 200
- `/config/public`: PASS, HTTP 200
- `/ws/extension` upgrade: PASS, accepted as `101 Switching Protocols`

Additional read-only bridge checks:

- `/runtime ok`: true
- `/debug connectedCount`: 1
- `/debug extension.hello` observed: yes

## Runtime Tab Evidence

- Runtime tab opened: false
- Health OK: unknown
- Clients observed in Runtime tab: unknown
- Heartbeat OK: unknown
- WebSocket reconnecting visible: unknown
- Bridge WebSocket error visible: unknown
- Screenshot provided: false

No manual Runtime tab screenshot or report was provided in this turn.

## Service Worker DevTools Evidence

- Service worker console inspected after reload with bridge live: false
- CSP violations observed: unknown
- ERR_CONNECTION_REFUSED observed: unknown
- invalid_token observed: unknown
- close 1008 / policy violation observed: unknown
- Bridge WebSocket error repeated observed: unknown
- WebSocket reconnecting observed: unknown
- Critical console errors observed: unknown
- Warnings observed: none captured

No service worker DevTools Console evidence was provided in this turn. No raw console excerpt was captured or stored.

## Go / No-Go

- Release evidence gate: GO, remains active from M638.
- Public release: NO-GO.
- JS changes: NO-GO.
- Runtime changes: NO-GO.
- Provider/cloud: NO-GO.
- Filesystem: NO-GO.

## Product Boundary

No product files were modified:

- `browser-extension/onebrain-chrome-lab/manifest.json`
- `browser-extension/onebrain-chrome-lab/sidepanel.html`
- `browser-extension/onebrain-chrome-lab/sidepanel.css`
- `browser-extension/onebrain-chrome-lab/sidepanel.js`
- `browser-extension/onebrain-chrome-lab/service_worker.js`
- `browser-extension/onebrain-chrome-lab/content_script.js`
- `browser-extension/onebrain-chrome-lab/recipe_core.js`
- `src/OneBrain.ChromeLab.Bridge/**`

## Required Manual Retry

With bridge liveness confirmed, the remaining evidence must be supplied manually:

1. Reload the installed extension from `chrome://extensions`.
2. Open sidepanel and Runtime tab.
3. Capture Runtime tab status: Health, clients, heartbeat, WebSocket state and visible bridge errors.
4. Open service worker DevTools Console after reload with bridge live.
5. Record whether CSP violations, `ERR_CONNECTION_REFUSED`, `invalid_token`, close `1008`, repeated Bridge WebSocket errors, reconnecting state, critical errors or warnings are present.

Recommended next milestone: `M638A-MANUAL-RETRY`.
