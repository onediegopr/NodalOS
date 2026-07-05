# QA Report - Local Approval Execution Route Negative Static Scan Hardening

Date: 2026-07-05

Decision: `GO_WITH_FINDINGS_LOCAL_APPROVAL_EXECUTION_ROUTE_NEGATIVE_STATIC_SCAN_HARDENING_READY`

## Summary

This block adds path-specific Safety static scans for the Product Ledger local approval execution route evidence path.

## Validations

- `dotnet test tests/OneBrain.Safety.Tests/OneBrain.Safety.Tests.csproj --no-restore --filter "FullyQualifiedName~ProductLedgerLocalApprovalExecutionRouteStaticScanTests"`: PASS, 3/3.
- `dotnet test tests/OneBrain.Safety.Tests/OneBrain.Safety.Tests.csproj --no-restore --no-build --filter "FullyQualifiedName~ProductLedger"`: PASS, 182/182.
- `dotnet test tests/OneBrain.Recipes.Tests/OneBrain.Recipes.Tests.csproj --no-restore --no-build --filter "FullyQualifiedName~ProductLedger"`: PASS, 53/53.
- JSON validation: PASS.
- `git diff --check`: PASS.
- Changed-file static scan: PASS; changed code limited to Safety static scan tests plus docs.

## Findings

P0: 0

P1: 0

P2: 0

P3:

- Static scan is source-fragment based and should remain paired with behavioral tests.
- Persisted approval state remains future work.

P4:

- The scan intentionally classifies unrelated Pilot routes out of scope.

TRUE_RISK: 0

## Next Safe Block

Recommended next macro-block: `NODAL_OS_LOCAL_APPROVAL_EXECUTION_OPERATOR_ACCEPTANCE_TEST_ONLY`.
