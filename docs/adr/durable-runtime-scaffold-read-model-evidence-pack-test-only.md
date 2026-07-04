# Durable Runtime Scaffold Read Model And Evidence Pack Test-Only

Date: 2026-07-04

Decision: `GO_WITH_FINDINGS_DURABLE_RUNTIME_SCAFFOLD_READ_MODEL_EVIDENCE_PACK_TEST_ONLY_READY`

## Scope

Test-only hardening for `DurableRuntimeEnablementSafetyScaffold` replay/failure readiness.

This block does not enable runtime/product behavior, create an active product ledger path, register services, add command handlers, add UI product actions, add DB/migration/provider/cloud/network integration, implement KMS/WORM/external trust, enable Browser/CDP/WCU/OCR/Recipes live behavior or claim release/commercial readiness.

## Implemented

- Added typed blockers for missing replay/failure evidence references.
- Added typed blockers for malformed or duplicate replay/failure evidence references.
- Added typed blockers for missing read-model snapshot evidence and missing replay/read-model consistency check.
- Added typed blockers for missing failure-mode catalog and rollback/non-rollback classification.
- Added typed blockers for live replay execution claims and raw payload evidence.
- Hardened live automation claim detection for separator variants such as `live-execution`.
- Expanded Safety tests and preserved the Recipes preview test.

## Read Model Position

The scaffold now requires test-only evidence that a read-model snapshot exists and that replay evidence has been checked against that read model.

This is not a product read model, not a product replay service and not durable product recovery. It is a fail-closed pre-enablement readiness preview.

## Evidence Pack Position

Evidence references must be local documentation/test evidence references only. Absolute URLs, empty refs, duplicate refs, live replay wording and raw payload evidence are rejected.

The evidence pack remains descriptive and test-only. It does not persist evidence, replay events, mutate ledgers, recover product state or provide external trust.

## Findings

| Severity | Count | Details |
| --- | ---: | --- |
| P0 | 0 | No runtime/product enablement added. |
| P1 | 0 | No active product ledger path, productive DI, command handler, UI action, provider/cloud/network, KMS/WORM, live automation or release/commercial path added. |
| P2 | 0 | No blocking safety gap remains after focused read-model/evidence-pack hardening. |
| P3 | 3 | Product read model, real replay service and real rollback/non-rollback execution policy remain future product work. |
| P4 | 2 | Evidence reference validation remains syntactic; historical docs still contain no-go vocabulary by design. |

## Percentages

| Track | Conservative status |
| --- | --- |
| Durable runtime design readiness | 74-82% |
| Durable runtime test-only scaffold | 50-60% |
| Property/corpus hardening | 38-48% |
| Symlink/junction readiness | 15-25% |
| Product ledger path product readiness | 10-15% |
| Redaction product wiring readiness | 22-32% |
| Runtime feature flag product readiness | 18-28% |
| Authority wiring readiness | 18-28% |
| Replay/failure evidence readiness | 32-42% |
| Runtime/live product enablement | 0% / NO-GO |
| Release/commercial readiness | 0% / NO-GO |
| Proyecto usable end-to-end para runtime productivo | 0% |

## Next Step

Recommended next macro-block:

`NODAL_OS_DURABLE_RUNTIME_SCAFFOLD_EXTERNAL_AUDIT_READ_ONLY_AFTER_READ_MODEL_EVIDENCE_PACK`

Product/runtime enablement remains blocked without explicit manual GO.
