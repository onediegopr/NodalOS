# Durable Audit Trail Pre-Enablement Control Plane Design-Only

## Status

Accepted as design-only. Enablement is not authorized.

## Baseline

- Baseline commit: `1d3a68bfd4e86d405634bbd87a1725a670e13d17`
- Branch: `chrome-lab-001-extension-local-ai-bridge`
- Existing capability: `DURABLE_AUDIT_TRAIL_APPEND_ONLY_MINIMAL`
- Current state: implemented-not-enabled local/test-safe append/write candidate.

This ADR extends the enablement gate into a pre-enablement control plane. It does not enable runtime, product behavior, service registration, command handlers, UI product actions, product ledger paths, DB/migration, provider/cloud/network, browser/CDP, WCU/OCR, recipes live writes, release readiness, or commercial readiness.

## Scope Lock

Implemented-not-enabled means the isolated candidate can append local/test-safe JSONL events under controlled test/dev boundaries, but it is not wired into product runtime, command routing, UI actions, service registration, storage infrastructure, or release flows.

Local/test-safe means:

- storage root remains constrained to approved local/test boundaries;
- writes are only local JSONL ledger writes for the isolated candidate;
- no product user data path is authorized;
- no DB, cloud, provider, network, browser/CDP, WCU/OCR, or recipes live path is authorized;
- every future stage remains blocked unless its gate evidence is complete.

Canonical commits:

- historical no-write/no-persistence preview baseline: `e65517c4e6ccf7a0c3f8538b06202072ea6ce39c`;
- first local/test-safe append/write candidate: `d80ea683c58a4a69c0ecca2545558b0a16971fad`;
- hardening baseline: `5f6e6b3feb5d3f052a0d3033f06f2fca6b6fb240`;
- head checkpoint design baseline: `2c6b6f59cdc45217f3b426c7d2f539e45d23c922`;
- enablement gate baseline: `1d3a68bfd4e86d405634bbd87a1725a670e13d17`.

## Redaction-Before-Persistence Design Gate

Before any stage beyond local/test-safe candidate use, an explicit redaction-before-persistence gate must exist.

Conceptual contract:

- classify event fields before append;
- reject raw payloads;
- reject secret-like content;
- redact or omit sensitive values before ledger entry construction;
- prove redaction ran before `File.AppendAllText`;
- fail closed if classification, redaction, or policy lookup is missing;
- record only safe summaries, references, hashes, reason codes, and non-secret metadata.

Data that cannot persist:

- credentials, tokens, API keys, cookies, session material, auth headers;
- raw approval payloads;
- raw user files or file contents;
- raw browser/CDP, WCU/OCR, recipe, provider, or network payloads;
- sensitive absolute paths;
- unreviewed PII;
- redaction inputs or redaction failure payloads.

Required evidence:

- redaction policy reference;
- field classification table;
- negative tests for each forbidden data class;
- proof that rejected inputs do not append;
- scan results showing no raw sensitive payloads in fixtures or docs;
- external audit before any product-scope persistence.

## Runtime Feature Flag Fail-Closed Design

Future feature flags must be modeled before implementation.

Required model:

- default off;
- missing flag means off;
- malformed flag means off;
- unknown environment means off;
- product flag remains forbidden in this design;
- test-only flag is scoped to controlled fixtures;
- dev-only sandbox flag is scoped to local non-product paths;
- every flag flip requires explicit human GO and audit evidence;
- no flag may bind service registration, command handlers, UI product actions, or command bus routing in this design.

Prohibited:

- default-on behavior;
- implicit enablement from build configuration;
- product runtime registration by flag alone;
- command handler binding by flag alone;
- UI action exposure by flag alone;
- release/commercial readiness claims.

## Append-Only / Property / Concurrency Test Plan

Future tests required before enablement:

- append-only invariant property tests;
- no overwrite tests;
- no delete tests;
- monotonic sequence tests;
- malformed ledger tests;
- invalid shape tests;
- tamper tests;
- replay and rollback tests;
- truncation and checkpoint mismatch tests;
- concurrent append stress tests;
- local lock behavior tests;
- path boundary tests;
- temp-only proof tests;
- secret-like rejection corpus tests;
- redaction-before-persistence tests;
- no service registration scan tests;
- no command handler scan tests;
- no product action scan tests;
- docs overclaim scan tests.

## Replay / Read Model / Checkpoint / Truncation Evidence Plan

The read model is design-only until a later scoped block.

Conceptual rules:

- read model must be read-only;
- replay validation must not mutate domain state;
- checkpoint comparison must distinguish internal hash-chain validity from external head evidence;
- truncation evidence requires checkpoint or head evidence outside the ledger chain;
- current checkpoint design is local/test-safe design-only;
- local checkpoints do not protect against replacing ledger and checkpoint together inside the same trust boundary;
- no runtime verifier, checkpoint writer, checkpoint reader service, DB, KMS, WORM, or cloud boundary is authorized.

Future evidence expectations:

- ledger internal verification result;
- expected head checkpoint result;
- checkpoint chain result when available;
- truncation suspicion reason;
- rollback suspicion reason;
- safe summary only;
- no raw payloads.

## Failure / Rollback / Non-Rollback Policy

Failures that must block append:

- missing or disabled policy;
- storage root outside local/test boundary;
- raw payload supplied;
- secret-like content;
- malformed existing ledger;
- invalid existing entry shape;
- hash mismatch;
- sequence gap, duplicate, or reorder;
- missing redaction-before-persistence proof in any future product stage;
- missing feature flag gate in any future runtime stage.

Failures that mark ledger invalid:

- parse failure;
- semantic shape failure;
- sequence continuity failure;
- hash-chain failure;
- checkpoint mismatch once checkpoints exist.

Non-rollback policy:

- audit records are append-only evidence;
- action rollback must be represented as a new audit event, not deletion or mutation of prior evidence;
- failed or reverted actions must preserve the original decision trail;
- pause protocol triggers on any P0/P1/P2, overclaim, dirty worktree, scope drift, path leak, or product enablement leak.

## External Audit Pack Preparation

Future external audit pack must include:

- baseline and commit graph;
- scope lock;
- gate matrix G0-G20;
- pre-enablement control plane sections;
- staged enablement plan;
- blockers and risk register;
- required future tests;
- required future docs;
- anti-capabilities;
- release/commercial NO-GO proof;
- static scans;
- git clean/origin sync proof.

## Anti-Capabilities

This control plane does not authorize:

- product audit trail enablement;
- runtime enablement;
- service registration;
- command handlers;
- command bus wiring;
- UI product actions;
- product ledger paths;
- DB-backed audit trail;
- cloud/network persistence;
- provider or LLM calls;
- browser/CDP, WCU/OCR, or recipes live writes;
- release/commercial readiness;
- production, WORM, compliance-grade, or product-ready claims.

## Percentages

- Durable audit trail local/test-safe append/write candidate: 90-95%.
- Enablement gate planning/docs: 90-95%.
- Pre-enablement control plane design: 75-85%.
- Product enablement: 0%.
- Runtime/live: 0%.
- Execution/mutation broad: 0%.
- Release/commercial readiness: 0% / NO-GO.
- Project usable end-to-end estimate: 20-30%.

## Decision

The pre-enablement control plane is documented for planning and audit only. It closes design/specification gaps, but does not authorize any runtime/product enablement.
