# NODAL OS Source Refactor Post-D13 Next Gate Selection

Date: 2026-07-08

Mode: docs-only / read-only / audit-only / post-d13-gate-selection-only.

Block: `AUTHORIZE_NODAL_OS_SOURCE_REFACTOR_POST_D13_NEXT_GATE_SELECTION_AUDIT_ONLY`.

Baseline HEAD: `6fbb588f507140ba14ffd07010603e900036a1cc`.

Decision: `GO_WITH_FINDINGS_POST_D13_SOURCE_REFACTOR_NEXT_GATE_SELECTED_READY`.

Resulting state: `SOURCE_REFACTOR_POST_D13_NEXT_GATE_SELECTED_NO_IMPLEMENTATION`.

Stop condition: `STOP_FOR_OPERATOR_DECISION_ON_POST_D13_SOURCE_REFACTOR_NEXT_GATE`.

## Executive Selection

Selected next gate:

`D7_PROOF_CHAIN_MICRO_REDUCTION_SELECTION_AUDIT_ONLY`

Exact next block:

`NODAL_OS_D7_PROOF_CHAIN_MICRO_REDUCTION_SELECTION_AUDIT_ONLY`

This selection does not implement anything. It does not touch `src/`, tests, CI, runtime/product, Product Ledger model consolidation, broad common-contract refactor or release/commercial posture.

## Post-D13 Confirmation

- D13 follow-up was microscopic and bounded to `ApprovalExecutionDesignOnlyProtected.cs`.
- D10 cleanup remaining is likely exhausted.
- D7 remains deferred and has a possible proof-chain reduction opportunity.
- Broad common-contract refactor remains not ready as a direct implementation.
- Product Ledger/model consolidation remains not ready as a direct implementation.
- Runtime/product remains `0%`.
- CI enforcement remains `0%`.
- Release/commercial remains `0% / NO-GO`.

## Candidate Matrix

| Candidate | Scope | Expected value | Risk | Runtime/product impact | Double-truth risk | Required tests if later implemented | Recommendation |
| --- | --- | --- | --- | --- | --- | --- | --- |
| A. `PAUSE_SOURCE_REFACTOR_AND_RETURN_TO_MAIN_ROADMAP` | docs-only / roadmap-only | Avoids churn after a tiny cleanup | Low | None | None | None beyond repo guard | Safe fallback, not selected because D7 audit-only selection has concrete value |
| B. `D7_PROOF_CHAIN_MICRO_REDUCTION_SELECTION_AUDIT_ONLY` | read-only / docs-only / audit-only | Clarifies whether D7 can safely mirror the D13 table-pattern reduction or should remain untouched | Low | None | Low/medium, evaluated before implementation | Future D7/Reentry Safety+Recipes, D8/PostReplacementAudit, NoAuthority, NoDoubleTruth, StaticGuardCatalog | Selected |
| C. `STATIC_GUARD_CATALOG_DOCS_INDEX_CLEANUP_ONLY` | docs/test metadata only | Reduces scanner/navigation noise | Low | None | Low | StaticGuardCatalog and Tier label discovery if touched later | Defer; lower value than D7 selection |
| D. `SOURCE_REFACTOR_STALE_READINESS_LINK_CLEANUP_ONLY` | docs-only | Reduces stale recommendation confusion | Low | None | None | `git diff --check`, overclaim scan | Defer; current addenda already mitigate stale links |
| E. `COMMON_CONTRACT_PARALLELIZATION_READINESS_AUDIT_ONLY` | read-only/docs-only audit | Prepares broad contracts, but still not implementation | Medium | None in audit | Medium/high if followed too quickly | Future CommonContracts, NoAuthority, NoDoubleTruth, source reference scans | Defer until D7 decision is settled |
| F. `PRODUCT_LEDGER_MODEL_CONSOLIDATION_READINESS_AUDIT_ONLY` | read-only/docs-only audit | Prepares high-value model cleanup | Medium | None in audit | High due Product Ledger authority boundaries | Future Product Ledger Safety/Recipes, path/redaction/hash/property suites | Defer; Product Ledger local/dev line is paused |

## Next Block Contract

Name:

`NODAL_OS_D7_PROOF_CHAIN_MICRO_REDUCTION_SELECTION_AUDIT_ONLY`

Objective:

Audit the D7 `ReentryDecisionPacketReadOnly.cs` proof-chain shape and decide whether a future one-file micro-reduction should be authorized, rejected or paused.

Allowed scope:

- read-only source inspection;
- docs-only/audit-only target analysis;
- line-count and duplicate-pattern inventory;
- no-runtime / no-product / no-release;
- no implementation.

Blocked scope:

- no `src/` changes;
- no tests changed;
- no D7 implementation;
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
- no DB/cloud/network/provider;
- no KMS/WORM/external trust;
- no CI enforcement;
- no release/commercial.

Candidate files/documents:

- `src/OneBrain.Core/Approval/ReentryDecisionPacketReadOnly.cs` as read-only candidate source.
- D7/D8/D13/SR3 docs as audit evidence.
- Existing Reentry Safety/Recipes and PostReplacementAudit tests as future gate evidence.

Minimum future tests if a later implementation is authorized:

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

NO-GO conditions:

- D7 proof-chain reduction would weaken reflection/audit discoverability.
- D7 change requires touching D4, D10, Product Ledger, routes, DI, command handlers, CI or broad contracts.
- Any public/product, Production route, latest pointer, read precedence or product authority claim appears.
- Any provider/cloud/network/DB/KMS/WORM/release/commercial change appears.
- P0/P1/P2 or TRUE_RISK.

Stop condition:

`STOP_FOR_OPERATOR_DECISION_ON_D7_PROOF_CHAIN_MICRO_REDUCTION_SCOPE`

## Findings

P0: 0.

P1: 0.

P2: 0.

P3:

- D7 has likely higher line-reduction value than D10 now, but its older/canonical role means it needs audit-only selection before implementation.
- Common-contract and Product Ledger/model consolidation remain high-risk due double-truth and authority-boundary concerns.

P4:

- Pausing source-refactor is safe but leaves a concrete D7 question unanswered.
- Additional stale-link cleanup is lower value than deciding D7 scope.

## Percentages

- Source-refactor readiness: `74%`.
- Broad source simplification readiness: `45%`.
- D7 selection-readiness: `68%`.
- Static guard catalog readiness: `92%`.
- Common contracts confidence: `98%` design/test-only, `0%` runtime authority.
- Runtime/product enablement: `0%`.
- CI enforcement: `0%`.
- Release/commercial: `0% / NO-GO`.

## Stop Condition

`STOP_FOR_OPERATOR_DECISION_ON_POST_D13_SOURCE_REFACTOR_NEXT_GATE`

This block selects the next gate only. It does not authorize source implementation, runtime/product, CI enforcement, release/commercial or external audit approval.
