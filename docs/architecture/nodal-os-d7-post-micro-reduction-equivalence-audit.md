# NODAL OS D7 Post Micro Reduction Equivalence Audit

Date: 2026-07-08

Mode: read-only / docs-only / audit-only / no-runtime / no-product.

Block: `AUTHORIZE_NODAL_OS_D7_POST_MICRO_REDUCTION_EQUIVALENCE_AUDIT_READ_ONLY`.

Baseline HEAD: `9f6e931d834b720b7b8e601241354620062e02fa`.

Decision: `GO_WITH_FINDINGS_D7_POST_MICRO_REDUCTION_EQUIVALENCE_AUDIT_READY`.

Resulting state: `D7_POST_MICRO_REDUCTION_EQUIVALENCE_AUDIT_READY`.

Stop condition: `STOP_AFTER_D7_POST_MICRO_REDUCTION_EQUIVALENCE_AUDIT_NO_RUNTIME_PRODUCT_AUTHORITY`.

## Executive Verdict

The D7 micro-reduction is equivalent for the audited scope.

The private `ExpectedFailClosedClaims` table in `src/OneBrain.Core/Approval/ReentryDecisionPacketReadOnly.cs` preserves the exact thirteen expected claim/state pairs, preserves the fail-closed checks, preserves no-authority behavior and does not create shared helper authority.

No source, tests, workflows or CI files were modified by this audit block.

## Equivalence Confirmation

Confirmed:

- `ExpectedFailClosedClaims` exists as a local private table in the D7 target.
- `CommonBoundaryClaim` and `CommonBoundaryClaimState` are local aliases.
- `CommonBoundaryClaimsRemainFailClosed()` still exists.
- `CommonBoundaryClaimsRemainFailClosed(NodalOsCommonBoundaryClaimsCandidate candidate)` still exists.
- `CommonBoundaryClaimRemainsFailClosed(...)` still exists.
- No helper global or shared contract was created.
- The previous implementation commit touched only one source file: `src/OneBrain.Core/Approval/ReentryDecisionPacketReadOnly.cs`.

Preserved fail-closed checks:

- `candidate.ParallelOnly`;
- `candidate.NonAuthoritative`;
- `!candidate.ExistingHardBlockAuthorityReplaced`;
- `!candidate.AllowsRuntimeProductOrAuthority()`;
- `candidate.StateFor(claim) == expectedState`;
- `candidate.IsFailClosed(claim)`;
- `!candidate.CanOverrideExistingHardBlock(claim)`.

## Exact Pair Audit

Automated pair scan result:

`PAIR_EXACT_MATCH_PASS`

| Claim | Expected state |
| --- | --- |
| `PublicProductBlocked` | `Blocked` |
| `ProductionRouteBlocked` | `Blocked` |
| `LatestPointerDisabled` | `Disabled` |
| `ReadPrecedenceDisabled` | `Disabled` |
| `ProductAuthorityBlocked` | `Blocked` |
| `CommandExecutionDenied` | `Denied` |
| `ShellSubprocessDenied` | `Denied` |
| `ProviderCloudNetworkNotClaimed` | `NotClaimed` |
| `DatabaseMigrationNotClaimed` | `NotClaimed` |
| `ExternalTrustNotClaimed` | `NotClaimed` |
| `ReleaseCommercialNoGo` | `NoGo` |
| `RuntimeProductEnablementNoGo` | `NoGo` |
| `CiEnforcementNotClaimed` | `NotClaimed` |

Pair count: `13`.

Missing pairs: none.

Extra pairs: none.

Renamed claims/states: none.

## Runner Hang Assessment

The prior block recorded that a broad Safety filter, `FullyQualifiedName~ReentryDecisionPacketReadOnly`, hung without output and was stopped.

This audit observed the same class of runner sensitivity on a D8 minimal-output invocation: the narrow D8 run with `-v:minimal` timed out, while the same D8 class with `-v:normal` passed `10/10` in `12.7057` seconds.

Assessment:

- This remains a P3 local runner/filter observability issue, not evidence of D7 equivalence failure.
- Focused class/category runs cover the D7/D8/no-authority/no-double-truth/no-runtime surface for this block.
- Avoid broad or silent filters on this lane until test-runner behavior is separately investigated.
- No test infrastructure or CI was changed in this block.

## Validation Evidence

- `dotnet build src/OneBrain.Core/OneBrain.Core.csproj --no-restore -v:minimal`: PASS, 0 warnings, 0 errors.
- `dotnet test tests/OneBrain.Safety.Tests/OneBrain.Safety.Tests.csproj --no-restore --no-build --filter "FullyQualifiedName~ReentryDecisionPacketReadOnlyCommonBoundaryD7Tests" -v:minimal`: PASS, 12/12.
- `dotnet test tests/OneBrain.Safety.Tests/OneBrain.Safety.Tests.csproj --no-restore --no-build --filter "FullyQualifiedName~ReentryDecisionPacketReadOnlyPostReplacementD8Tests" -v:normal`: PASS, 10/10.
- `dotnet test tests/OneBrain.Safety.Tests/OneBrain.Safety.Tests.csproj --no-restore --no-build --filter "FullyQualifiedName~ReentryDecisionPacketReadOnlySafetyTests" -v:minimal`: PASS, 6/6.
- `dotnet test tests/OneBrain.Recipes.Tests/OneBrain.Recipes.Tests.csproj --no-restore --no-build --filter "FullyQualifiedName~ReentryDecisionPacketReadOnlyTests" -v:minimal`: PASS, 4/4.
- `dotnet test tests/OneBrain.Safety.Tests/OneBrain.Safety.Tests.csproj --no-restore --no-build --filter "FullyQualifiedName~NodalOsStaticGuardCatalogTests" -v:minimal`: PASS, 9/9.
- `dotnet test tests/OneBrain.Safety.Tests/OneBrain.Safety.Tests.csproj --no-restore --no-build --filter "TestCategory=NoAuthority" -v:minimal`: PASS, 71/71.
- `dotnet test tests/OneBrain.Safety.Tests/OneBrain.Safety.Tests.csproj --no-restore --no-build --filter "TestCategory=NoDoubleTruth" -v:minimal`: PASS, 71/71.
- `dotnet test tests/OneBrain.Safety.Tests/OneBrain.Safety.Tests.csproj --no-restore --no-build --filter "TestCategory=NoRuntimeWiring" -v:minimal`: PASS, 109/109.
- `git diff --check`: PASS.
- Origin divergence check: PASS, `0 0`.
- Anti-overclaim diff scan: PASS.
- Anti-scope context scan: PASS.
- Dotnet residual process scan: PASS.

## Findings

P0: 0.

P1: 0.

P2: 0.

P3:

- Broad or silent `dotnet test` filters can hang locally on this lane; focused class/category filters passed and should remain the preferred validation pattern until runner behavior is separately investigated.

P4:

- D7 and D10 now use parallel local table shapes. This is acceptable intentional local duplication and not a shared authority helper.

## Percentages

- D7 selected-target readiness: `100%`.
- Source-refactor readiness: `77%`.
- Broad source simplification readiness: `45%`.
- Static guard catalog readiness: `92%`.
- Common contracts confidence: `98%` design/test-only, `0%` runtime authority.
- Runtime/product enablement: `0%`.
- CI enforcement: `0%`.
- Release/commercial: `0% / NO-GO`.

## Not Opened

- No `src/` changes in this audit block.
- No tests added.
- No runtime/product.
- No public/product.
- No Production route.
- No latest pointer.
- No read precedence.
- No product authority.
- No DB/cloud/network/provider.
- No KMS/WORM/external trust.
- No DI/service registration.
- No command handlers.
- No CI enforcement.
- No release/commercial.
- No shared helper extraction.
- No broad common-contract refactor.
- No Product Ledger/model consolidation.

## Next Step

Recommended next macro-block:

`NODAL_OS_SOURCE_REFACTOR_MICRO_LANE_CLOSEOUT_AND_NEXT_SAFE_GATE_SELECTION_AUDIT_ONLY`

Purpose:

Close the D7/D13 micro-reduction lane as validated, reconcile current source-refactor readiness, and select whether the next safe move is another bounded audit-only target, runner-hang investigation docs/test-only, or a pause back to the main roadmap.
