# M638 Installed Extension Release Evidence Gate

Decision: `M638 CERRADO / INSTALLED_EXTENSION_RELEASE_EVIDENCE_GATE_READY`

## Scope

M638 creates the formal release evidence gate for the installed NODAL OS Chrome extension after M637H.

This milestone is evidence-only. It does not modify product files, JavaScript, runtime, bridge, provider, CSP, permissions, host permissions, storage keys or capabilities.

## Accepted Audit Verdict

`AUDIT_CONDITIONAL_GO`

The installed extension QA line M615-M637H can be treated as closed for installed-extension QA/evidence. Release evidence gate can start. Public release remains NO-GO.

## Evidence Available

- Bridge liveness script: pass, inherited from M637H.
- TCP 127.0.0.1:8787: pass, inherited from M637H.
- `/health`, `/runtime`, `/debug`, `/config/public`: pass, inherited from M637H.
- `/ws/extension` upgrade: pass, inherited from M637H.
- Extension reload: pass, inherited from M637H.
- Sidepanel/runtime tab: pass, inherited from M637H.
- Health OK, clients observed and heartbeat OK: pass, inherited from M637H.
- WebSocket stuck reconnecting: fail, meaning not stuck reconnecting in M637H.
- Bridge WebSocket error visible: fail, meaning not visible/repeated in M637H.

## Evidence Missing

- Runtime screenshot: not provided.
- Clean service worker DevTools Console screenshot/text: not provided.
- CSP violation check: unknown.
- ERR_CONNECTION_REFUSED DevTools check: unknown.
- invalid_token check: unknown.
- close 1008 check: unknown.
- repeated Bridge WebSocket error DevTools check: unknown.

No missing evidence is invented. Public release remains blocked until clean DevTools evidence is captured.

## Release Blockers

- `RELEASE-BLOCKER-DEVTOOLS-CLEAN-EVIDENCE`: open, critical for public release.
- `RELEASE-BLOCKER-HOST-PERMISSIONS-REVIEW`: open, medium.
- `RELEASE-BLOCKER-PROVIDER-RUNTIME-GATE`: open, critical for provider release.
- `RELEASE-CAVEAT-IPV6-LOOPBACK`: documented.
- `RELEASE-CAVEAT-CORS-LAN`: documented.
- `RELEASE-CAVEAT-MIXED-MICROCOPY`: documented.

## Provider / Runtime Risk

The bridge includes an OpenAI/provider path. It must remain gated for release. Provider runtime is not ready, provider/cloud is not enabled for release, and provider prompt naming debt remains documented as `ONE BRAIN Chrome Lab Agent`.

## Host Permissions

`manifest.json` still declares broad host permissions:

- `http://*/*`
- `https://*/*`

This is reviewed and registered as a release blocker requiring justification before public publication. M638 does not change manifest permissions.

## IPv6 Loopback Caveat

CSP loopback currently supports `127.0.0.1` and `localhost`, but not IPv6 `::1`. The default bridge path remains `127.0.0.1:8787`.

## Go / No-Go

- Release evidence gate: GO.
- Public release: NO-GO.
- JavaScript changes: NO-GO.
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

`M638A Clean DevTools Evidence Capture`
