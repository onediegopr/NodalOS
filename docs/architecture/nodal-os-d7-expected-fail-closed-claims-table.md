# NODAL OS D7 Expected Fail Closed Claims Table

Date: 2026-07-08

Mode: source-minimal / test-only / no-runtime / no-product / D7-selected-target-only.

Block: `AUTHORIZE_NODAL_OS_D7_PROOF_CHAIN_SELECTED_MICRO_REDUCTION_TEST_ONLY`.

Baseline HEAD: `2448e6979594aac81c5aacdf23b737cf44abe4a6`.

Decision: `GO_WITH_FINDINGS_D7_EXPECTED_FAIL_CLOSED_CLAIMS_TABLE_READY`.

Resulting state: `D7_EXPECTED_FAIL_CLOSED_CLAIMS_TABLE_READY`.

Stop condition: `STOP_AFTER_D7_EXPECTED_FAIL_CLOSED_CLAIMS_TABLE_READY_NO_RUNTIME_PRODUCT_AUTHORITY`.

## Scope

Implemented the selected D7 micro-target:

`D7_EXPECTED_FAIL_CLOSED_CLAIMS_TABLE`

Authorized source file changed:

`src/OneBrain.Core/Approval/ReentryDecisionPacketReadOnly.cs`

No other `src/` file changed. No tests, workflows or CI files changed.

## What Changed

- Added local aliases for `NodalOsCommonBoundaryClaimsCandidate.Claim` and `ClaimState`.
- Added a private `ExpectedFailClosedClaims` table inside `ReentryDecisionPacketReadOnly`.
- Replaced the repeated thirteen-call `CommonBoundaryClaimRemainsFailClosed(...)` chain with `ExpectedFailClosedClaims.All(...)`.
- Preserved `CommonBoundaryClaimsRemainFailClosed()` and `CommonBoundaryClaimRemainsFailClosed(...)` helper names.
- Preserved all fail-closed checks and no-authority checks.
- Reduced `ReentryDecisionPacketReadOnly.cs` from 493 to 461 lines, net `-32`.

## Preserved Claim/State Pairs

The D7 table preserves exactly:

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

## Preserved Fail-Closed Checks

The implementation preserves:

- `candidate.ParallelOnly`;
- `candidate.NonAuthoritative`;
- `!candidate.ExistingHardBlockAuthorityReplaced`;
- `!candidate.AllowsRuntimeProductOrAuthority()`;
- `candidate.StateFor(claim) == expectedState`;
- `candidate.IsFailClosed(claim)`;
- `!candidate.CanOverrideExistingHardBlock(claim)`.

## Validation Evidence

- `dotnet build src/OneBrain.Core/OneBrain.Core.csproj --no-restore -v:minimal`: PASS, 0 warnings, 0 errors.
- `dotnet test tests/OneBrain.Safety.Tests/OneBrain.Safety.Tests.csproj --no-restore --no-build --filter "FullyQualifiedName~ReentryDecisionPacketReadOnlyCommonBoundaryD7Tests" -v:normal`: PASS, 12/12.
- `dotnet test tests/OneBrain.Safety.Tests/OneBrain.Safety.Tests.csproj --no-restore --no-build --filter "FullyQualifiedName~ReentryDecisionPacketReadOnlyPostReplacementD8Tests" -v:normal`: PASS, 10/10.
- `dotnet test tests/OneBrain.Safety.Tests/OneBrain.Safety.Tests.csproj --no-restore --no-build --filter "FullyQualifiedName~ReentryDecisionPacketReadOnlySafetyTests" -v:normal`: PASS, 6/6.
- `dotnet test tests/OneBrain.Recipes.Tests/OneBrain.Recipes.Tests.csproj --no-restore --no-build --filter "FullyQualifiedName~ReentryDecisionPacketReadOnlyTests" -v:normal`: PASS, 4/4.
- `dotnet test tests/OneBrain.Safety.Tests/OneBrain.Safety.Tests.csproj --no-restore --no-build --filter "FullyQualifiedName~NodalOsStaticGuardCatalogTests" -v:normal`: PASS, 9/9.
- `dotnet test tests/OneBrain.Safety.Tests/OneBrain.Safety.Tests.csproj --no-restore --no-build --filter "TestCategory=NoAuthority" -v:minimal`: PASS, 71/71.
- `dotnet test tests/OneBrain.Safety.Tests/OneBrain.Safety.Tests.csproj --no-restore --no-build --filter "TestCategory=NoDoubleTruth" -v:minimal`: PASS, 71/71.
- `git diff --check`: PASS.
- Changed-file scope scan: PASS; only the authorized D7 source file plus continuity docs changed.
- Anti-overclaim diff scan: PASS.
- Anti-scope context scan: PASS.
- Expected table pair count scan: PASS, exactly 13 claim/state pairs.

One broader `FullyQualifiedName~ReentryDecisionPacketReadOnly` Safety run hung without output and was stopped. The narrower class/category runs above passed and cover the selected D7/D8/no-authority/no-double-truth surface.

## Findings

P0: 0.

P1: 0.

P2: 0.

P3:

- A broad Safety filter can hang in this local environment; use focused class/category filters for this lane unless the broader test runner behavior is separately investigated.

P4:

- The D7 and D10 proof targets now use parallel local table shapes. This is intentional local duplication, not shared helper extraction.

## Percentages

- D7 selected-target readiness: `100%` for this micro-target.
- Source-refactor readiness: `76%`.
- Broad source simplification readiness: `45%`.
- Static guard catalog readiness: `92%`.
- Common contracts confidence: `98%` design/test-only, `0%` runtime authority.
- Runtime/product enablement: `0%`.
- CI enforcement: `0%`.
- Release/commercial: `0% / NO-GO`.

## Not Opened

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

`NODAL_OS_D7_POST_MICRO_REDUCTION_EQUIVALENCE_AUDIT_READ_ONLY`

Purpose:

Read-only audit the D7 table reduction against D7/D8/D10/D13 evidence, confirm no double-truth or authority drift, then decide whether to close this source-refactor micro-lane or select another bounded docs/test-only gate.
