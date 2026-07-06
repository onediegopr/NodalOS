# Product Ledger Durable Latest State Authority / Read Precedence / Public Product Decision Matrix Design-Only

Date: 2026-07-06

Decision: `GO_WITH_FINDINGS_DURABLE_LATEST_STATE_AUTHORITY_READ_PRECEDENCE_PUBLIC_PRODUCT_DECISION_MATRIX_DESIGN_ONLY_READY`

Baseline HEAD: `3923a87dedd64426d5511eca5953755d858eea15`

## Scope

This is a design-only/readiness-only/audit-only/test-only/guard-only decision matrix after:

`LocalOperatorSurfaceLatestStateSnapshotCreateOnly -> LocalOperatorSurfaceLatestStateManifestCreateOnly -> LocalDurableLatestStateReaderCandidateNotAuthority`

No durable authority, live/product authority, active read precedence, latest pointer, latest pointer overwrite, public/product exposure, Production route, broader workspace action, edit/update/delete, user-selected path, shell/subprocess, command execution, Browser/CDP/WCU/OCR/Recipes live, Pilot `/run`, provider/cloud/network, DB/migration, KMS/WORM/external trust, compliance custody or release/commercial capability is implemented by this block.

## Backward Reality Check

Confirmed current state:

- snapshots are historical create-only/versioned JSON evidence under `docs/test-output/product-ledger/operator-surface-latest-state-snapshots/`;
- manifests are create-only/versioned JSON indexes under `docs/test-output/product-ledger/operator-surface-latest-state-manifests/`;
- `LocalDurableLatestStateReaderCandidateNotAuthority` exists;
- the reader candidate is local-only/internal-only/Development-only/read-only;
- the reader candidate is candidate evidence only;
- no latest pointer exists;
- no read precedence exists;
- no live/product authority exists;
- no public/product or Production route exists.

Explicitly not present:

- durable latest-state authority;
- active read precedence;
- latest pointer or overwrite;
- product authority;
- public/product route;
- Production route;
- broader workspace action;
- edit/update/delete;
- shell/subprocess or command execution;
- provider/cloud/network/DB;
- KMS/WORM/compliance custody;
- release/commercial readiness.

## Decision Matrix

| Option | User value | Technical delta | Files/routes/stores touched | Read authority level | Write scope | Stale-state risk | Tamper/corruption risk | Privacy risk | Overclaim risk | Rollback/invalidation need | Idempotency need | Redaction need | Evidence need | Auditability | Test burden | Blockers | Why now / why not now | Recommendation |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| A. Auxiliary evidence integration | Makes validated durable evidence easier to inspect without changing authority. | Add richer candidate evidence labels/read-model panel only in a future window. | Operator surface/read-model only; no new writer/store. | Candidate evidence only. | None. | Medium, visible and non-authoritative. | Low if existing validator remains source. | Low with current redaction. | Low. | Minimal; stale warning/fallback only. | Minimal. | Existing redaction metadata only. | Candidate validation result, manifest/snapshot refs. | High. | Moderate. | Must not become precedence. | Best next step: improves operator value while keeping current safety boundary. | Recommended. |
| B. Development-only read precedence candidate | Lets operator surface prefer validated durable state in Development. | Introduces precedence/fallback rules. | Read-model/route/rendering; maybe no writer. | Precedence candidate, not product authority. | None. | High unless fallback is explicit. | Medium: precedence magnifies stale/tamper bugs. | Medium. | Medium-high. | Required. | Required for state selection. | Required. | Stronger freshness and invalidation evidence. | Medium. | High. | Active precedence is explicitly not authorized now. | Useful later, but too close to authority semantics for this design-only step. | Not now. |
| C. Versioned/latest manifest-index pointer | Gives stable "current" lookup. | Adds pointer/index selection semantics. | New pointer/index file or route. | Pointer evidence, not authority if carefully constrained. | New local write. | High. | High; pointer tamper becomes selection tamper. | Medium. | High. | Required. | Required. | Required. | Pointer provenance and rollback evidence. | Medium. | High. | Latest pointer is explicitly not authorized. | Not now; pointer is a larger semantic jump than auxiliary evidence. | Blocked. |
| D. Durable local authority Development-only | Makes durable state authoritative for local Development. | Promotes candidate to authority source. | Read model, routes, tests and docs. | Durable local authority. | Possibly none, but authority semantics change. | High. | High. | Medium. | Very high. | Required. | Required. | Required. | Strong freshness, rollback and manual-go evidence. | Medium. | Very high. | Durable authority/live-product authority not authorized. | Too early; authority requires separate proof pack. | Blocked. |
| E. Public/product exposure | Operator value outside internal dev. | Public route/product surface, auth/UX/release risks. | Public UI/route/service boundary. | Public/product operator surface. | Possibly none initially. | High. | High. | High. | Very high. | Required. | Required. | Required. | Auth, privacy, release and abuse evidence. | High only after auth/release prep. | Very high. | Public/product and release/commercial not authorized. | Not now; current chain is internal evidence only. | Blocked. |
| F. More local/internal hardening | Reduces risk before any semantic jump. | Additional tests/static scans/docs. | Tests/docs only. | None. | None. | Low. | Low. | Low. | Low. | Low. | Low. | Existing. | Existing plus corpus. | High. | Low-moderate. | Diminishing returns after current guard coverage. | Valuable as follow-up, but A adds clearer user value with similar safety. | Secondary. |

## Recommendation

Recommend option A:

`LocalDurableLatestStateAuxiliaryEvidenceNotPrecedenceNotAuthority`

This keeps the reader candidate as evidence only while improving how the operator surface and read-model explain the validated manifest/snapshot chain.

Why not public/product now:

- no auth/release/commercial boundary is ready;
- the current evidence chain is Development-only;
- public/product exposure would invite authority and user-facing interpretation claims.

Why not durable authority now:

- stale candidate evidence is expected;
- authority needs invalidation, rollback, freshness and precedence proofs that are not yet implemented;
- current validator was designed as candidate evidence only.

Why not latest pointer now:

- pointer semantics create selection authority;
- pointer tamper/rollback risk is larger than auxiliary evidence display;
- latest pointer remains explicitly unauthorized.

Why not read precedence now:

- even Development-only precedence changes what the surface trusts first;
- fallback/invalidation semantics are not yet designed deeply enough;
- option A reduces operator ambiguity without changing trust order.

## Future Boundary Spec: Recommended Option A

Future capability name:

`LocalDurableLatestStateAuxiliaryEvidenceNotPrecedenceNotAuthority`

Future read kind:

`LocalDurableLatestStateAuxiliaryEvidence`

Future reader/view name:

`ProductLedgerLocalDurableLatestStateAuxiliaryEvidencePresenter`

Future route:

No new route required unless a separate explicit GO authorizes one. Prefer enriching the existing Development-only operator surface route.

Input boundaries:

- snapshots: `docs/test-output/product-ledger/operator-surface-latest-state-snapshots/`;
- manifests: `docs/test-output/product-ledger/operator-surface-latest-state-manifests/`.

Authority classification:

`LOCAL_INTERNAL_DEV_ONLY_AUXILIARY_EVIDENCE_NOT_PRECEDENCE_NOT_AUTHORITY`

Forbidden authority claims:

- product authority;
- public authority;
- live authority;
- release evidence;
- compliance custody;
- WORM/KMS;
- cloud-backed durability;
- business signoff.

Read precedence rules:

- no read precedence;
- no first-source selection;
- no replacing fixture-safe/live read model;
- auxiliary evidence may be displayed next to current read-model status only.

Latest pointer rules:

- no latest pointer;
- no pointer overwrite;
- no inferred latest;
- no directory scan to choose latest;
- no mutable pointer file.

Stale handling:

- stale is allowed only as visible evidence state;
- stale evidence cannot become authority;
- stale evidence must preserve source manifest/snapshot refs.

Tamper/corruption handling:

- tampered/corrupt manifest or snapshot blocks auxiliary evidence;
- blocked state must remain visible and non-authoritative;
- no fallback to authority.

Fallback rules:

- fallback is display-only pending/blocked state;
- current read model remains unchanged.

Invalidation rules:

- invalidation is a label, not a delete/update;
- no edit/update/delete or cleanup route.

Redaction rules:

- safe metadata only;
- no raw secrets, tokens, env values, provider URLs or absolute sensitive paths.

Evidence refs required:

- source manifest id/path/hash/checkpoint;
- source snapshot ids/paths/hashes/checkpoints;
- validator classification;
- stale/tamper/corruption state;
- negative flags.

DOM/read-model expectations:

- show auxiliary evidence state;
- show `not authority`;
- show `no read precedence`;
- show `no latest pointer`;
- show stale/tamper/corruption labels;
- show evidence refs and safe relative paths only.

Production/public blocking:

- Production remains 404 for internal route surfaces;
- no public/product exposure;
- no public UI action.

Static scan rules:

- fail on product authority, public/product, Production route, latest pointer, pointer overwrite, durable authority, active read precedence, shell/subprocess, command execution, provider/cloud/network, DB/migration, KMS/WORM/compliance custody and release/commercial claims outside docs/tests design-only wording.

## Future Test Plan

Positive:

- valid reader candidate can be displayed only as auxiliary evidence;
- stale classification visible;
- tamper-free hash validated;
- redacted safe metadata only;
- evidence refs preserved;
- operator surface shows auxiliary evidence safely;
- fallback behavior explicit and safe.

Negative:

- tampered candidate blocks;
- corrupted manifest blocks;
- missing hash blocks;
- missing evidence refs blocks;
- stale candidate cannot be authority;
- unredacted secret blocks;
- raw absolute path blocks;
- latest pointer absent;
- authority overclaim blocks;
- command/shell/network fields block;
- Production route remains 404;
- public/product absent.

Security/static:

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

`AUTHORIZE_NODAL_OS_DURABLE_LATEST_STATE_AUXILIARY_EVIDENCE_NOT_PRECEDENCE_NOT_AUTHORITY_IMPLEMENTATION_WINDOW`

Any move to read precedence, durable authority, latest pointer, public/product exposure or Production route requires a different explicit GO.
