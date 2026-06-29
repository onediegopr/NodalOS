# NODAL OS Handoff: EIL Local Persistence Dry-Run Migration Plan

Decision target: `GO_EIL_LOCAL_PERSISTENCE_DRY_RUN_MIGRATION_PLAN_READY`

## Summary

This hito adds a design-only dry-run migration plan contract for EIL local persistence.

The contract models schema versions, migration steps, blockers, redaction-at-write preconditions, rollback design state, human approval requirements and no-side-effect result flags. It does not execute migrations and does not create a migration runner.

## Files

- `src/OneBrain.Core/Evidence/EvidenceIntelligencePersistenceDesign.cs`
  - Adds dry-run migration contract types and fail-closed planner evaluation.
- `tests/OneBrain.Recipes.Tests/EvidenceIntelligencePersistenceDesignTests.cs`
  - Adds deterministic fixture plans and behavior checks.
- `tests/OneBrain.Safety.Tests/EvidenceIntelligencePersistenceDesignSafetyTests.cs`
  - Adds no-side-effect and overclaim safety checks.
- `docs/adr/eil-local-first-persistence-design-read-only.md`
  - Adds dry-run migration plan addendum.
- `docs/qa/eil-local-persistence-dry-run-migration-plan/report.md`
  - Records QA scope and expected evidence.

## Status

The dry-run migration plan remains:

- design-only;
- fixture-safe;
- fail-closed;
- no-runtime;
- no-live;
- no-provider/cloud;
- no filesystem read/write product capability;
- no DB/dependency;
- no durable persistence active.

## Unlock Requirements

Future migration work still requires:

- schema compatibility guards;
- redaction-at-write hostile fixture coverage remains green;
- no-side-effect scans;
- manual approval requirements;
- migration audit packet;
- explicit future hito before any runner or durable store.

## Risks

- The plan is not a runner and cannot validate real persisted data.
- Schema compatibility rules are modeled at blocker level only.
- Rollback/restore remains design-only.

These risks are expected for this hito and are not blocking if tests/scans pass.

## Recommended Next Block

Recommended: `EIL_LOCAL_PERSISTENCE_SCHEMA_COMPATIBILITY_GUARDS`

Reason: after a design-only dry-run plan exists, schema/version compatibility should be hardened before any dry-run audit packet or durable store implementation is considered.
