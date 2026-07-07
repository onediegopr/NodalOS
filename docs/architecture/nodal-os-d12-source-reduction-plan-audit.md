# NODAL OS D12 Source Reduction Plan Audit

Date: 2026-07-07

Mode: audit-only / plan-only / docs-only. This block does not modify `src/`, tests, CI, runtime behavior, product behavior, service registration, routes, command handlers, UI actions, Product Ledger writer behavior, DB/provider/cloud/network, KMS/WORM/external trust, Browser/CDP/WCU/OCR/Recipes live behavior, release or commercial readiness.

Baseline HEAD: `0877743c50dcd4844b0eddd2ce4d04e14c28dd99`.

Decision: `GO_WITH_FINDINGS_SOURCE_REDUCTION_PLAN_AUDIT_ONLY_READY`.

## 1. Executive Verdict

D12 confirms that the D-series has proven isolation and equivalence, but has not reduced source bloat yet. D7 and D10 each added the same private common-boundary fail-closed proof pattern to a narrow non-runtime source target. The safe next move is not a third proof-only replacement. The safe next move is one minimal source reduction that removes repetition inside the newer D10 target while preserving the same private proof surface and all D4/D5/D7/D8/D10/D11 guards.

Selected next block:

`AUTHORIZE_NODAL_OS_BLOCK_D13_MINIMAL_SOURCE_REDUCTION_IMPLEMENTATION_NO_RUNTIME_CHANGE`.

D13 remains unauthorized until Diego explicitly starts that block. D12 does not implement it.

## 2. Current Source Shape

Current allowed candidate source references under `src/` remain limited to:

- `src/OneBrain.Core/Approval/NodalOsCommonBoundaryClaimsCandidate.cs`
- `src/OneBrain.Core/Approval/ReentryDecisionPacketReadOnly.cs`
- `src/OneBrain.Core/Approval/ApprovalExecutionDesignOnlyProtected.cs`

Current source line counts observed during D12:

| File | Current role | Lines |
| --- | --- | ---: |
| `src/OneBrain.Core/Approval/NodalOsCommonBoundaryClaimsCandidate.cs` | D4 source-side candidate, parallel-only/non-authoritative | 109 |
| `src/OneBrain.Core/Approval/ReentryDecisionPacketReadOnly.cs` | D7 read-only proof target | 524 |
| `src/OneBrain.Core/Approval/ApprovalExecutionDesignOnlyProtected.cs` | D10 design-only/protected proof target | 368 |

D7 and D10 both repeat:

- `CommonBoundaryClaimsRemainFailClosed()`;
- `CommonBoundaryClaimsRemainFailClosed(NodalOsCommonBoundaryClaimsCandidate candidate)`;
- 13 explicit `CommonBoundaryClaimRemainsFailClosed(...)` calls;
- the same expected claim/state table.

That repetition is useful as audit evidence, but now has enough test coverage to support one tightly bounded compaction.

## 3. Reduction Candidate Inventory

| Candidate | Decision | Risk | Expected benefit | Reason |
| --- | --- | --- | --- | --- |
| Compact D10 helper repetition in `ApprovalExecutionDesignOnlyProtected.cs` | Select for D13 | P3 | Approx. 30-45 source lines, exact count must be measured by D13 | Newer duplicated proof target, non-runtime, already covered by D10/D11 focused tests. |
| Compact D7 helper repetition in `ReentryDecisionPacketReadOnly.cs` | Defer | P3 | Similar line reduction | D7 is older/canonical reentry evidence; reduce D10 first and audit before touching D7. |
| Move common expected-claim table into `NodalOsCommonBoundaryClaimsCandidate.cs` | Reject for D13 | P2/P3 | More reuse | Touches the D4 candidate and can make the candidate look authoritative. |
| Add a shared public helper used by D7/D10 | Reject for D13 | P2/P3 | More reuse | Broadens source API and increases risk of future runtime adoption. |
| Delete D7/D10 proof-only checks | Reject | P2 | Line reduction | Would weaken fail-closed evidence and D5/D8/D11 guard meaning. |
| Replace Product Ledger contract families now | Reject for D13 | P2/P3 | Higher long-term benefit | Too broad for a first reduction and would mix source reduction with product-domain refactor. |
| Add a third proof-only replacement | Reject | P4/P3 | More equivalence evidence | Increases source bloat and delays actual reduction. |
| Reduce or delete tests | Reject | P1/P2 | Lower test count only | Forbidden by scope and unsafe for the current refactor line. |

## 4. Selected D13 Target

Future D13 may touch exactly one source file:

`src/OneBrain.Core/Approval/ApprovalExecutionDesignOnlyProtected.cs`.

Future D13 should preserve these private method surfaces unless the focused tests prove an equivalent private shape is still discoverable:

- `CommonBoundaryClaimsRemainFailClosed()`
- `CommonBoundaryClaimsRemainFailClosed(NodalOsCommonBoundaryClaimsCandidate candidate)`
- `CommonBoundaryClaimRemainsFailClosed(NodalOsCommonBoundaryClaimsCandidate candidate, NodalOsCommonBoundaryClaimsCandidate.Claim claim, NodalOsCommonBoundaryClaimsCandidate.ClaimState expectedState)`

Recommended implementation shape for D13:

- Introduce a private static expected-claims collection inside `ApprovalExecutionAntiCapabilityProof`.
- Replace the repeated `&& CommonBoundaryClaimRemainsFailClosed(...)` chain with an `All(...)` or equivalent loop.
- Keep every claim/state pair exactly equivalent to D10.
- Keep fail-closed behavior for null/missing/unknown/unsafe candidate states.
- Keep D4 candidate non-authoritative and parallel-only.
- Keep D7 unchanged in D13.

## 5. D13 Hard Constraints

D13 must not touch:

- `src/OneBrain.Core/Approval/NodalOsCommonBoundaryClaimsCandidate.cs`;
- `src/OneBrain.Core/Approval/ReentryDecisionPacketReadOnly.cs`;
- Product Ledger writer/path/latest-state/handoff/route/UI source files;
- Pilot routes, CLI, DI/service registration, command handlers or CI;
- tests, unless a test update is strictly required to preserve an existing guard after the one-file source compaction;
- docs beyond D13 QA/handoff/decision-log updates.

D13 must not introduce:

- new source candidate;
- new runtime/product authority;
- public/product exposure or Production route;
- latest pointer, active read precedence or product authority;
- DB/provider/cloud/network;
- KMS/WORM/external trust;
- Browser/CDP/WCU/OCR/Recipes live behavior;
- release/commercial readiness.

## 6. Required D13 Gates

Minimum D13 validation set:

```powershell
dotnet build src/OneBrain.Core/OneBrain.Core.csproj
dotnet build src/OneBrain.Pilot/OneBrain.Pilot.csproj
dotnet build OneBrain.slnx
dotnet test tests/OneBrain.Safety.Tests/OneBrain.Safety.Tests.csproj --no-build --filter "ProductLedger" -v:minimal
dotnet test tests/OneBrain.Recipes.Tests/OneBrain.Recipes.Tests.csproj --no-build --filter "ProductLedger" -v:minimal
dotnet test tests/OneBrain.Safety.Tests/OneBrain.Safety.Tests.csproj --no-build --filter "TestCategory=NodalOsTier1Safety" -v:minimal
dotnet test tests/OneBrain.Safety.Tests/OneBrain.Safety.Tests.csproj --no-build --filter "TestCategory=CommonContracts" -v:minimal
dotnet test tests/OneBrain.Safety.Tests/OneBrain.Safety.Tests.csproj --no-build --filter "TestCategory=SourceCandidate" -v:minimal
dotnet test tests/OneBrain.Safety.Tests/OneBrain.Safety.Tests.csproj --no-build --filter "TestCategory=NoRuntimeWiring" -v:minimal
dotnet test tests/OneBrain.Safety.Tests/OneBrain.Safety.Tests.csproj --no-build --filter "TestCategory=NoAuthority" -v:minimal
dotnet test tests/OneBrain.Safety.Tests/OneBrain.Safety.Tests.csproj --no-build --filter "TestCategory=NoDoubleTruth" -v:minimal
dotnet test tests/OneBrain.Safety.Tests/OneBrain.Safety.Tests.csproj --no-build --filter "TestCategory=PostReplacementAudit" -v:minimal
dotnet test tests/OneBrain.Safety.Tests/OneBrain.Safety.Tests.csproj --no-build --filter "TestCategory=ApprovalExecution" -v:minimal
dotnet test tests/OneBrain.Safety.Tests/OneBrain.Safety.Tests.csproj --no-build --filter "FullyQualifiedName~ApprovalExecutionDesignOnlyProtectedCommonBoundaryD10" -v:minimal
dotnet test tests/OneBrain.Safety.Tests/OneBrain.Safety.Tests.csproj --no-build --filter "FullyQualifiedName~ApprovalExecutionPostSecondReplacementD11" -v:minimal
rg -n "NodalOsCommonBoundaryClaimsCandidate" src -g "*.cs"
git diff --check
```

D13 should also re-run the D7/D10 command/execution exception scans and the added-line forbidden enablement scan before commit.

## 7. Rollback Plan

D13 rollback must be one-file and mechanical:

- revert the D13 change to `src/OneBrain.Core/Approval/ApprovalExecutionDesignOnlyProtected.cs`;
- keep D4, D7, D10 and D11 artifacts intact;
- rerun the same gates;
- record rollback in decision-log/handoff if needed.

Revert criteria:

- any P0/P1;
- any D4 candidate authority drift;
- any runtime/product, route, DI, command handler, CI or Product Ledger writer drift;
- any reduction that removes a required claim/state pair;
- any failed D10/D11 focused guard;
- any source reference broadening beyond D4/D7/D10.

## 8. Findings

P0: 0.

P1: 0.

P2: 0 new. D13 would become P2 if it touches the D4 candidate, deletes proof coverage or broadens source references.

P3:

- Actual source reduction is still pending; D7+D10 remain net additive.
- D13 must keep the D10 source compaction local to a private proof helper.
- The expected-claim list must remain exhaustive and exact.

P4:

- The D-series terminology now has enough history that docs must keep pointing future work away from a third additive proof-only replacement.

## 9. Percentages

- D12 source-reduction plan/audit: `100%` when this document and linked logs are committed.
- Actual source bloat reduction: `0%`.
- D13 planning readiness: `80%`; exact final readiness depends on local D13 validation.
- Common-boundary confidence: `98%` for the narrow D4/D7/D10/D11 line, not for broad Product Ledger refactor.
- Runtime/product enablement: `0%`.
- CI enforcement: `0%`.
- Release/commercial readiness: `0% / NO-GO`.

## 10. D12 Validation Plan And Result

D12 is docs-only. Required local validation for the D12 commit:

- Core build;
- Pilot build;
- solution build;
- Product Ledger Safety focused tests;
- Product Ledger Recipes focused tests;
- Tier 1, CommonContracts, SourceCandidate, NoRuntimeWiring, NoAuthority, NoDoubleTruth, PostReplacementAudit and ApprovalExecution category tests;
- StaticGuardCatalog, PublicProductBlock and ProductionRouteBlock focused tests;
- Reentry and ApprovalExecution focused Safety/Recipes tests;
- Safety and Recipes discovery counts;
- no-runtime reference scans;
- exact D4/D7/D10 source reference scan;
- D7/D10 command/runtime exception scan;
- added-line forbidden enablement scan;
- `git diff --check`;
- final `git status --short`;
- origin sync check.

Result: PASS.

- Core build: PASS, 0 warnings, 0 errors.
- Pilot build: PASS, 0 warnings, 0 errors.
- Solution build: PASS, 0 errors, inherited test/OCR/analyzer warnings only.
- Product Ledger Safety broad focused: PASS 275/275.
- Product Ledger Safety category-only guard: PASS 69/69.
- Product Ledger Recipes: PASS 72/72.
- Tier 1 Safety: PASS 127/127.
- CommonContracts: PASS 101/101.
- SourceCandidate: PASS 82/82.
- NoRuntimeWiring: PASS 101/101.
- NoAuthority: PASS 63/63.
- NoDoubleTruth: PASS 63/63.
- PostReplacementAudit: PASS 37/37.
- ApprovalExecution category: PASS 27/27.
- StaticGuardCatalog: PASS 9/9.
- PublicProductBlock: PASS 46/46.
- ProductionRouteBlock: PASS 39/39.
- Reentry Safety: PASS 28/28.
- Reentry Recipes: PASS 4/4.
- ApprovalExecution broad Safety: PASS 47/47.
- ApprovalExecution broad Recipes: PASS 13/13.
- ApprovalExecutionDesignOnlyProtected Safety: PASS 16/16.
- ApprovalExecutionDesignOnlyProtected Recipes: PASS 3/3.
- D10 focused: PASS 15/15.
- D11 focused: PASS 12/12.
- Safety discovery: 6469.
- Recipes discovery: 1580.
- Exact D4/D7/D10 source reference scan: PASS.
- Pilot/CLI/CI candidate reference scan: PASS.
- D7/D10 command-runtime exception scan: PASS.
- Added-line forbidden enablement scan: PASS.
- `git diff --check`: PASS.
- Changed source/test/CI file scan: PASS, no files.
- Origin sync before commit: `0 0`.

## 11. Next Recommended Step

`AUTHORIZE_NODAL_OS_BLOCK_D13_MINIMAL_SOURCE_REDUCTION_IMPLEMENTATION_NO_RUNTIME_CHANGE`.

Do not run `AUTHORIZE_NODAL_OS_BLOCK_D13_ADDITIONAL_REDUCTION_GUARD_HARDENING_ONLY` first unless D12 validation fails or the D13 one-file target cannot preserve the current D10/D11 guard surface.

Do not choose `STOP_FOR_AUDIT` unless a future auditor finds P0/P1/P2 drift.

Do not choose `C7_MORE_TIER1_LABEL_EXPANSION` unless D13 gate execution proves current labels are insufficient for the selected one-file compaction.
