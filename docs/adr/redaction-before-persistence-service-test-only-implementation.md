# Redaction Before Persistence Service Test-Only Implementation

Status: `TEST_ONLY_IMPLEMENTED / LOCAL_SAFE / RUNTIME_PRODUCT_NO_GO`

Baseline HEAD: `7ba1e9fe3cf3d7e4c4819f5d02e994dbd052f639`

Decision: implement and test a deterministic redaction-before-persistence service for test-only/local-safe use. This does not authorize runtime/live product enablement, productive service registration, command handlers, UI product actions, product ledger paths, DB/migration, provider/cloud/network, Browser/CDP, WCU/OCR, Recipes live execution or release/commercial readiness.

## Implemented

- `RedactionBeforePersistenceService` in Core Approval, isolated from runtime registration.
- `RedactionBeforePersistencePolicy`, `RedactionBeforePersistenceResult`, `RedactionBeforePersistenceEvidence` and reason/decision enums.
- Fail-closed evaluation for missing/unknown policy, missing candidate, malformed references, malformed metadata, raw payload, secret-like, PII-like and path-like content.
- Evidence-safe summary with policy id/version, classifier/redactor versions, candidate hash, decision, classification categories, reason codes and no raw values.
- Stage 2 test-only append integration requiring a successful redaction result tied by candidate hash before append.
- Stage 2 rejection for missing, failed, stale/mismatched or unsafe redaction result.
- Safety and Recipes tests for service behavior, Stage 2 integration and static no-enable guards.

## Scope Boundaries

| Boundary | Result |
| --- | --- |
| Test-only service | IMPLEMENTED |
| Durable Stage 2 integration | IMPLEMENTED_TEST_ONLY |
| Runtime/product enablement | 0% / NO-GO |
| Product service registration | ABSENT |
| Command handlers / command bus | ABSENT |
| UI product actions | ABSENT |
| Product ledger path | PROHIBITED |
| DB/migration/provider/cloud/network | ABSENT |
| Browser/CDP/WCU/OCR/Recipes live | ABSENT |
| Release/commercial readiness | 0% / NO-GO |

## Safety Properties

The service returns only safe reason codes and evidence. Rejected candidates do not produce a safe request, and result rendering does not include the raw secret/PII/path fixtures covered by tests.

`AppendStage2TestOnly` now appends the service-produced safe request, not the raw candidate. The append path rejects when service evidence is missing, failed, contains raw values, was not completed before persistence, or has a candidate hash mismatch.

## Known Limits

| Severity | Finding |
| --- | --- |
| P0 | None. No runtime/product/live authority introduced. |
| P1 | None. No registration, handlers, UI action, product ledger path or release/commercial claim. |
| P2 | None for the authorized test-only implementation scope. |
| P3 | Corpus v1 is focused on deterministic strings and should expand before any broader implementation claim. |
| P3 | Current metadata model is flat `IReadOnlyDictionary<string,string>`; nested metadata remains a future design/implementation expansion. |
| P3 | Product/runtime adoption remains blocked after implementation until external audit and explicit manual GO. |
| P4 | Historical design/read-only docs remain traceability records. |

## Validation Summary

| Validation | Result |
| --- | --- |
| Full solution build | PASS, 0 warnings, 0 errors on final successful run |
| Core build | PASS, 0 warnings, 0 errors |
| Safety focused tests | PASS, 32/32 (`DurableAuditTrailAppendOnlyMinimalSafetyTests` + `RedactionBeforePersistenceServiceSafetyTests`) |
| Recipes focused tests | PASS, 6/6 (`DurableAuditTrailAppendOnlyMinimalTests`) |
| Test project compile warnings | Pre-existing unrelated legacy warnings in broader Safety/Recipes test projects; none attributed to changed Core files |
| `git diff --check` | PASS |
| JSON validation | PASS |
| Static scan changed files | PASS; hits are test literals, no-enable guard strings or prohibited-boundary documentation |

## Percentages

| Track | Conservative status |
| --- | --- |
| Redaction-before-persistence test-only service | 82-88% |
| Redaction-before-persistence product service | 0% / NO-GO |
| Durable Stage 2 test-only implementation | 90-94% |
| Runtime/live product enablement | 0% / NO-GO |
| Release/commercial readiness | 0% / NO-GO |
| Proyecto usable end-to-end | 24-34% |

## Decision

`GO_WITH_FINDINGS_REDACTION_BEFORE_PERSISTENCE_TEST_ONLY_SERVICE_READY`

Recommended next safe block: `NODAL_OS_REDACTION_BEFORE_PERSISTENCE_SERVICE_TEST_ONLY_EXTERNAL_AUDIT_AND_FIXES`.

Runtime/product wiring still requires a separate manual GO.
