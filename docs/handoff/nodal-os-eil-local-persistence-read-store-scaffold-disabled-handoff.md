# NODAL OS EIL Local Persistence Read Store Scaffold Disabled Handoff

Decision target: `GO_EIL_LOCAL_PERSISTENCE_READ_STORE_SCAFFOLD_DISABLED_READY`

## Summary

Evidence Intelligence Layer now has a disabled/fail-closed local read-store scaffold. It provides explicit future query and result contracts without enabling durable reads, writes, migrations, database usage, provider/cloud, semantic/vector backend, runtime actions or UI data-source changes.

## Key Artifacts

- Contract: `src/OneBrain.Core/Evidence/EvidenceIntelligencePersistenceDesign.cs`
- ADR addendum: `docs/adr/eil-local-first-persistence-design-read-only.md`
- QA report: `docs/qa/eil-local-persistence-read-store-scaffold-disabled/report.md`
- Core tests: `tests/OneBrain.Recipes.Tests/EvidenceIntelligencePersistenceDesignTests.cs`
- Safety tests: `tests/OneBrain.Safety.Tests/EvidenceIntelligencePersistenceDesignSafetyTests.cs`

## Current Boundary

- `DisabledEvidenceIntelligenceReadStore` is disabled by default.
- Queries return fail-closed results.
- No durable read source is configured.
- No fallback store is used.
- EIL UI still uses deterministic fixture/local snapshots.
- No product service registration is added.

## Future Unlock Requirements

A future activation hito must provide:

- approved ADR update;
- redaction-at-write hostile fixture tests;
- schema compatibility report;
- dry-run migration audit before migrations exist;
- manual QA for any visible persistence status;
- full no-runtime/no-provider/no-live guards.

## Deferred Work

- Real read-store implementation.
- Disabled write-store scaffold.
- Storage backend decision.
- Migration dry-run plan.
- Manual installed-extension QA for any future visible persistence status.

## Recommended Next Block

`EIL_LOCAL_PERSISTENCE_WRITE_STORE_SCAFFOLD_DISABLED`

Reason: the read-store scaffold is now fail-closed. The next safe step is the matching write-store scaffold, still disabled, before any dry-run migration or durable persistence implementation.
