# Product Ledger Local Approved Handoff Report Draft Implementation

Date: 2026-07-05

Decision: `GO_WITH_FINDINGS_LOCAL_APPROVED_HANDOFF_REPORT_DRAFT_IMPLEMENTATION_READY`

## Scope

This block implements the first real local user-facing action for the Product Ledger line:

`LocalApprovedHandoffReportDraft`

The action is local-only, internal-only, Development-only, explicit-approval-gated and fail-closed. It creates a redacted handoff/report draft file only under:

`docs/test-output/product-ledger/approved-local-handoff-drafts/`

## Implemented Boundary

- Core executor: `ProductLedgerLocalApprovedHandoffReportDraftExecutor`.
- Development-only POST route: `POST /internal/product-ledger/approval/create-local-handoff-draft`.
- Development-only GET state route: `GET /internal/product-ledger/approval/local-handoff-draft-state`.
- Operator surface/read-model section: `product-ledger-approved-handoff-report-draft-state`.
- Output mode: create-only through `FileMode.CreateNew`.
- Replay mode: exact same request/content returns idempotent replay; conflicting existing content blocks.

## Required Pre-State

- Persisted approval decision state is `ApprovedLocalOnly`.
- Approved no-op execution is completed.
- Bounded internal completion marker is completed.
- Candidate action kind matches the approved chain.
- Candidate evidence hash matches approval, no-op, bounded execution and current evidence exactly.
- Evidence references are present and safe.
- Redaction-before-persistence policy accepts the draft material before write.

## Explicit Negative Boundary

- No arbitrary path.
- No path traversal.
- No overwrite.
- No user workspace write.
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

## Safety Rules

- The route is mapped only in Development.
- Production remains 404 for route and state.
- The output directory is derived internally from repo boundary options, never from payload.
- Payload path, command, URL, provider and DB fields block.
- Unsafe action/candidate ids block.
- Secret-like and path-like draft content blocks through redaction-before-persistence.
- The generated draft includes only relative output path, safe ids, evidence refs and negative assertions.

## Known Findings

P0: 0

P1: 0

P2: 0

P3:

- A real local write now exists, but only under `docs/test-output/product-ledger/approved-local-handoff-drafts/`.
- The generated draft is useful local handoff evidence, not product export, compliance custody or release evidence.
- Static scans remain path-specific and must stay paired with route/executor tests.

P4:

- The default route stores latest state in process for operator surface visibility.
- The generated file is same-boundary local evidence and can be cleaned by deleting the allowlisted test-output artifact.

TRUE_RISK: 0

## Next Frontier

The next large frontier is public/product exposure or user-workspace action. Both remain not authorized.
