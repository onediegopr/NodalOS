# Product Ledger Local/Dev — Manual Gate Decision Table

Date: 2026-07-08

Mode: docs/test-only / manual-gate-decision-table-only.

Block: `NODAL_OS_BLOCK_E14_PRODUCT_LEDGER_LOCAL_DEV_MANUAL_GATE_DECISION_TABLE_DOCS_TEST_ONLY`.

Baseline HEAD: `9aba45fd06139811409b3930da84098159bf8d98`.

Decision target: `GO_WITH_FINDINGS_PRODUCT_LEDGER_LOCAL_DEV_MANUAL_GATE_DECISION_TABLE_READY`.

Current authority state: `PRODUCT_LEDGER_LOCAL_DEV_MANUAL_GATE_DECISION_TABLE_READY_NO_PRODUCT_AUTHORITY`.

Stop condition: `STOP_AFTER_MANUAL_GATE_DECISION_TABLE_READY_NO_PRODUCT_AUTHORITY`.

## Purpose

Clarify manual/operator decisions and prevent misinterpretation as product/runtime authority.

Every current E-series gate has Product/runtime authority = `NO`.

Manual/operator-run gates are not CI enforcement.

No gate may claim external review response, external reviewer approval or external audit pass unless actual response content is provided and recorded.

## Decision Table

| Gate / Decision name | Who can decide | Required evidence | Allowed result | Not allowed result | Required stop condition | Requires future explicit authorization? | Product/runtime authority? |
| --- | --- | --- | --- | --- | --- | --- | --- |
| `APPROVE_PACKET_FOR_EXTERNAL_REVIEW` | Diego/operator | E6 packet, E7 review, E8 operator handoff | Prepare external/manual review handoff | Runtime/product authorization; external submission by Codex | `STOP_FOR_EXTERNAL_REVIEW_SUBMISSION_BY_OPERATOR` | Yes, for any later runtime/product, CI or release step | `NO` |
| `STOP_FOR_OPERATOR_TO_SUBMIT_EXTERNAL_REVIEW_MANUALLY` | Diego/operator | E10 operator submission packet | Operator may manually submit outside Codex | Claim that Codex submitted, review completed or audit passed | `STOP_FOR_OPERATOR_TO_SUBMIT_EXTERNAL_REVIEW_MANUALLY` | Yes, for response intake or any correction scope | `NO` |
| `STOP_FOR_OPERATOR_TO_SUBMIT_EXTERNAL_REVIEW_AND_RETURN_RESPONSE` | Diego/operator | E11 response intake scaffold | Wait for Diego-provided response content | Invent response, reviewer identity, approval or findings | `STOP_FOR_OPERATOR_TO_SUBMIT_EXTERNAL_REVIEW_AND_RETURN_RESPONSE` | Yes, for response recording | `NO` |
| `CLOSE_EXTERNAL_REVIEW_WAIT_WITHOUT_EXTERNAL_RESPONSE_AND_CONTINUE_INTERNAL_ONLY` | Diego/operator | E11 scaffold and explicit E12 operator decision | Close wait as internal/operator-attested only | External approval, audit pass, reviewer findings or product authority | `STOP_AFTER_EXTERNAL_REVIEW_WAIT_CLOSED_INTERNAL_ONLY_NO_PRODUCT_AUTHORITY` | Yes, for next internal safe gate | `NO` |
| `APPROVE_EXTERNAL_REVIEW_RESPONSE_FOR_RECORDING_WITH_CONTENT` | Diego/operator | Actual external/manual response content provided by Diego | Record attributed response as documentation/audit feedback | Treat response as runtime/product, CI or release authority | `STOP_FOR_OPERATOR_DECISION_ON_EXTERNAL_REVIEW_FINDINGS_CORRECTION_SCOPE` | Yes, for any correction beyond docs/test/metadata | `NO` |
| `INTERNAL_CONTINUATION_GATE_RECONCILED_NO_PRODUCT_AUTHORITY` | Codex records; Diego/operator decides next step | E13 reconciliation | Recommend a future internal safe gate | Treat internal continuation as external approval or product progression | `STOP_FOR_OPERATOR_DECISION_ON_NEXT_INTERNAL_SAFE_GATE` | Yes, to execute the recommended gate | `NO` |
| `PRODUCT_LEDGER_LOCAL_DEV_MANUAL_GATE_DECISION_TABLE_DOCS_TEST_ONLY` | Diego/operator authorizes; Codex records/tests | E13 recommendation and E14 scope | Clarify manual gates and add focal guard | Runtime/product, CI enforcement or release/commercial claim | `STOP_AFTER_MANUAL_GATE_DECISION_TABLE_READY_NO_PRODUCT_AUTHORITY` | Yes, for any next gate | `NO` |
| `FUTURE_DOCS_TEST_METADATA_CORRECTION_GATE` | Diego/operator | Specific finding or ambiguity scoped to docs/test/metadata | Docs/test/metadata correction only | Product behavior, source runtime, CI enforcement or release claim | `STOP_FOR_OPERATOR_DECISION_ON_DOCS_TEST_METADATA_CORRECTION_SCOPE` | `REQUIRES_SEPARATE_EXPLICIT_OPERATOR_AUTHORIZATION`; `REQUIRES_NEW_BLOCK`; `REQUIRES_NO_GO_IF_AMBIGUOUS` | `NO` |
| `FUTURE_RUNTIME_PRODUCT_AUTHORIZATION_GATE` | Diego/operator only | Separate future authorization and passing safety gates | `NOT_AUTHORIZED_NOW` | Any implicit runtime/product enablement from E-series evidence | `STOP_FOR_SEPARATE_RUNTIME_PRODUCT_AUTHORIZATION` | `REQUIRES_SEPARATE_EXPLICIT_OPERATOR_AUTHORIZATION`; `REQUIRES_NEW_BLOCK`; `REQUIRES_NO_GO_IF_AMBIGUOUS` | `NO` |
| `FUTURE_CI_ENFORCEMENT_GATE` | Diego/operator only | Separate future CI policy authorization | `NOT_AUTHORIZED_NOW` | Treat manual/discovery gates as CI enforcement | `STOP_FOR_SEPARATE_CI_ENFORCEMENT_AUTHORIZATION` | `REQUIRES_SEPARATE_EXPLICIT_OPERATOR_AUTHORIZATION`; `REQUIRES_NEW_BLOCK`; `REQUIRES_NO_GO_IF_AMBIGUOUS` | `NO` |
| `FUTURE_RELEASE_COMMERCIAL_GATE` | Diego/operator only | Separate future release/commercial authorization and passing gates | `NOT_AUTHORIZED_NOW` | Release/commercial/public-product readiness claim | `STOP_FOR_SEPARATE_RELEASE_COMMERCIAL_AUTHORIZATION` | `REQUIRES_SEPARATE_EXPLICIT_OPERATOR_AUTHORIZATION`; `REQUIRES_NEW_BLOCK`; `REQUIRES_NO_GO_IF_AMBIGUOUS` | `NO` |

## Required Rule

Every current E-series gate has Product/runtime authority = `NO`.

## Future Gate Rule

Future runtime/product, CI enforcement and release/commercial gates are:

- `NOT_AUTHORIZED_NOW`
- `REQUIRES_SEPARATE_EXPLICIT_OPERATOR_AUTHORIZATION`
- `REQUIRES_NEW_BLOCK`
- `REQUIRES_NO_GO_IF_AMBIGUOUS`

## External Review Honesty Rule

No gate may claim external review response, external reviewer approval or external audit pass unless actual response content is provided and recorded.

## Manual vs CI Rule

Manual/operator-run gates must not be described as CI enforcement.

Manual/discovery gates can support local/dev evidence, but they do not create CI enforcement, product authority, runtime authority or release/commercial readiness.

## Canonical Stop Conditions

- `STOP_FOR_OPERATOR_DECISION_ON_NEXT_INTERNAL_SAFE_GATE`
- `STOP_AFTER_MANUAL_GATE_DECISION_TABLE_READY_NO_PRODUCT_AUTHORITY`
- `STOP_FOR_OPERATOR_TO_PROVIDE_EXTERNAL_REVIEW_RESPONSE_CONTENT`
- `STOP_FOR_OPERATOR_DECISION_ON_DOCS_TEST_METADATA_CORRECTION_SCOPE`
- `STOP_FOR_SEPARATE_RUNTIME_PRODUCT_AUTHORIZATION`
- `STOP_FOR_SEPARATE_CI_ENFORCEMENT_AUTHORIZATION`
- `STOP_FOR_SEPARATE_RELEASE_COMMERCIAL_AUTHORIZATION`

## Findings

P0: 0.

P1: 0.

P2: 0.

P3:

- Manual/operator gate ambiguity is reduced but still requires future operator decisions.
- Future gates can still be overclaimed if separated from this table.

P4:

- The table intentionally repeats blocked states to reduce ambiguity.

## Stop Condition

`STOP_AFTER_MANUAL_GATE_DECISION_TABLE_READY_NO_PRODUCT_AUTHORITY`

E14 clarifies manual gates only. It does not authorize runtime/product, CI enforcement, release/commercial or external audit approval.
