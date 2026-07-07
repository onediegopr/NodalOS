# NODAL OS D13 Minimal Source Reduction Implementation

Date: 2026-07-07

Mode: source-minimal / reduction-only / no-runtime-behavior-change / no-product-enablement.

Baseline HEAD: `439c69eb5a5b3f984efd507063e88884c735d532`.

Decision: `GO_WITH_FINDINGS_MINIMAL_SOURCE_REDUCTION_NO_RUNTIME_CHANGE_READY`.

## 1. Scope

D13 implements the D12-selected minimal reduction in exactly one source file:

`src/OneBrain.Core/Approval/ApprovalExecutionDesignOnlyProtected.cs`.

D13 does not modify:

- `src/OneBrain.Core/Approval/NodalOsCommonBoundaryClaimsCandidate.cs`;
- `src/OneBrain.Core/Approval/ReentryDecisionPacketReadOnly.cs`;
- any other `src/` file;
- tests;
- CI;
- routes, DI/service registration or command handlers;
- Product Ledger runtime/path/latest-state/handoff/writer behavior;
- public/product surfaces;
- DB/provider/cloud/network;
- KMS/WORM/external trust;
- Browser/CDP/WCU/OCR/Recipes live behavior;
- release/commercial readiness.

## 2. Implemented Reduction

The D10 target had 13 repeated private calls to `CommonBoundaryClaimRemainsFailClosed(...)`, one for each expected common-boundary claim/state pair.

D13 replaces that repeated chain with:

- private aliases for the D4 candidate claim/state nested types;
- one private static expected-claims table inside `ApprovalExecutionAntiCapabilityProof`;
- one `ExpectedFailClosedClaims.All(...)` loop that delegates to the unchanged private claim/state helper.

Preserved private helpers:

- `CommonBoundaryClaimsRemainFailClosed()`;
- `CommonBoundaryClaimsRemainFailClosed(NodalOsCommonBoundaryClaimsCandidate candidate)`;
- `CommonBoundaryClaimRemainsFailClosed(NodalOsCommonBoundaryClaimsCandidate candidate, NodalOsCommonBoundaryClaimsCandidate.Claim claim, NodalOsCommonBoundaryClaimsCandidate.ClaimState expectedState)`.

Preserved behavior:

- design-only/read-only/preview-only fixture state;
- public/product blocked;
- Production route blocked;
- latest pointer disabled;
- read precedence disabled;
- product authority blocked;
- command execution denied;
- shell/subprocess denied;
- provider/cloud/network, DB and external trust not claimed;
- release/commercial no-go;
- runtime/product enablement no-go;
- CI enforcement not claimed;
- unknown/missing/unsafe candidate states fail closed;
- D4 candidate remains non-authoritative and parallel-only.

## 3. Source Bloat Trajectory

Measured line counts:

| Window | File | Net impact |
| --- | --- | ---: |
| D7 | `src/OneBrain.Core/Approval/ReentryDecisionPacketReadOnly.cs` | `+70` |
| D10 | `src/OneBrain.Core/Approval/ApprovalExecutionDesignOnlyProtected.cs` | `+70` |
| D13 | `src/OneBrain.Core/Approval/ApprovalExecutionDesignOnlyProtected.cs` | `-30` |

Target file line count:

- before D13: 368 lines;
- after D13: 338 lines;
- D13 diff: `+22 / -52`, net `-30`.

Cumulative D7+D10+D13 source impact: `+110` lines.

Honest status: D13 achieved a real local reduction in the selected D10 target, but the cumulative D-series remains net additive. Actual broad source bloat reduction remains partial, not complete.

## 4. Guard Status

Preserved:

- D4 candidate source unchanged;
- D7 target source unchanged;
- no new source candidate;
- no third replacement;
- no public API change;
- no test deletion or assertion weakening;
- no CI change;
- D10 command/execution exception remains file-exact;
- D7 and D10 exceptions remain independent;
- existing hard-block tests remain authoritative;
- D1/D2 remain design/test-only;
- Tier 1 remains manual/discovery-only.

## 5. Validation Result

Initial post-change smoke:

- Core build: PASS, 0 warnings, 0 errors.
- ApprovalExecutionDesignOnlyProtected Safety: PASS 16/16.
- ApprovalExecutionDesignOnlyProtected Recipes: PASS 3/3.
- D10 focused: PASS 15/15.
- D11 focused: PASS 12/12.

Full D13 validation result: PASS.

- Core build: PASS, 0 warnings, 0 errors.
- Pilot build: PASS, 0 warnings, 0 errors.
- Solution build: PASS, 0 errors, inherited test/OCR/analyzer warnings only.
- Product Ledger Safety broad focused: PASS 275/275.
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
- ApprovalExecutionDesignOnlyProtected Safety: PASS 16/16.
- ApprovalExecutionDesignOnlyProtected Recipes: PASS 3/3.
- D10 focused: PASS 15/15.
- D11 focused: PASS 12/12.
- Safety discovery: 6469.
- Recipes discovery: 1580.
- Exact D4/D7/D10-D13 source reference scan: PASS.
- Pilot/CLI/CI candidate reference scan: PASS.
- D7/D10-D13 command-runtime exception scan: PASS.
- Added-line forbidden enablement scan: PASS.
- `git diff --check`: PASS.
- Source scope scan: PASS, only `src/OneBrain.Core/Approval/ApprovalExecutionDesignOnlyProtected.cs` changed under `src/`.
- Tests changed: none.
- CI changed: none.
- Origin sync before commit: `0 0`.

## 6. Findings

P0: 0.

P1: 0.

P2: 0.

P3:

- D13 reduces only D10 private proof repetition; it does not address broad Product Ledger contract bloat.
- The D-series remains net additive by `+110` source lines even after D13.

P4:

- Future docs and handoffs must distinguish local D13 reduction from full architecture simplification.

## 7. Next Recommended Step

`D14 post-source-reduction isolation/equivalence audit`.

Do not continue directly to another reduction implementation before auditing D13.
