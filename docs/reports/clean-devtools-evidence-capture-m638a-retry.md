# M638A-RETRY Bridge Startup Liveness Recovery + Clean DevTools Evidence Capture

Decision: `M638A-RETRY BLOQUEADO / CLEAN_DEVTOOLS_EVIDENCE_NOT_PROVIDED`

## Scope

M638A-RETRY is ops/evidence-only. It does not modify product files, JavaScript, manifest, service worker, CSP, permissions, host permissions, sidepanel, bridge source, runtime, provider/cloud, or filesystem behavior.

## Preflight

- Project path: `C:\DESARROLLO\NodalOS\Codigo-m12-audit`
- Branch: `chrome-lab-001-extension-local-ai-bridge`
- Base commit: `040615e36b514403726278e93c7d44720c5df9d0`
- Origin branch HEAD: `040615e36b514403726278e93c7d44720c5df9d0`
- Remote: `https://github.com/onediegopr/NodalOS.git`
- NODRIX: frozen / not used

## Bridge Startup / Runbook Evidence

Runbook used:

`docs/runbooks/chrome-lab-bridge-startup-and-liveness.md`

Startup command used:

`dotnet run --project src/OneBrain.ChromeLab.Bridge/OneBrain.ChromeLab.Bridge.csproj`

Observed:

- Bridge process: yes, `OneBrain.ChromeLab.Bridge`
- Listener: `127.0.0.1:8787` PASS
- Product source files changed: no

## Bridge Liveness Evidence

The existing read-only liveness script was executed:

`tools/scripts/bridge-liveness-check.ps1`

Observed result:

- TCP `127.0.0.1:8787`: PASS
- Listener process: PASS
- `/health`: PASS, HTTP 200
- `/runtime`: PASS, HTTP 200
- `/debug`: PASS, HTTP 200
- `/config/public`: PASS, HTTP 200
- `/ws/extension` upgrade: PASS, accepted as `101 Switching Protocols`
- `/debug` connected count observed after startup: 1
- `/debug` extension hello event observed: yes

Bridge liveness was recovered.

## Runtime Tab Evidence

- Runtime tab opened: false
- Health OK: unknown
- Clients observed in Runtime tab: unknown
- Heartbeat OK: unknown
- WebSocket reconnecting visible: unknown
- Bridge WebSocket error visible: unknown
- Screenshot provided: false

Note: `/debug` showed `connectedCount=1` and `extension.hello`, but that is not counted as visual Runtime tab evidence.

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

Chrome automation could not open `chrome://extensions` because the browser connector blocked that URL by security policy. No workaround was attempted and no evidence is inferred.

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

## Required Follow-up

With bridge liveness recovered, capture manual installed-extension evidence:

1. Reload the installed extension from `chrome://extensions`.
2. Open sidepanel and Runtime tab.
3. Capture Runtime tab evidence.
4. Open service worker DevTools Console after reload with bridge live.
5. Confirm absence or presence of CSP violations, `ERR_CONNECTION_REFUSED`, `invalid_token`, close `1008`, repeated Bridge WebSocket error, and critical errors.

Recommended next milestone: `M638A-Retry Clean DevTools Evidence Capture`.
