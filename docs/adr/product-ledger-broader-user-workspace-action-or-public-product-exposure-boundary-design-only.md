# Product Ledger Broader User Workspace Action Or Public Product Exposure Boundary Design-Only

Date: 2026-07-06

Decision: `GO_WITH_FINDINGS_BROADER_USER_WORKSPACE_OR_PUBLIC_PRODUCT_EXPOSURE_BOUNDARY_DESIGN_ONLY_READY`

## Scope

This window is design-only, readiness-only, audit-only, test-only and guard-only.

It does not implement:

- Broader user-workspace action.
- Public/product exposure.
- Production route.
- User-selected path.
- Overwrite, edit or delete.
- Destructive cleanup.
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

- Broader workspace action.
- User-selected path.
- Overwrite/edit/delete path.
- Public/product path.
- Production route.
- Shell/subprocess.
- Command execution.
- Cloud/provider/network/DB.
- Browser/CDP/OCR/WCU/Recipes live.
- KMS/WORM/compliance custody.
- Release/commercial readiness.

## Authorization Matrix

| Option | User value | Technical delta | Files/routes/stores touched | Write scope | Destructive risk | Security risk | Privacy risk | Path risk | Rollback/cleanup need | Idempotency need | Redaction need | Evidence need | Auditability | Overclaim risk | Test burden | Blockers | Why now / why not now | Recommendation |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| A. Broader user-workspace create-only action in another allowlisted boundary | Medium-high: expands useful local artifacts beyond handoff drafts. | New boundary, executor, route, surface and tests. | Future Core executor, Pilot Development route, operator surface, Safety/Recipes tests. | Another fixed allowlisted boundary, create-only only. | Low if create-only; medium if boundary is broad. | Medium due new path semantics. | Medium because more workspace content classes may be touched. | Medium; requires new canonicalization/reparse proof. | Procedural only; no destructive cleanup. | Required exact replay or conflict. | Required before write. | Required from full approved chain. | Good if fully guarded. | Medium; may be mistaken for general workspace authority. | High. | Needs dedicated boundary and implementation GO. | Valuable later, but current latest-state evidence remains in-process and should be hardened first. | Not next. |
| B. Controlled edit/update action under `docs/nodal-os/handoffs/` | Medium: lets drafts evolve. | Requires update semantics, conflict resolution and rollback policy. | Existing action family plus new update executor/route/store. | Existing handoff boundary. | Higher because it modifies existing files. | Medium-high due tamper/overwrite risk. | Medium. | Medium. | Strong rollback and conflict handling required. | More complex than create-only. | Required before update. | Required before/after update. | Moderate. | High; may imply editor/product workflow. | Very high. | Edit/update/delete is explicitly not authorized. | Not now because create-only discipline should remain stable before mutation. | No. |
| C. Public/product exposure of local operator surface/action path | High discoverability. | Public route/product surface/auth/UX/release hardening. | Product/public UI, routing, auth/policy, docs and release gates. | Could expose read/action path publicly. | Medium-high if users can trigger action. | High due auth/exposure. | High. | Medium. | Product-grade support needed. | Required. | Required. | Required. | Good only after product controls. | Very high; release/commercial claims nearby. | Very high. | Public/product, Production and release decisions are not authorized. | Not now; local-only action chain still needs durable state hardening. | No. |
| D. Durable/latest-state persistence hardening before public/product | Medium: reduces P4 in-process state fragility and improves auditability. | Design a local-only create-only snapshot/read model boundary; no public/product. | Future Core snapshot writer, Development route/state read-model tests, docs. | Local/internal snapshot boundary; no user workspace expansion. | Low if create-only snapshot, no overwrite. | Low-medium. | Low-medium if absolute paths remain hashed/redacted. | Low if boundary fixed. | Procedural only; snapshots are evidence. | Required by content hash. | Required for snapshot material. | Required from current surface/action chain. | High. | Low if framed as local evidence only. | Medium. | Needs future implementation GO and no pointer overwrite. | Best next: hardens current local product line without broader workspace or public exposure. | Recommended. |
| E. Additional static/property hardening only | Low-medium: more confidence, no capability. | Tests/docs only. | Safety/Recipes tests and docs. | None. | Very low. | Very low. | Very low. | Very low. | None. | N/A. | N/A. | Test evidence only. | Good. | Low. | Low. | Does not address latest-state P4. | Useful if drift appears, but less valuable than designing durable latest-state. | Fallback. |

## Recommendation

Recommend option D as the single next frontier:

`LocalOperatorSurfaceLatestStateSnapshotCreateOnly`

This should be designed before implementation as a local-only/internal-only/Development-only create-only snapshot of the operator surface latest state.

Why public/product remains blocked:

- The route family is still Development-only and local/internal.
- Public/product exposure would require auth, UX, policy, support, release and commercial decisions outside this window.
- The latest surface state is currently in-process evidence, so public/product exposure would magnify a known P4.

Why edit/update/delete remains blocked:

- Mutation creates overwrite, rollback and tamper risk.
- Current action chain intentionally proves create-only/no-overwrite discipline.
- A durable create-only snapshot boundary can improve evidence without crossing into mutation.

Why durable/latest-state hardening before broader create-only:

- It reduces current P4 evidence fragility.
- It improves auditability before adding more workspace write surfaces.
- It can be local-only and create-only, preserving the safety shape already proven.

## Future Boundary Spec

Future action name:

`LocalOperatorSurfaceLatestStateSnapshotCreateOnly`

Future action kind:

`LocalOperatorSurfaceLatestStateSnapshotCreateOnly`

Future route, not implemented in this window:

`POST /internal/product-ledger/operator-surface/latest-state-snapshot`

Future state route, not implemented in this window:

`GET /internal/product-ledger/operator-surface/latest-state-snapshot-state`

Future executor:

`ProductLedgerLocalOperatorSurfaceLatestStateSnapshotExecutor`

Allowed boundary:

`docs/test-output/product-ledger/operator-surface-latest-state/snapshots/`

Classification:

`LOCAL_OPERATOR_SURFACE_LATEST_STATE_SNAPSHOT_ONLY`

Forbidden boundaries:

- `docs/nodal-os/handoffs/` unless read-only as predecessor evidence.
- Arbitrary workspace paths.
- Repository root.
- `src/`.
- `tests/`.
- `.git/`.
- User profile folders.
- Network shares.
- Any absolute payload path.

Trusted source:

- Operator surface model generated internally from the existing local read-model.
- Existing Product Ledger action snapshots as read-only predecessor evidence.
- Internal/test harness output root only.
- Never payload, query, header, direct UI text field, unsafe env or user-selected path.

Rules:

- Create-only/no-overwrite.
- No latest pointer overwrite.
- Snapshot filename internally generated from deterministic surface id, timestamp strategy and content hash prefix.
- `.json` allowlist only.
- Canonicalize output root and final path.
- Validate final path remains under the canonical snapshot boundary by path-segment comparison.
- Check reparse/symlink/junction metadata and fail closed on uncertainty.
- Reject payload path/root/filename/command/url/provider fields.
- No filesystem scan.
- Redaction-before-persistence required for any persisted snapshot metadata.
- Evidence refs required from approval, no-op, bounded marker, local approved handoff draft, workspace test-jail draft and user workspace allowlisted draft.
- Exact idempotent replay allowed only if content hash matches.
- Existing different content blocks.
- Rollback is procedural; no destructive cleanup route.

Future DOM/read-model expectations:

- Snapshot state visible in operator surface.
- Safe relative path visible.
- Content hash prefix visible.
- Evidence refs visible.
- Blockers visible.
- Negative flags visible: no user-selected path, no edit/update/delete, no public/product, no Production, no shell/subprocess, no command execution, no cloud/network/DB, no KMS/WORM, no release/commercial.

Production blocking:

- Future routes must remain unmapped outside Development until a separate public/product authorization exists.

Public/product blocking:

- No public UI action.
- No product command handler.
- No release/commercial claim.

Static scan rules:

- Future names may appear only in docs/tests until an implementation GO is granted.
- Any source implementation before that GO is a scope leak.

## Future Test Plan

Positive:

- Full approved chain can create exactly one latest-state snapshot inside the allowed boundary.
- Safe relative state/path/result visible.
- Evidence refs generated.
- Operator surface shows snapshot state.
- Exact idempotency stable.
- Content hash stable.

Negative:

- Pending/rejected/request-changes blocked.
- Expired/invalid blocked.
- Missing no-op blocked.
- Missing bounded marker blocked.
- Missing `LocalApprovedHandoffReportDraft` blocked.
- Missing `LocalWorkspaceTestJailHandoffDraft` blocked.
- Missing `LocalUserWorkspaceAllowlistedHandoffDraft` blocked.
- Tampered candidate blocked.
- Hash mismatch blocked.
- Malformed payload blocked.
- Unknown action kind blocked.
- Absolute path blocked.
- Traversal and encoded traversal blocked.
- Symlink/junction escape blocked or fail-closed.
- Overwrite/edit/delete blocked.
- User-selected arbitrary path blocked.
- Payload-controlled root blocked.
- Filesystem scan absent.
- Command field blocked.
- Shell/subprocess blocked.
- URL/network/provider field blocked.
- Production route 404.
- Public/product absent.

Security/static:

- No secrets in output.
- Redaction before persistence.
- No cloud/network/DB.
- No Browser/CDP/OCR/WCU/Recipes live.
- No Pilot `/run`.
- No KMS/WORM/compliance custody claim.
- No release/commercial claim.
- Future names only in docs/tests unless later implementation is explicitly authorized.

## Exact Next GO Required

`AUTHORIZE_NODAL_OS_LOCAL_OPERATOR_SURFACE_LATEST_STATE_SNAPSHOT_BOUNDARY_DESIGN_ONLY`

That next block should design the snapshot persistence boundary in more detail. Implementation should still require a later implementation GO.
