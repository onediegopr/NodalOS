# EIL Redaction-At-Write Hostile Fixtures QA Report

Decision target: `GO_EIL_REDACTION_AT_WRITE_HOSTILE_FIXTURES_READY`

## Summary

This hito adds deterministic hostile fixtures for the disabled Evidence Intelligence Layer write-store scaffold. The fixtures harden the redaction-at-write contract without implementing durable persistence or executing a product redaction pipeline.

No durable persistence implementation was added.

## Files

- `src/OneBrain.Core/Evidence/EvidenceIntelligencePersistenceDesign.cs`
- `tests/OneBrain.Recipes.Tests/EvidenceIntelligencePersistenceDesignTests.cs`
- `tests/OneBrain.Safety.Tests/EvidenceIntelligencePersistenceDesignSafetyTests.cs`
- `docs/adr/eil-local-first-persistence-design-read-only.md`
- `docs/qa/eil-redaction-at-write-hostile-fixtures/report.md`
- `docs/handoff/nodal-os-eil-redaction-at-write-hostile-fixtures-handoff.md`

## Fixture Coverage

Total fixtures: 20.

Covered categories:

- synthetic API key;
- synthetic bearer token;
- synthetic JWT;
- synthetic GitHub token;
- synthetic AWS-style access key;
- synthetic private key block;
- synthetic credential pair;
- synthetic cookie/session token;
- synthetic email and phone PII;
- synthetic fiscal ID;
- raw OCR payload;
- raw browser/CDP payload;
- raw WCU payload;
- unknown sensitivity payload;
- sensitive-never-persist payload;
- mixed safe plus secret-like payload;
- redacted-looking but still raw payload;
- integrity hash envelope before canonical redaction;
- graph node/edge with sensitive payload;
- safe next step with embedded secret-like content.

All secret-like strings are synthetic and marked as fixture/not-real.

## Expected Decision

Every hostile fixture is expected to return:

- `EvidenceIntelligenceWriteResultStatus.Rejected`;
- `FailClosed: true`;
- no persisted or success state;
- no filesystem read/write;
- no database write;
- no migration;
- no provider/cloud call;
- no semantic/vector backend;
- no runtime action;
- no product redaction pipeline execution;
- no fallback target.

## Contract Hardening

`EvidenceIntelligenceWriteCommand` now explicitly models:

- `SensitivityKnown`;
- `IntegrityHashBeforeCanonicalRedaction`.

The existing rejection rule remains design-only and now rejects unknown sensitivity and integrity hash envelopes before canonical redaction in addition to raw payload, secret-like fields, sensitive-never-persist and fixture-only classifications.

## Safety Proof

The hostile fixtures are test-only and in-memory. The disabled write-store scaffold remains unregistered and is not consumed by the EIL UI mount.

The EIL UI remains fixture-compatible:

- `EvidenceIntelligenceReadOnlyUiMount.CreateFixture()` remains the UI boundary.
- `UsesDeterministicFixture` remains true.
- durable persistence remains disabled.

## Findings

| Severity | Finding | Status |
| --- | --- | --- |
| P0 | None. | Closed |
| P1 | None. | Closed |
| P2 | None. | Closed |
| P3 | Additional adversarial redaction cases may be added before any real store exists. | Deferred |
| P3 | Migration dry-run remains future work. | Deferred |

## Validation Notes

Required validation evidence is recorded in the milestone final report. Tests added in this hito cover all 20 hostile fixtures, synthetic secret sanity, no persisted/success state, no side effects and redaction-overclaim guards.

## Recommended Next Block

`EIL_LOCAL_PERSISTENCE_DRY_RUN_MIGRATION_PLAN`

Reason: after read/write disabled scaffolds and hostile fixture guards, the next safe persistence step is a design-only dry-run migration plan, still without durable persistence or migration execution.
