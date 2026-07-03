# Redaction Before Persistence Service Property Corpus Expansion Test Only

Status: `TEST_ONLY_CORPUS_EXPANSION / LOCAL_SAFE / RUNTIME_PRODUCT_NO_GO`

Baseline HEAD: `af9ebdae4ba8e040beddd58c940bd238e63a42c9`

Decision: expand the redaction-before-persistence test-only corpus with property-style Safety coverage. No runtime/live product enablement, productive service registration, command handlers, UI product actions, product ledger paths, DB/migration, provider/cloud/network, Browser/CDP, WCU/OCR, Recipes live execution or release/commercial readiness is authorized.

## Scope

This block adds Safety tests only. It does not change service behavior, Stage 2 production authority, runtime wiring or product paths.

## Added Coverage

- Sensitive samples are tested across actor reference, approval reference, evidence reference, metadata key and metadata value.
- Corpus includes token/secret/API-key assignment variants, authorization bearer value, email-like value, Windows path and UNC path.
- Safe controls verify benign text remains allowed.
- Rejection assertions confirm no raw sensitive fixture material appears in result rendering.

## Findings

| Severity | Finding |
| --- | --- |
| P0 | None. No runtime/product/live authority introduced. |
| P1 | None. No registration, handlers, UI product action, product ledger path or release/commercial claim. |
| P2 | None for test-only corpus expansion scope. |
| P3 | Corpus is broader but still deterministic and finite; future property generation can continue expanding. |
| P3 | Nested metadata remains future work because durable request metadata is flat. |
| P3 | Product/runtime adoption remains blocked by external audit and explicit manual GO. |
| P4 | Historical docs remain traceability records. |

## Decision

`GO_WITH_FINDINGS_REDACTION_BEFORE_PERSISTENCE_PROPERTY_CORPUS_EXPANSION_READY`

Recommended next safe block: `NODAL_OS_REDACTION_BEFORE_PERSISTENCE_SERVICE_TEST_ONLY_CLOSEOUT_EXTERNAL_AUDIT_READ_ONLY`.

Runtime/product wiring still requires a separate manual GO.
