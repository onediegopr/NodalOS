# NODAL OS EIL Redaction-At-Write Hostile Fixtures Handoff

Decision target: `GO_EIL_REDACTION_AT_WRITE_HOSTILE_FIXTURES_READY`

## Summary

Evidence Intelligence Layer now has hostile fixture coverage for the disabled local write-store scaffold. The coverage proves synthetic secret-like, raw, unknown-sensitivity, sensitive-never-persist, graph, readiness and safe-next-step write commands reject fail-closed without filesystem, database, migration, provider/cloud, semantic/vector, runtime or redaction pipeline execution.

## Key Artifacts

- Contract: `src/OneBrain.Core/Evidence/EvidenceIntelligencePersistenceDesign.cs`
- ADR addendum: `docs/adr/eil-local-first-persistence-design-read-only.md`
- QA report: `docs/qa/eil-redaction-at-write-hostile-fixtures/report.md`
- Core tests: `tests/OneBrain.Recipes.Tests/EvidenceIntelligencePersistenceDesignTests.cs`
- Safety tests: `tests/OneBrain.Safety.Tests/EvidenceIntelligencePersistenceDesignSafetyTests.cs`

## Current Boundary

- `DisabledEvidenceIntelligenceWriteStore` remains disabled by default.
- Hostile commands return rejected/fail-closed results.
- Unknown sensitivity is rejected.
- Integrity hash before canonical redaction is rejected.
- Raw, secret-like, sensitive-never-persist and fixture-only payloads are rejected.
- No durable write target is configured.
- No fallback target is used.
- Redaction-at-write remains required for future unlock but is not executable now.
- EIL UI still uses deterministic fixture/local snapshots.
- No product service registration is added.

## Future Unlock Requirements

A future persistence hito must provide:

- approved ADR update;
- broader adversarial redaction coverage if new fields are introduced;
- schema compatibility report;
- dry-run migration audit before migrations exist;
- manual QA for any visible persistence status;
- full no-runtime/no-provider/no-live guards.

## Deferred Work

- Real read-store implementation.
- Real write-store implementation.
- Dry-run migration plan.
- Storage backend decision.
- Manual installed-extension QA for any future visible persistence status.

## Recommended Next Block

`EIL_LOCAL_PERSISTENCE_DRY_RUN_MIGRATION_PLAN`

Reason: read/write disabled scaffolds now have hostile redaction guard coverage. The next safe step is a dry-run migration plan that remains design-only and does not execute migrations.
