# Nodal OS User Workspace Or Public Product Authorization Boundary Design-Only Handoff

Date: 2026-07-06

Decision: `GO_WITH_FINDINGS_USER_WORKSPACE_OR_PUBLIC_PRODUCT_AUTHORIZATION_BOUNDARY_DESIGN_ONLY_READY`

## What This Window Did

- Compared user-workspace allowlisted write, public/product exposure, local hardening and fixture-only alternatives.
- Recommended a future controlled user-workspace allowlisted handoff draft boundary.
- Kept the recommendation design-only.
- Added guard coverage so no route/executor/write outside test-jail exists yet.

## Recommended Future Frontier

`LocalUserWorkspaceAllowlistedHandoffDraftCreateOnly`

Future allowed boundary candidate:

`docs/nodal-os/handoffs/`

Future route candidate:

`POST /internal/product-ledger/approval/create-user-workspace-allowlisted-handoff-draft`

## Still Not Implemented

- No action outside workspace test-jail.
- No workspace real user write.
- No user-selected path.
- No public/product exposure.
- No Production route.
- No shell/subprocess.
- No command execution.
- No Browser/CDP/WCU/OCR/Recipes live.
- No Pilot `/run`.
- No provider/cloud/network.
- No DB/migration.
- No KMS/WORM/external trust.
- No release/commercial.

## Findings

P0: 0

P1: 0

P2: 0

P3:

- User-workspace allowlisted write is the best next value frontier, but needs its own boundary design.
- Public/product exposure remains blocked by auth/UX/release/commercial risk.

P4:

- This is planning evidence only.
- No business signoff or release readiness is claimed.

TRUE_RISK: 0

## Exact Next GO Required

`AUTHORIZE_NODAL_OS_USER_WORKSPACE_ALLOWLISTED_HANDOFF_DRAFT_BOUNDARY_DESIGN_ONLY`

