# NODAL OS EIL Local Persistence Design Read-Only Handoff

Decision target: `GO_EIL_LOCAL_PERSISTENCE_DESIGN_READ_ONLY_READY`

## Summary

The Evidence Intelligence Layer now has a design-only, fail-closed local persistence contract. The contract describes future local-first persistence without enabling durable storage, migration, product writes, semantic/vector backend, provider/cloud or runtime/live automation.

## Key Artifacts

- ADR: `docs/adr/eil-local-first-persistence-design-read-only.md`
- Contract: `src/OneBrain.Core/Evidence/EvidenceIntelligencePersistenceDesign.cs`
- QA report: `docs/qa/eil-local-persistence-design-read-only/report.md`
- Core tests: `tests/OneBrain.Recipes.Tests/EvidenceIntelligencePersistenceDesignTests.cs`
- Safety tests: `tests/OneBrain.Safety.Tests/EvidenceIntelligencePersistenceDesignSafetyTests.cs`

## Current Boundary

- EIL UI still reads deterministic fixture/local snapshots.
- Durable store is disabled.
- Durable reads are disabled.
- Durable writes are disabled.
- Migration runner is disabled.
- Product filesystem writes are disabled.
- Semantic/vector backend is disabled.
- Provider/cloud calls are disabled.
- Runtime/live remains blocked.

## Future Unlock Requirements

A future implementation hito must provide:

- approved ADR update;
- redaction-at-write hostile fixture tests;
- filesystem-write audit;
- migration dry-run audit;
- schema compatibility report;
- manual QA packet for visible persistence status;
- no-runtime/no-provider/no-live guards.

## Recommended Next Block

`EIL_LOCAL_PERSISTENCE_READ_STORE_SCAFFOLD_DISABLED`

Reason: after the design contract, the next safe step is a disabled/fail-closed read-store scaffold. It should not add writes, migrations, provider/cloud, semantic/vector backend or runtime.
