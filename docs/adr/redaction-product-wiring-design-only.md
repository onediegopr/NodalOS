# Redaction Product Wiring Design-Only

Date: 2026-07-04

Decision: `GO_WITH_FINDINGS_REDACTION_PRODUCT_WIRING_DESIGN_ONLY_READY`

## Scope

Docs-only design for future redaction-before-persistence product wiring.

This ADR does not implement product redaction wiring, register a service, add command handlers, add UI actions, enable runtime/product behavior, create a product ledger path, call DB/provider/cloud/network, enable Browser/CDP/WCU/OCR/Recipes live behavior, or claim release/commercial readiness.

## Current State

`RedactionBeforePersistenceService` is isolated in Core and test-only. It rejects missing/unknown policy, malformed candidates, malformed metadata/references, raw payloads, secret-like content, PII-like content and path-like content. Stage 2 test-only binds evidence to the exact candidate hash before append.

It is not a productive runtime service.

## Product Wiring Requirements

Future product wiring must define:

- service lifetime and ownership;
- product policy id/version separate from test-only;
- corpus ownership, update cadence and review workflow;
- evidence schema stable across product writes;
- no raw value logging in errors, telemetry or audit evidence;
- product ledger path dependency;
- product feature flag dependency;
- command/UI authority dependency;
- failure behavior: fail closed, no fallback append;
- rollback behavior: append compensating evidence, never mutate prior entries;
- observability that stores reason codes only;
- manual GO evidence and external audit evidence.

## Forbidden Wiring

- No implicit DI registration from namespace discovery.
- No command handler calls redaction directly before authority review.
- No UI action bypasses redaction.
- No product ledger append without redaction success.
- No provider/cloud/network redaction calls by default.
- No Browser/CDP/WCU/OCR/Recipes raw payloads in evidence.
- No release/commercial claim from redaction wiring alone.

## Required Future Negative Tests

- Product append rejects missing redaction result.
- Product append rejects stale candidate hash.
- Product append rejects test-only policy id in product mode.
- Product append rejects product policy without manual GO evidence.
- Product append rejects errors containing raw sensitive values.
- Product append rejects Browser/CDP/WCU/OCR/Recipes live metadata unless separately authorized.
- Product append rejects fallback to unredacted request.
- DI scan proves no accidental service registration.
- Command/UI scans prove no accidental product action path.

## Findings

| Severity | Count | Details |
| --- | ---: | --- |
| P0 | 0 | No product redaction wiring or runtime/product enablement added. |
| P1 | 0 | No DI registration, command handler, UI action, product ledger path or provider/network call added. |
| P2 | 0 | No blocker in this design-only block. |
| P3 | 4 | Product policy versioning, corpus governance, product evidence schema and no-raw logging policy remain future work. |
| P4 | 1 | Current test-only service remains finite deterministic detection, not full product redaction. |

## Percentages

| Track | Conservative status |
| --- | --- |
| Redaction-before-persistence test-only | 91-95% |
| Redaction product wiring design | 55-65% |
| Redaction product implementation | 0% / NO-GO |
| Runtime/product enablement | 0% / NO-GO |
| Release/commercial readiness | 0% / NO-GO |

## Next Safe Option

`NODAL_OS_DURABLE_RUNTIME_ENABLEMENT_DESIGN_ONLY_NO_CODE`
