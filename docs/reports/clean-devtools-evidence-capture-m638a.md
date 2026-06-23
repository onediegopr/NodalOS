# M638A Clean DevTools Evidence Capture

Decision: `M638A BLOQUEADO / CLEAN_DEVTOOLS_EVIDENCE_NOT_PROVIDED`

## Scope

M638A is evidence-capture-only. It does not modify product files, JavaScript, manifest, service worker, CSP, permissions, host permissions, sidepanel, bridge, runtime, provider/cloud or filesystem behavior.

## Preflight

- Project path: `C:\DESARROLLO\NodalOS\Codigo-m12-audit`
- Branch: `chrome-lab-001-extension-local-ai-bridge`
- Base commit: `32fb7c1042b17b3b2061b814db3f1467496645d9`
- NODRIX: frozen / not used

## Bridge Liveness Evidence

The existing read-only liveness script was executed:

`tools/scripts/bridge-liveness-check.ps1`

Observed result:

- TCP `127.0.0.1:8787`: fail, no listener
- Listener process: fail, no process listening on 8787
- `/health`: fail, HTTP 0
- `/runtime`: fail, HTTP 0
- `/debug`: fail, HTTP 0
- `/config/public`: fail, HTTP 0
- `/ws/extension` upgrade: fail, upgrade not accepted

Because bridge liveness failed, extension reload Runtime evidence and clean service worker DevTools evidence were not captured.

## Runtime Tab Evidence

- Runtime tab opened: false
- Health OK: unknown
- Clients observed: unknown
- Heartbeat OK: unknown
- WebSocket reconnecting visible: unknown
- Bridge WebSocket error visible: unknown
- Screenshot provided: false

## DevTools Console Evidence

- DevTools Console provided: false
- Service worker console inspected after reload with bridge live: false
- CSP violations observed: unknown
- ERR_CONNECTION_REFUSED observed: unknown
- invalid_token observed: unknown
- close 1008 / policy violation observed: unknown
- Bridge WebSocket error repeated observed: unknown
- Critical console errors observed: unknown
- Warnings observed: none captured

No evidence is inferred or invented.

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

## Required Retry

Start the bridge, verify liveness, reload the installed extension, then capture Runtime tab and service worker DevTools Console evidence.

Recommended next milestone: `M638A-Retry Clean DevTools Evidence Capture`.
