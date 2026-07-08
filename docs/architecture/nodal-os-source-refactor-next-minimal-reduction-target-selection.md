# NODAL OS Source Refactor Next Minimal Reduction Target Selection

Date: 2026-07-08

Mode: docs-only / read-only / audit-only / target-selection-only.

Block: `AUTHORIZE_NODAL_OS_SOURCE_REFACTOR_NEXT_MINIMAL_REDUCTION_TARGET_SELECTION_AUDIT_ONLY`.

Baseline HEAD: `ae9cc990c69a40d2a1f24337b57764d473f43297`.

Decision: `GO_WITH_FINDINGS_SOURCE_REFACTOR_MINIMAL_TARGET_SELECTED_READY`.

Resulting state: `SOURCE_REFACTOR_NEXT_MINIMAL_REDUCTION_TARGET_SELECTED_NO_IMPLEMENTATION`.

Stop condition: `STOP_FOR_OPERATOR_DECISION_ON_SELECTED_SOURCE_REFACTOR_TARGET`.

## Executive Selection

Selected target:

`D13_FOLLOW_UP_BOUNDED_SOURCE_CLEANUP`

Recommended next implementation block:

`NODAL_OS_D13_FOLLOW_UP_BOUNDED_SOURCE_CLEANUP_NO_RUNTIME_CHANGE`

This block selects a target only. It does not implement, does not touch `src/`, does not add tests, does not authorize runtime/product, does not authorize CI enforcement and does not authorize release/commercial.

## Readiness Confirmation

- Bounded/no-runtime reduction selection is ready.
- Broad source simplification is not ready.
- D7 proof-chain reduction is not ready as the direct next implementation because D7 is older/canonical reentry evidence.
- Common-contract parallelization is not ready as the direct next implementation because it carries double-truth risk.
- Product Ledger/model consolidation is not ready as the direct next implementation because Product Ledger local/dev is paused and authority/product/runtime boundaries remain blocked.
- Runtime/product remains `0%`.
- CI enforcement remains `0%`.
- Release/commercial remains `0% / NO-GO`.

## Candidate Matrix

| Candidate | Scope | Expected reduction | Risk | Files likely touched in a future implementation | Tests likely required | Runtime/product impact | Double-truth risk | Reversibility | Recommendation |
| --- | --- | ---: | --- | --- | --- | --- | --- | --- | --- |
| A. D13 follow-up bounded source cleanup | source-minimal / no-runtime / one proven D10-family target | Small, expected `10-25` lines or no-op if audit finds no safe reduction | P3 | `src/OneBrain.Core/Approval/ApprovalExecutionDesignOnlyProtected.cs` only unless the future block proves no change is needed | Core build; ApprovalExecutionDesignOnlyProtected Safety/Recipes; D10 focused; D11 focused; NoRuntimeWiring; NoAuthority; NoDoubleTruth; StaticGuardCatalog; `git diff --check` | None allowed | Low, because it stays inside the D10 target already reduced by D13 | High, one-file revert | Selected |
| B. D7 proof-chain micro-reduction | source-minimal / no-runtime / older reentry target | Approx. `25-45` lines if mirroring D13 pattern | P3/P2 if helper shape becomes less auditable | `src/OneBrain.Core/Approval/ReentryDecisionPacketReadOnly.cs` | Reentry Safety/Recipes; D7/D8 focused; PostReplacementAudit; NoAuthority; NoDoubleTruth | None allowed | Medium, because D7 is canonical older evidence | Medium/high | Defer |
| C. Static guard catalog duplicate wording cleanup | test-only/docs-only | Small test/documentation noise reduction | P3 false-positive risk | `tests/OneBrain.Safety.Tests/NodalOsStaticGuardCatalog.cs` and focused tests | StaticGuardCatalog; Tier1 labels/discovery | None allowed | Low | High | Defer; useful only if scanner noise becomes blocker |
| D. Old readiness docs stale-link cleanup | docs-only | No source reduction | P4/P3 if history is over-edited | readiness/roadmap docs only | `git diff --check`, overclaim scan | None | None | High | Defer; already mitigated by addendum |
| E. Common-contract broad refactor | broad source/test refactor | Medium/high eventually | P2/P3 | Common contracts plus adapters and invariants | Tier1, CommonContracts, NoAuthority, NoDoubleTruth, Product Ledger gates | None allowed, but risk is high | High | Medium | Reject as next |
| F. Product Ledger/model consolidation | broad model/source refactor | High eventually | P2/P3 | Product Ledger/latest-state/writer/model areas | Product Ledger Safety/Recipes, path/redaction/hash/replay/property suites | None allowed, but risk is high | High | Low/medium | Reject as next |

## Why The Selected Target Is Minimal

The D10/ApprovalExecution target already received the D13 reduction and has the clearest focused evidence. A follow-up block can audit for remaining local cleanup in the same bounded target and either:

- make a very small one-file cleanup if a safe repetition remains; or
- record `NO_SOURCE_CHANGE_NEEDED` if D13 already exhausted the safe local reduction.

That is safer than touching D7 first, because D7 remains the older reentry proof target and still functions as canonical comparison evidence for the D10/D13 path.

## Next Block Contract

Name:

`NODAL_OS_D13_FOLLOW_UP_BOUNDED_SOURCE_CLEANUP_NO_RUNTIME_CHANGE`

Objective:

Audit and, only if clearly safe, perform one minimal follow-up cleanup inside the D13/D10 bounded source target while preserving all behavior, private proof intent and no-runtime/no-product boundaries.

Allowed scope:

- one-file source-minimal cleanup in `src/OneBrain.Core/Approval/ApprovalExecutionDesignOnlyProtected.cs`;
- focused tests only if needed to preserve existing proof/equivalence evidence;
- docs/decision/handoff updates;
- no-runtime / no-product / no-release.

Blocked scope:

- no D4 candidate source change;
- no D7 source change;
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

Candidate file:

`src/OneBrain.Core/Approval/ApprovalExecutionDesignOnlyProtected.cs`

Minimum tests for future implementation:

- `dotnet build src/OneBrain.Core/OneBrain.Core.csproj --no-restore -v:minimal`
- ApprovalExecutionDesignOnlyProtected Safety focused
- ApprovalExecutionDesignOnlyProtected Recipes focused
- D10 focused tests
- D11 focused tests
- `TestCategory=NoRuntimeWiring`
- `TestCategory=NoAuthority`
- `TestCategory=NoDoubleTruth`
- `FullyQualifiedName~NodalOsStaticGuardCatalogTests`
- `git diff --check`
- changed-file scan proving only the selected file, needed tests/docs, and no CI/workflows

Stop condition:

`STOP_AFTER_D13_FOLLOW_UP_BOUNDED_SOURCE_CLEANUP_NO_RUNTIME_CHANGE`

NO-GO conditions:

- any runtime/product behavior change;
- any route/DI/service registration/command handler change;
- any D4 or D7 source change;
- any Product Ledger source/model consolidation;
- any assertion weakening or test deletion;
- any public/product, Production route, latest pointer, read precedence or product authority claim;
- any provider/cloud/network/DB/KMS/WORM/release/commercial change;
- P0/P1/P2 or TRUE_RISK.

## Findings

P0: 0.

P1: 0.

P2: 0.

P3:

- D13 follow-up may find no safe remaining D10 cleanup; the future implementation block must be allowed to close `NO_SOURCE_CHANGE_NEEDED`.
- D7 likely has a larger local reduction opportunity, but it is older/canonical evidence and should not be the next direct implementation target.
- Common-contract and Product Ledger/model consolidation still carry double-truth and authority-boundary risks.

P4:

- Old readiness docs retain stale historical recommendations by design.
- Some documentation churn remains, but docs-only cleanup is lower value than selecting one bounded source target.

## Percentages

- Target-selection confidence: `86%`.
- Selected-target implementation readiness: `74%`.
- Broad source simplification readiness: `45%`.
- Static guard catalog readiness: `92%`.
- Common contracts confidence: `98%` design/test-only, `0%` runtime authority.
- Runtime/product enablement: `0%`.
- CI enforcement: `0%`.
- Release/commercial: `0% / NO-GO`.

## Stop Condition

`STOP_FOR_OPERATOR_DECISION_ON_SELECTED_SOURCE_REFACTOR_TARGET`

This selection does not authorize source implementation, runtime/product, CI enforcement, release/commercial or external audit approval.
