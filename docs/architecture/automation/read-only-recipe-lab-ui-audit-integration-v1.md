# Read-Only Recipe Lab UI Audit Integration v1

Status: `NODAL_OS_M13_READ_ONLY_RECIPE_LAB_UI_AUDIT_INTEGRATION_EXTERNAL_AUDIT_HANDOFF_REVIEW`

## Decision

NODAL OS adds a read-only Recipe Lab audit surface presenter for the M1-M12 Reliable Recipe foundation. The presenter turns existing fixture-safe viewmodels into a UI-ready product surface with Mission Control-style sections, status badges, milestone timeline, external audit handoff and no-runtime proof copy.

M13 intentionally does not add a frontend route or executable UI shell. The repo currently exposes the relevant Recipe Lab product layer through .NET contracts/viewmodels and tests. A presenter-only surface is the safest integration point because it avoids touching browser/runtime/stealth panels or unrelated frontend packages.

## Dependency Chain

The presenter integrates:

- M1 Reliable Recipe foundation contracts.
- M2 quality and preflight reports.
- M3 read-only Recipe Lab viewmodels.
- M4 recorder draft review panels.
- M5 eval harness fixture reports.
- M6 sandbox readiness panels.
- M7 perception integration panels.
- M8 adapter readiness design panels.
- M9 structured evidence and validation prerequisite panels.
- M10 prerequisite authoring and migration panels.
- M11 operator review pack panels.
- M12 no-runtime closeout and audit readiness panels.

## UI Integration Choice

M13 adds:

- `ReliableRecipeLabAuditSurfaceViewModel`
- `ReliableRecipeLabAuditSurfacePresenter`
- section, metric, badge, milestone, design-system and external-audit handoff models

No UI framework files are touched. No route or page is registered. The presenter is deterministic and consumes existing fixture/viewmodel data.

M1-M13 did not add or enable runtime; existing protected runtime scopes remain present and untouched.

## UI Sections

The surface exposes:

- Header with `Recipe Lab`, `Runtime not enabled`, `External audit required`, `Fixture-only`.
- Overall status strip with product readiness, audit readiness, adapter readiness design and runtime autonomy `0%`.
- Quality/preflight.
- Evidence/validation.
- Recorder draft.
- Eval harness.
- Sandbox readiness.
- Perception.
- Adapter readiness.
- Structured prerequisites.
- Structured prerequisite authoring.
- Operator review pack.
- Closeout/audit.
- M1-M13 milestone timeline.

## Visual Direction

The presenter records design tokens for a dark-first Mission Control surface:

- calm graphite background,
- compact command typography,
- numeric score badges,
- asymmetric control-room panels,
- vertical milestone rail,
- dense but scannable panels,
- warnings and audit gates visible.

Success states mean fixture/read-only readiness only. They do not imply runtime.

## Timeline Semantics

The Recipe Lab audit presenter separates the closeout model from the product timeline:

- M12 closeout models the M1-M11 no-runtime foundation, invariant matrix, protected-scope proof and external audit handoff.
- M13 adds the read-only presenter/audit surface.
- The visible timeline therefore includes M1-M13 rows, while the M12 closeout report keeps its M1-M11 foundation summary intact.

This avoids claiming that the M12 closeout generator itself owns M12/M13 milestones.

## Product Wording

Required copy includes:

- Fixture-ready does not mean runtime-ready.
- External audit is required before any runtime or adapter work.
- OCR is a supporting signal, not action authority.
- Operator signoff cannot approve runtime.
- No live browser, desktop, recorder, OCR or sandbox is enabled.

Allowed actions are:

- Review.
- Inspect.
- Open details.
- Copy summary.
- Export review pack.
- View audit handoff.

Forbidden labels remain absent from action labels:

- Run.
- Execute.
- Start adapter.
- Launch browser.
- Connect CDP.
- Replay.
- Record live.
- Capture screen.
- Enable runtime.
- Approve runtime.
- Production ready.
- Automation ready.
- Live validated.

## No-Runtime Boundary

M13 does not add an executable adapter, runtime command, browser launch, CDP connection, browser driver framework path, Cloak mutation, desktop/UIA/Win32 live behavior, OCR live activation, screenshot capture, recorder runtime, sandbox/VM/container runtime, provider/LLM call, network call, shell/process runner, productive filesystem action or UI execution action.

This is a scoped claim about the M1-M13 Reliable Recipe foundation. It is not a repo-wide claim that browser/CDP/runtime projects do not exist elsewhere. Existing protected runtime scopes remain present and untouched.

## External Audit Handoff Visibility

The presenter surfaces the M12 external audit handoff with audit questions, evidence references, runtime prohibited statement and audit decision labels.

## Shared Wording And Capability Centralization Backlog

Before mounting this presenter into a real UI host, centralize the following shared data to reduce drift:

- forbidden product action labels,
- blocked runtime capability names,
- no-runtime notices,
- protected-scope wording,
- M1-M13 milestone metadata.

M13.1 adds focused static guard tests, but a mounted UI should use one shared source for these labels and capability lists.

## Future Work

The next safe block can render this presenter in an existing read-only shell if an approved UI host is selected. Runtime, adapters and live capture remain out of scope.
