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

## Addendum: Disabled Write Store Scaffold

Decision target: `GO_EIL_LOCAL_PERSISTENCE_WRITE_STORE_SCAFFOLD_DISABLED_READY`

The complementary implementation-facing artifact is a disabled write-store scaffold. It exists to make the future write API explicit while preserving the current no-write boundary.

The scaffold defines:

- command shapes for evidence records, claim scan snapshots, action scan snapshots, contradiction records, graph nodes, graph edges, readiness snapshots, safe next steps, human action requirements, redaction metadata and integrity hash envelopes;
- a scaffold status with durable write, filesystem write, database write, migration, runtime, provider/cloud, semantic/vector and service-registration flags all disabled;
- a redaction-at-write requirement stating that redaction is mandatory, raw payloads are never persisted, secret fields are rejected, unknown sensitivity fails closed and integrity hashes are computed only after canonical redaction;
- write results that return `FailClosed` for normal commands and `Rejected` for raw, secret, sensitive-never-persist or fixture-only data.

The scaffold does not:

- write evidence;
- read evidence from a durable source;
- create or use a database;
- run migrations;
- execute a redaction pipeline;
- register an active product service;
- replace the deterministic fixture source used by the EIL UI.

Any future write activation still requires a separate explicit hito, hostile redaction-at-write fixtures, schema compatibility report, migration dry-run audit and manual QA.

## Addendum: Redaction-At-Write Hostile Fixtures

Decision target: `GO_EIL_REDACTION_AT_WRITE_HOSTILE_FIXTURES_READY`

The disabled write-store scaffold is now covered by deterministic hostile fixtures. The fixtures exercise synthetic secret-like payloads, raw payload classes, unknown sensitivity, sensitive-never-persist fields, integrity envelopes before canonical redaction, graph payloads and safe-next-step payloads that accidentally embed secret-like content.

The fixture strategy proves:

- raw payload commands are rejected before any durable persistence boundary;
- synthetic secret-like payload commands are rejected fail-closed;
- unknown sensitivity fails closed;
- sensitive-never-persist and fixture-only payloads are rejected by design;
- integrity hash envelopes before canonical redaction are rejected;
- graph/readiness/safe-next-step write commands keep the same no-side-effect guarantees.

The fixture strategy does not:

- execute a redaction pipeline;
- write evidence;
- read evidence from a durable source;
- create or use a database;
- run migrations;
- register an active product service;
- change the EIL UI fixture source.

All hostile values are synthetic and intentionally marked as fixture/not-real. Any future write activation still requires additional adversarial redaction coverage, schema compatibility report, migration dry-run audit and manual QA.

## Addendum: Dry-Run Migration Plan Contract

Decision target: `GO_EIL_LOCAL_PERSISTENCE_DRY_RUN_MIGRATION_PLAN_READY`

The migration planning artifact is a design-only contract for future local persistence schema movement. It models migration plans, steps, blockers, schema descriptors and dry-run results without introducing a migration runner, durable store, filesystem access, database access or service registration.

The dry-run migration contract defines:

- schema version descriptors for current, unknown and future-unsupported schema states;
- migration step metadata for future schema checks, redaction gate checks, evidence shape checks, graph shape checks, readiness checks, integrity hash checks, rollback planning and human approval;
- blocker codes for unknown schema, unsupported future schema, filesystem read/write requirements, database requirements, migration runner requirements, durable-write requirements, missing redaction gate, raw payload risk, unavailable rollback, missing human approval, schema downgrade, incompatible graph shape, stale evidence and unsafe integrity hash plans;
- capability status with migration runner, migration execution, durable store, filesystem read/write, database, provider/cloud, semantic/vector, runtime and service registration all disabled;
- result flags that assert no execution attempt, no migration execution, no durable persistence, no filesystem touch, no database touch, no provider/cloud touch, no semantic/vector touch, no runtime touch and no product-write fallback.

The dry-run migration contract does not:

- execute migrations;
- create a migration runner;
- read or write filesystem data;
- create or use a database;
- enable durable persistence;
- register product services;
- change the EIL UI fixture source;
- enable runtime/live/browser/CDP/WCU/OCR/provider/cloud behavior.

Deterministic fixtures cover:

- no-op same schema version;
- unknown schema;
- unsupported future schema;
- schema downgrade;
- steps requiring filesystem read, filesystem write, database, migration runner or durable write;
- missing redaction-at-write gate;
- raw payload risk;
- rollback unavailable;
- human approval required;
- incompatible graph edge shape;
- stale evidence version;
- unsafe integrity hash plan.

Any future migration work still requires an explicit hito, schema compatibility guards, hostile redaction coverage, manual approval requirements, no-side-effect proof, and a separate audit before any runner or durable store can be considered.

## Addendum: Schema Compatibility Guards

Decision target: `GO_EIL_LOCAL_PERSISTENCE_SCHEMA_COMPATIBILITY_GUARDS_READY`

The schema compatibility guard is a design-only, fixture-safe contract that evaluates in-memory compatibility checks before any future durable persistence or migration runner can be considered. It does not parse files, read persisted data, create a database, execute migrations or register product services.

The guard defines:

- artifact kinds for evidence records, evidence sources, references, claim/action scans, graph nodes, graph edges, readiness snapshots, safe next steps, redaction metadata, integrity hash envelopes and migration plans;
- issue kinds for unknown schema, unsupported future schema, downgrade attempts, incompatible field shape, missing required fields, deprecated fields, unknown enum values, graph/evidence/claim/action/readiness incompatibilities, redaction metadata issues, integrity hash issues and migration target incompatibility;
- decisions for `CompatibleDesignOnly`, `Rejected` and `Blocked`;
- status flags proving durable persistence, filesystem read/write, database, migration runner, migration execution, provider/cloud, semantic/vector, runtime and service registration remain disabled.

Compatibility policy:

- `v1` known compatible artifacts may be considered compatible for design-only planning.
- Unknown schema versions block.
- Unsupported future schema versions block.
- Downgrade attempts block.
- Missing required fields block.
- Unknown enum values block.
- Deprecated fields block until a future migration policy exists.
- Graph, evidence, claim/action and readiness shape incompatibilities block.
- Missing redaction metadata or unknown sensitivity blocks.
- Missing integrity hash or pre-redaction hash blocks.
- Migration target incompatibility blocks.

No compatibility result may enable durable persistence. Any future store implementation still requires schema compatibility audit evidence, hostile redaction coverage, dry-run migration audit and a separate explicit hito.

## Addendum: Evidence Timeline Export Read-Only Preview

Decision target: `GO_EIL_EVIDENCE_TIMELINE_EXPORT_READ_ONLY_READY`

The Evidence Timeline Export is a read-only, fixture-safe preview model for sharing EIL state without creating files or enabling persistence. It converts the existing EIL read-only fixture surface plus disabled persistence guards into deterministic copy-ready text and structured sections.

The preview includes:

- executive summary;
- evidence index summary;
- timeline events;
- claims and evidence links;
- action scan results;
- contradictions and risks;
- typed evidence graph summary;
- readiness matrix;
- safe next step;
- human actions required;
- persistence capability status;
- read/write scaffold status;
- redaction-at-write hostile coverage;
- dry-run migration plan status;
- schema compatibility guard status;
- export blockers and warnings;
- no-side-effect proof;
- deferred capabilities and documented debt.

The preview explicitly excludes raw payloads, secret-like content, sensitive-never-persist fields, browser/CDP payloads, OCR raw payloads and provider/cloud payloads.

The preview does not:

- create a file;
- read filesystem data;
- write filesystem data;
- create PDF/DOCX/JSON/ZIP artifacts;
- call clipboard APIs;
- use a database;
- enable durable persistence;
- execute migrations;
- register a service;
- call provider/cloud/network;
- enable runtime/live/browser/CDP/WCU/OCR.

Any future physical export requires a separate explicit hito, manual QA, no-side-effect audit, redaction proof and a clear filesystem-write review before implementation.
