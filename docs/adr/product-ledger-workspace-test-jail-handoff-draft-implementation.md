# Product Ledger Workspace Test-Jail Handoff Draft Implementation

Date: 2026-07-06

Decision: `GO_WITH_FINDINGS_WORKSPACE_TEST_JAIL_HANDOFF_DRAFT_IMPLEMENTATION_READY`

## Scope

This block implements the controlled local workspace test-jail action:

`LocalWorkspaceTestJailHandoffDraftCreateOnly`

The action performs a real local write only inside a controlled workspace test-jail. It remains local-only, internal-only, Development-only, explicit-approval-gated and fail-closed.

## Implemented Boundary

- Core executor: `ProductLedgerLocalWorkspaceTestJailHandoffDraftExecutor`.
- Development-only POST route: `POST /internal/product-ledger/approval/create-workspace-test-jail-handoff-draft`.
- Development-only GET state route: `GET /internal/product-ledger/approval/workspace-test-jail-handoff-draft-state`.
- Operator surface/read-model section: `product-ledger-workspace-test-jail-handoff-draft-state`.
- Output mode: create-only through `FileMode.CreateNew`.
- Relative output boundary: `.nodal/product-ledger/handoff-drafts/` under the trusted workspace test-jail root.
- Replay mode: exact same request/content returns idempotent replay; conflicting existing content blocks.

## Required Pre-State

- Persisted approval decision state is `ApprovedLocalOnly`.
- Approved no-op execution is completed.
- Bounded internal completion marker is completed.
- `LocalApprovedHandoffReportDraft` predecessor is completed.
- Predecessor content hash matches exactly.
- Candidate action kind matches the approved chain.
- Candidate evidence hash matches approval, no-op, bounded execution and current evidence exactly.
- Evidence references are present and safe.
- Redaction-before-persistence policy accepts the draft material before write.

## Path And Jail Rules

- The workspace test-jail root is supplied internally by the factory/test harness, not by payload, query, header, UI or env.
- The default route root is under `docs/test-output/product-ledger/workspace-test-jail`.
- Test harness roots must include `.tmp-product-ledger-workspace-test-jail-tests`.
- The final path is canonicalized before write and must remain under the canonical jail root.
- The writer validates `FileAttributes.ReparsePoint` on the jail root and parent directory and fails closed if reparse metadata is unsafe or uncertain.
- Payload path, root and filename fields are rejected.
- No filesystem scan is used.

## Explicit Negative Boundary

- No workspace-free write.
- No user-selected path.
- No arbitrary path.
- No path traversal.
- No overwrite.
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

- A real local write now exists, but only under the controlled workspace test-jail root and `.nodal/product-ledger/handoff-drafts/` relative boundary.
- Reparse/symlink/junction defense is bounded by platform metadata APIs and intentionally fails closed on uncertainty.
- The generated draft is local test-jail evidence only, not product export, compliance custody or release evidence.

P4:

- The default route keeps latest state in process for operator surface visibility.
- Generated artifacts are local test-output/test-jail artifacts and can be cleaned by deleting the test-jail boundary.

TRUE_RISK: 0

## Next Frontier

The next large frontier is user-workspace action outside test-jail or public/product exposure. Both remain not authorized.
