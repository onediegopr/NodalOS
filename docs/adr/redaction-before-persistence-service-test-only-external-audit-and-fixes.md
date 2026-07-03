# Redaction Before Persistence Service Test-Only External Audit And Fixes

Status: `TEST_ONLY_EXTERNAL_AUDIT_WITH_FIXES / LOCAL_SAFE / RUNTIME_PRODUCT_NO_GO`

Baseline HEAD: `ce27d6775dad77a1bd93a47bcb76ec6dc8b64b3f`

Decision: accept the test-only redaction-before-persistence service after external-audit style hardening fixes. Runtime/live product enablement, productive service registration, command handlers, UI product actions, product ledger paths, DB/migration, provider/cloud/network, Browser/CDP, WCU/OCR, Recipes live execution and release/commercial readiness remain prohibited.

## Audit Result

`GO_WITH_FINDINGS_REDACTION_BEFORE_PERSISTENCE_TEST_ONLY_EXTERNAL_AUDIT_READY`

The service remains isolated in Core and test-only. No product wiring was added.

## Fixes Applied

| Area | Fix |
| --- | --- |
| Stage 2 evidence coherence | `AppendStage2TestOnly` now rejects null `Reasons`, null `Evidence`, mismatched evidence decision, wrong policy id/version, blank candidate hash and safe-request hash mismatch. |
| Tamper resistance | Safety tests now cover manually constructed redaction results with tampered safe request, missing hash, null reasons and null evidence. |
| Secret variants | Service now detects obvious whitespace/casing variants such as `PASSWORD : value` and `api key = value`. |
| Path variants | UNC detection now trims leading whitespace before matching. |
| Corpus hardening | Safety corpus now includes uppercase email and leading-space UNC path variants. |

## Findings

| Severity | Finding |
| --- | --- |
| P0 | None. No runtime/product/live authority introduced. |
| P1 | None. No registration, handlers, UI product action, product ledger path or release/commercial claim. |
| P2 | 0 after fix. The audited evidence-coherence gap is closed for test-only scope. |
| P3 | Corpus remains intentionally focused and should continue expanding before broader claims. |
| P3 | Nested metadata remains future work because the durable request model is flat. |
| P3 | Product/runtime adoption remains blocked by external audit and explicit manual GO. |
| P4 | Historical design and implementation docs remain traceability records. |

## Validation Summary

| Validation | Result |
| --- | --- |
| Core build | PASS, 0 warnings, 0 errors |
| Safety focused tests | PASS, 33/33 |
| Recipes focused tests | PASS, 6/6 |
| Full solution build | PASS, 0 warnings, 0 errors |
| `git diff --check` | PASS |
| JSON validation | PASS |
| Static scan changed files | PASS; hits are guard strings or prohibited-boundary documentation only |

## Decision

`GO_WITH_FINDINGS_REDACTION_BEFORE_PERSISTENCE_TEST_ONLY_EXTERNAL_AUDIT_READY`

Recommended next safe block: `NODAL_OS_REDACTION_BEFORE_PERSISTENCE_SERVICE_PROPERTY_CORPUS_EXPANSION_TEST_ONLY`.

Runtime/product wiring still requires a separate manual GO.
