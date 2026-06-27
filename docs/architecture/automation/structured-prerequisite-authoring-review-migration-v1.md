# Structured Prerequisite Authoring / Review Migration v1

Status: `NODAL_OS_M10_STRUCTURED_PREREQUISITE_AUTHORING_REVIEW_MIGRATION_REPORTS`

## Decision

NODAL OS adds a no-runtime authoring and migration report layer for structured evidence and validation prerequisites. The layer explains which requirements are explicit, fixture-explicit, mapped from legacy contracts, inferred from block kind or label, or still missing. It proposes fixture-only structured requirements and records review decisions without applying any live migration or enabling any adapter.

## Dependency Chain

M10 builds on:

- M1 Reliable Recipe contracts.
- M2 quality/preflight scoring.
- M3 read-only Recipe Lab view models.
- M4 recorder draft fixture models.
- M5 eval fixture scenarios.
- M6 sandbox readiness reports.
- M7 perception integration reports.
- M8 protected dry-run adapter readiness design.
- M9 structured evidence and validation prerequisites.

## No-Runtime Boundary

M10 does not add an executable adapter, runtime command, browser launch, CDP connection, Playwright/Selenium/Puppeteer path, Cloak mutation, desktop/UIA/Win32 behavior, OCR live activation, screenshot capture, recorder runtime, sandbox/VM/container runtime, provider/LLM call, network call, shell/process runner or productive filesystem action.

Accepting a proposal means only: accepted for fixture/read-only design review. It does not enable runtime.

## Authoring Model

`StructuredPrerequisiteAuthoringReport` summarizes:

- proposals for mapped, inferred or missing requirements,
- review checklist items,
- migration summary counts,
- accepted and rejected requirement proposal ids,
- still-missing critical requirements,
- adapter gate impact,
- no-runtime notice.

Proposals are deterministic and generated from `ReliableRecipeStructuredPrerequisiteProfile`. No AI or live environment is used.

## Review Decisions

Review decisions are fixture-only:

- `PendingReview`
- `AcceptedForFixture`
- `AcceptedMappedLegacy`
- `RejectedNeedsRedesign`
- `RejectedUnsafe`
- `Deferred`

Critical proposals are not auto-accepted. Rejected critical proposals block adapter readiness. Pending critical proposals block M8 gates. Accepted fixture proposals can improve design-only readiness, but runtime remains blocked.

## Migration Summary

The migration summary reports:

- explicit count,
- fixture-explicit count,
- mapped legacy count,
- inferred count,
- missing count,
- proposed count,
- accepted count,
- rejected count,
- still-blocking count.

Mapped legacy requirements remain compatibility evidence, not full explicit proof.

## Adapter Gate Impact

M10 exposes:

- `NoImpact`
- `WarningOnly`
- `BlocksUntilReview`
- `BlocksUntilExplicit`
- `BlocksUntilExternalAudit`
- `RuntimeAlwaysBlockedInM10`

M8 consumes authoring reports optionally. If no authoring report is supplied, M8 keeps M9 behavior. If a report is supplied, pending/rejected critical proposals block structured evidence and validation gates. Accepted fixture proposals can satisfy design-only gates but cannot enable runtime.

## Recipe Lab Integration

Recipe Lab gains a read-only authoring panel with proposal counts, pending review count, accepted/rejected counts, top proposals, checklist summaries, migration summary and adapter gate impact.

Allowed product copy:

- Proposed structured requirement.
- Needs review.
- Accepted for fixture.
- Rejected unsafe.
- Mapped legacy requirement.
- Adapter gate blocked.
- Runtime not enabled.

Forbidden product copy:

- Ready to execute.
- Adapter enabled.
- Run now.
- Auto-migrate live.
- Validated live.
- Evidence captured live.
- Production-ready.

## OCR / Perception / Recorder / Sandbox Protection

OCR remains a protected existing capability. M10 only references OCR/perception requirement kinds from M7/M9 as supporting evidence and validation prerequisites.

M10 does not add live perception, recorder capture, sandbox runtime or adapter execution.

## Future M11 Recommendation

M11 should add fixture-only structured prerequisite authoring UX review packs and operator handoff templates, still no-runtime. It should focus on reviewing proposal copy, operator approvals, and explicit requirement authoring workflows before any future protected runtime audit.
