# NODAL OS Handoff: EIL Evidence Timeline Export Read-Only

Decision target: `GO_EIL_EVIDENCE_TIMELINE_EXPORT_READ_ONLY_READY`

## Summary

This hito adds a read-only, fixture-safe Evidence Timeline Export preview for EIL.

The preview is an in-memory model and copy-ready text. It does not create files, write filesystem data, read product filesystem data, use a database, call provider/cloud/network, execute migrations, register services, or enable runtime/live behavior.

## Files

- `src/OneBrain.Core/Evidence/EvidenceIntelligenceTimelineExportReadOnly.cs`
  - Adds export preview model, manifest, sections, timeline events, presenter and no-side-effect proof.
- `tests/OneBrain.Recipes.Tests/EvidenceIntelligenceTimelineExportReadOnlyTests.cs`
  - Adds fixture-safe functional tests.
- `tests/OneBrain.Safety.Tests/EvidenceIntelligencePersistenceDesignSafetyTests.cs`
  - Adds safety guards for export preview.
- `docs/adr/eil-local-first-persistence-design-read-only.md`
  - Adds read-only export preview addendum.
- `docs/qa/eil-evidence-timeline-export-read-only/report.md`
  - Records QA scope and expected evidence.

## Status

The export preview remains:

- read-only;
- fixture-safe;
- deterministic;
- in-memory;
- copy-ready only;
- no filesystem export;
- no durable persistence;
- no DB/dependency;
- no migration runner;
- no runtime/live/provider/cloud.

## Risks

- The preview is not a physical export artifact.
- It covers deterministic fixtures only.
- Manual UI QA is not part of this hito because UI was not changed.

These risks are expected and non-blocking if tests/scans pass.

## Recommended Next Block

Recommended: `READ_ONLY_AUDIT_DASHBOARD_SURFACE`

Reason: after export preview, the next safe step is an audit-safe dashboard that consolidates Fase C state before closeout.
