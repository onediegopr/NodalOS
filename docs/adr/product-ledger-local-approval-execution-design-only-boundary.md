# Product Ledger Local Approval Execution Design-Only Boundary

Date: 2026-07-05

Decision: `GO_WITH_FINDINGS_LOCAL_APPROVAL_EXECUTION_DESIGN_ONLY_BOUNDARY_READY`

## Context

The Product Ledger local-only line now has a canonical local/dev operator route, rendered interaction tests, a read-only approval-to-action preview loop and test-safe live ledger read-model evidence. This ADR defines the next approval execution boundary without implementing execution, adding runtime wiring or enabling product actions.

The design narrows "approval execution" to a future local-only/internal-only/non-destructive action invocation after explicit human approval and a second policy check. It does not authorize public UI actions, productive command handlers, append/write/export, arbitrary path input, DB, cloud, KMS/WORM or release/commercial behavior.

## Approved Design Boundary

Future implementation may be considered only if all of these gates are true:

- scope is `ProductLedgerLocalOnlyInternalApprovalExecution`;
- runtime flag remains local-only, internal-only and default-off;
- route remains Development-only and internal;
- action is in the read-only/non-destructive allowlist;
- approval decision is explicit, fresh and tied to the rendered candidate action;
- policy is re-evaluated immediately before execution;
- read model is verified from fixture-safe or injected test-safe live ledger source;
- no arbitrary path is accepted from query, body, headers, environment or UI;
- execution returns an in-memory result only;
- result evidence is rendered/read-only unless a later block explicitly authorizes durable evidence append.

## Future Candidate Allowlist

The initial execution candidate must stay read-only and non-destructive:

- `ViewDiagnostics`;
- `ViewLedgerReadiness`;
- `ViewRuntimeGateStatus`;
- `ViewCheckpointHeadStatus`;
- `ViewEvidenceGates`;
- `StaticScanPreview`;
- `RequestExternalAuditPreview`.

`LocalReportPhysicalExportBoundedInternal` is intentionally excluded from the first approval execution implementation candidate because it writes a physical file even when bounded and local.

## Required State Machine

1. `PreviewRendered`: candidate action, policy gate and evidence refs are visible.
2. `HumanApprovedLocalOnly`: explicit local-only approval is present and fresh.
3. `PolicyRechecked`: policy and authority boundaries are re-evaluated after approval.
4. `ReadModelVerified`: fixture-safe or test-safe live ledger read model verifies current evidence.
5. `ActionAllowlisted`: action maps to a read-only internal command.
6. `ExecutedInMemory`: future handler returns an in-memory read-only result.
7. `EvidenceRendered`: result appears as read-only local evidence without append/write/export.

Every missing or malformed state must fail closed.

## Explicit Non-Capabilities

- No implementation in this block.
- No approval state mutation.
- No product ledger append/write/export.
- No bounded export execution.
- No public UI action.
- No productive command handler exposure.
- No productive DI/service registration.
- No runtime enabled by default.
- No arbitrary path input.
- No provider/cloud/network.
- No telemetry/sync/billing cloud.
- No DB/migration.
- No KMS/WORM/external trust.
- No Browser/CDP/WCU/OCR/Recipes live execution.
- No release/commercial readiness.
- No compliance custody claim.

## Required Future Tests

- approval missing, stale, mismatched action and mismatched evidence fail closed;
- policy recheck failure blocks execution after approval;
- unsafe action casing/whitespace/raw text fails closed;
- export/write/destructive actions stay blocked;
- query/body/header path injection is ignored or rejected;
- route remains Development-only and internal;
- no form/script/onclick/formaction or executable public UI surface appears;
- execution result contains no raw sensitive payload;
- static scans prove no DB/cloud/KMS/live automation/release paths.

## Findings

P0: 0

P1: 0

P2: 0

P3:

- Approval execution is still design-only and has no executable implementation in this block.
- First future implementation must exclude bounded export/write even though bounded export exists elsewhere.
- Persisted approval state and durable approval evidence remain separate future design scopes.

P4:

- Local-only approval evidence is not a compliance-grade signature or WORM custody proof.
- In-memory execution results are operator readiness evidence, not a release/commercial claim.

TRUE_RISK: 0

## Decision

`GO_WITH_FINDINGS_LOCAL_APPROVAL_EXECUTION_DESIGN_ONLY_BOUNDARY_READY`

