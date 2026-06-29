# NODAL OS Handoff: EIL Local Persistence Schema Compatibility Guards

Decision target: `GO_EIL_LOCAL_PERSISTENCE_SCHEMA_COMPATIBILITY_GUARDS_READY`

## Summary

This hito adds design-only schema compatibility guards for EIL local persistence.

The guard models artifact kinds, compatibility issues, decisions, status and no-side-effect results. It evaluates deterministic in-memory checks only. It does not parse files, read persisted data, write persisted data, create a database, execute migrations or register a service.

## Files

- `src/OneBrain.Core/Evidence/EvidenceIntelligencePersistenceDesign.cs`
  - Adds schema compatibility guard contract and evaluator.
- `tests/OneBrain.Recipes.Tests/EvidenceIntelligencePersistenceDesignTests.cs`
  - Adds 20 deterministic compatibility fixtures and behavior checks.
- `tests/OneBrain.Safety.Tests/EvidenceIntelligencePersistenceDesignSafetyTests.cs`
  - Adds no-side-effect and overclaim safety checks.
- `docs/adr/eil-local-first-persistence-design-read-only.md`
  - Adds schema compatibility guard addendum.
- `docs/qa/eil-local-persistence-schema-compatibility-guards/report.md`
  - Records QA scope and expected evidence.

## Status

The schema compatibility guard remains:

- design-only;
- fixture-safe;
- fail-closed;
- no-runtime;
- no-live;
- no-provider/cloud;
- no filesystem read/write product capability;
- no DB/dependency;
- no durable persistence active;
- no migration runner.

## Unlock Requirements

Future persistence work still requires:

- read-only evidence timeline/export evidence;
- audit dashboard or Fase C closeout packet;
- hostile redaction coverage remains green;
- dry-run migration plan remains green;
- schema compatibility guards remain green;
- explicit future hito before any durable store or runner.

## Risks

- The guard validates modeled fixtures only, not real persisted data.
- It is not a parser and does not load external schema files.
- Future schema hash/baseline checks remain deferred.

These risks are expected for this hito and are not blocking if tests/scans pass.

## Recommended Next Block

Recommended: `EIL_EVIDENCE_TIMELINE_EXPORT_READ_ONLY`

Reason: after design, scaffold, hostile guards, dry-run plan and schema compatibility guards, the next safe step is read-only evidence export/surface work before any durable implementation is considered.
