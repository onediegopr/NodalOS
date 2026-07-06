# Nodal OS User Workspace Allowlisted Handoff Draft Boundary Design-Only Handoff

Date: 2026-07-06

Decision: `GO_WITH_FINDINGS_USER_WORKSPACE_ALLOWLISTED_HANDOFF_DRAFT_BOUNDARY_DESIGN_ONLY_READY`

## What This Window Did

- Defined the future `LocalUserWorkspaceAllowlistedHandoffDraftCreateOnly` boundary.
- Kept the boundary design-only/readiness-only/test-only/guard-only.
- Defined the allowed output boundary `docs/nodal-os/handoffs/`.
- Defined trusted workspace root source requirements.
- Defined canonicalization, reparse, traversal and no-overwrite rules.
- Defined redaction, evidence, idempotency and rollback policy.
- Added future route, DOM/read-model and test expectations.
- Added guard coverage so future names remain docs/tests only until implementation GO.

## Still Not Implemented

- No write outside workspace test-jail.
- No active route `create-user-workspace-allowlisted-handoff-draft`.
- No active executor `ProductLedgerLocalUserWorkspaceAllowlistedHandoffDraftExecutor`.
- No public/product exposure.
- No Production route.
- No user-selected path.
- No payload-controlled root/path/filename.
- No workspace-free write.
- No shell/subprocess.
- No command execution.
- No Browser/CDP/WCU/OCR/Recipes live.
- No Pilot `/run`.
- No provider/cloud/network.
- No DB/migration.
- No KMS/WORM/external trust.
- No release/commercial.
- No compliance custody claim.

## Future Boundary Summary

Future action:

`LocalUserWorkspaceAllowlistedHandoffDraftCreateOnly`

Future route:

`POST /internal/product-ledger/approval/create-user-workspace-allowlisted-handoff-draft`

Future state route:

`GET /internal/product-ledger/approval/user-workspace-allowlisted-handoff-draft-state`

Future executor:

`ProductLedgerLocalUserWorkspaceAllowlistedHandoffDraftExecutor`

Allowed boundary:

`docs/nodal-os/handoffs/`

Workspace root source:

Trusted workspace model, fixture/test harness or audited internal config only. Never payload, query, header, direct UI text, unsafe env or user-selected path.

Required classification:

`USER_WORKSPACE_ALLOWLISTED_BOUNDARY_ONLY`

## Findings

P0: 0

P1: 0

P2: 0

P3:

- Future write outside test-jail remains blocked until a dedicated implementation GO.
- Trusted workspace root source must exist before implementation; if absent, future implementation must block.
- Reparse/symlink/junction proof must fail closed on uncertain metadata.

P4:

- This is planning/readiness evidence only.
- No business, release or compliance custody signoff is claimed.

TRUE_RISK: 0

## Exact Next GO Required

`AUTHORIZE_NODAL_OS_USER_WORKSPACE_ALLOWLISTED_HANDOFF_DRAFT_IMPLEMENTATION_WINDOW`
