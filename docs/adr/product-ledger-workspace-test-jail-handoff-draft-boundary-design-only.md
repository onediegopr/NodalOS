# Product Ledger Workspace Test-Jail Handoff Draft Boundary Design-Only

Date: 2026-07-06

Decision: `GO_WITH_FINDINGS_WORKSPACE_TEST_JAIL_HANDOFF_DRAFT_BOUNDARY_DESIGN_ONLY_READY`

## Scope

This block is design-only, readiness-only, test-only and guard-only. It defines the technical boundary for a future first controlled user-workspace action:

`LocalWorkspaceTestJailHandoffDraftCreateOnly`

No workspace write, active route, executor, public/product path, Production route, shell/subprocess, command execution, Browser/CDP/WCU/OCR/Recipes live, Pilot `/run`, provider/cloud/network, DB/migration, KMS/WORM/external trust, release/commercial readiness or compliance custody claim is implemented here.

## Backward Reconciliation

Current real chain:

`candidate -> approval persisted -> approved no-op execution -> bounded internal marker -> LocalApprovedHandoffReportDraft -> docs/test-output create-only/no-overwrite -> evidence/read-model/operator surface`

Still absent:

- No workspace test-jail write implementation.
- No active route `POST /internal/product-ledger/approval/create-workspace-test-jail-handoff-draft` in `src`.
- No active executor `ProductLedgerLocalWorkspaceTestJailHandoffDraftExecutor` in `src`.
- No public/product path.
- No Production route.
- No shell/subprocess.
- No command execution.
- No cloud/network/DB.
- No Browser/CDP/OCR/WCU/Recipes live.
- No release/commercial.

## Future Action Contract

Action name: `LocalWorkspaceTestJailHandoffDraftCreateOnly`.

Action kind: `LocalWorkspaceTestJailHandoffDraftCreateOnly`.

Future route: `POST /internal/product-ledger/approval/create-workspace-test-jail-handoff-draft`.

Future executor name: `ProductLedgerLocalWorkspaceTestJailHandoffDraftExecutor`.

Future state route, if needed: `GET /internal/product-ledger/approval/workspace-test-jail-handoff-draft-state`.

Implementation status: not implemented in this block.

## Workspace Test-Jail Root Strategy

The future implementation must use a trusted, pre-registered workspace test-jail root. The root must not come from payload, query string, headers, UI text, environment overrides or user-selected arbitrary paths.

Recommended future root shape:

`<registered-workspace-test-jail>/.nodal/product-ledger/handoff-drafts/`

The future route may accept only business identifiers and safe evidence references. It must not accept an output directory, absolute path, relative path, filename override, extension override or cleanup path.

## Allowed Relative Output Path Strategy

Allowed future output path:

`<canonical-workspace-test-jail-root>/.nodal/product-ledger/handoff-drafts/<safe-action-id>.md`

Rules:

- The final persisted path is derived internally.
- The file extension is `.md` only for the first implementation window.
- The filename is generated from a strict allowlist id such as `actionId`, normalized to lowercase ASCII `[a-z0-9-]`.
- Empty, long, dot-prefixed, reserved-device, whitespace-only and separator-containing filenames block.
- The relative path returned to read-model/operator surface must be relative to the workspace test-jail root, never absolute.

## Forbidden Paths

The future implementation must reject:

- Any payload, query, header, environment or UI supplied output path.
- Absolute paths.
- `..` traversal.
- Encoded traversal variants such as `%2e%2e`, `%252e%252e`, `%2f`, `%5c`, mixed slash/backslash variants and Unicode lookalike separators when present in payload fields.
- UNC paths and network shares.
- Drive-qualified Windows paths.
- Desktop, Downloads, Documents, home-directory roots and temp roots outside the registered jail.
- Paths outside the canonical jail after normalization.
- Symlink, junction, mount-point or reparse escapes outside the canonical jail.
- Existing file with different content.
- Directory paths, device names and alternate data stream names.

## Path Safety Design

The future implementation must:

1. Load the registered test-jail root from trusted fixture/configuration.
2. Canonicalize the registered root before any write.
3. Combine only internally derived fixed segments under that root.
4. Reject any payload-controlled path or filename override before combining.
5. Canonicalize the intended parent directory.
6. Create the parent directory only inside the canonical jail.
7. Re-read/reparse the parent directory after creation.
8. Build the final file path from the reparsed parent plus the safe generated filename.
9. Re-read/reparse the final parent and final path target, if available, immediately before create.
10. Assert the final canonical path starts with the canonical jail root using path-segment comparison, not string prefix alone.
11. Use create-only semantics.
12. Re-read/reparse after create and confirm the file still lives inside the jail.

No filesystem scan is allowed. The implementation may inspect only the registered jail root, the derived parent directory and the derived final file path.

## Symlink/Reparse Handling

The future implementation must fail closed when platform APIs report a symlink, junction, mount point, reparse point or uncertain metadata on:

- The registered jail root.
- Any derived parent segment.
- The final file target if it already exists.

If a platform does not expose reliable reparse detection, the implementation must require a narrower test-safe mode and document the gap before any write is enabled.

## Create-Only / No-Overwrite Policy

The future writer must use create-new semantics.

Allowed:

- Create one new `.md` draft inside the jail.
- Return idempotent exact replay when the existing file content hash exactly matches the deterministic expected content and all chain hashes still match.

Blocked:

- Overwrite.
- Append.
- Truncate.
- Replace.
- Delete.
- Move.
- Rename.
- Chmod/ACL modification.
- Existing different content.

## Idempotency Policy

The future draft content should be deterministic from:

- Action id.
- Candidate id.
- Approval decision id.
- No-op execution id.
- Bounded marker id.
- Predecessor `LocalApprovedHandoffReportDraft` id/hash.
- Evidence refs.
- Redacted summary.
- Negative assertions.

Exact replay may return `IdempotentReplay` only if:

- The file exists inside the jail.
- The content hash matches exactly.
- The current approval/no-op/bounded/predecessor chain still matches exactly.
- No unsafe payload fields are present.

Any mismatch blocks.

## Redaction Policy

Redaction-before-persistence is mandatory. The future implementation must reject:

- Secret-like content.
- Absolute path-like content.
- Raw user workspace paths.
- Provider keys, tokens and credentials.
- Email/PII-like content unless already redacted by the policy.
- URLs, provider names, DB/migration claims, KMS/WORM claims or release/commercial claims.

Error messages, blockers and evidence summaries must not echo raw unsafe values.

## Evidence Policy

Required evidence:

- Approval state is `ApprovedLocalOnly`.
- Approved no-op execution completed.
- Bounded internal marker completed.
- `LocalApprovedHandoffReportDraft` predecessor completed.
- Exact candidate evidence hash matches approval, no-op, bounded marker, predecessor and current request.
- Safe evidence refs are present.
- Output relative path.
- Output content hash.
- Create-only/no-overwrite status.
- Negative capability assertions.

The future read-model/operator surface may show only relative jail path, content hash, ids, evidence refs, blockers and negative flags.

## Exact Preconditions

- `ApprovedLocalOnly`.
- No-op execution completed.
- Bounded marker completed.
- `LocalApprovedHandoffReportDraft` predecessor completed.
- Exact hash match across candidate, approval, no-op, bounded marker, predecessor and current request.
- Evidence refs present and safe.
- Action kind exactly `LocalWorkspaceTestJailHandoffDraftCreateOnly`.
- Development/local/internal-only scope.

## Exact Blockers

- Pending approval.
- Rejected approval.
- Request-changes approval.
- Expired or invalid approval.
- Missing approval state.
- Missing no-op execution.
- Missing bounded marker.
- Missing predecessor draft.
- Tamper or hash mismatch.
- Missing evidence refs.
- Malformed payload.
- Unknown action kind.
- Any path field.
- Absolute path.
- Traversal or encoded traversal.
- Unsafe filename.
- Symlink/reparse outside jail.
- Existing different content.
- Overwrite intent.
- Shell/subprocess intent.
- Command field or command execution intent.
- URL, provider, cloud/network, DB/migration, KMS/WORM, live automation, Pilot `/run`, release/commercial or compliance custody claim.

## Cleanup / Rollback Policy

No cleanup implementation is authorized here.

Future cleanup, if ever authorized, must be:

- Test-only or Development-only.
- Explicitly gated.
- Restricted to the same registered jail.
- Refuse directories outside the jail after canonicalization and reparse.
- Never delete arbitrary user files.

Manual cleanup of test artifacts inside the jail remains acceptable as an operator procedure, not as product behavior.

## Future Route / DOM / Read-Model Expectations

Future route:

- Development-only.
- Production returns 404.
- Strict JSON body.
- Small request body limit.
- No query/path/header overrides.
- Fail-closed on malformed payload.
- No public/product mapping.

Future DOM/read-model:

- Draft state visible.
- Relative jail path visible.
- Content hash visible.
- Evidence refs visible.
- Blockers visible.
- Create-only/no-overwrite visible.
- Redaction applied visible.
- Negative flags visible: no shell/subprocess, no command execution, no public/product, no Production route, no cloud/network/DB, no KMS/WORM/compliance custody, no release/commercial.
- No forms/scripts/executable public affordance unless separately authorized.

## Future Test Plan

Positive:

- Approved chain creates exactly one draft inside test-jail.
- Relative path is safe.
- Content is redacted.
- Evidence refs are generated.
- Read-model/operator surface shows state.
- Idempotency exact replay works or blocks consistently.

Negative:

- Pending/rejected/request-changes block.
- Expired/invalid block.
- Missing no-op blocks.
- Missing bounded marker blocks.
- Missing predecessor blocks.
- Tampered candidate blocks.
- Hash mismatch blocks.
- Malformed payload blocks.
- Unknown action kind blocks.
- Absolute path blocks.
- Traversal blocks.
- Encoded traversal blocks.
- Symlink/reparse escape blocks when testable.
- Overwrite different content blocks.
- Command field blocks.
- Shell/subprocess intent blocks.
- URL/network/provider field blocks.
- Production route 404.
- Public/product absent.

Security/static:

- No shell/subprocess.
- No command execution.
- No cloud/network/DB.
- No Browser/CDP/OCR/WCU/Recipes live.
- No Pilot `/run`.
- No KMS/WORM/compliance custody claim.
- No release/commercial claim.
- Future names only in docs/tests, not active `src`, until separate implementation GO.

## Decision

`GO_WITH_FINDINGS_WORKSPACE_TEST_JAIL_HANDOFF_DRAFT_BOUNDARY_DESIGN_ONLY_READY`

## Exact Next GO Required

`AUTHORIZE_NODAL_OS_WORKSPACE_TEST_JAIL_HANDOFF_DRAFT_IMPLEMENTATION_WINDOW`
