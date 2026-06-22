# M625 - Installed Extension QA Enablement / Chrome Connector Repair Plan / Manual QA Runbook

Decision target: `INSTALLED_EXTENSION_QA_ENABLEMENT_READY`.

## Scope

M625 is audit-only. It prepares installed-extension QA and does not modify product extension files.

Product files preserved:

- `browser-extension/onebrain-chrome-lab/sidepanel.html`
- `browser-extension/onebrain-chrome-lab/sidepanel.css`
- `browser-extension/onebrain-chrome-lab/sidepanel.js`
- `browser-extension/onebrain-chrome-lab/manifest.json`

## M624 Finding

M624 could not complete installed-extension QA because the Chrome connector failed before live sidepanel control. The failure happened during local browser asset initialization, so Codex could not open or inspect the installed sidepanel.

M624 unknowns that remain:

- Active tab contrast in installed Chrome.
- STOP responsive state in installed Chrome.
- Keyboard focus ring in installed Chrome.
- Runtime/model blocked perception.
- Consent and handoff governance perception.
- Live console status.

## QA Enablement Diagnosis

The repository contains an unpacked Chrome extension at:

`C:\DESARROLLO\NodalOS\Codigo-m12-audit\browser-extension\onebrain-chrome-lab`

The manifest declares a side panel entry:

`side_panel.default_path=sidepanel.html`

Existing README install guidance already points to `chrome://extensions`, Developer mode, and Load unpacked. M625 formalizes that guidance into a manual QA runbook and screenshot checklist.

Headless/static rendering is not accepted as a replacement for installed-extension QA because the sidepanel must be validated inside Chrome with the unpacked extension loaded.

## Manual QA Runbook Summary

The runbook covers:

- Open Chrome.
- Navigate to `chrome://extensions`.
- Enable Developer mode.
- Use Load unpacked.
- Select `C:\DESARROLLO\NodalOS\Codigo-m12-audit\browser-extension\onebrain-chrome-lab`.
- Open the sidepanel.
- Inspect DevTools console if available.
- Validate Operar, Aprender, Recetas, and Runtime.
- Validate STOP at narrow width.
- Validate keyboard focus.
- Validate runtime/model, consent, handoff, and logs.

## Screenshot Checklist Summary

Required screenshots:

- Normal Operar.
- Narrow 420px Operar.
- Aprender tab.
- Recetas tab.
- Runtime tab.
- Consent surface.
- Handoff surface.
- Status badges.
- Keyboard focus ring.
- DevTools console.

## Go / No-Go Criteria

GO for a future HTML minimum patch only if:

- Extension installed and sidepanel opens.
- No critical console errors.
- Active tab contrast passes visually.
- STOP does not clip.
- Runtime and model surfaces do not appear active.
- Governance surfaces read correctly.
- Logs read as auxiliary.
- User confirms screenshots.

NO-GO if:

- Extension does not load.
- Sidepanel does not open.
- Critical console errors appear.
- STOP clips.
- Active tab is illegible.
- Runtime or model appears active.
- Consent appears productive.
- Layout is broken.
- Manual QA was not executed.

## Current Decision

- HTML minimum patch: NO-GO.
- Manifest/naming cleanup: NO-GO.
- JS changes: NO-GO.

## Safety Confirmation

No CSS, HTML, JS, or manifest changes were made.

No runtime, provider/cloud integration, filesystem feature, productive consent, capability enablement, or source-of-truth promotion was introduced.
