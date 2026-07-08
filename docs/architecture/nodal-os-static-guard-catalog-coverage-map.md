# NODAL OS Static Guard Catalog Coverage Map

Date: 2026-07-08

Mode: docs-only / read-only / coverage-map-refresh-only.

Block: `AUTHORIZE_NODAL_OS_STATIC_GUARD_CATALOG_COVERAGE_MAP_REFRESH_TEST_ONLY`.

Baseline HEAD: `de13d941d4a3f81c6e9ec4b18c9735b6ce907d84`.

Decision: `GO_WITH_FINDINGS_STATIC_GUARD_COVERAGE_MAP_REFRESH_READY`.

Current state: `STATIC_GUARD_CATALOG_COVERAGE_MAP_REFRESH_READY`.

Stop condition: `STOP_AFTER_STATIC_GUARD_CATALOG_COVERAGE_MAP_REFRESH_NO_RUNTIME_PRODUCT_AUTHORITY`.

## Executive Verdict

The Static Guard Catalog is present, useful and high-confidence for the current no-runtime/no-product roadmap posture, but its coverage is not yet a complete replacement for Product Ledger Safety/Recipes or all historical local assertions.

This refresh is docs-only. It maps current coverage and defers any forbidden phrase expansion, metadata consistency hardening or consolidation-adjacent guard work to later explicit blocks.

## Current Catalog Categories

Current catalog source:

`tests/OneBrain.Safety.Tests/NodalOsStaticGuardCatalog.cs`

Current test source:

`tests/OneBrain.Safety.Tests/NodalOsStaticGuardCatalogTests.cs`

Current catalog categories:

- `PublicProductExposure`
- `ProductionRoutes`
- `RuntimeExecutionClaims`
- `LatestPointer`
- `ReadPrecedence`
- `ProductAuthority`
- `CommandExecution`
- `ShellSubprocess`
- `CloudNetworkDb`
- `KmsWormCompliance`
- `ReleaseCommercial`
- `RunClaimCoherence`

## Coverage Map

| Coverage area | Existing tests/docs | Category labels | Current confidence | Known gaps | False-positive risk | Recommended next action |
| --- | --- | --- | --- | --- | --- | --- |
| Runtime/product blocking | Catalog category `RuntimeExecutionClaims`; Product Ledger canon guard blocks runtime/product claims | `NodalOsTier1Safety`, `StaticGuard`, `ProductLedger`, `NoRuntimeWiring` | Medium/high | Runtime category has less visible method-specific label coverage than public/product and route blockers | Medium in negative docs | Add metadata consistency check before expanding phrases |
| Public/product blocking | Catalog category `PublicProductExposure`; `StaticGuardCatalog_PublicProductAndProductionRouteAssertionsRemainHardFailing`; Product Ledger canon guard | `PublicProductBlock`, `ProductLedger`, `NodalOsTier1Safety` | High | Coverage map was not previously centralized | Medium in docs with negative claims | Keep as Tier 1 focal guard |
| Production route blocking | Catalog category `ProductionRoutes`; hard-failing static guard sample; Product Ledger canon guard | `ProductionRouteBlock`, `ProductLedger`, `NodalOsTier1Safety` | High | None requiring implementation now | Low/medium | Keep as Tier 1 focal guard |
| Latest pointer/read precedence blocking | Catalog categories `LatestPointer`, `ReadPrecedence`; C2 mirror tests; Product Ledger canon guard | `LatestPointerBlock`, `ReadPrecedenceBlock` | High | Some latest-state role docs remain outside the catalog map | Medium in negative docs | Future metadata consistency check |
| Product authority blocking | Catalog category `ProductAuthority`; C2 source/docs scope tests; Product Ledger canon guard | `ProductAuthorityBlock`, `NoAuthority`, `ProductLedger` | High | No broad Product Ledger/model consolidation guard should be inferred | Medium/high around design docs | Keep consolidation preconditions deferred |
| Product Ledger local/dev no-authority | `ProductLedgerLocalDevCanonGuardTests`; E2/E3/E4/E14/E15 docs | `ProductLedger`, `NoAuthority`, `NoDoubleTruth`, `NoRuntimeWiring` | High | Local/dev line remains separate from runtime/product authority | Medium in historical docs | Maintain current canon guard; no source change |
| Release/commercial NO-GO | Catalog category `ReleaseCommercial`; Product Ledger canon guard | `ReleaseCommercialBlock` | High | Release/commercial wording is common in negative docs | High | Avoid phrase expansion until corpus exists |
| NoRuntimeWiring | Product Ledger canon guard and no-runtime focused labels elsewhere | `NoRuntimeWiring` | Medium/high | Not all no-runtime tests are catalog-backed | Medium | Metadata consistency check |
| NoDoubleTruth | Product Ledger canon guard and source-refactor equivalence tests | `NoDoubleTruth` | Medium/high | Broad common-contract double-truth risk remains deferred | Medium/high | Keep broad common-contract work deferred |
| NoAuthority | Product Ledger canon guard and authority-boundary tests | `NoAuthority` | High | Authority claims in historical docs require negative-claim interpretation | Medium/high | Future metadata consistency check |
| StaticGuardCatalog discovery | `StaticGuardCatalog_ContainsExpectedC1Categories`; C4/C6 metadata discoverability tests | `StaticGuard`, `NodalOsTier1Safety` | High | Coverage map itself was missing before this block | Low | This document closes the map gap |
| Tier 1 labels / TestCategory metadata | C4/C6 reflection tests; Product Ledger canon guard class labels | `NodalOsTier1Safety` plus blocker labels | Medium | C6 labels are partial and not the complete Tier 1 suite | Low | Metadata consistency check is the best next increment |
| Runner safe command guidance relation | Runner guidance docs; current index says broad filters are not gates | Docs-only, not catalog-backed | Medium | No catalog guard cross-check yet | Low/medium | Defer runner-safe-command documentation cross-check |

## Tier 1 Label Status

Mapped now:

- `NodalOsTier1Safety`
- `StaticGuard`
- `ProductLedger`
- `PublicProductBlock`
- `ProductionRouteBlock`
- `RunClaimCoherence`
- `LatestPointerBlock`
- `ReadPrecedenceBlock`
- `ProductAuthorityBlock`
- `ReleaseCommercialBlock`
- `NoRuntimeWiring`
- `NoAuthority`
- `NoDoubleTruth`

Partial or not complete:

- `NodalOsTier1Safety` is not a complete Tier 1 suite.
- Product Ledger Safety and Recipes remain required for Product Ledger-adjacent confidence.
- Runtime/product, CI and release/commercial gates are not enabled by any label.
- Runner-safe-command guidance is documented but not catalog-backed.
- Product Ledger/model consolidation and broad common-contract preconditions remain deferred.

## Deferred Increments

- Forbidden phrase expansion: deferred because false-positive risk is high in negative docs.
- Metadata consistency check: best next safe increment after this map.
- Stale recommendation docs scan: lower priority after the global roadmap current index cleanup.
- Runner safe command cross-check: useful later, but runner guidance already exists.
- Product Ledger/model consolidation precondition: deferred due double-truth risk.
- Runtime/product precondition guard: deferred because runtime/product remains `0%`.

## What Must Not Be Inferred

- No CI enforcement is enabled.
- No runtime/product authority is granted.
- No release/commercial readiness exists.
- No product authority is granted.
- No Product Ledger runtime/model consolidation is authorized.
- No broad common-contract implementation is authorized.
- No broad forbidden phrase expansion was performed.
- No test-infra fix was performed.

## Next Recommended Increment

Recommended next increment:

`STATIC_GUARD_CATALOG_METADATA_CONSISTENCY_CHECK_TEST_ONLY`

Exact next block:

`NODAL_OS_STATIC_GUARD_CATALOG_METADATA_CONSISTENCY_CHECK_TEST_ONLY`

Why:

The coverage map shows that metadata labels are useful but partial. A focused metadata consistency check can prove the existing catalog/category labels stay aligned without changing source, runtime/product, CI or assertions.

## Findings

P0: 0.

P1: 0.

P2: 0.

P3:

- Tier 1 labels remain partial and must not replace Product Ledger Safety/Recipes.
- Runtime/product blocking and runner guidance are not fully catalog-backed.
- Forbidden phrase expansion would have meaningful false-positive risk without a corpus.

P4:

- Coverage remains intentionally redundant across catalog tests, Product Ledger canon guards and docs.

## Final Boundary

This map refresh does not edit source, tests, project files, workflows or CI. It does not enable runtime/product, public/product, Production route, latest pointer, read precedence, product authority, Product Ledger runtime/model consolidation, broad common-contract implementation, DB/cloud/network/provider, KMS/WORM, release/commercial or broad forbidden phrase expansion.
