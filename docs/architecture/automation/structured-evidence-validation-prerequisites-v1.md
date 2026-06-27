# Structured Evidence / Validation Prerequisites v1

Decision target: `NODAL_OS_M9_STRUCTURED_EVIDENCE_VALIDATION_PREREQUISITE_HARDENING`

## Decision

NODAL OS adds a no-runtime structured prerequisite layer for Reliable Recipes. The layer models evidence and validation requirements as explicit, typed, auditable prerequisites before any future dry-run adapter candidate can be considered.

M9 does not add runtime behavior. It does not add an executable adapter, browser launch, CDP connection, Playwright/Selenium/Puppeteer path, Cloak mutation, desktop/UIA/Win32 behavior, OCR live activation, screenshot capture, recorder runtime, sandbox/VM/container runtime, provider/LLM calls, network calls or shell/process execution.

## Dependency Chain

M9 builds on:

- M1 Reliable Recipe foundation contracts.
- M2 quality/preflight scoring.
- M3 read-only Recipe Lab view models.
- M4 recorder-to-recipe fixture drafts.
- M5 fixture eval harness.
- M6 computer-use sandbox readiness reports.
- M7 perception integration reports.
- M8 protected dry-run adapter readiness design.

## Why Structured Prerequisites

Before M9, evidence and validation readiness could be inferred from legacy string refs such as `evidence.download` or `validation.timeline`, from block kinds, or from labels. Those remain useful compatibility inputs, but they are weaker than explicit structured requirements.

M9 classifies each requirement source:

- `Explicit`
- `FixtureExplicit`
- `MappedFromLegacyContract`
- `InferredFromBlockKind`
- `InferredFromLabel`
- `Missing`

Explicit and fixture-explicit requirements are strongest. Legacy mapped requirements pass design review with warning. Inferred non-critical requirements warn. Inferred or missing critical requirements block future adapter gates.

## Evidence Requirement Model

Structured evidence requirements include:

- before state,
- after state,
- action proposal,
- action result,
- validation report,
- timeline event,
- approval decision,
- human intervention,
- perception snapshot,
- OCR supporting signal reference,
- sandbox readiness report,
- eval scenario report,
- download artifact reference,
- extracted data schema,
- redaction report,
- rollback or restore plan,
- policy decision,
- secret handling report.

Evidence remains reference-only. M9 does not capture screenshots, OCR, browser state, desktop state or raw payloads.

## Validation Requirement Model

Structured validation requirements include:

- visible text assertion,
- element state assertion,
- URL or route assertion,
- file downloaded assertion,
- data extracted assertion,
- schema match assertion,
- field value assertion,
- modal state assertion,
- loop termination assertion,
- external side effect confirmation,
- human confirmation,
- policy decision assertion,
- perception confidence assertion,
- sandbox readiness assertion,
- eval expected outcome assertion.

No success is implied without structured validation coverage.

## Adapter Gate Impact

M9 feeds the M8 gates:

- `EvidenceExpectationsStructured`
- `ValidationExpectationsStructured`

Those gates are satisfied only when the M9 structured profile has no missing critical evidence or validation requirements and no critical inferred requirement that blocks future adapter readiness.

Legacy mapped requirements can pass design-only readiness with warning. They are not runtime authorization. External audit remains required before any runtime-adjacent work.

## Integrations

M2 quality/preflight now carries `StructuredPrerequisites`.

M5 eval reports now carry a structured prerequisite summary.

M6 sandbox readiness reports now carry a structured prerequisite summary and list missing structured prerequisites as future unlock conditions.

M8 dry-run adapter readiness now uses the M9 structured profile for the evidence and validation gates.

M3 Recipe Lab view models now include a read-only structured prerequisites panel.

## Protected Boundary

OCR remains protected and is referenced only as a supporting signal requirement. OCR-only sensitive targets cannot authorize sensitive action.

Perception remains fixture-only. No live DOM, accessibility, UIA, Win32, screenshot, VLM or provider capture is added.

Recorder remains fixture-only. No mouse, keyboard, clipboard, active-window or screen capture is added.

Sandbox remains readiness-only. No VM, Docker, container, browser, desktop or remote session is started.

Runtime real autonomy remains 0%.

## Future M10 Recommendation

M10 should harden prerequisite authoring and review workflows: structured profile authoring fixtures, review checklists, and migration reports from legacy refs to explicit structured requirements. It should still remain no-runtime unless a separate protected external audit authorizes runtime-adjacent implementation.
