# M638A-MANUAL-RETRY Manual Runtime + Service Worker DevTools Evidence

Decision: `M638A-MANUAL-RETRY CERRADO / CLEAN_RUNTIME_DEVTOOLS_EVIDENCE_READY`

## Scope

M638A-MANUAL-RETRY is manual evidence-only. It does not modify product files, JavaScript, manifest, service worker, CSP, permissions, host permissions, sidepanel, bridge source, runtime, provider/cloud, or filesystem behavior.

## Preflight

- Project path: `C:\DESARROLLO\NodalOS\Codigo-m12-audit`
- Branch: `chrome-lab-001-extension-local-ai-bridge`
- Base commit: `8abf08caa40f690506f69fe3175fa5c05bcc97a8`
- Origin branch HEAD at start: `8abf08caa40f690506f69fe3175fa5c05bcc97a8`
- NODRIX: frozen / not used

## Manual Evidence Received

User reported:

- Visual validation: OK
- Runtime connects correctly
- Bridge connects correctly
- No visible errors
- Everything connects
- Everything OK
- Service worker / DevTools: no visible errors reported
- Runtime tab: no visible errors reported

## Runtime Tab Evidence

- Runtime tab opened: true
- Health OK: pass
- Clients observed: pass
- Heartbeat OK: pass
- WebSocket reconnecting visible: no
- Bridge WebSocket error visible: no
- Screenshot provided: false
- Evidence source: user-reported

## Service Worker DevTools Evidence

- Service worker console inspected after reload with bridge live: true
- CSP violations observed: no
- ERR_CONNECTION_REFUSED observed: no
- invalid_token observed: no
- close 1008 / policy violation observed: no
- Bridge WebSocket error repeated observed: no
- WebSocket reconnecting observed: no
- Critical console errors observed: no
- Warnings observed: none reported
- Raw console excerpt stored: no
- Evidence source: user-reported

## Go / No-Go

- Release evidence gate: GO.
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

## Next Milestone

Recommended next milestone: `M639 Roadmap Consolidation Update / Extended Architecture Pack`.
