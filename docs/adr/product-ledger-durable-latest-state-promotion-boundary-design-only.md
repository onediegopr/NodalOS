# Product Ledger Durable Latest-State Promotion Boundary Design-Only

Date: 2026-07-06

Decision: `GO_WITH_FINDINGS_DURABLE_LATEST_STATE_PROMOTION_BOUNDARY_DESIGN_ONLY_READY`

## Scope

This window is design-only, readiness-only, audit-only, test-only and guard-only.

It does not implement:

- durable latest-state promotion;
- live/product latest-state authority;
- active durable latest-state reader;
- read precedence changes;
- latest pointer overwrite;
- public/product exposure;
- Production route;
- broader workspace action;
- edit/update/delete;
- user-selected path;
- shell/subprocess or command execution;
- Browser/CDP/WCU/OCR/Recipes live authority;
- Pilot `/run`;
- provider/cloud/network, DB/migration or KMS/WORM/external trust;
- release/commercial readiness or compliance custody.

## Backward Reality Check

Current implemented Product Ledger chain:

`approval persisted -> approved no-op execution -> bounded internal marker -> LocalApprovedHandoffReportDraft -> LocalWorkspaceTestJailHandoffDraftCreateOnly -> LocalUserWorkspaceAllowlistedHandoffDraftCreateOnly -> LocalOperatorSurfaceLatestStateSnapshotCreateOnly -> immutable/versioned json snapshots under docs/test-output/product-ledger/operator-surface-latest-state-snapshots/ -> property/corpus/static guard hardening`

Confirmed implemented:

- `LocalOperatorSurfaceLatestStateSnapshotCreateOnly`.
- `.json` snapshots with immutable/versioned create-only filenames.
- Output only under `docs/test-output/product-ledger/operator-surface-latest-state-snapshots/`.
- No overwrite.
- No latest pointer overwrite.
- Redaction-before-persistence.
- Safe metadata only.
- Content hash and checkpoint hash.
- Source chain evidence refs and hashes.
- Stale snapshots classified as historical evidence only.
- Property/corpus/static guard hardening for unsafe ids, missing fields and unsafe option capabilities.

Confirmed still absent:

- durable latest-state promotion;
- latest-state authority;
- active durable reader;
- latest pointer or mutable latest alias;
- public/product path;
- Production route;
- broader workspace action;
- edit/update/delete;
- shell/subprocess;
- command execution;
- cloud/provider/network/DB;
- KMS/WORM/compliance custody;
- release/commercial readiness.

## Promotion Options Matrix

| Option | User value | Technical delta | Files/routes/stores touched | Read authority level | Write scope | Stale-state risk | Tamper/corruption risk | Privacy risk | Overclaim risk | Rollback/invalidation need | Idempotency need | Redaction need | Evidence need | Auditability | Test burden | Blockers | Why now / why not now | Recommendation |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| A. Keep snapshots as historical evidence only | Preserves current safety and avoids new authority. | None beyond docs/tests. | Docs/tests only. | Historical evidence only. | Existing snapshot boundary only. | Medium because operator state remains in-process for current truth. | Medium unless every consumer validates hashes manually. | Low after existing redaction. | Low. | Low. | Existing idempotency only. | Existing redaction only. | Existing evidence refs. | Medium. | Low. | Does not reduce stale/latest-state fragility. | Safe now, but does not answer durable read candidate need. | Not enough. |
| B. Promote latest snapshot to local durable read-model candidate, but not authority | Gives a local durable candidate without product authority. | Future reader/selector over existing snapshots, stale/tamper labels. | Future Core candidate reader, possibly Development-only state route tests/docs. | Candidate only, not live/product authority. | No new write beyond existing snapshots if reader only. | Medium-high if selection is implicit by timestamp or filesystem scan. | Medium-high unless every candidate is hash/reparse validated. | Low-medium. | Medium because "latest" wording invites authority claims. | Medium; stale and invalidation labels required. | N/A for reader; exact selection rules required. | Required when presenting candidate summaries. | Required for selected snapshot and predecessor chain. | Medium-high. | Medium. | Risk of implicit latest pointer semantics. | Useful, but selection must be auditable first. | Defer until manifest/index design is implemented or explicitly proven. |
| C. Promote latest snapshot to local durable read source for Development-only operator surface | Higher operator value: surface survives process restarts. | Future read precedence change in local operator surface. | Core/Pilot/read-model/Recipes/Safety/docs. | Development-only durable read source, still not product authority. | Reader only if using existing snapshots. | High because it can silently outrank fresher in-process state. | Medium-high. | Medium. | High because read precedence feels like authority. | High; invalidation/fallback must be precise. | N/A for reader; candidate choice deterministic. | Required for any displayed summaries. | Required. | High. | High. | Needs prior candidate/manifest proof and more UX semantics. | Not now; too close to authority. | Not recommended now. |
| D. Create explicit latest-state manifest/index create-only, no overwrite, versioned, but not product authority | Best next value: durable candidate selection becomes explicit and auditable without a mutable latest pointer. | Future manifest writer/validator; optional read-only candidate state that does not change precedence. | Future Core manifest candidate writer/validator, Safety tests, focused Recipes if a Development-only route is later authorized, docs. | `LOCAL_INTERNAL_DEV_ONLY_VERSIONED_MANIFEST_NOT_AUTHORITY`. | New fixed manifest boundary under `docs/test-output/product-ledger/operator-surface-latest-state-manifests/`. | Medium, but staleness is explicit in manifest. | Medium, reduced by manifest hash, selected snapshot hash and predecessor hashes. | Low-medium; manifest must persist redacted safe metadata only. | Low-medium if classification is visible everywhere. | Medium; create a newer manifest to invalidate, never overwrite. | Required by manifest content hash and idempotent replay. | Required before manifest persistence. | Required for selected snapshot, predecessor hashes and invalidation reason. | High. | Medium-high but bounded. | Requires future implementation GO; no active reader/product route in this window. | Good now because it converts implicit selection into immutable evidence. | Recommended. |
| E. Two-phase D then B: manifest candidate first, reader later | Highest safety sequencing. | Implement D first; only later allow a reader to consume valid manifests as candidate state. | Same as D first, then future reader. | Manifest not authority first; candidate reader later. | Manifest boundary first. | Lower than B/C because read behavior is delayed. | Lower than B/C because manifest validation is proven first. | Low-medium. | Low. | Medium. | Required. | Required. | Required. | High. | Medium-high across two windows. | Slower. | Best sequencing if Diego wants durable promotion without public/product authority. | Adopt as sequencing around option D. |

## Recommendation

Recommend option D, sequenced like option E:

`LocalDurableLatestStateManifestCreateOnly`

The next implementation should create an immutable, versioned manifest/index that names one selected historical snapshot as a durable latest-state candidate. It must not create a mutable latest pointer, must not change read precedence and must not claim live/product authority.

Why not promote snapshots directly to authority:

- The current snapshots are historical evidence only and can become stale.
- A timestamp-based "latest file wins" rule would be a hidden latest pointer.
- A durable reader that outranks in-process state would turn evidence into operational authority too early.
- Public/product exposure still lacks auth, UX, release/commercial and external audit gates.

Why a manifest/index now:

- It makes candidate selection explicit, hash-checked and reviewable.
- It avoids directory scanning as authority.
- It provides invalidation evidence without overwrite.
- It creates a future bridge to a candidate reader while preserving "not authority" labels.

Risks introduced:

- A new local test-output write boundary exists in the future implementation.
- Operators may overread "latest" as live authority unless classification is explicit.
- Manifest freshness and invalidation require strong tests.

Risks reduced:

- No implicit latest-by-timestamp selection.
- No mutable latest pointer.
- No hidden read precedence change.
- Better tamper/corruption detection before any reader is introduced.

Still blocked:

- live authority;
- product authority;
- active durable reader with precedence;
- public/product exposure;
- Production route;
- broader workspace action;
- edit/update/delete;
- shell/subprocess or command execution;
- provider/cloud/network, DB/migration, KMS/WORM/external trust;
- release/commercial readiness.

## Future Boundary Spec

Future capability name:

`DurableLatestStatePromotionManifestBoundary`

Future action kind:

`LocalDurableLatestStateManifestCreateOnly`

Future read kind, if later authorized:

`LocalDurableLatestStateReadCandidateNotAuthority`

Future writer:

`ProductLedgerLocalDurableLatestStateManifestWriter`

Future validator:

`ProductLedgerLocalDurableLatestStateManifestValidator`

Future reader, not authorized for the first implementation:

`ProductLedgerLocalDurableLatestStateCandidateReader`

Future route, only if a later implementation window explicitly authorizes a Development-only route:

`POST /internal/product-ledger/operator-surface/create-durable-latest-state-manifest`

Future state route, only if later authorized and still candidate-only:

`GET /internal/product-ledger/operator-surface/durable-latest-state-candidate-state`

Input snapshot boundary:

`docs/test-output/product-ledger/operator-surface-latest-state-snapshots/`

Output manifest boundary:

`docs/test-output/product-ledger/operator-surface-latest-state-manifests/`

Authority classification:

`LOCAL_INTERNAL_DEV_ONLY_VERSIONED_MANIFEST_NOT_AUTHORITY`

Forbidden authority claims:

- live authority;
- product authority;
- compliance custody;
- WORM/KMS;
- release evidence;
- business signoff;
- cloud-backed durability;
- production source of truth.

## Manifest Rules

- `.json` only.
- Fixed output root.
- No payload-controlled root, path or filename.
- Create-only versioned filename.
- No overwrite.
- No mutable `latest.json`.
- No symlink, junction or reparse escape.
- No directory scan as authority; a selected snapshot path must be supplied by a trusted internal predecessor state or explicit test fixture only.
- The selected snapshot path must be a safe relative path under the input snapshot boundary.
- The selected snapshot must parse as canonical snapshot JSON.
- The selected snapshot classification must be historical evidence only.
- The selected snapshot must carry `historicalEvidenceOnly: true`.
- The selected snapshot must carry `authorityLiveProduct: false`.
- The selected snapshot must carry `publicProduct: false`.
- The selected snapshot must carry `production: false`.
- The selected snapshot content hash, checkpoint hash and operator surface model hash must be validated.
- Manifest content hash must be computed over redacted canonical JSON.
- Exact idempotent replay may return the same immutable manifest only when full canonical content matches.
- Existing different content blocks as conflict.

## Stale, Tamper And Corruption Handling

Stale handling:

- A manifest can select a candidate snapshot, but cannot declare it current/live.
- Candidate freshness must be represented as `candidateFreshness: current-model-match`, `candidateFreshness: stale-model-mismatch` or `candidateFreshness: unknown`.
- Stale candidates remain displayable only as historical evidence.
- Stale candidates cannot authorize public/product exposure or write/action execution.

Tamper/corruption handling:

- Corrupted snapshot JSON blocks manifest creation.
- Missing snapshot hash blocks.
- Malformed snapshot hash blocks.
- Hash mismatch blocks.
- Missing evidence refs block.
- Missing source chain hashes block.
- Any unsafe metadata or unredacted secret-like/path-like/provider-like value blocks.
- A malformed existing manifest blocks instead of repairing or overwriting.

Invalidation:

- No overwrite-based invalidation.
- No mutable latest pointer invalidation.
- Invalidation is represented by a newer create-only manifest with an explicit redacted reason and evidence refs.
- An invalidated manifest remains historical evidence, not the candidate to display as current.
- A later reader must prefer an explicitly valid non-invalidated manifest only if that reader is separately authorized.

Read precedence:

- Current in-process/read-model state remains primary.
- Existing historical snapshots remain evidence only.
- Future manifests remain candidate selection evidence only.
- No public/product surface may treat the manifest as source of truth.
- No Production route may expose the manifest.
- Fallback to in-process/read-model must be explicit and safe.
- Missing manifest does not fail open.

Redaction:

- Redaction-before-persistence is mandatory.
- Manifest may contain safe relative paths, hashes, timestamps, classifications and redacted summaries only.
- Absolute paths, secrets, raw notes, raw payloads, URLs/providers, DB strings, command text and environment values block or redact before write.

Evidence refs required:

- selected snapshot safe relative path;
- selected snapshot content hash;
- selected snapshot checkpoint hash;
- selected snapshot operator surface model hash;
- approval decision evidence ref;
- no-op execution evidence ref;
- bounded marker evidence ref;
- local handoff draft evidence ref;
- workspace test-jail draft evidence ref;
- user workspace allowlisted draft evidence ref;
- latest-state snapshot evidence ref;
- manifest creation evidence ref.

DOM/read-model expectations, if a later reader is authorized:

- Show candidate manifest state.
- Show classification `LOCAL_INTERNAL_DEV_ONLY_VERSIONED_MANIFEST_NOT_AUTHORITY`.
- Show selected snapshot hash prefix.
- Show manifest hash prefix.
- Show stale/fresh/unknown label.
- Show invalidation state.
- Show negative flags: no live authority, no product authority, no Production, no public/product, no latest pointer overwrite, no command execution, no shell/subprocess, no provider/cloud/network, no DB/migration, no KMS/WORM/compliance custody, no release/commercial.

Production and public/product blocking:

- Production route remains absent/404.
- Public/product route remains absent.
- Public UI action remains absent.
- Command handler exposure remains absent.
- Any claim to promote manifest to user-facing/product truth blocks.

Static scan rules:

- Future manifest names may appear only in docs/tests until implementation GO.
- No `ProductLedgerLocalDurableLatestStateManifestWriter` source class before implementation GO.
- No `ProductLedgerLocalDurableLatestStateManifestValidator` source class before implementation GO.
- No `ProductLedgerLocalDurableLatestStateCandidateReader` source class before a reader-specific GO.
- No `LocalDurableLatestStateManifestCreateOnly` source action before implementation GO.
- No `/internal/product-ledger/operator-surface/create-durable-latest-state-manifest` route before implementation GO.
- No `/internal/product-ledger/operator-surface/durable-latest-state-candidate-state` route before reader/route GO.
- No mutable `latest.json` pointer.
- No latest pointer overwrite.
- No public/product route.
- No Production mapping.
- No broader workspace action.
- No edit/update/delete.
- No shell/subprocess.
- No command execution.
- No cloud/network/DB.
- No KMS/WORM/compliance custody.
- No release/commercial claim.

## Future Test Plan

Positive:

- valid snapshot can be selected as candidate manifest entry;
- stale classification is visible;
- tamper-free snapshot hash is validated;
- manifest hash is deterministic;
- redacted safe metadata only is persisted;
- evidence refs are preserved;
- operator surface can display candidate state safely after a later reader GO;
- missing latest pointer does not fail open;
- fallback to in-process/read-model is explicit and safe;
- idempotent replay returns the same immutable manifest.

Negative:

- tampered snapshot blocks;
- corrupted JSON blocks;
- missing hash blocks;
- malformed hash blocks;
- missing evidence refs blocks;
- stale snapshot is marked stale, not authority;
- unredacted secret blocks;
- raw absolute path blocks;
- malformed manifest blocks;
- `latest.json` pointer overwrite absent;
- unknown action/read kind blocks;
- path field blocks;
- traversal blocks;
- shell/subprocess field blocks;
- command field blocks;
- URL/network/provider field blocks;
- Production route 404;
- public/product absent;
- authority overclaim blocks.

Security/static:

- no live authority claim;
- no product authority claim;
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

`AUTHORIZE_NODAL_OS_DURABLE_LATEST_STATE_MANIFEST_CREATE_ONLY_IMPLEMENTATION_WINDOW`

That future implementation window may implement only the local/internal/Development-only versioned manifest writer/validator under the boundary above. It must not implement active durable reader precedence, live/product authority, mutable latest pointer, public/product exposure, Production route, broader workspace action, edit/update/delete, shell/subprocess, command execution, Browser/CDP/WCU/OCR/Recipes live, provider/cloud/network, DB/migration, KMS/WORM/external trust or release/commercial readiness.
