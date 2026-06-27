# M7 Perception Stack Formal Integration Reports

Decision target: `NODAL_OS_M7_PERCEPTION_STACK_FORMAL_INTEGRATION_REPORTS`

## Decision

NODAL OS adds a fixture-only Perception Stack reporting layer for Reliable Recipes, Recorder Drafts, Eval Scenarios and Sandbox Readiness. The layer formalizes perception signals, signal agreement, contradictions, target confidence and action authority decisions without activating live perception or runtime automation.

This is a read-only/report-only contract and view-model expansion. It does not create or enable browser capture, desktop capture, OCR runtime, screenshots, CDP, Playwright, provider/VLM calls, recorder runtime, sandbox runtime or recipe execution.

## Dependency Chain

M7 builds on:

- M1 Reliable Recipe contracts and fixture-only perception contracts.
- M2 quality/preflight scoring and target-resolution quality.
- M3 read-only Recipe Lab view models.
- M4 recorder-to-recipe fixture drafts.
- M5 fixture eval harness reports.
- M6 computer-use sandbox readiness reports.

## Protected OCR Status

OCR / WCU OCR interop remains protected. M7 uses OCR only as fixture signal/report data:

- OCR can support a target explanation.
- OCR can improve confidence when it agrees with DOM/accessibility fixture signals.
- OCR-only sensitive targets are blocked.
- OCR does not grant action authority.
- OCR runtime/live activation is not changed.

## Signal Model

M7 reports fixture signal kinds:

- DOM fixture.
- Accessibility fixture.
- OCR fixture.
- Visual bounding box fixture.
- Set-of-marks fixture.
- State classifier fixture.
- Human correction fixture.

The report computes:

- signal agreement score,
- contradiction list,
- dominant signal kind,
- target confidence,
- missing signal list,
- action authority decision,
- human review reasons.

## Action Authority

Action authority remains blocked for live or sensitive actions. In M7:

- DOM + accessibility + OCR agreement can be sufficient for low-risk fixture review.
- OCR-only sensitive action authority is blocked.
- Visual-only and set-of-marks-only targets are not enough for sensitive action authority.
- Contradictory signals require human review or blocking.
- Human correction markers require review before target stabilization.
- High confidence cannot override risk or policy blocks.

## Integrations

M2 quality/preflight:

- M7 maps existing M2 target/perception findings into product-facing perception reports.
- M2 blocked target-resolution findings remain authoritative.
- Perception confidence cannot unblock policy/risk blocks.

M5 eval:

- Eval reports include a fixture-only perception summary.
- Expected perception blocks can count as expected outcomes.
- Unexpected passes remain regression signals.

M6 sandbox:

- Sandbox reports include perception summaries.
- Missing perception signals become future unlock conditions.
- OCR-only sensitive targets block future sandbox candidate status.

M3 Recipe Lab:

- Recipe Lab view models include a dedicated perception integration panel.
- The panel is read-only and fixture-only.
- The panel exposes no runtime action labels.

## Explicit Exclusions

M7 does not implement:

- live perception,
- no screenshot capture,
- no live DOM capture,
- live accessibility/UIA/Win32 capture,
- no OCR live execution,
- browser runtime,
- desktop runtime,
- recorder runtime,
- no sandbox/VM/container runtime,
- no provider/VLM/LLM calls,
- network calls,
- action execution.

## Future M8 Recommendation

Before any dry-run adapter or browser/desktop runtime work, run a protected-scope audit that verifies M1-M7 contracts remain fixture-only and that any future adapter stays behind explicit feature flags, no-live defaults, redaction policy, evidence policy, human handoff and approval gates.
