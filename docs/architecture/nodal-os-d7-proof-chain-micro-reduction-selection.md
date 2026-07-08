# NODAL OS D7 Proof Chain Micro Reduction Selection

Date: 2026-07-08

Mode: docs-only / read-only / audit-only / D7-selection-only.

Block: `AUTHORIZE_NODAL_OS_D7_PROOF_CHAIN_MICRO_REDUCTION_SELECTION_AUDIT_ONLY`.

Baseline HEAD: `2a08b8888dfe4ad5c350a0dd89b653bd57ec3052`.

Decision: `GO_WITH_FINDINGS_D7_PROOF_CHAIN_MICRO_REDUCTION_SCOPE_SELECTED_READY`.

Resulting state: `D7_PROOF_CHAIN_MICRO_REDUCTION_SCOPE_SELECTED_NO_IMPLEMENTATION`.

Stop condition: `STOP_FOR_OPERATOR_DECISION_ON_D7_PROOF_CHAIN_MICRO_REDUCTION_SCOPE`.

## Executive Selection

Selected future micro-target:

`D7_EXPECTED_FAIL_CLOSED_CLAIMS_TABLE`

Recommended next implementation block:

`NODAL_OS_D7_PROOF_CHAIN_SELECTED_MICRO_REDUCTION_TEST_ONLY`

This block selects the scope only. It does not implement, does not touch `src/`, does not add tests, does not authorize runtime/product, does not authorize CI enforcement and does not authorize release/commercial.

## Read-Only Source Evidence

The D7 target is:

`src/OneBrain.Core/Approval/ReentryDecisionPacketReadOnly.cs`

The audited proof-chain area is `CommonBoundaryClaimsRemainFailClosed(NodalOsCommonBoundaryClaimsCandidate candidate)` plus the private `CommonBoundaryClaimRemainsFailClosed(...)` helper.

Current D7 shape:

- `PassesSafetyProof` still calls `CommonBoundaryClaimsRemainFailClosed()`.
- D7 still uses `NodalOsCommonBoundaryClaimsCandidate.DefaultBlocked()` as a private local fail-closed proof.
- The target keeps the D7 post-replacement guard role and remains read-only/non-runtime.
- The method repeats thirteen `CommonBoundaryClaimRemainsFailClosed(...)` calls.
- The claim/state pairs match the D10/D13 expected fail-closed proof set:
  `PublicProductBlocked/Blocked`, `ProductionRouteBlocked/Blocked`, `LatestPointerDisabled/Disabled`, `ReadPrecedenceDisabled/Disabled`, `ProductAuthorityBlocked/Blocked`, `CommandExecutionDenied/Denied`, `ShellSubprocessDenied/Denied`, `ProviderCloudNetworkNotClaimed/NotClaimed`, `DatabaseMigrationNotClaimed/NotClaimed`, `ExternalTrustNotClaimed/NotClaimed`, `ReleaseCommercialNoGo/NoGo`, `RuntimeProductEnablementNoGo/NoGo`, `CiEnforcementNotClaimed/NotClaimed`.

## Candidate Matrix

| Candidate | Exact location | Proposed future change | Expected reduction | Risk | Double-truth risk | Runtime/product impact | Tests likely required | Recommendation |
| --- | --- | --- | ---: | --- | --- | --- | --- | --- |
| `D7_EXPECTED_FAIL_CLOSED_CLAIMS_TABLE` | `src/OneBrain.Core/Approval/ReentryDecisionPacketReadOnly.cs`; `CommonBoundaryClaimsRemainFailClosed(NodalOsCommonBoundaryClaimsCandidate candidate)` and helper area | Add local aliases plus a private expected claim/state table mirroring the D13/D10 pattern, then replace the repeated thirteen-call chain with `ExpectedFailClosedClaims.All(...)` while preserving helper names and fail-closed checks | `25-45` lines | P3 | Low/medium if the exact thirteen pairs and helper behavior are preserved | None allowed | Core build; Reentry Safety focused; Reentry Recipes focused; D7/D8 focused; `TestCategory=NoAuthority`; `TestCategory=NoDoubleTruth`; StaticGuardCatalog; exact D4/D7/D10 source reference scan; `git diff --check` | Selected |
| `D7_ALIAS_ONLY_CLEANUP` | Same helper signature area | Use local aliases for claim/state parameter types only | `0-2` lines | P4 | Low | None allowed | Same focused D7 safety checks if implemented | Defer; too little value |
| `D7_SHARED_HELPER_EXTRACTION` | D4 candidate plus D7/D10 proof targets | Extract a common proof-chain helper shared across proof targets | Higher possible reduction | P2/P3 | High; risks turning parallel proof evidence into shared authority | None allowed, but risk is too high | Broad CommonContracts, NoAuthority, NoDoubleTruth, Product Ledger gates | Reject as next |
| `D7_PRESENTER_OR_LABEL_CLEANUP` | D7 presenter/read-only labels | Deduplicate strings or presentation labels | Unclear | P3 | Medium if output/audit labels drift | None allowed | Reentry Safety/Recipes and snapshot-equivalent checks | Defer; not the proof-chain target |
| `DOCS_ONLY_STALE_LINK_CLEANUP` | Readiness/roadmap docs | Clean historical stale recommendations | No source reduction | P4 | None | None | `git diff --check`, overclaim scan | Defer; lower value |
| `NO_SAFE_D7_MICRO_REDUCTION_FOUND` | N/A | Pause D7 and return to roadmap | No reduction | Low | None | None | None | Not selected because a bounded target exists |

## Why The Selected Target Is Bounded

The selected future target is one private proof-chain compaction inside the existing D7 file. It keeps the D7 proof non-authoritative, fail-closed and local to the same fixture evidence path.

The future implementation must preserve:

- the exact thirteen claim/state pairs;
- `candidate.ParallelOnly`;
- `candidate.NonAuthoritative`;
- `!candidate.ExistingHardBlockAuthorityReplaced`;
- `!candidate.AllowsRuntimeProductOrAuthority()`;
- `candidate.StateFor(claim) == expectedState`;
- `candidate.IsFailClosed(claim)`;
- `!candidate.CanOverrideExistingHardBlock(claim)`;
- private helper names or reflection-discoverable intent, unless focused tests prove equivalent discoverability.

## Next Block Contract

Name:

`NODAL_OS_D7_PROOF_CHAIN_SELECTED_MICRO_REDUCTION_TEST_ONLY`

Objective:

Implement the selected one-file D7 expected-claims-table micro-reduction, if and only if the exact fail-closed proof behavior is preserved and focused tests/scans stay green.

Allowed scope:

- one-file source-minimal cleanup in `src/OneBrain.Core/Approval/ReentryDecisionPacketReadOnly.cs`;
- focused tests only if needed to preserve D7/D8 proof/equivalence evidence;
- docs/decision/handoff updates;
- no-runtime / no-product / no-release.

Blocked scope:

- no D4 candidate source change;
- no D10 source change;
- no Product Ledger source/model consolidation;
- no broad common-contract refactor;
- no route/DI/service registration;
- no command handlers;
- no public/product or Production route;
- no latest pointer;
- no read precedence;
- no product authority;
- no Product Ledger writer/runtime real;
- no DB/cloud/network/provider;
- no KMS/WORM/external trust;
- no CI enforcement;
- no release/commercial.

Minimum future validation:

- `dotnet build src/OneBrain.Core/OneBrain.Core.csproj --no-restore -v:minimal`
- Reentry Safety focused
- Reentry Recipes focused
- D7 focused tests
- D8/PostReplacementAudit focused tests
- `TestCategory=NoAuthority`
- `TestCategory=NoDoubleTruth`
- `FullyQualifiedName~NodalOsStaticGuardCatalogTests`
- exact D4/D7/D10 source reference scan
- `git diff --check`
- changed-file scan proving no route/DI/handler/runtime/product/CI/release scope

NO-GO conditions:

- cannot preserve the exact thirteen claim/state pairs;
- weakens fail-closed checks;
- weakens reflection/audit discoverability;
- requires touching D4, D10, Product Ledger, shared contracts, routes, DI, command handlers, CI or workflows;
- any public/product, Production route, latest pointer, read precedence or product authority claim appears;
- any provider/cloud/network/DB/KMS/WORM/release/commercial change appears;
- P0/P1/P2 or TRUE_RISK.

## Findings

P0: 0.

P1: 0.

P2: 0.

P3:

- D7 has a concrete local proof-chain reduction opportunity, but it remains older/canonical evidence and should only move through a focused test-only implementation block.
- The selected target must preserve exact fail-closed claim/state semantics; a table typo would be the main future risk.
- A shared helper extraction would be higher-value on lines but too risky as the next move because it could blur parallel evidence into shared authority.

P4:

- Alias-only cleanup is safe but too small to justify a separate implementation block.
- Docs-only stale-link cleanup remains lower value than resolving the D7 proof-chain question.

## Percentages

- D7 micro-reduction scope confidence: `82%`.
- D7 selected-target implementation readiness: `76%`.
- Source-refactor readiness: `75%`.
- Broad source simplification readiness: `45%`.
- Static guard catalog readiness: `92%`.
- Common contracts confidence: `98%` design/test-only, `0%` runtime authority.
- Runtime/product enablement: `0%`.
- CI enforcement: `0%`.
- Release/commercial: `0% / NO-GO`.

## Stop Condition

`STOP_FOR_OPERATOR_DECISION_ON_D7_PROOF_CHAIN_MICRO_REDUCTION_SCOPE`

This selection does not authorize source implementation, runtime/product, CI enforcement, release/commercial or external audit approval.
