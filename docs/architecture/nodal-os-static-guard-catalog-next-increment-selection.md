# NODAL OS Static Guard Catalog Next Increment Selection

Date: 2026-07-08

Mode: read-only / docs-only / audit-only / selection-only.

Block: `AUTHORIZE_NODAL_OS_STATIC_GUARD_CATALOG_NEXT_INCREMENT_SELECTION_AUDIT_ONLY`.

Baseline HEAD: `e124fc008b5f0bd98dba5d2125b6ab57d3298020`.

Decision: `GO_WITH_FINDINGS_STATIC_GUARD_NEXT_INCREMENT_SELECTED_READY`.

Resulting state: `STATIC_GUARD_CATALOG_NEXT_INCREMENT_SELECTED_NO_IMPLEMENTATION`.

Stop condition: `STOP_FOR_OPERATOR_DECISION_ON_STATIC_GUARD_NEXT_INCREMENT`.

## Current Source Of Truth

Current roadmap index:

`docs/architecture/nodal-os-global-roadmap-current-index.md`

Current roadmap posture:

- Global roadmap readiness: `77%`.
- Roadmap index freshness: `88%`.
- Static guard catalog readiness: `92%`.
- Source-refactor readiness: `78%`.
- Product Ledger local/dev readiness: `92%`.
- Runtime/product enablement: `0%`.
- CI enforcement: `0%`.
- Release/commercial: `0% / NO-GO`.

Current confirmed boundaries:

- Product Ledger/model consolidation remains deferred.
- Broad common-contract implementation remains deferred.
- Runner/test-infra fix is not implemented.
- Static Guard Catalog exists as test-only safety infrastructure, not CI enforcement.

## Candidate Matrix

| Candidate | Expected value | Risk | Test-only suitability | Runtime/product impact | CI impact | Double-truth risk | Required future authorization | Recommendation |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| A. Static guard catalog coverage map refresh | Clarifies which guard families are already catalog-backed, duplicated, uncovered or docs-only | Low | High; can be docs/test-only without changing assertions | None | None | Reduces risk before future guard increments | Test-only/docs-only implementation GO | Selected |
| B. No-authority guard metadata consistency check | Improves label/category coherence for no-authority tests | Low | High | None | None | Low | Test-only GO | Defer until coverage map identifies exact gaps |
| C. Forbidden-claim phrase family expansion test-only | Could catch more overclaim variants | Medium | Medium/high | None | None | Medium due false positives in negative docs | Test-only GO plus corpus review | Defer until map defines phrase families |
| D. Stale recommendation guard read-only docs scan | Guards stale roadmap recommendations | Low/medium | Docs-only possible | None | None | Low | Docs-only/test-only GO | Defer; GR2 already cleaned current index |
| E. Runner-safe-command guard documentation cross-check | Reduces misuse of broad filters | Low | Docs/test-only possible | None | None | Low | Docs/test-only GO | Defer; runner guidance is recorded |
| F. Product Ledger/model consolidation guard precondition only | Prepares future consolidation boundary | Medium/high | Audit-only only for now | None if audit-only | None | High | Separate audit-only GO | Defer; adjacent to double-truth risk |
| G. Runtime/product precondition guard precondition only | Prepares runtime/product boundary | High | Audit-only only for now | Adjacent to blocked runtime/product | None | High | Separate explicit operator GO | Reject as next |

## Selected Increment

Selected future increment:

`STATIC_GUARD_CATALOG_COVERAGE_MAP_REFRESH_TEST_ONLY`

Exact next block:

`NODAL_OS_STATIC_GUARD_CATALOG_COVERAGE_MAP_REFRESH_TEST_ONLY`

Why:

The Static Guard Catalog is already strong enough to support source-refactor confidence, but it is not complete. A coverage-map refresh gives the next block a precise inventory of catalog-backed guard families, legacy local assertions, metadata labels, docs-only scans and remaining gaps before any phrase expansion, no-authority metadata hardening or consolidation-adjacent guard work.

## Next Block Contract

Name:

`NODAL_OS_STATIC_GUARD_CATALOG_COVERAGE_MAP_REFRESH_TEST_ONLY`

Objective:

Refresh the Static Guard Catalog coverage map and add or update test-only/docs-only evidence that describes current catalog coverage, duplicated local assertions and safe next increments without changing source behavior, runtime/product, CI enforcement or existing assertions.

Allowed scope:

- docs-only coverage map update;
- test-only metadata/evidence if explicitly needed;
- read-only inventory of `NodalOsStaticGuardCatalog` usage;
- static scan coverage classification;
- no-runtime / no-product / no-release.

Blocked scope:

- no `src/`;
- no product/runtime code;
- no test assertion weakening;
- no test deletion, movement, skip behavior or broad suite migration;
- no `.csproj` / `.slnx`;
- no workflows/CI;
- no CI enforcement;
- no public/product route or Production route;
- no latest pointer/read precedence/product authority;
- no Product Ledger runtime/model consolidation;
- no broad common-contract implementation;
- no DB/cloud/network/provider;
- no KMS/WORM;
- no release/commercial.

Candidate files:

- `tests/OneBrain.Safety.Tests/NodalOsStaticGuardCatalog.cs`;
- `tests/OneBrain.Safety.Tests/NodalOsStaticGuardCatalogTests.cs`;
- `tests/OneBrain.Safety.Tests/ProductLedgerLocalDevCanonGuardTests.cs`;
- `docs/architecture/nodal-os-test-tiering-and-static-scan-consolidation-design.md`;
- `docs/architecture/nodal-os-pre-refactor-gate-commands-and-discovery.md`;
- `docs/architecture/nodal-os-global-roadmap-current-index.md`;
- `docs/architecture/nodal-os-simplification-backlog.md`.

Minimum future validations:

- repo guard;
- `git diff --check`;
- changed-file scope scan;
- anti-overclaim scan;
- focused StaticGuardCatalog test or list-tests only if the future block edits tests;
- no broad local execution filters as gates.

NO-GO conditions:

- any source/runtime/product/CI/workflow edit becomes necessary;
- any assertion weakening, test deletion, test movement or skip behavior appears;
- broad Product Ledger/model consolidation or common-contract implementation becomes necessary;
- runtime/product, release/commercial or product authority wording becomes ambiguous;
- P0/P1/P2 or TRUE_RISK appears;
- worktree/origin guard fails.

Stop condition:

`STOP_FOR_OPERATOR_DECISION_ON_STATIC_GUARD_COVERAGE_MAP_REFRESH_IMPLEMENTATION`

## Findings

P0: 0.

P1: 0.

P2: 0.

P3:

- Static Guard Catalog readiness is high but coverage is not fully mapped after later Product Ledger/source-refactor/runner closeouts.
- C6 Tier 1 labels remain partial and must not be treated as full Product Ledger Safety/Recipes replacement.
- Forbidden phrase expansion could create false positives if attempted before coverage map refresh.

P4:

- Historical C1/C2/C6 docs still contain old next-step recommendations, but the current index and this selection supersede them as active selectors.

## Final Boundary

This block selects only. It does not implement the coverage map refresh, edit tests, touch source, enable CI, authorize runtime/product, authorize release/commercial, authorize Product Ledger/model consolidation or authorize broad common-contract implementation.
