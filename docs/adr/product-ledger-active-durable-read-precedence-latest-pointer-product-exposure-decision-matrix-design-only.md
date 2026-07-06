# Product Ledger Active Durable Read Precedence / Latest Pointer / Product Exposure Decision Matrix Design-Only

Date: 2026-07-06

Decision: `GO_WITH_FINDINGS_ACTIVE_DURABLE_READ_PRECEDENCE_LATEST_POINTER_PRODUCT_EXPOSURE_DECISION_MATRIX_DESIGN_ONLY_READY`

Baseline HEAD: `5a185ae69a53954fd7e9fc6e2bd115ca724fe6a2`

## Scope

This window is design-only, readiness-only, audit-only, test-only and guard-only. It does not implement active durable read precedence, latest pointer behavior, product read-model authority, durable authority, public/product exposure, Production routes, broader workspace actions, shell/subprocess, command execution, provider/cloud/network, DB/migration, KMS/WORM/external trust, compliance custody or release/commercial readiness.

## Backward Reality Check

The implemented durable latest-state chain is:

1. `LocalOperatorSurfaceLatestStateSnapshotCreateOnly`: historical local/internal/Development-only snapshot evidence.
2. `LocalOperatorSurfaceLatestStateManifestCreateOnly`: immutable versioned create-only manifest evidence, no latest pointer and no read precedence.
3. `LocalDurableLatestStateReaderCandidateNotAuthority`: validated reader candidate over fixed manifest/snapshot evidence, no authority and no precedence.
4. `LocalDurableLatestStateAuxiliaryEvidenceNotPrecedenceNotAuthority`: auxiliary evidence over the reader candidate, no authority, no read precedence and no latest pointer.

Confirmed absent:

- active durable read precedence;
- latest pointer or latest pointer overwrite;
- durable authority;
- product read-model authority;
- live/product authority;
- public/product route;
- Production route;
- broader workspace action;
- edit/update/delete;
- user-selected path;
- shell/subprocess or command execution;
- cloud/provider/network/DB;
- KMS/WORM/compliance custody;
- release/commercial readiness.

## Decision Matrix

| option | user value | technical delta | files/routes/stores touched | read authority level | write scope | stale-state risk | tamper/corruption risk | privacy risk | overclaim risk | rollback/invalidation need | idempotency need | redaction need | evidence need | auditability | test burden | blockers | why now / why not now | recommendation |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| A. Active durable read precedence Development-only, not product authority | Lets the local operator surface prefer validated durable evidence over in-memory/pending state while staying internal | Add an explicit read-ordering candidate/gate over auxiliary evidence and reader candidate | Future Core presenter/gate, local Development GET/state surface only; no writer | Candidate precedence only, not durable/product authority | None; read-only | Medium: stale evidence can be ordered first if classification is weak | Medium: must revalidate hashes/checkpoints/evidence refs before ordering | Low-medium: only redacted safe metadata may surface | Medium: "precedence" can be mistaken for authority | Required: fallback to in-memory/pending state when stale/tampered/corrupt | Required for deterministic read ordering | Required before any surfaced evidence metadata | Required: manifest id, snapshot hash, checkpoint, candidate hash, auxiliary evidence refs | High if every decision emits blockers/reason/evidence refs | Medium-high | Product authority, latest pointer and public/product must stay blocked | Best next frontier because it improves local usefulness without pointer or product authority if named as candidate precedence only | Recommended |
| B. Latest pointer versioned/no-overwrite, not product authority | Gives a stable "latest" lookup without scanning manifests | Add pointer/index writer and reader | Future create-only pointer store, pointer route/state, invalidation metadata | Higher than A because pointer can become de facto authority | Bounded local write | High: pointer can stale or lag latest manifest | High: pointer tamper can redirect reads | Medium | High: "latest" implies authority | Strong invalidation and rollback rules required | Required for repeated pointer creation | Required | Required: pointer target hash, manifest hash, checkpoint, predecessor refs | Medium if pointer chain is explicit | High | Needs separate pointer safety design; Diego preference says not yet | Valuable later but too easy to overclaim now | Not now |
| C. Product read-model authority local/internal only | Clarifies one local read model as authority | Promote durable chain to local product authority | Core read model, route/state, operator surface labels | Local product authority | Potential reads only, but semantic authority changes | High | High | Medium-high | Very high | Required | Required | Required | Required | High if all provenance is explicit | High | Requires authority policy, invalidation, rollback and user-facing semantics | Too large a semantic jump from auxiliary evidence | Not now |
| D. Public/product exposure of operator surface | Makes state visible to real product users | Public route/UI, auth/session/release gates | Public product route, UI surface, policy gates | Public/product read surface | Possibly none at first | High | High | High | Very high | Required | Required | Required | Required | Medium-high | Very high | Public auth, privacy, release, product support and messaging absent | User explicitly prefers no public/product yet | Blocked |
| E. More local/internal hardening before crossing frontier | Reduces risk without new semantics | Add property/static guard coverage and docs | Tests/docs only | No change | None | Low | Low | Low | Low | Low | Low | Low | Existing evidence reused | High | Low-medium | Does not improve operator workflow | Useful if scans reveal drift; current chain is ready for a controlled A design/implementation GO | Secondary |
| F. Repo-detected safer option | Keep auxiliary evidence only and add no new read-order behavior | No implementation; audit-only | Docs/tests only | No change | None | Low | Low | Low | Low | None | None | None | Existing evidence reused | High | Low | Delays real frontier | No repo drift found requiring this over A | Not selected |

## Recommendation

Recommend option A as the next frontier, but only under a separate implementation authorization:

`LocalDurableLatestStateReadPrecedenceCandidateNotProductAuthority`

Recommended classification:

`LOCAL_INTERNAL_DEV_ONLY_ACTIVE_READ_PRECEDENCE_CANDIDATE_NOT_PRODUCT_AUTHORITY`

Why A now:

- It is the smallest useful semantic step after auxiliary evidence.
- It can improve Development-only operator read ordering without a latest pointer.
- It does not require product authority, public/product exposure or Production mapping.
- It forces stale/tamper/corruption/fallback semantics before any pointer or product authority work.

Why not public/product:

- Public/product would require auth, privacy copy, release support, product commitments and user-facing authority semantics that are not in scope.

Why not product authority:

- The current chain is evidence and candidate state. It is not a policy-authoritative read model, and stale/tamper/fallback semantics are not yet promoted to product authority rules.

Why not latest pointer:

- A pointer can become de facto authority even if labeled otherwise. It needs pointer-specific invalidation, rollback, no-overwrite and tamper tests before implementation.

## Future Boundary Spec For Option A

- Future capability name: `LocalDurableLatestStateReadPrecedenceCandidateNotProductAuthority`.
- Future read kind: `LocalDurableLatestStateReadPrecedenceCandidate`.
- Future action kind: read-only/evaluate-only; no write action.
- Future reader/gate name: `ProductLedgerLocalDurableLatestStateReadPrecedenceCandidateGate`.
- Future route name if authorized: `/internal/product-ledger/operator-surface/durable-latest-state-read-precedence-candidate`.
- Input boundary: only validated snapshot, create-only manifest, reader candidate and auxiliary evidence results.
- Authority classification: `LOCAL_INTERNAL_DEV_ONLY_ACTIVE_READ_PRECEDENCE_CANDIDATE_NOT_PRODUCT_AUTHORITY`.
- Forbidden authority claims: product authority, public authority, durable authority, live/product authority, release evidence, compliance custody, WORM/KMS, cloud-backed durability and business signoff.
- Read precedence rules: may rank Development-only safe sources for local/internal display only; must never claim product read-model authority.
- Latest pointer rules: must not create, read, overwrite, update or infer a latest pointer.
- Stale handling: stale sources may be visible but cannot become product authority; stale state must be explicit in result/DOM.
- Tamper/corruption handling: hash, checkpoint, manifest id and evidence refs must validate before a precedence candidate is accepted.
- Fallback rules: missing, stale-blocked, tampered, corrupted or unsafe evidence falls back to pending/in-memory/operator-surface state with explicit blockers.
- Invalidation rules: if source snapshot/manifest/candidate hash changes, precedence candidate must be invalidated and recomputed.
- Redaction rules: only redacted safe metadata may appear in reasons, evidence refs, JSON or DOM.
- Evidence refs required: snapshot id/hash/checkpoint, manifest id/hash/checkpoint, candidate hash, auxiliary evidence id/hash and stale classification.
- DOM/read-model expectations: display `candidate precedence only`, `not product authority`, `no latest pointer`, `Development-only`, stale/tamper/corruption labels and fallback reason.
- Production blocking: route remains unmapped outside Development.
- Public/product blocking: no public route, no public UI action and no product command handler.
- Static scan rules: no pointer filename, no `latest.json`, no public/product route, no `Authority: true`, no `ProductAuthority: true`, no `ReadPrecedence: true` outside the authorized future class/tests, no shell/subprocess/network/DB/KMS/WORM/release/commercial claims.

## Future Test Plan

Positive tests:

- auxiliary evidence/candidate can influence Development-only read ordering inside the authorized boundary;
- stale classification remains visible;
- tamper-free hash/checkpoint is validated;
- redacted safe metadata only;
- evidence refs are preserved;
- fallback behavior is explicit and safe;
- no product authority claim;
- no latest pointer unless separately authorized.

Negative tests:

- tampered candidate blocks;
- corrupted manifest blocks;
- missing hash blocks;
- missing evidence refs block;
- stale candidate cannot be product authority;
- unredacted secret blocks;
- raw absolute path blocks;
- latest pointer absent;
- authority overclaim blocks;
- command/shell/network fields block;
- Production route 404;
- public/product route absent.

Security/static tests:

- no product authority claim;
- no public/product claim;
- no WORM/KMS claim;
- no compliance custody claim;
- no cloud-backed claim;
- no release/commercial claim;
- no command execution;
- no shell/subprocess;
- no cloud/network/DB;
- no Browser/CDP/OCR/WCU/Recipes live;
- no Pilot `/run`.

## Exact Next GO Required

`AUTHORIZE_NODAL_OS_ACTIVE_DURABLE_READ_PRECEDENCE_CANDIDATE_NOT_PRODUCT_AUTHORITY_DEVELOPMENT_ONLY_IMPLEMENTATION_WINDOW`

Until that GO exists, active durable read precedence, latest pointer behavior, product read-model authority and public/product exposure remain blocked.
