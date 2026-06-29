# ADR: EIL Local-First Persistence Design Read-Only

Decision target: `GO_EIL_LOCAL_PERSISTENCE_DESIGN_READ_ONLY_READY`

## Status

Accepted for design-only planning. No durable store is implemented or enabled by this ADR.

## Context

Evidence Intelligence Layer currently exposes a deterministic, local, read-only fixture surface through:

- `EvidenceIntelligenceReadOnlyPresenter`
- `EvidenceIntelligenceReadOnlyUiMount`
- `EvidenceIntelligenceSurfaceFixtureCatalog`
- the sidepanel Mission Control section `#evidenceIntelligenceSurface`

The next roadmap step needs a local-first persistence architecture, but the current hito must not write evidence, create a store, run migrations, enable semantic/vector backend, or connect provider/cloud/runtime systems.

## Decision

Design local-first EIL persistence as disabled-by-default, redaction-at-write, schema-versioned, workspace-bounded, fail-closed and no-runtime until explicitly unlocked by a future hito.

The recommended future backend shape is append-only redacted evidence records plus a derived read model. SQLite can be considered later for the read model, but this hito does not add any database dependency or implementation.

## Goals

- Define local-first evidence persistence boundaries.
- Define conceptual data model and field classifications.
- Define redaction-at-write policy.
- Define schema/versioning, integrity and retention strategy.
- Define migration plan and unlock criteria.
- Add fail-closed capability/status contracts and guards.

## Non-Goals

- No durable store implementation.
- No product filesystem write path.
- No JSON store used by product runtime.
- No SQLite/DB implementation.
- No migration runner.
- No background indexing.
- No semantic/vector backend.
- No provider/network/cloud.
- No runtime/live browser/CDP/WCU/OCR.
- No Recipe execution.

## Local-First Constraints

- Evidence is scoped by `WorkspaceId` and `SessionId`.
- Future storage must remain local by default.
- Cross-workspace evidence reads are forbidden unless a future explicit policy gate exists.
- Raw DOM, screenshots, logs, cookies, tokens, credentials and provider payloads are not persisted.
- Evidence references are preferred over raw payloads.
- Current EIL UI continues to use deterministic fixture snapshots.

## Data Model

Conceptual entities are defined in `EvidenceIntelligencePersistenceSchemaCatalog`:

- `EvidenceRecord`
- `EvidenceSource`
- `EvidenceReference`
- `ClaimScanSnapshot`
- `ActionScanSnapshot`
- `ContradictionRecord`
- `EvidenceGraphNode`
- `EvidenceGraphEdge`
- `ReadinessMatrixSnapshot`
- `HumanActionRequirement`
- `SafeNextStep`
- `RedactionMetadata`
- `SchemaVersion`
- `RetentionPolicy`
- `IntegrityHash`

Field classifications:

- `SafeToDisplay`: visible text safe for UI/reporting.
- `RedactedBeforeWrite`: must pass redaction before future durable write.
- `SensitiveNeverPersist`: raw or sensitive data that must be rejected.
- `Derived`: computed from redacted/reference data.
- `FixtureOnly`: valid only for deterministic fixture data.
- `FuturePersisted`: eligible only after future persistence hito unlock.

## Schema And Versioning

- Schema id: `eil.local-evidence.schema.v1.design-only`.
- Version: major `1`, minor `0`.
- Schema version is required on future records.
- Redaction metadata is required.
- Integrity hash is required.
- Workspace boundary is required.
- Raw secrets, raw DOM and raw screenshots are not allowed.

## Redaction At Write

Future write path must:

- run redaction before durable write;
- reject `SensitiveNeverPersist` fields;
- persist field names or redaction status, not raw secret values;
- compute `IntegrityHash` only after canonical redaction;
- fail closed if redaction status is missing, rejected or ambiguous.

No write path exists in this hito.

## Retention

Retention policy design:

- fixture snapshots remain in-memory for this hito;
- future redacted records are workspace-bounded;
- future claim/action snapshots expire with source evidence;
- revoked sources require tombstone metadata only;
- raw payload retention is not allowed.

## Integrity Strategy

Future persisted records require canonical redacted payload hashing:

- recommended algorithm: SHA-256;
- hash after redaction;
- include schema id/version and workspace boundary in canonical payload;
- never hash raw secret payload as a retained artifact.

## Migration Plan

Migration is disabled in this hito.

Future migration must require:

- explicit future hito;
- dry-run report;
- backup/rollback plan;
- schema compatibility tests;
- redaction audit;
- no product-write proof for disabled scaffold before any active store.

## Threat Model

Assets:

- redacted evidence records;
- evidence refs and source refs;
- claim/action snapshots;
- contradiction/readiness decisions;
- workspace/session boundaries.

Threats:

- raw secret or credential persisted before redaction;
- cross-workspace evidence leakage;
- stale or corrupted evidence treated as current;
- migration silently broadens retained fields;
- semantic/vector index implies capability that remains disabled.

Mitigations:

- redaction-at-write is mandatory before any durable write exists;
- workspace/session boundaries are required schema fields;
- integrity hash and schema version are required;
- migration remains disabled until future dry-run/audit hito;
- semantic/vector backend remains explicitly disabled.

## Alternatives Considered

- In-memory only: safest now, but not sufficient for later audit history.
- JSON local store: simple, but higher corruption and migration risk.
- SQLite local store: good future read model candidate, but premature now.
- Append-only event log: best future audit trail when paired with redacted records.
- Hybrid append-only log plus read model: preferred future direction, with SQLite read model only after guards.

## Compatibility

- EIL UI mount remains fixture/local read-only.
- Existing lexical search remains deterministic and real.
- Semantic/vector backend remains disabled.
- Recipe Lab can later reference evidence refs, but cannot execute recipes.
- No provider/cloud/runtime bridge is introduced.

## Unlock Criteria

Implementation can only start in a future explicit hito after:

- approved ADR update;
- hostile redaction-at-write tests;
- filesystem-write audit;
- migration dry-run audit;
- schema compatibility report;
- manual QA packet for any visible persistence status;
- full no-runtime/no-provider/no-live guards.

Runtime/live unlock is not allowed by this ADR.

## Addendum: Disabled Read Store Scaffold

Decision target: `GO_EIL_LOCAL_PERSISTENCE_READ_STORE_SCAFFOLD_DISABLED_READY`

The first implementation-facing artifact is a disabled read-store scaffold. It exists to make the future read API explicit while preserving the current no-store boundary.

The scaffold defines:

- query shapes for evidence id, workspace id, claim id, action scan id, graph node, graph edge, latest readiness snapshot and safe next step snapshot;
- a scaffold status with durable read, filesystem read, database read, migration, write, runtime, provider/cloud and semantic/vector flags all disabled;
- a query result that returns `FailClosed` and does not fall back to any local store;
- unlock requirements inherited from the design-only persistence plan.

The scaffold does not:

- read evidence from a durable source;
- write evidence;
- create or use a database;
- run migrations;
- register an active product service;
- replace the deterministic fixture source used by the EIL UI.

Any future activation still requires a separate explicit hito, updated ADR, redaction-at-write hostile tests, schema compatibility report, migration dry-run audit and manual QA.
