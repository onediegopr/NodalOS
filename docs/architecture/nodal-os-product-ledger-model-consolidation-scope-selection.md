# NODAL OS Product Ledger Model Consolidation Scope Selection

Date: 2026-07-08

Mode: read-only / docs-only / audit-only / scope-selection-only.

Block: `AUTHORIZE_NODAL_OS_PRODUCT_LEDGER_MODEL_CONSOLIDATION_SCOPE_SELECTION_AUDIT_ONLY`.

Baseline HEAD: `ca5446b1bc6998513c3c1ac4b0f6555d8f5c0a80`.

Decision: `GO_WITH_FINDINGS_PRODUCT_LEDGER_MODEL_CONSOLIDATION_SCOPE_SELECTED_READY`.

Current state: `PRODUCT_LEDGER_MODEL_CONSOLIDATION_SCOPE_SELECTED_NO_IMPLEMENTATION`.

Stop condition: `STOP_FOR_OPERATOR_DECISION_ON_SELECTED_PRODUCT_LEDGER_MODEL_CONSOLIDATION_TARGET`.

Follow-up status: the selected authority-map terminology reconciliation was executed in `NODAL_OS_PRODUCT_LEDGER_AUTHORITY_MAP_TERMINOLOGY_RECONCILIATION_DOCS_TEST_ONLY` and records state `PRODUCT_LEDGER_AUTHORITY_MAP_TERMINOLOGY_RECONCILED_NO_PRODUCT_AUTHORITY`.

Post-terminology next-scope selection was executed in `NODAL_OS_PRODUCT_LEDGER_MODEL_CONSOLIDATION_POST_AUTHORITY_TERMINOLOGY_NEXT_SCOPE_SELECTION_AUDIT_ONLY` and selected `PRODUCT_LEDGER_AUTHORITY_MAP_NO_DOUBLE_TRUTH_EQUIVALENCE_AUDIT_READ_ONLY`.

## Scope

This document selects exactly one future Product Ledger/model consolidation target. It does not implement consolidation, edit `src/`, edit tests, change CI/workflows, enable runtime/product, create a latest pointer, activate read precedence, grant product authority, wire a Product Ledger writer/runtime, enable DB/cloud/network/provider, claim KMS/WORM/external trust, claim external audit approval or change release/commercial posture.

## Current State

Current readiness baseline:

- Product Ledger model consolidation readiness: `45%`.
- Double-truth mitigation confidence: `68%`.
- Product Ledger local/dev readiness: `92%`.
- Global roadmap readiness: `80%`.
- Runtime/product enablement: `0%`.
- CI enforcement: `0%`.
- Release/commercial readiness: `0% / NO-GO`.

The readiness audit concluded that broad Product Ledger/model consolidation is not ready. The safest next step is one-target scope selection before any source or test implementation.

## Candidate Matrix

| Candidate | Current source of truth | Duplicate/competing source | Authority owner | Double-truth risk | Runtime/product risk | Readiness | Future test requirement | Recommendation |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| `PRODUCT_LEDGER_AUTHORITY_MAP_TERMINOLOGY_RECONCILIATION_DOCS_TEST_ONLY` | `docs/audit/product-ledger-local-dev/current-authority-map.md`, E2 canon, E14 manual gate table, E15 no-authority scan contract | `ProductLedgerLocalLedgerTaxonomy` uses local active writer authority terms; historical docs repeat no-product authority terms | Product Ledger local/dev authority map and E2 canon | Low/medium; docs terminology can be aligned without making source authoritative | Low; docs/test-only future block, no runtime path | `READY_FOR_NEXT_DOCS_TEST_ONLY_BLOCK` | Add/update a focal guard proving authority-map terminology separates local ledger authority from product/runtime authority | Select |
| `LATEST_STATE_EVIDENCE_ROLE_ALIGNMENT` | Latest-state snapshot, manifest, reader candidate and auxiliary evidence source/tests | Common `EvidenceRole` candidates and latest pointer/read precedence blocker docs | Not selected | High; latest-state naming sits near latest pointer and read precedence | Medium/high if misread as precedence authority | `DEFER_NOT_READY` | Would require no latest pointer/read precedence/product authority assertions | Defer |
| `WRITER_MODE_TERMINOLOGY_ALIGNMENT` | Active local-only writer, local-temp writer and disabled scaffold source/tests | Common `WriterMode` candidates and Product Ledger local ledger taxonomy | Not selected | High; active local-only writer can be mistaken for product writer/runtime | Medium/high due physical local writes | `DEFER_NOT_READY` | Would require no writer/runtime real and no product ledger path activation assertions | Defer |
| `OPERATOR_SURFACE_READ_MODEL_ALIGNMENT` | Operator surface model, read-model provider and Pilot local-dev route preview | Public/product UI blockers, fixture-safe read model and test-safe live ledger read model | Not selected | Medium/high; route/read-model names can imply product surface | Medium due route preview and local-dev runtime surface | `DEFER_NOT_READY` | Would require route exposure and public action no-enable assertions | Defer |
| `COMMON_BOUNDARY_CLAIMS_MAPPING_ALIGNMENT` | `NodalOsCommonBoundaryClaimsCandidate` and D-series guard tests | D1/D2 Safety-only candidates, Static Guard categories and Product Ledger no-authority docs | Not selected | Medium/high; source candidate must remain non-authoritative | Low if docs-only, higher if source-facing | `DEFER_UNTIL_AUTHORITY_TERMINOLOGY_CLEAR` | Would require candidate non-authority and no replacement assertions | Defer |
| `PRODUCT_LEDGER_CANON_REFERENCE_COMPACTION` | E2 canon, E3 plan, E16 closeout, current roadmap index | Historical QA/handoff files | Not selected | Low, but high doc churn risk | Low | `DEFER_LOW_VALUE` | Would require no stale-entrypoint overclaim assertions | Defer |

## Selection Rules

- Exactly one target may be selected.
- The target must be docs/test/source-safe for a future block.
- The target must not enable runtime/product.
- The target must not change latest pointer, read precedence or product authority.
- The target must not make a Product Ledger writer/runtime real.
- The target must have a clear no-double-truth assertion path.
- The target must have a rollback/defer option.
- If any selected target becomes ambiguous, the future block must stop with NO-GO instead of widening scope.

## Selected Target

Selected target:

`PRODUCT_LEDGER_AUTHORITY_MAP_TERMINOLOGY_RECONCILIATION_DOCS_TEST_ONLY`

Follow-up result:

`PRODUCT_LEDGER_AUTHORITY_MAP_TERMINOLOGY_RECONCILED_NO_PRODUCT_AUTHORITY`.

Why this target:

- It is the smallest target that directly reduces double-truth risk before model/source consolidation.
- Authority owner is clear: Product Ledger local/dev authority map plus E2 canon.
- It does not require source, runtime, route, writer, latest-state or product authority changes.
- It can be guarded with a focal docs/test-only assertion in a future block.
- It is reversible: if terminology cannot be clarified without overclaim, the future block can defer.

Authority owner:

`docs/audit/product-ledger-local-dev/current-authority-map.md` plus `docs/architecture/nodal-os-product-ledger-local-dev-safety-backlog-canon.md`.

Current source terminology to reconcile in the future block:

- `ProductLedgerLocalLedgerTaxonomy` can use local ledger authority terms for local-only writer evidence.
- Product Ledger local/dev docs must continue to state no product/runtime authority.
- Future wording must distinguish `local-only ledger authority` from `product authority`, `runtime/product authority`, `read precedence authority` and `latest pointer authority`.

## Contract For Next Block

Exact next block:

`NODAL_OS_PRODUCT_LEDGER_AUTHORITY_MAP_TERMINOLOGY_RECONCILIATION_DOCS_TEST_ONLY`

Objective:

Reconcile authority-map terminology so Product Ledger local-only ledger authority is not confused with product/runtime authority, latest pointer authority or read precedence authority.

Allowed scope:

- Docs-only terminology reconciliation in the current authority map, E2 canon references and continuity records.
- Test-only focal guard if explicitly authorized by the next block.
- Static no-overclaim scan over changed files.
- No source implementation.

Blocked scope:

- No `src/` changes.
- No Product Ledger model consolidation implementation.
- No latest-state model merge.
- No writer mode merge.
- No operator surface/read-model merge.
- No common boundary replacement.
- No CI/workflows or CI enforcement.
- No runtime/product.
- No public/product or Production route.
- No latest pointer, read precedence or product authority changes.
- No Product Ledger writer/runtime real.
- No DB/cloud/network/provider.
- No KMS/WORM.
- No external audit approval claim.
- No release/commercial.

Candidate files for the future block:

- `docs/audit/product-ledger-local-dev/current-authority-map.md`.
- `docs/architecture/nodal-os-product-ledger-local-dev-safety-backlog-canon.md`.
- `docs/audit/product-ledger-local-dev/no-authority-static-scan-contract.md`.
- `tests/OneBrain.Safety.Tests/ProductLedgerLocalDevCanonGuardTests.cs` only if the next block explicitly authorizes test-only guard edits.
- `docs/architecture/nodal-os-product-ledger-model-consolidation-scope-selection.md`.
- `docs/architecture/nodal-os-global-roadmap-current-index.md`.
- `docs/architecture/nodal-os-simplification-backlog.md`.
- `docs/decision-log.md`.
- `docs/handoff/handoff-log.md`.

Minimum future tests if test-only is authorized:

- A focal guard that distinguishes `local-only ledger authority` from blocked `product authority`.
- A focal guard that keeps latest pointer/read precedence/product authority blocked in current authority docs.
- Existing Product Ledger canon guard if touched.

Expected no-double-truth assertion:

`LOCAL_LEDGER_AUTHORITY_IS_NOT_PRODUCT_RUNTIME_AUTHORITY_AND_DOES_NOT_GRANT_LATEST_POINTER_OR_READ_PRECEDENCE`.

Implemented focal guard:

`ProductLedgerLocalDevAuthorityMapTerminologyRemainsLocalDevOnlyAndNoProductAuthority`.

Selected post-terminology next scope:

`PRODUCT_LEDGER_AUTHORITY_MAP_NO_DOUBLE_TRUTH_EQUIVALENCE_AUDIT_READ_ONLY`.

NO-GO conditions:

- Any wording implies product/runtime authority.
- Any wording implies latest pointer promotion or active read precedence.
- Any wording makes the Product Ledger local-only writer a product writer/runtime.
- Any test edit weakens Product Ledger no-authority or no-double-truth evidence.
- Any scope expands into source/model implementation.
- Any origin/worktree/HEAD guard fails.

Rollback/defer option:

If terminology cannot be reconciled without ambiguity, close the future block as:

`PRODUCT_LEDGER_MODEL_CONSOLIDATION_DEFER_NO_SAFE_TARGET`.

## Findings

P0: 0.

P1: 0.

P2: 0.

P3:

- Source-side local ledger authority vocabulary and docs-side no-product-authority vocabulary can be misread as competing truths if future consolidation jumps directly to models.
- Latest-state, writer, operator-surface and common-boundary candidates remain too risky for immediate implementation.

P4:

- The selected target is deliberately small and terminology-heavy; it improves future safety but does not reduce source bloat yet.

## Updated Percentages

- Product Ledger model consolidation readiness: `50%`.
- Double-truth mitigation confidence: `72%`.
- Product Ledger local/dev readiness: `92%`.
- Global roadmap readiness: `81%`.
- Runtime/product enablement: `0%`.
- CI enforcement: `0%`.
- Release/commercial readiness: `0% / NO-GO`.

## Final Boundary

This scope selection does not authorize Product Ledger/model consolidation implementation, source changes, test edits, CI enforcement, runtime/product, public/product, Production route, latest pointer, read precedence, product authority, writer/runtime, DB/cloud/network/provider, KMS/WORM, external audit approval or release/commercial work.
