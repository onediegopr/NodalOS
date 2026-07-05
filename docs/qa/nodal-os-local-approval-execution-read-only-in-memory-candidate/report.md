# QA Report - Local Approval Execution Read-Only In-Memory Candidate

Date: 2026-07-05

Decision: `GO_WITH_FINDINGS_LOCAL_APPROVAL_EXECUTION_READ_ONLY_IN_MEMORY_CANDIDATE_READY`

## Summary

This block adds a Core-only local approval execution candidate that executes only read-only/in-memory internal commands after explicit fresh approval, action/evidence binding, policy recheck and verified read-model evidence.

## Tests Added

- `ProductLedgerLocalApprovalExecutionCandidateTests`
- `ProductLedgerLocalApprovalExecutionCandidateRecipeTests`

## Validations

- `dotnet test tests/OneBrain.Safety.Tests/OneBrain.Safety.Tests.csproj --no-restore --filter "FullyQualifiedName~ProductLedgerLocalApprovalExecutionCandidateTests"`: PASS, 5/5.
- `dotnet test tests/OneBrain.Recipes.Tests/OneBrain.Recipes.Tests.csproj --no-restore --filter "FullyQualifiedName~ProductLedgerLocalApprovalExecutionCandidateRecipeTests"`: PASS, 2/2.
- `dotnet test tests/OneBrain.Safety.Tests/OneBrain.Safety.Tests.csproj --no-restore --no-build --filter "FullyQualifiedName~ProductLedger"`: PASS, 181/181.
- `dotnet test tests/OneBrain.Recipes.Tests/OneBrain.Recipes.Tests.csproj --no-restore --no-build --filter "FullyQualifiedName~ProductLedger"`: PASS, 53/53.
- `dotnet build OneBrain.slnx --no-restore -v:minimal`: PASS, 0 warnings, 0 errors.
- JSON validation: PASS.
- `git diff --check`: PASS.
- Changed-file static scan: PASS; changed code limited to Core approval candidate and Safety/Recipes tests plus docs.

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

## Boundary Confirmation

- No route wiring.
- No public UI action.
- No productive command handler.
- No productive DI/service registration.
- No approval state persistence.
- No append/write/export.
- No bounded export.
- No arbitrary path input.
- No provider/cloud/network.
- No DB/migration.
- No KMS/WORM/external trust.
- No Browser/CDP/WCU/OCR/Recipes live.
- No release/commercial readiness.

## Percentages

- Product Ledger local-only core: 94-96%.
- Approval/Human Review: 93-96%.
- Evidence/Timeline/Audit Trail: 84-90%.
- Runtime/Command/Execution: 54-62%.
- UI/Operator Surface: 55-65%.
- Local-only internal product: 68-76%.
- Usable end-to-end local product: 42-52%.
- External/cloud: 0%.
- Release/commercial: 0%.

## Next Safe Block

Recommended next macro-block: `NODAL_OS_LOCAL_APPROVAL_EXECUTION_ROUTE_PREVIEW_EVIDENCE_TEST_ONLY`.
