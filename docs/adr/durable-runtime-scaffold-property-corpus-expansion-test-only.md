# Durable Runtime Scaffold Property Corpus Expansion Test-Only

Date: 2026-07-04

Decision: `GO_WITH_FINDINGS_DURABLE_RUNTIME_SCAFFOLD_PROPERTY_CORPUS_EXPANSION_TEST_ONLY_READY`

## Scope

Test-only/property-corpus hardening for `DurableRuntimeEnablementSafetyScaffold`.

This block does not enable runtime/product behavior, create an active product ledger path, register services, add command handlers, add UI product actions, add DB/migration/provider/cloud/network integration, implement KMS/WORM/external trust, enable Browser/CDP/WCU/OCR/Recipes live behavior or claim release/commercial readiness.

## Implemented

- Added typed blockers for path traversal, environment-variable paths, reserved Windows device names, mixed separators, symlink/junction/reparse-point risk and canonical realpath evidence gaps.
- Added typed blockers for malformed, duplicate, stale and inconsistent evidence references.
- Added typed blockers for claims of real human authorization, production operator approval, product authority and release approval.
- Expanded Safety property/corpus tests for path corpus, evidence reference corpus, overclaim wording and test-only authority.
- Preserved readiness preview behavior with `NO_PRODUCT_RUNTIME_ENABLEMENT`.

## Symlink/Junction/Reparse Position

The scaffold now blocks product-readiness preview unless test-only evidence declares:

- no symlink/junction/reparse-point unresolved risk;
- canonical realpath evidence present.

This is not real product filesystem protection. Product ledger path enablement remains blocked until a future product scope implements real canonical realpath and reparse-point checks.

## Human GO Position

Human GO remains a test-only evidence flag in this scaffold.

It is not:

- real human authorization;
- production operator approval;
- authentication;
- approval service output;
- product authority;
- release approval.

Any wording that claims those stronger meanings is rejected by typed blockers.

## Findings

| Severity | Count | Details |
| --- | ---: | --- |
| P0 | 0 | No runtime/product enablement added. |
| P1 | 0 | No active product ledger path, productive DI, command handler, UI action, provider/cloud/network, KMS/WORM, live automation or release/commercial path added. |
| P2 | 0 | No blocking safety gap found after focused corpus expansion tests. |
| P3 | 3 | Real symlink/junction protection, real human authorization and product policy ownership remain future product work. |
| P4 | 2 | Path/provider detection remains heuristic; historical docs still contain no-go vocabulary by design. |

## Percentages

| Track | Conservative status |
| --- | --- |
| Durable runtime design readiness | 72-80% |
| Durable runtime test-only scaffold | 45-55% |
| Property/corpus hardening | 35-45% |
| Symlink/junction readiness | 15-25% |
| Product ledger path product readiness | 10-15% |
| Redaction product wiring readiness | 22-32% |
| Runtime feature flag product readiness | 18-28% |
| Authority wiring readiness | 18-28% |
| Replay/failure evidence readiness | 22-32% |
| Runtime/live product enablement | 0% / NO-GO |
| Release/commercial readiness | 0% / NO-GO |
| Proyecto usable end-to-end para runtime productivo | 0% |

## Next Step

Recommended next macro-block:

`NODAL_OS_DURABLE_RUNTIME_SCAFFOLD_READ_MODEL_AND_EVIDENCE_PACK_TEST_ONLY`

Product/runtime enablement remains blocked without explicit manual GO.
