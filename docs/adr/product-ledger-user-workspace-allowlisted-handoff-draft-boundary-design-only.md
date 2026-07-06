# Product Ledger User Workspace Allowlisted Handoff Draft Boundary Design-Only

Date: 2026-07-06

Decision: `GO_WITH_FINDINGS_USER_WORKSPACE_ALLOWLISTED_HANDOFF_DRAFT_BOUNDARY_DESIGN_ONLY_READY`

## Scope

This window is design-only, readiness-only, test-only and guard-only.

It does not implement:

- Write outside the workspace test-jail.
- Active route in `src`.
- Active executor in `src`.
- Public/product exposure.
- Production route.
- User-selected path or arbitrary workspace path.
- Payload-controlled root, filename or path.
- Shell/subprocess or command execution.
- Browser/CDP/WCU/OCR/Recipes live authority.
- Pilot `/run`.
- Provider/cloud/network, DB/migration or KMS/WORM/external trust.
- Release/commercial readiness or compliance custody.

## Backward Reality Check

Current implemented chain:

`candidate -> approval persisted -> approved no-op execution -> bounded internal marker -> LocalApprovedHandoffReportDraft -> LocalWorkspaceTestJailHandoffDraftCreateOnly -> workspace test-jail create-only/no-overwrite -> evidence/read-model/operator surface`

Confirmed implemented:

- `LocalApprovedHandoffReportDraft`.
- `LocalWorkspaceTestJailHandoffDraftCreateOnly`.
- Real local write only inside the workspace test-jail.
- Create-only/no-overwrite.
- Canonical final path under the jail.
- Reparse/symlink/junction fail-closed checks.
- Redaction-before-persistence.
- Evidence references.
- Operator surface/read-model state.

Confirmed still absent:

- Write outside the workspace test-jail.
- `LocalUserWorkspaceAllowlistedHandoffDraftCreateOnly` active in `src`.
- Active route `create-user-workspace-allowlisted-handoff-draft`.
- Public/product exposure.
- Production route.
- User-selected path.
- Shell/subprocess.
- Command execution.
- Cloud/provider/network/DB.
- Browser/CDP/OCR/WCU/Recipes live.
- KMS/WORM/compliance custody.
- Release/commercial readiness.

## Future Action Boundary

Future action name:

`LocalUserWorkspaceAllowlistedHandoffDraftCreateOnly`

Future action kind:

`LocalUserWorkspaceAllowlistedHandoffDraftCreateOnly`

Future route, not implemented in this window:

`POST /internal/product-ledger/approval/create-user-workspace-allowlisted-handoff-draft`

Future state route, not implemented in this window:

`GET /internal/product-ledger/approval/user-workspace-allowlisted-handoff-draft-state`

Future executor, not implemented in this window:

`ProductLedgerLocalUserWorkspaceAllowlistedHandoffDraftExecutor`

Allowed output boundary:

`docs/nodal-os/handoffs/`

Classification:

`USER_WORKSPACE_ALLOWLISTED_BOUNDARY_ONLY`

## Trusted Workspace Root Source

The future implementation must treat workspace root authority as internal configuration, not operator payload.

Allowed future sources:

- A trusted workspace model already validated by the local Product Ledger runtime boundary.
- A fixture/test harness root for tests.
- A future internal config object whose source and lifecycle are audited before implementation.

Forbidden sources:

- Payload.
- Query string.
- Header.
- Direct UI text field without a separate future approval design.
- Environment variable unless a later block proves it is trusted, fixed and fail-closed.
- Current directory if it is not explicitly bound to the trusted workspace model.
- Any user-selected path.

If no trusted workspace root source exists at implementation time, the future implementation must block with `NO_TRUSTED_WORKSPACE_ROOT_SOURCE` and perform no write.

The workspace root must be canonicalized before composing any output path. If canonicalization fails, if the root cannot be classified, or if the root classification is not `USER_WORKSPACE_ALLOWLISTED_BOUNDARY_ONLY`, the future implementation must fail closed.

## Path Safety Design

Allowed relative boundary is fixed:

`docs/nodal-os/handoffs/`

Forbidden output boundaries:

- Repository root.
- Arbitrary `docs/` subtrees.
- `src/`.
- `tests/`.
- `.git/`.
- User profile folders.
- Temp folders except fixture-only tests.
- Network shares.
- Any absolute payload path.
- Any path outside the canonical allowlisted boundary.

Rules:

- Reject payload fields named or shaped like `path`, `root`, `filename`, `directory`, `url`, `command`, `provider` or equivalent authority fields.
- Reject absolute paths.
- Reject `..`.
- Reject traversal using mixed separators.
- Reject encoded traversal variants when decoding is relevant to the future route parser.
- Combine only the canonical workspace root plus the fixed relative boundary plus an internally generated filename.
- Canonicalize the final path before write.
- Validate the final path remains under the canonical allowlisted boundary by path-segment comparison, not string prefix alone.
- Check boundary root and parent directory reparse metadata before write.
- Fail closed on symlink, junction, mount point, reparse point or uncertain metadata.
- Do not filesystem-scan the workspace.
- Allow only `.md`.
- Use create-only/no-overwrite semantics.
- Allow exact idempotent replay only when existing content hash matches the future generated content hash.
- Block any different existing content as `EXISTING_CONTENT_CONFLICT`.

Filename strategy:

- Use an internally generated deterministic slug.
- Include a short evidence hash prefix.
- Normalize to lowercase ASCII.
- Allow only letters, digits and hyphen before `.md`.
- Never accept raw filename from payload.

## Required Future Preconditions

The future implementation must require all of these before attempting a write:

- Persisted approval decision is `ApprovedLocalOnly`.
- Approval is not expired or invalid.
- Approved no-op execution completed.
- Bounded internal marker completed.
- `LocalApprovedHandoffReportDraft` completed.
- `LocalWorkspaceTestJailHandoffDraftCreateOnly` completed.
- Predecessor hashes match exactly.
- Candidate evidence hash matches exactly.
- Evidence references are present and safe.
- Redaction-before-persistence returns success for the exact future draft material.
- Trusted workspace root is present, canonicalized and classified as `USER_WORKSPACE_ALLOWLISTED_BOUNDARY_ONLY`.

## Required Future Blockers

The future implementation must fail closed for:

- Pending, rejected or request-changes decisions.
- Expired or invalid approval state.
- Missing no-op execution.
- Missing bounded marker.
- Missing `LocalApprovedHandoffReportDraft`.
- Missing `LocalWorkspaceTestJailHandoffDraftCreateOnly`.
- Tampered predecessor.
- Hash mismatch.
- Malformed payload.
- Unknown action kind.
- Payload path field.
- Payload root field.
- Absolute path.
- Traversal.
- Encoded traversal where relevant.
- Mixed separator traversal.
- Symlink/reparse/junction escape.
- Existing file with different content.
- User-selected arbitrary path.
- Filesystem scan requirement.
- Shell/subprocess or command field.
- URL/network/provider field.
- Any Production route.
- Any public/product exposure.

## Redaction And Evidence Policy

Redaction-before-persistence is mandatory. The future write must use only the redacted candidate material accepted by the redaction service. Raw sensitive values must not appear in the output, evidence, errors, state model or route response.

Evidence references must include:

- Approval decision evidence.
- No-op execution evidence.
- Bounded marker evidence.
- Local approved handoff draft evidence.
- Workspace test-jail handoff draft predecessor evidence.
- Future allowlisted handoff draft request evidence.
- Candidate content hash.
- Future output content hash.

## Rollback And Cleanup Policy

The future action is create-only. It must not add an automatic destructive cleanup route.

Rollback is procedural:

- If the write did not happen, state records the fail-closed reason.
- If the write completed, the created `.md` remains evidence.
- If a retry sees exact same content, it may return idempotent replay.
- If a retry sees different content, it blocks as a conflict.
- Manual cleanup, if required, belongs to a separate operator procedure outside Product Ledger action execution.

## Future Route, DOM And Read Model Expectations

The future local operator surface should show:

- Action state.
- Safe relative path under `docs/nodal-os/handoffs/`.
- Content hash prefix.
- Evidence references.
- Blocking reasons.
- Negative boundary flags: no public/product, no Production, no shell/subprocess, no command execution, no cloud/network/DB, no KMS/WORM, no release/commercial.

The future route must remain internal/local-only/Development-only unless a later public/product authorization explicitly changes that boundary.

## Future Test Plan

Positive tests:

- Approved full chain creates exactly one `.md` under `<workspace-root>/docs/nodal-os/handoffs/`.
- Safe relative path is reported.
- Canonical final path remains under the allowed boundary.
- Content is redacted before persistence.
- Evidence references are generated.
- Read-model/operator surface shows state.
- Exact idempotency is stable.

Negative tests:

- Pending/rejected/request-changes blocked.
- Expired/invalid blocked.
- Missing no-op blocked.
- Missing bounded marker blocked.
- Missing `LocalApprovedHandoffReportDraft` blocked.
- Missing `LocalWorkspaceTestJailHandoffDraftCreateOnly` blocked.
- Tampered candidate blocked.
- Hash mismatch blocked.
- Malformed payload blocked.
- Unknown action kind blocked.
- Payload path field blocked.
- Payload root field blocked.
- Absolute path blocked.
- Traversal blocked.
- Encoded traversal blocked where relevant.
- Mixed separator traversal blocked where relevant.
- Symlink/junction escape blocked or fail-closed.
- Overwrite different content blocked.
- User-selected arbitrary path blocked.
- Filesystem scan absent.
- Command field blocked.
- Shell/subprocess blocked.
- URL/network/provider field blocked.
- Production route 404.
- Public/product absent.

Security/static tests:

- No secrets in output.
- Redaction before write.
- No cloud/network/DB.
- No Browser/CDP/OCR/WCU/Recipes live.
- No Pilot `/run`.
- No KMS/WORM/compliance custody claim.
- No release/commercial claim.
- Future names only in docs/tests until a later implementation GO.

## Findings

P0: 0

P1: 0

P2: 0

P3:

- Future write outside test-jail remains blocked until a dedicated implementation GO.
- Trusted workspace root source is the highest-risk implementation prerequisite; absent source must block.
- Reparse/symlink/junction proof remains platform-metadata-bound and must fail closed.

P4:

- This document is readiness/design evidence only.
- Percentage changes are estimates and not release or business signoff.

TRUE_RISK: 0

## Exact Next GO Required

`AUTHORIZE_NODAL_OS_USER_WORKSPACE_ALLOWLISTED_HANDOFF_DRAFT_IMPLEMENTATION_WINDOW`
