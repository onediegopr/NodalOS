# M628 - Installed Extension Manual QA Evidence Capture

Decision: `MANUAL_QA_EVIDENCE_CAPTURE_REQUIRES_USER_ACTION`.

## Scope

M628 attempted to begin installed-extension manual QA evidence capture, but it did not complete live QA.

No product files were modified:

- `browser-extension/onebrain-chrome-lab/sidepanel.html`
- `browser-extension/onebrain-chrome-lab/sidepanel.css`
- `browser-extension/onebrain-chrome-lab/sidepanel.js`
- `browser-extension/onebrain-chrome-lab/manifest.json`

## Attempt Result

Chrome installed-extension QA was not possible from this Codex environment.

The Chrome automation connector failed before browser control with a local browser asset initialization issue. Because Chrome was not controlled, M628 did not open `chrome://extensions`, did not execute Load unpacked, did not open the installed sidepanel, did not inspect DevTools, and did not capture live screenshots.

## Evidence Status

- `manualQaCompleted=false`
- `extensionLoaded=unknown`
- `sidepanelOpened=unknown`
- `consoleCriticalErrors=unknown`
- Required screenshots captured: none

All required scenarios remain missing:

- Operar normal.
- Operar 420px / narrow viewport.
- Aprender.
- Recetas.
- Runtime.
- Tab active.
- STOP button.
- Focus ring con teclado.
- Consent surface.
- Handoff surface.
- Runtime/provider visible.
- Logs/pre.
- DevTools console sin errores críticos.

## Go / No-Go After Attempt

- HTML minimum patch: NO-GO.
- Manifest/naming cleanup: NO-GO.
- JS changes: NO-GO.
- Runtime changes: NO-GO.
- Provider/cloud: NO-GO.
- Filesystem: NO-GO.
- Productive consent: NO-GO.
- Capability enablement: NO-GO.

## Required User Action

Run the M625 manual runbook locally and return:

- Completed M626 result template.
- Required screenshot set.
- Chrome version.
- Confirmation that the extension loaded.
- Confirmation that the sidepanel opened.
- Confirmation that DevTools console has no critical errors, or a list of the critical errors observed.

## Safety Confirmation

No runtime, provider/cloud integration, filesystem feature, productive consent, capability enablement, analytics wiring, source-of-truth promotion, HTML, CSS, JS, or manifest change was introduced.
