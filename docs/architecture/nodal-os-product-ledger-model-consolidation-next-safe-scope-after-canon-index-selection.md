# NODAL OS Product Ledger Model Consolidation Next Safe Scope After Canon Index Selection

Date: 2026-07-08

Mode: read-only / docs-only / audit-only / selection-only.

Block: `AUTHORIZE_NODAL_OS_PRODUCT_LEDGER_MODEL_CONSOLIDATION_NEXT_SAFE_SCOPE_SELECTION_AFTER_CANON_REFERENCE_CLEANUP_AUDIT_ONLY`.

Baseline HEAD: `355282c80b78146c2115cee443c664eb820cb241`.

Decision: `GO_WITH_FINDINGS_PRODUCT_LEDGER_MODEL_CONSOLIDATION_NEXT_SCOPE_AFTER_CANON_INDEX_SELECTED_READY`.

Resulting state: `PRODUCT_LEDGER_MODEL_CONSOLIDATION_NEXT_SAFE_SCOPE_AFTER_CANON_REFERENCE_CLEANUP_SELECTED_NO_IMPLEMENTATION`.

Stop condition: `STOP_FOR_OPERATOR_DECISION_ON_NEXT_PRODUCT_LEDGER_MODEL_CONSOLIDATION_SCOPE_AFTER_CANON_INDEX`.

## Scope

This selector evaluates the Product Ledger model-consolidation lane after canon reference index cleanup. It selects exactly one next safe scope and defines the next-block contract. It does not implement consolidation, edit `src/`, edit tests, change CI/workflows, enable runtime/product, create a latest pointer, activate read precedence, grant product authority, wire a Product Ledger writer/runtime, enable DB/cloud/network/provider, claim KMS/WORM/external trust, claim external audit approval or change release/commercial posture.

## Confirmed Baseline

- Canon reference index exists and is the current navigation entrypoint: `docs/audit/product-ledger-local-dev/canon-reference-index.md`.
- Current authority references remain the E2 canon and current authority map.
- Current audit references include the no-double-truth equivalence audit.
- Historical artifacts are classified as historical evidence, superseded recommendations or future-only gates.
- Product Ledger model consolidation readiness: `57%`.
- Double-truth mitigation confidence: `86%`.
- Product Ledger local/dev readiness: `95%`.
- Global roadmap readiness: `86%`.
- Runtime/product enablement: `0%`.
- CI enforcement: `0%`.
- Release/commercial readiness: `0% / NO-GO`.

The cleanup lowered navigation ambiguity, but model/source consolidation remains deferred. The next useful reduction is a narrow evidence-role terminology lane that stays docs/test-only and does not change source, read precedence, latest pointer, route exposure, writer behavior or product authority.

## Candidate Matrix

| Candidate | Expected value | Double-truth risk | Runtime/product risk | Latest/read-precedence risk | Product authority risk | Testability | Required future authorization | Recommendation |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| `PRODUCT_LEDGER_EVIDENCE_ROLE_TERMINOLOGY_RECONCILIATION_DOCS_TEST_ONLY` | Align evidence-role vocabulary across Product Ledger local/dev docs, audit references and readiness records before any model/source merge | Medium; touches latest-state/audit/read-model vocabulary but can remain docs/test-only | Low if scoped to terminology and guard evidence only | Medium; must explicitly avoid latest pointer/read precedence wording | Medium; must separate evidence role from authority role | High enough; can use docs mapping plus optional focal guard in an authorized next block | Explicit docs/test-only authorization | Select |
| `PRODUCT_LEDGER_OPERATOR_SURFACE_READ_MODEL_TERMINOLOGY_AUDIT_ONLY` | Audit operator-surface/read-model naming before surface consolidation | Medium/high; route/read-model language can imply product surface | Medium due local-dev route previews | Low/medium | Medium/high due product-surface proximity | Medium | Separate audit-only authorization | Defer |
| `PRODUCT_LEDGER_WRITER_MODE_CONSOLIDATION_READINESS_AUDIT_ONLY` | Evaluate writer-mode terminology and competing writer variants | High; writer language is close to Product Ledger writer/runtime real | Medium/high due physical local writer evidence | Low | Medium/high | Medium | Separate audit-only authorization | Defer |
| `PRODUCT_LEDGER_LATEST_STATE_CONSOLIDATION_READINESS_AUDIT_ONLY` | Evaluate latest-state naming and role consolidation readiness | High; latest-state vocabulary is close to latest pointer/read precedence | Low/medium | High | Medium | Medium | Separate audit-only authorization | Defer |
| `PRODUCT_LEDGER_COMMON_BOUNDARY_CANDIDATES_READINESS_AUDIT_ONLY` | Recheck common-boundary candidate relation after authority/canon cleanup | Medium/high; source candidate must remain non-authoritative | Low if audit-only | Medium if mapped into latest-state/read-precedence wording | Medium/high; boundary claims can be misread as replacement authority | Medium | Separate audit-only authorization | Defer |
| `PRODUCT_LEDGER_MODEL_CONSOLIDATION_PAUSE_RETURN_TO_MAIN_ROADMAP` | Pause Product Ledger model-consolidation lane and return to broader roadmap | Low | Low | Low | Low | High | Operator decision to pause | Defer; one narrow docs/test-only terminology lane remains useful |
| `PRODUCT_LEDGER_CANON_REFERENCE_INDEX_EQUIVALENCE_AUDIT_READ_ONLY` | Re-audit canon reference index equivalence after cleanup | Low | Low | Low | Low | High | Separate read-only authorization | Defer; no drift was found that requires re-audit before evidence-role selection |

## Selected Next Scope

Selected scope:

`PRODUCT_LEDGER_EVIDENCE_ROLE_TERMINOLOGY_RECONCILIATION_DOCS_TEST_ONLY`

Why selected:

- It is the next smallest semantic risk after authority/canon/index cleanup.
- It can stay docs/test-only and avoid source/model consolidation.
- It addresses a known P3 ambiguity from the readiness audit: evidence roles touch latest-state, audit packet evidence, Product Ledger read model and common contracts.
- It is safer than writer-mode, latest-state or operator-surface work because it can first define vocabulary and blocked inferences before any behavior or model shape changes.

Rejected for now:

- Operator surface/read-model is closer to product surface.
- Writer-mode is closer to Product Ledger writer/runtime real.
- Latest-state is closer to latest pointer/read precedence.
- Common-boundary candidates are closer to source replacement authority.
- Returning to the main roadmap is premature while a narrow evidence-role terminology scope can reduce ambiguity.

## Contract For Next Block

Exact next block:

`NODAL_OS_PRODUCT_LEDGER_EVIDENCE_ROLE_TERMINOLOGY_RECONCILIATION_DOCS_TEST_ONLY`

Objective:

Reconcile Product Ledger evidence-role terminology so evidence roles cannot be read as product authority, read precedence, latest pointer authority, route authority, writer/runtime authority or model-consolidation implementation.

Allowed scope:

- Docs-only evidence-role terminology mapping across current Product Ledger local/dev canon/index/audit records.
- Docs-only updates to current roadmap index, simplification backlog, decision-log and handoff-log.
- Optional test-only focal guard if the next block explicitly authorizes it.
- Diff-only docs scope scan and anti-overclaim scan over changed files.
- Focal test command only if a focal guard is added.

Blocked scope:

- No `src/`.
- No implementation.
- No broad model consolidation.
- No tests unless the next block explicitly authorizes a focal test-only guard.
- No CI/workflows or CI enforcement.
- No runtime/product.
- No public/product or Production route.
- No latest pointer or read precedence changes.
- No product authority changes.
- No Product Ledger writer/runtime real.
- No writer/latest-state/operator-surface/common-boundary merge implementation.
- No broad common-contract implementation.
- No DB/cloud/network/provider.
- No KMS/WORM.
- No external audit approval claim.
- No release/commercial.
- No test-infra fix.

Candidate files for the next block:

- `docs/audit/product-ledger-local-dev/canon-reference-index.md`.
- `docs/audit/product-ledger-local-dev/current-authority-map.md`.
- `docs/architecture/nodal-os-product-ledger-local-dev-safety-backlog-canon.md`.
- `docs/architecture/nodal-os-product-ledger-model-consolidation-readiness-audit.md`.
- `docs/architecture/nodal-os-product-ledger-model-consolidation-next-safe-scope-after-canon-index-selection.md`.
- `docs/architecture/nodal-os-global-roadmap-current-index.md`.
- `docs/architecture/nodal-os-simplification-backlog.md`.
- `docs/decision-log.md`.
- `docs/handoff/handoff-log.md`.
- `tests/OneBrain.Safety.Tests/ProductLedgerLocalDevCanonGuardTests.cs` only if the next block explicitly authorizes a focal test-only guard.

Validation allowed in next block:

- `git diff --check`.
- Changed-file docs-only/test-only scope scan.
- Anti-overclaim scan over changed files.
- Focal test only if a focal guard is added.
- Final repo guard and origin divergence check.

NO-GO conditions:

- Any wording turns evidence role into product authority.
- Any wording implies latest pointer promotion or active read precedence.
- Any wording implies route/product surface authority.
- Any wording makes Product Ledger writer/runtime real.
- Any scope expands into source/model implementation.
- Any test edit weakens existing no-authority/no-double-truth guard evidence.
- Any origin/worktree/HEAD guard fails.

Expected stop condition:

`STOP_FOR_OPERATOR_DECISION_ON_PRODUCT_LEDGER_EVIDENCE_ROLE_TERMINOLOGY_SCOPE`

## Findings

P0: 0.

P1: 0.

P2: 0.

P3:

- Evidence-role terminology remains a medium-risk semantic lane because it touches latest-state/read-model/audit evidence vocabulary.
- Writer-mode, latest-state, operator-surface and common-boundary lanes remain deferred and higher-risk.

P4:

- Another selector adds documentation overhead, but it keeps the model-consolidation lane narrow before any source work.

## Updated Percentages

- Product Ledger model consolidation readiness: `58%`.
- Double-truth mitigation confidence: `87%`.
- Product Ledger local/dev readiness: `95%`.
- Global roadmap readiness: `87%`.
- Runtime/product enablement: `0%`.
- CI enforcement: `0%`.
- Release/commercial readiness: `0% / NO-GO`.

## Final Boundary

This selector does not authorize Product Ledger/model consolidation implementation, source changes, test edits, CI enforcement, runtime/product, public/product, Production route, latest pointer, read precedence, product authority, writer/runtime, DB/cloud/network/provider, KMS/WORM, external audit approval or release/commercial work.
