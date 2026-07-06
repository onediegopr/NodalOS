# Product Ledger Local Operator Surface Latest State Snapshot Boundary Design-Only

Date: 2026-07-06

Decision: `GO_WITH_FINDINGS_LOCAL_OPERATOR_SURFACE_LATEST_STATE_SNAPSHOT_BOUNDARY_DESIGN_ONLY_READY`

## Scope

This window is design-only, readiness-only, test-only and guard-only.

It does not implement:

- `LocalOperatorSurfaceLatestStateSnapshotCreateOnly`.
- Active snapshot writer.
- Active snapshot route.
- Public/product exposure.
- Production route.
- Broader workspace action.
- User-selected path.
- Overwrite/edit/delete.
- Shell/subprocess or command execution.
- Browser/CDP/WCU/OCR/Recipes live authority.
- Pilot `/run`.
- Provider/cloud/network, DB/migration or KMS/WORM/external trust.
- Release/commercial readiness or compliance custody.

## Backward Reality Check

Current implemented chain:

`candidate -> approval persisted -> approved no-op execution -> bounded internal marker -> LocalApprovedHandoffReportDraft -> LocalWorkspaceTestJailHandoffDraftCreateOnly -> LocalUserWorkspaceAllowlistedHandoffDraftCreateOnly -> .md create-only under docs/nodal-os/handoffs/ -> evidence/read-model/operator surface`

Confirmed implemented:

- `LocalApprovedHandoffReportDraft`.
- `LocalWorkspaceTestJailHandoffDraftCreateOnly`.
- `LocalUserWorkspaceAllowlistedHandoffDraftCreateOnly`.
- Real write outside test-jail only under `docs/nodal-os/handoffs/`.
- Create-only/no-overwrite.
- Canonical final path under allowlisted boundary.
- Reparse/symlink/junction fail-closed.
- Redaction-before-persistence.
- Evidence references.
- Operator surface/read-model state.

Confirmed still absent:

- `LocalOperatorSurfaceLatestStateSnapshotCreateOnly`.
- Active latest-state snapshot writer.
- Active latest-state snapshot route.
- Public/product path.
- Production route.
- Broader workspace action.
- User-selected path.
- Overwrite/edit/delete path.
- Shell/subprocess.
- Command execution.
- Cloud/provider/network/DB.
- Browser/CDP/OCR/WCU/Recipes live.
- KMS/WORM/compliance custody.
- Release/commercial readiness.

## Snapshot Candidate Matrix

| Option | User value | Technical delta | Files/routes/stores touched | Write scope | Destructive risk | Privacy risk | Tamper risk | Stale state risk | Cleanup/retention need | Idempotency need | Redaction need | Evidence need | Auditability | Overclaim risk | Test burden | Blockers | Recommendation | Why now / why not now |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| A. Snapshot create-only under `docs/test-output/product-ledger/operator-surface-latest-state-snapshots/` | High for local auditability and low-risk persistence. | Future local-only writer/read model and dev-only route, still no product exposure. | Future Core executor, Development-only Pilot route/state route, Safety/Recipes tests and docs. | Fixed repo-local test-output boundary. | Low if immutable create-only. | Low-medium; metadata must be redacted. | Medium unless hashes/checkpoints are enforced. | Medium; snapshots can become stale by design. | Medium; retention is docs-only with no delete route. | Required by content hash. | Required before persistence. | Required from full approved chain and surface model. | High. | Low if named test-output/local-only. | Medium. | Needs later implementation GO. | Recommended. | Best fit for first implementation because it is local/test-safe and reduces in-process latest-state fragility. |
| B. Snapshot create-only under `docs/nodal-os/operator-surface-snapshots/` | Medium-high because it reads like durable docs evidence. | Same as A but closer to human-facing canonical docs. | Future Core/Pilot/tests/docs. | Repo docs boundary. | Low if immutable. | Medium because docs path may invite durable/product overclaims. | Medium. | Medium. | Medium. | Required. | Required. | Required. | High. | Medium-high. | Medium. | Could be mistaken for canonical product documentation. | Not first. | Better after implementation proves safety in test-output. |
| C. Snapshot in-memory only hardening, no persistence | Low-medium; avoids write risk. | Add read-model checks only. | Tests/docs, maybe future model checks. | None. | Very low. | Very low. | Low. | High because state remains in-process. | None. | N/A. | N/A. | Existing evidence only. | Medium. | Low. | Low. | Does not solve P4 latest-state durability. | Fallback only. | Safe but insufficient for the stated objective. |
| D. More constrained local-temp snapshot boundary | Medium; avoids repo writes. | Future temp-only writer/read model with ephemeral retention. | Future temp store, route/state tests/docs. | OS temp boundary only. | Low. | Low. | Medium. | High across restarts/cleanup. | Medium; external cleanup can erase evidence. | Required. | Required. | Required. | Medium. | Low. | Medium. | Temp volatility undermines audit value. | Not preferred. | Useful for experiments, weaker for handoff evidence. |

## Recommendation

Recommend option A:

`docs/test-output/product-ledger/operator-surface-latest-state-snapshots/`

This path supersedes the earlier shorthand candidate `docs/test-output/product-ledger/operator-surface-latest-state/snapshots/` for the next implementation window. The older path remains historical planning text only.

Why option A:

- It stays inside existing repo-local test-output evidence conventions.
- It avoids `docs/nodal-os/` public-facing overclaim risk for the first writer.
- It provides durable local evidence without expanding user-workspace write authority.
- It keeps the first implementation clearly readiness/test-safe before public/product decisions.

## Recommended Boundary Spec

Future action name:

`LocalOperatorSurfaceLatestStateSnapshotCreateOnly`

Future action kind:

`LocalOperatorSurfaceLatestStateSnapshotCreateOnly`

Future executor/writer:

`ProductLedgerLocalOperatorSurfaceLatestStateSnapshotExecutor`

Future route, not implemented in this window:

`POST /internal/product-ledger/operator-surface/latest-state-snapshot`

Future state/read route, not implemented in this window:

`GET /internal/product-ledger/operator-surface/latest-state-snapshot-state`

Output boundary:

`docs/test-output/product-ledger/operator-surface-latest-state-snapshots/`

Classification:

`LOCAL_OPERATOR_SURFACE_LATEST_STATE_SNAPSHOT_ONLY`

Forbidden boundaries:

- `docs/nodal-os/handoffs/` except read-only as predecessor evidence.
- `docs/nodal-os/operator-surface-snapshots/` until a later docs-facing GO.
- Arbitrary workspace paths.
- Repository root.
- `src/`.
- `tests/`.
- `.git/`.
- User profile folders.
- Network shares.
- Any payload-provided absolute path.
- Any user-selected path.

Trusted source:

- Existing Product Ledger operator surface model generated internally.
- Existing local read-model and action snapshots as read-only predecessor evidence.
- Existing local approval, no-op, bounded marker, local handoff draft, workspace test-jail draft and user workspace allowlisted draft evidence.
- Never payload path, query, header, UI free text, unsafe env or user-selected path.

## Snapshot Schema

Format: `.json`.

Allowed fields:

- `schemaVersion`.
- `snapshotId`.
- `createdAtUtc`.
- `classification`.
- `sourceChainIds`.
- `sourceChainContentHashes`.
- `operatorSurfaceModelHash`.
- `snapshotContentHash`.
- `safeRelativeSnapshotPath`.
- `safeRelativePredecessorPaths`.
- `evidenceRefs`.
- `decisionStateSummaryRedacted`.
- `executionStateSummaryRedacted`.
- `handoffDraftStateSummaryRedacted`.
- `workspaceDraftStateSummaryRedacted`.
- `userWorkspaceAllowlistedDraftStateSummaryRedacted`.
- `blockerSummaryRedacted`.
- `warningSummaryRedacted`.
- `negativeFlags`.
- `staleStateClassification`.
- `localInternalDevelopmentOnly`.

Forbidden fields:

- Secrets, API keys, tokens or provider credentials.
- Raw absolute sensitive paths.
- Environment values.
- Raw payloads or unredacted operator notes.
- User-selected paths.
- Query/header/UI/env-provided output roots.
- Shell commands, subprocess requests or command output.
- URLs/providers/network config.
- DB connection strings or migrations.
- KMS/WORM/compliance custody claims.
- Release/commercial claims.
- Browser/CDP/WCU/OCR/Recipes live evidence.

## Redaction Rules

- Redaction-before-persistence is mandatory.
- Any persisted text summary must be redacted and bounded in length.
- Evidence refs must be safe relative refs or stable ids, not raw payloads.
- Absolute paths may only appear as hashes or safe relative paths under the snapshot boundary.
- Secret-like, PII-like, path-like, token-like, URL/provider-like and environment-like values block or redact before write.

## Path And Write Safety Design

- Output root is fixed and internal.
- No payload-controlled path, root or filename.
- No query/header/UI/env root.
- `.json` only for first implementation because it supports deterministic schema validation and hash canonicalization.
- Create-only versioned filename.
- No overwrite.
- No latest pointer overwrite.
- Filename generated from snapshot id, UTC timestamp and content hash prefix.
- Canonicalize output directory.
- Canonicalize final path.
- Final path must remain under the canonical output boundary by path-segment comparison.
- Traversal and encoded traversal blocked.
- Absolute payload paths blocked.
- Symlink/junction/reparse uncertainty fails closed.
- No filesystem scan.
- Exact idempotent replay may return the existing immutable snapshot only when the full content hash matches.
- Existing different content blocks as conflict.
- Retention policy is docs-only: keep immutable snapshots for audit until a later retention GO.
- No delete route and no destructive cleanup in the first implementation.

## Hash And Checkpoint Strategy

- Build a canonical JSON payload with stable property ordering.
- Compute `snapshotContentHash` over the redacted canonical payload.
- Compute `operatorSurfaceModelHash` over the redacted model projection.
- Store predecessor content hashes for approval, no-op, bounded marker, local handoff draft, workspace test-jail draft and user workspace allowlisted draft.
- Verify the current model hash before write and before idempotent replay.
- Any hash mismatch, missing hash, malformed hash or changed source chain blocks.
- Corrupt unreadable existing snapshot fails closed.

## Stale Snapshot Handling

- A snapshot is immutable evidence, not a live latest pointer.
- Staleness must be visible in future read-model/DOM state.
- The future state route may return last created snapshot metadata, but must mark whether the current in-process model hash still matches.
- A stale snapshot is readable as historical evidence only.
- Stale state cannot authorize product/public exposure.

## Future DOM/Read-Model Expectations

- Snapshot state visible in operator surface.
- Safe relative snapshot path visible.
- Snapshot content hash prefix visible.
- Operator surface model hash prefix visible.
- Evidence refs visible.
- Stale/current classification visible.
- Blockers/warnings visible.
- Negative flags visible: no user-selected path, no edit/update/delete, no public/product, no Production, no shell/subprocess, no command execution, no cloud/network/DB, no KMS/WORM, no release/commercial.

## Static Scan Rules

- Future names may appear only in docs/tests until implementation GO.
- No `ProductLedgerLocalOperatorSurfaceLatestStateSnapshotExecutor` source class before implementation GO.
- No `LocalOperatorSurfaceLatestStateSnapshotCreateOnly` source enum/action before implementation GO.
- No active snapshot route mapping before implementation GO.
- No public/product route.
- No Production mapping.
- No broader workspace action.
- No edit/update/delete.

## Future Test Plan

Positive:

- Approved/local chain can create one latest-state snapshot.
- Snapshot is under the allowed boundary.
- Safe relative path only.
- Redaction is applied before persistence.
- Evidence refs included.
- Source chain hashes stable.
- Snapshot content hash stable.
- Operator surface can read snapshot state safely.
- Stale snapshot visibly classified.
- Exact idempotent replay returns the same immutable snapshot.

Negative:

- Missing chain blocks or snapshot unavailable.
- Tampered source blocks.
- Hash mismatch blocks.
- Malformed payload blocks.
- Unknown action kind blocks.
- Path field blocks.
- Absolute path blocks.
- Traversal blocks.
- Symlink/reparse escape blocks or fails closed.
- Overwrite blocked.
- User-selected path blocked.
- Unredacted secret blocks.
- Raw absolute path blocks.
- Public/product claim blocks.
- Production route 404.
- Command/shell/network fields block.
- DB/provider/cloud fields block.
- KMS/WORM/release/commercial claims block.

Security/static:

- No command execution.
- No shell/subprocess.
- No cloud/network/DB.
- No Browser/CDP/OCR/WCU/Recipes live.
- No Pilot `/run`.
- No KMS/WORM/compliance custody claim.
- No release/commercial claim.
- Future names only in docs/tests unless later implementation is explicitly authorized.

## Exact Next GO Required

`AUTHORIZE_NODAL_OS_LOCAL_OPERATOR_SURFACE_LATEST_STATE_SNAPSHOT_IMPLEMENTATION_WINDOW`

The next window may implement the local-only create-only snapshot writer if it keeps this boundary intact. It must not add public/product exposure, Production route, broader workspace action, edit/update/delete, user-selected path, shell/subprocess, command execution, Browser/CDP/WCU/OCR/Recipes live, provider/cloud/network, DB/migration, KMS/WORM/external trust or release/commercial readiness.
