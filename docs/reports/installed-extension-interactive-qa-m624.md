# M624 - Installed Extension Interactive QA / Live Sidepanel State Audit

Decision target: `INSTALLED_EXTENSION_INTERACTIVE_QA_READY`.

## Scope

M624 is audit-only. It does not modify product extension files.

Product files preserved:

- `browser-extension/onebrain-chrome-lab/sidepanel.html`
- `browser-extension/onebrain-chrome-lab/sidepanel.css`
- `browser-extension/onebrain-chrome-lab/sidepanel.js`
- `browser-extension/onebrain-chrome-lab/manifest.json`

## Interactive QA Result

Installed-extension QA could not be completed from Codex in this environment.

The Chrome connector failed before live sidepanel control with a local browser asset initialization error. Because the installed extension could not be controlled, M624 does not claim live visual pass/fail results.

Artifact status:

- `interactiveExtensionQaAvailable=false`
- `manualUserQaRequired=true`

## Prepared Manual QA Checklist

Manual or repaired-connector QA must validate:

- Extension loads unpacked and opens the sidepanel.
- Tabs: Operar, Aprender, Recetas, Runtime.
- `.tab.active` contrast and navigation semantics.
- STOP responsive state at narrow width.
- Keyboard focus visibility.
- `status-running`, blocked, warning, and danger badges.
- Consent, handoff, and human banner governance surfaces.
- Runtime and model surfaces read as blocked/non-active.
- Logs and `pre` blocks read as auxiliary, not authoritative product evidence.
- Normal, narrow, and wide sidepanel widths.

## Required Answers

1. Extension loads without critical live errors: unknown.
2. Sidepanel feels like Research OS / Local Mission Control: unknown.
3. `.tab.active` passes visually and does not read as execution: unknown live; M623 static estimate passed.
4. STOP no longer clips at 420px: unknown live; M623 static capture passed.
5. Focus ring visible with keyboard: unknown.
6. Aprender/Recetas/Runtime coherent with Operar: unknown.
7. Runtime appears blocked/non-active: unknown.
8. Provider/model appears non-active: unknown.
9. Consent/handoff read as governance surfaces: unknown.
10. `status-running` reads as attention/risk, not productive execution: unknown.
11. Logs/pre read as auxiliary: unknown.
12. Console errors: unknown.
13. Broken layout signs: unknown.
14. Generic SaaS dashboard signs: unknown.
15. Ready for minimum HTML patch: no.
16. Ready for manifest/naming cleanup: no.
17. Ready for JS: no.
18. Next milestone: installed extension manual QA rerun or connector repair before HTML/manifest work.

## Go / No-Go

- HTML minimum patch: NO-GO.
- Manifest/naming cleanup: NO-GO.
- JS changes: NO-GO.

## Safety Confirmation

No CSS, HTML, JS, or manifest changes were made.

No runtime, provider/cloud integration, filesystem feature, productive consent, capability enablement, or source-of-truth promotion was introduced.
