# NODAL OS EIL Local Persistence Write Store Scaffold Disabled Handoff

Decision target: `GO_EIL_LOCAL_PERSISTENCE_WRITE_STORE_SCAFFOLD_DISABLED_READY`

## Summary

Evidence Intelligence Layer now has a disabled/fail-closed local write-store scaffold. It provides explicit future command and result contracts without enabling durable writes, filesystem access, database usage, migrations, provider/cloud, semantic/vector backend, runtime actions, executable redaction or UI data-source changes.

## Key Artifacts

- Contract: `src/OneBrain.Core/Evidence/EvidenceIntelligencePersistenceDesign.cs`
- ADR addendum: `docs/adr/eil-local-first-persistence-design-read-only.md`
- QA report: `docs/qa/eil-local-persistence-write-store-scaffold-disabled/report.md`
- Core tests: `tests/OneBrain.Recipes.Tests/EvidenceIntelligencePersistenceDesignTests.cs`
- Safety tests: `tests/OneBrain.Safety.Tests/EvidenceIntelligencePersistenceDesignSafetyTests.cs`

## Current Boundary

- `DisabledEvidenceIntelligenceWriteStore` is disabled by default.
- Normal write commands return fail-closed results.
- Raw, secret, sensitive-never-persist and fixture-only commands are rejected by design.
- No durable write target is configured.
- No fallback target is used.
- Redaction-at-write is required for future unlock but is not executable now.
- EIL UI still uses deterministic fixture/local snapshots.
- No product service registration is added.

## Future Unlock Requirements

A future write activation hito must provide:

- approved ADR update;
- hostile redaction-at-write fixture tests;
- schema compatibility report;
- dry-run migration audit before migrations exist;
- manual QA for any visible persistence status;
- full no-runtime/no-provider/no-live guards.

## Deferred Work

- Real read-store implementation.
- Real write-store implementation.
- Hostile redaction-at-write fixtures.
- Storage backend decision.
- Migration dry-run plan.
- Manual installed-extension QA for any future visible persistence status.

## Recommended Next Block

`EIL_REDACTION_AT_WRITE_HOSTILE_FIXTURES`

Reason: the read and write scaffolds are now disabled/fail-closed. The next safe step is adversarial redaction coverage before any migration dry-run or durable persistence implementation.
