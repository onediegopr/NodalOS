# Product Ledger Local Approval Real Operator Input and State Persistence

Date: 2026-07-05

Decision: `GO_WITH_FINDINGS_LOCAL_APPROVAL_REAL_OPERATOR_INPUT_AND_STATE_PERSISTENCE_READY`

## Scope

This block implements a local-only/internal-only Development route for operator approval decision input and a minimal local approval decision state store.

It does not execute the approved action. It records only the operator decision state (`Approve`, `Reject`, `RequestChanges`) for the already-rendered local approval execution candidate.

## Implemented

- `ProductLedgerLocalApprovalDecisionStateStore` in Core.
- Development-only POST route `/internal/product-ledger/approval/decision`.
- Development-only GET route `/internal/product-ledger/approval/state`.
- Canonical operator surface section for persisted approval decision state.
- Safety tests for fail-closed request validation, idempotency, conflict rejection, redaction and tamper/corruption detection.
- Recipes in-process tests for Development/Production route behavior and persisted surface rendering.
- Static scans updated from the previous GET-only contract to exactly one authorized Product Ledger approval decision POST.

## Boundary

Allowed:

- local-only operator decision state persistence;
- internal-only Development route;
- controlled local state-store write;
- redacted operator note storage;
- idempotent replay of the same decision;
- fail-closed rejection for malformed, stale/tampered, unsafe or conflicting input.

Not allowed:

- executing an approved action;
- public UI action;
- productive command handler exposure;
- productive DI/service registration;
- product ledger append/write/export from approval execution;
- arbitrary path input;
- provider/cloud/network;
- DB/migration;
- KMS/WORM/external trust;
- Browser/CDP/WCU/OCR/Recipes live;
- Pilot `/run`;
- release/commercial readiness or compliance custody claim.

## Findings

P0: 0

P1: 0

P2: 0

P3:

- The local approval decision state is a single-file same-boundary local store and is not compliance-grade custody.
- The POST route is Development-only and internal; public UI/product exposure remains a future protected scope.
- The approved action remains non-executed; execution requires a separate policy/authority block.

P4:

- Static scans are path-specific and do not claim that unrelated Pilot routes are absent.
- Operator note redaction is conservative local safety hardening, not a full privacy/compliance policy.

TRUE_RISK: 0

## Decision

`GO_WITH_FINDINGS_LOCAL_APPROVAL_REAL_OPERATOR_INPUT_AND_STATE_PERSISTENCE_READY`

