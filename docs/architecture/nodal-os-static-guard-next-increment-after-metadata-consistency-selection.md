# NODAL OS Static Guard Next Increment After Metadata Consistency Selection

Date: 2026-07-08

Mode: read-only / docs-only / audit-only / selection-only.

Block: `AUTHORIZE_NODAL_OS_STATIC_GUARD_NEXT_INCREMENT_AFTER_METADATA_CONSISTENCY_SELECTION_AUDIT_ONLY`.

Baseline HEAD: `4d0cddb2f97fc041846a0541f15180f422022193`.

Decision: `GO_WITH_FINDINGS_STATIC_GUARD_NEXT_INCREMENT_AFTER_METADATA_SELECTED_READY`.

Resulting state: `STATIC_GUARD_NEXT_INCREMENT_AFTER_METADATA_CONSISTENCY_SELECTED_NO_IMPLEMENTATION`.

Stop condition: `STOP_FOR_OPERATOR_DECISION_ON_STATIC_GUARD_NEXT_INCREMENT_AFTER_METADATA_CONSISTENCY`.

## Current Source Of Truth

Recent Static Guard line:

- Coverage map: `docs/architecture/nodal-os-static-guard-catalog-coverage-map.md`.
- Metadata consistency check: `docs/architecture/nodal-os-static-guard-catalog-metadata-consistency-check.md`.
- Focal test: `StaticGuardCatalog_MetadataConsistencyKeepsTier1PartialAndSemanticLabelsSeparate`.

Current posture:

- Static Guard Catalog readiness: `94%`.
- Tier 1 label coverage: `70%`.
- Metadata consistency confidence: `82%`.
- Global roadmap readiness: `77%`.
- Runtime/product enablement: `0%`.
- CI enforcement: `0%`.
- Release/commercial: `0% / NO-GO`.

Confirmed boundaries:

- Forbidden phrase expansion remains deferred.
- Product Ledger/model consolidation remains deferred.
- Broad common-contract implementation remains deferred.
- Runtime/product, CI enforcement and release/commercial remain blocked.

## Candidate Matrix

| Candidate | Expected value | Risk | False-positive risk | Test-only suitability | Runtime/product impact | CI impact | Required future authorization | Recommendation |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| A. `FORBIDDEN_PHRASE_EXPANSION_CORPUS_SELECTION_AUDIT_ONLY` | Selects exact phrase families, negative allowlists and corpus boundaries before any phrase expansion | Low | Reduces future false positives | High as audit/docs-only | None | None | Later test-only corpus/phrase GO | Selected |
| B. `STALE_RECOMMENDATION_DOCS_SCAN_GUARD_SELECTION_AUDIT_ONLY` | Could guard stale roadmap recommendation drift | Low/medium | Medium in historical docs | Medium/high | None | None | Later docs/test-only GO | Defer; GR2 already cleaned current index |
| C. `RUNNER_SAFE_COMMAND_GUIDANCE_CROSS_CHECK_TEST_ONLY` | Could prevent broad local filter misuse | Low/medium | Low | Medium | None | None | Later test-only GO | Defer; runner guidance is recorded |
| D. `TIER1_LABEL_COVERAGE_NEXT_REPRESENTATIVE_TEST_ONLY` | Expands representative label coverage | Low/medium | Low | High | None | None | Later test-only GO | Defer after corpus selection |
| E. `PRODUCT_LEDGER_MODEL_CONSOLIDATION_PRECONDITION_GUARD_AUDIT_ONLY` | Prepares consolidation guard conditions | Medium/high | Medium/high | Audit-only | None if audit-only | None | Separate audit-only GO | Defer due double-truth risk |
| F. `RUNTIME_PRODUCT_PRECONDITION_GUARD_AUDIT_ONLY` | Prepares runtime/product preconditions | High | Medium | Audit-only | Adjacent to blocked runtime/product | None | Separate explicit operator GO | Reject as next |
| G. `PAUSE_STATIC_GUARD_LINE_AND_RETURN_TO_MAIN_ROADMAP` | Stops the Static Guard line | Low | None | Docs-only | None | None | Operator decision | Not selected; phrase corpus selection is lower-risk value |

## Selected Increment

Selected next increment:

`FORBIDDEN_PHRASE_EXPANSION_CORPUS_SELECTION_AUDIT_ONLY`

Exact next block:

`NODAL_OS_FORBIDDEN_PHRASE_EXPANSION_CORPUS_SELECTION_AUDIT_ONLY`

Why:

The catalog has coverage and representative metadata consistency. The next risky area is phrase expansion, but expanding phrases directly would create false-positive risk in negative/no-go docs. The safe next step is to select corpus scope, phrase families and allowlist rules without implementing phrase expansion.

## Next Block Contract

Name:

`NODAL_OS_FORBIDDEN_PHRASE_EXPANSION_CORPUS_SELECTION_AUDIT_ONLY`

Objective:

Select a safe forbidden-phrase expansion corpus and boundaries for a future test-only guard increment. Define phrase families, negative/no-go allowlist rules, candidate docs/tests and no-go conditions without adding phrases or changing tests.

Allowed scope:

- read-only inventory of current catalog fragments;
- docs-only corpus selection;
- audit-only phrase family matrix;
- false-positive risk classification;
- future test-only contract definition.

Blocked scope:

- no `src/`;
- no tests;
- no phrase expansion implementation;
- no broad docs scan guard implementation;
- no CI/workflow changes;
- no CI enforcement;
- no runtime/product;
- no public/product route or Production route;
- no latest pointer/read precedence/product authority;
- no Product Ledger/model consolidation;
- no broad common-contract implementation;
- no DB/cloud/network/provider;
- no KMS/WORM;
- no release/commercial.

Candidate documents/tests for future inventory:

- `tests/OneBrain.Safety.Tests/NodalOsStaticGuardCatalog.cs`;
- `tests/OneBrain.Safety.Tests/NodalOsStaticGuardCatalogTests.cs`;
- `docs/architecture/nodal-os-static-guard-catalog-coverage-map.md`;
- `docs/architecture/nodal-os-static-guard-catalog-metadata-consistency-check.md`;
- `docs/architecture/nodal-os-global-roadmap-current-index.md`;
- Product Ledger local/dev canon and no-authority scan docs;
- runner safe command guidance docs;
- current decision-log and handoff-log entries.

Permitted validations:

- repo guard;
- `git diff --check`;
- docs-only scope scan;
- anti-overclaim scan;
- no test execution required unless a later implementation block edits tests.

NO-GO conditions:

- any phrase expansion implementation becomes necessary;
- any test/source/CI/workflow edit becomes necessary;
- negative/no-go docs cannot be distinguished from positive claims;
- runtime/product or release/commercial wording becomes ambiguous;
- Product Ledger/model consolidation or broad common-contract work becomes necessary;
- P0/P1/P2 or TRUE_RISK appears;
- worktree/origin guard fails.

Stop condition:

`STOP_FOR_OPERATOR_DECISION_ON_FORBIDDEN_PHRASE_EXPANSION_CORPUS_SELECTION`

## Findings

P0: 0.

P1: 0.

P2: 0.

P3:

- Forbidden phrase expansion remains useful but false-positive prone without a selected corpus and negative allowlist rules.
- Historical docs contain many negative/no-go claims that must not be treated as positive readiness.
- Runtime/product and release/commercial remain blocked, so any phrase expansion must remain test-only and non-authoritative.

P4:

- Static Guard line is now mature enough that selection blocks should stay narrow and avoid broad scans by default.

## Final Boundary

This block selects only. It does not implement forbidden phrase expansion, edit tests, touch source, enable CI, authorize runtime/product, authorize release/commercial, authorize Product Ledger/model consolidation or authorize broad common-contract implementation.
