# M626 - Manual QA Evidence Intake / Screenshot Evidence Contract

Decision target: `INSTALLED_EXTENSION_QA_EVIDENCE_GATE_READY`.

## Scope

M626 is audit-only. It defines the evidence contract for future installed-extension manual QA intake.

No product extension files were modified.

## Accepted Evidence

The evidence contract accepts only structured manual installed-extension results using:

- `manual-qa-evidence-contract.json`
- `manual-screenshot-checklist.json`
- `manual-qa-result-template.json`

Required screenshots:

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

Each evidence item must include:

- `evidenceId`
- `screenshotFilename`
- `surface`
- `scenario`
- `expectedResult`
- `actualResult`
- `status`
- `notes`
- `blocksHtml`
- `blocksManifest`
- `blocksJs`
- `requiresFollowUp`

Allowed status values are `pass`, `fail`, and `unknown`.

## No-Go Rules

- If `manualQaCompleted=false`, HTML, manifest, and JS remain NO-GO.
- If `extensionLoaded` is not `pass`, HTML, manifest, and JS remain NO-GO.
- If `sidepanelOpened` is not `pass`, HTML, manifest, and JS remain NO-GO.
- If `consoleCriticalErrors` is not `pass`, HTML, manifest, and JS remain NO-GO.
- JS remains NO-GO even if the rest passes.
- Manifest can only be evaluated in a separate milestone.
- HTML minimum patch can only be evaluated in a later milestone and without JS.

## Safety

No runtime, provider/cloud integration, filesystem feature, productive consent, capability enablement, or source-of-truth promotion was introduced.
