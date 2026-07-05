# Product Ledger Local Approval Execution Read-Only In-Memory Candidate

Date: 2026-07-05

Decision: `GO_WITH_FINDINGS_LOCAL_APPROVAL_EXECUTION_READ_ONLY_IN_MEMORY_CANDIDATE_READY`

## Scope

This block implements the first local-only/internal-only approval execution candidate for Product Ledger. It is default-off by construction, Core-only, fail-closed and limited to read-only/in-memory internal command results.

It does not add route wiring, public UI actions, productive command handlers, DI/service registration, approval persistence, append/write/export, DB/cloud/KMS/live automation or release/commercial behavior.

## Implemented

- `ProductLedgerLocalApprovalExecutionCandidate`.
- Request/result/decision/blocker models.
- Narrow approval-execution command allowlist.
- Fresh approval validation.
- action/evidence binding.
- post-approval policy recheck gate.
- verified read-model gate.
- authority blockers for public UI, product command handler, service registration, path input, raw payload, write/export, external/provider/DB/KMS/live/release claims.
- Safety and Recipes tests.

## Allowed Commands

- `ViewDiagnostics`
- `ViewLedgerReadiness`
- `ViewRuntimeGateStatus`
- `ViewCheckpointHeadStatus`
- `ViewEvidenceGates`
- `StaticScanPreview`
- `RequestExternalAuditPreview`

`LocalReportPhysicalExportBoundedInternal` remains excluded from this candidate.

## Explicit Non-Capabilities

- No route wiring.
- No public UI action.
- No productive command handler exposure.
- No productive DI/service registration.
- No approval state persistence.
- No append/write/export.
- No bounded export.
- No arbitrary path input.
- No provider/cloud/network.
- No DB/migration.
- No KMS/WORM/external trust.
- No Browser/CDP/WCU/OCR/Recipes live execution.
- No release/commercial readiness.

## Findings

P0: 0

P1: 0

P2: 0

P3:

- Candidate is Core-only and not rendered on the route yet.
- Approval token/state is caller-provided and not persisted.
- Route preview evidence and audit read-only remain future safe blocks.

P4:

- In-memory command results are local operator evidence, not compliance custody.
- The candidate delegates to existing internal router/handler and inherits their local-only preview semantics.

TRUE_RISK: 0

## Decision

`GO_WITH_FINDINGS_LOCAL_APPROVAL_EXECUTION_READ_ONLY_IN_MEMORY_CANDIDATE_READY`

