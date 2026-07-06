# NODAL OS Workspace Test-Jail Handoff Draft Boundary Design-Only Handoff

Date: 2026-07-06

Decision: `GO_WITH_FINDINGS_WORKSPACE_TEST_JAIL_HANDOFF_DRAFT_BOUNDARY_DESIGN_ONLY_READY`

## Completed

- Defined design-only boundary for future `LocalWorkspaceTestJailHandoffDraftCreateOnly`.
- Specified trusted workspace test-jail root strategy.
- Specified path canonicalization, reparse validation, traversal blocking and symlink/reparse fail-closed rules.
- Specified create-only/no-overwrite, exact idempotency, redaction, evidence, cleanup and failure policies.
- Added guard-test requirements to keep the future action absent from active `src` until separate implementation GO.

## Still Not Implemented

- No workspace write.
- No active route `POST /internal/product-ledger/approval/create-workspace-test-jail-handoff-draft`.
- No active executor `ProductLedgerLocalWorkspaceTestJailHandoffDraftExecutor`.
- No public/product path.
- No Production route.
- No shell/subprocess.
- No command execution.
- No Browser/CDP/WCU/OCR/Recipes live.
- No Pilot `/run`.
- No provider/cloud/network.
- No DB/migration.
- No KMS/WORM/external trust or compliance custody.
- No release/commercial.

## Exact Next GO Required

`AUTHORIZE_NODAL_OS_WORKSPACE_TEST_JAIL_HANDOFF_DRAFT_IMPLEMENTATION_WINDOW`

That future GO would be the first window allowed to implement the workspace test-jail write.
