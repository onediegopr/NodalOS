# NODAL OS WCU Read-Only Live Design Gate Handoff

## Block

`WCU-031-036 — READ-ONLY LIVE DESIGN GATE + AUDIT PACK`

## Decision

`GO_WCU_READ_ONLY_LIVE_DESIGN_GATE_AUDIT_PACK_READY`

## What Changed

- Added a fixture-safe read-only live gate catalog.
- Added regression tests proving the gates stay fail-closed and no source signal grants action authority.
- Tightened unified evidence sensitive-field aggregation without adding live collection or action authority.
- Added threat model, gate design, readiness checklist, audit prompts, QA index, report, and next prompt.

## What Did Not Change

- No live read-only provider was implemented.
- No Windows action executor was added.
- No mouse, keyboard, clipboard, screenshot, window manipulation, UIA Invoke/Click/SetValue, or browser live path was added.
- No OCR engine was duplicated.
- Protected stealth/browser scope remains out of scope.

## Required Future Condition

The next block can only be used if the audit pack receives external GO. Future work must remain read-only, disabled by default, allowlisted-test-app-only, redacted-metadata-only, and no-action.

## Residual Risks

- Future live metadata collection has privacy risk even without actions.
- UIA event floods and stale UIA identities require throttling, debouncing, and handoff logic.
- Window titles and process paths can contain sensitive data and must be redacted before evidence.
