# Phase D Context Memory Artifact Index

## Core Code

- `src/OneBrain.Core/Context/WorkspaceContextReadOnlyFoundation.cs`
  - Foundation packet presenter.
  - Authority/freshness guard.
  - Selection/lock/exclusion guard.
  - Memory candidate contradiction/risk guard.
  - Workspace context packet surface.
  - Context packet export preview.

## Tests

- `tests/OneBrain.Recipes.Tests/WorkspaceContextReadOnlyFoundationTests.cs`
  - Functional fixture-safe coverage for packet, guards, surface and export preview.
- `tests/OneBrain.Safety.Tests/WorkspaceContextReadOnlyFoundationSafetyTests.cs`
  - No-side-effect, no-runtime, no-memory-real, no-provider/cloud, no-vector/semantic, no-export-real and overclaim guards.

## ADRs

- `docs/adr/phase-d-context-workspace-memory-read-only-foundation.md`
- `docs/adr/phase-d-context-authority-freshness-guards.md`
- `docs/adr/phase-d-context-selection-lock-exclusion-guards.md`
- `docs/adr/phase-d-memory-candidate-contradiction-risk-read-only.md`
- `docs/adr/phase-d-workspace-context-packet-surface-read-only.md`
- `docs/adr/phase-d-context-packet-read-only-export-preview.md`

## QA Reports

- `docs/qa/phase-d-context-workspace-memory-read-only-foundation/report.md`
- `docs/qa/phase-d-context-authority-freshness-guards/report.md`
- `docs/qa/phase-d-context-selection-lock-exclusion-guards/report.md`
- `docs/qa/phase-d-memory-candidate-contradiction-risk-read-only/report.md`
- `docs/qa/phase-d-workspace-context-packet-surface-read-only/report.md`
- `docs/qa/phase-d-context-packet-read-only-export-preview/report.md`
- `docs/qa/phase-d-context-memory-closeout-audit-prep/report.md`

## Handoffs

- `docs/handoff/nodal-os-phase-d-context-workspace-memory-read-only-foundation-handoff.md`
- `docs/handoff/nodal-os-phase-d-context-authority-freshness-guards-handoff.md`
- `docs/handoff/nodal-os-phase-d-context-selection-lock-exclusion-guards-handoff.md`
- `docs/handoff/nodal-os-phase-d-memory-candidate-contradiction-risk-read-only-handoff.md`
- `docs/handoff/nodal-os-phase-d-workspace-context-packet-surface-read-only-handoff.md`
- `docs/handoff/nodal-os-phase-d-context-packet-read-only-export-preview-handoff.md`
- `docs/handoff/nodal-os-phase-d-context-memory-closeout-audit-prep-handoff.md`

## Explicitly Deferred

- Real workspace scan.
- Durable memory.
- Provider/cloud.
- Semantic/vector backend.
- LLM live.
- Runtime/live.
- Physical export, clipboard and browser download.
- Product UI action surface.
- Manual installed-extension QA.
