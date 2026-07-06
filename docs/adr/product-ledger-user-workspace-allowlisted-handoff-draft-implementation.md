# Product Ledger User Workspace Allowlisted Handoff Draft Implementation

Date: 2026-07-06

Decision: `GO_WITH_FINDINGS_USER_WORKSPACE_ALLOWLISTED_HANDOFF_DRAFT_IMPLEMENTATION_READY`

## Scope

This window implements the controlled local user-workspace action:

`LocalUserWorkspaceAllowlistedHandoffDraftCreateOnly`

The action performs a real local write outside the workspace test-jail, but only under the allowlisted workspace boundary:

`docs/nodal-os/handoffs/`

It remains local-only, internal-only, Development-only, explicit-approval-gated and fail-closed.

## Implemented Boundary

- Core executor: `ProductLedgerLocalUserWorkspaceAllowlistedHandoffDraftExecutor`.
- Development-only POST route: `POST /internal/product-ledger/approval/create-user-workspace-allowlisted-handoff-draft`.
- Development-only GET state route: `GET /internal/product-ledger/approval/user-workspace-allowlisted-handoff-draft-state`.
- Operator surface/read-model section: `product-ledger-user-workspace-allowlisted-handoff-draft-state`.
- Output mode: create-only through `FileMode.CreateNew`.
- Relative output boundary: `docs/nodal-os/handoffs/` under a trusted workspace root.
- Workspace classification: `USER_WORKSPACE_ALLOWLISTED_BOUNDARY_ONLY`.
- Replay mode: exact same request/content returns idempotent replay; conflicting existing content blocks.

## Required Pre-State

- Persisted approval decision state is `ApprovedLocalOnly`.
- Approved no-op execution is completed.
- Bounded internal completion marker is completed.
- `LocalApprovedHandoffReportDraft` predecessor is completed.
- `LocalWorkspaceTestJailHandoffDraftCreateOnly` predecessor is completed.
- Local approved handoff draft content hash matches exactly.
- Workspace test-jail handoff draft content hash matches exactly.
- Candidate evidence hash matches approval, no-op, bounded execution and current evidence exactly.
- Evidence references are present and safe.
- Redaction-before-persistence accepts the draft material before write.

## Trusted Workspace Root

The trusted workspace root comes from internal configuration/test harness only. It is not read from payload, query, header, direct UI input or unsafe environment values.

The executor canonicalizes the workspace root and final output path before write. UI/state evidence exposes classification, safe relative path and hashes only; it does not expose raw absolute workspace paths.

## Path And Write Rules

- Fixed allowed relative boundary: `docs/nodal-os/handoffs/`.
- Internal deterministic filename only.
- `.md` only.
- Payload path, root and filename fields are rejected.
- Absolute path and traversal authority are unavailable because the final path is internally composed and canonicalized.
- Final path must remain inside the canonical allowlisted boundary by path-segment comparison.
- Boundary parent directories are checked for reparse metadata and fail closed when unsafe or uncertain.
- No filesystem scan is used.
- No overwrite.
- Existing same content is exact idempotent replay.
- Existing different content is blocked.

## Explicit Negative Boundary

- No workspace-free write.
- No user-selected path.
- No arbitrary path.
- No overwrite or edit existing file.
- No delete route.
- No automatic destructive cleanup.
- No shell/subprocess.
- No command execution.
- No Pilot `/run`.
- No Browser/CDP/WCU/OCR/Recipes live.
- No public/product path.
- No Production route.
- No cloud/provider/network.
- No DB/migration.
- No KMS/WORM/external trust.
- No compliance custody claim.
- No release/commercial claim.
- No business signoff claim.

## Known Findings

P0: 0

P1: 0

P2: 0

P3:

- A real local write now exists outside the workspace test-jail, but only under the fixed allowlisted boundary `docs/nodal-os/handoffs/`.
- Reparse/symlink/junction defense remains platform-metadata-bound and intentionally fails closed on uncertainty.
- The output is a local handoff draft, not product export, release evidence, business signoff or compliance custody.

P4:

- The route keeps latest state in process for operator surface visibility.
- Generated artifacts are local workspace artifacts and cleanup is procedural; no destructive cleanup route was introduced.

TRUE_RISK: 0

## Next Frontier

The next safe step is read-only external-audit-style review of this implementation.

Broader user-workspace action, public/product exposure, Production execution, release/commercial readiness, KMS/WORM/external trust, provider/cloud/network and DB/migration remain not authorized.
