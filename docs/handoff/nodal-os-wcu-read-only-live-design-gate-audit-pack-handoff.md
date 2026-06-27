# NODAL OS WCU Read-Only Live Design Gate Handoff

## Block

`WCU-031-036 — READ-ONLY LIVE DESIGN GATE + AUDIT PACK`

## Decision

`GO_WCU_READ_ONLY_LIVE_DESIGN_GATE_AUDIT_PACK_READY`

## What Changed

- Added a fixture-safe read-only live gate catalog.
- Added regression tests proving the gates stay fail-closed and no source signal grants action authority.
- Tightened selector/evidence sensitive-field aggregation without adding live collection or action authority.
- Added threat model, gate design, readiness checklist, audit prompts, QA index, report, and next prompt.

## What Did Not Change

- No live read-only provider was implemented.
- No Windows action executor was added.
- No mouse, keyboard, clipboard, screenshot, window manipulation, UIA Invoke/Click/SetValue, or browser live path was added.
- No OCR engine was duplicated.
- Protected stealth/browser scope remains out of scope.

## External Audit Reconciliation

External audit of current HEAD `c0ce467f5472dc65cafd9faeed6ee406930f7b6d` found containment/read-only PASS and protected scope PASS, but did not authorize live advancement. The reconciled result is `AUDIT_CONTAINMENT_PASS_BUT_LIVE_ADVANCE_NO_GO`.

The audit did not run build/tests and did not prove behavioral live safety.

## Required Future Condition

`WCU-037-044 — READ-ONLY LIVE PROTOTYPE GATED` is blocked as `BLOCKED_PENDING_HUMAN_POLICY_DECISION_AND_EXTERNAL_GO`. The next block must be containment-only, such as `WCU-CONTAINMENT-PROPERTY-AUDIT-*` or `WCU-REDACTION-EVIDENCE-NEGATIVE-PROPERTY-LOCK`.

## Residual Risks

- Future live metadata collection has privacy risk even without actions.
- UIA event floods and stale UIA identities require throttling, debouncing, and handoff logic.
- Window titles and process paths can contain sensitive data and must be redacted before evidence.
