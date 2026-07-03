# Redaction Before Persistence Service Test Plan Design Only

Status: `TEST_PLAN_DESIGN_ONLY / PRE_IMPLEMENTATION / NO_CODE_NO_RUNTIME`

Baseline HEAD: `1cd0188927fc7b8167c3245b98f0671b82673fe6`

Decision: define the required future test plan for a redaction-before-persistence service before any implementation. This ADR does not add tests, source code, runtime wiring, service registration, command handlers, UI product actions, product ledger paths, DB/migration, provider/cloud/network, Browser/CDP, WCU/OCR, Recipes live execution or release/commercial readiness.

## Context

The external audit of the service design accepted the design with findings and required a pre-implementation test-plan design for:

- candidate hash binding;
- stale, missing, mismatched and after-the-fact evidence rejection;
- nested metadata fixtures;
- log/error redaction assertions;
- corpus versioning, ownership and update cadence;
- post-implementation external audit plus manual GO before product/runtime adoption.

## Test Plan Principles

Future tests must prove the service fails closed before persistence. They must not rely on product paths, live runtime registration, DB/cloud/network, Browser/CDP, WCU/OCR or Recipes live execution.

Tests must be written before implementation in the future implementation macro-block, or explicitly imported as pre-existing fixtures with line-level evidence.

## Fixture Corpus

Future implementation must define a versioned corpus, initially conceptualized as `redaction-before-persistence-corpus.v1`.

| Corpus area | Required fixture classes |
| --- | --- |
| Secret-like | API-key-like values, token prefixes, JWT-like values, private-key markers, authorization headers, cookies. |
| PII-like | Email-like values, person-name-adjacent examples, phone-like examples if policy adds them, safe non-PII controls. |
| Path-like | Windows absolute paths, UNC paths, user-profile fragments, temp paths that are safe only as storage roots, safe opaque ids. |
| Raw payload | Raw payload present, raw payload blank, raw payload with forbidden content, policy-disallowed raw payload. |
| Metadata | Null, empty, flat safe map, sensitive key, sensitive value, unknown type, nested object, nested list, mixed safe/unsafe nested values. |
| References | Safe opaque references, malformed references, path-like references, URL-like references, dereference-attempt sentinels. |
| Errors | Classifier exception, redactor exception, policy lookup miss, evidence generation failure. |

Corpus ownership, update cadence and compatibility rules must be recorded in the future implementation QA report.

## Required Future Tests

| Gate | Test intent | Required result |
| --- | --- | --- |
| RBP-T0 | Missing policy id/version. | Reject before persistence with safe reason code. |
| RBP-T1 | Unknown policy version. | Reject before persistence. |
| RBP-T2 | Secret-like content in any candidate field. | Reject; raw value absent from result, logs and persisted evidence. |
| RBP-T3 | Email-like PII in actor, approval, metadata, evidence reference or raw payload. | Reject or redact only under explicit policy. |
| RBP-T4 | Windows/UNC/path-like data in candidate fields. | Reject before persistence unless explicitly classified as safe storage boundary metadata. |
| RBP-T5 | Nested metadata with unsupported shape. | Reject before persistence. |
| RBP-T6 | Nested metadata with all leaves classified. | Redact/omit deterministically or reject according to policy. |
| RBP-T7 | Missing redaction evidence at append. | Append blocked. |
| RBP-T8 | Failed redaction evidence at append. | Append blocked. |
| RBP-T9 | Stale or after-the-fact evidence. | Append blocked. |
| RBP-T10 | Evidence candidate hash mismatch. | Append blocked. |
| RBP-T11 | Evidence contains raw sensitive value. | Reject evidence; append blocked. |
| RBP-T12 | Safe redacted envelope. | Append allowed only within authorized local/temp or explicitly approved boundary. |
| RBP-T13 | Log/exception/error message. | Contains safe reason codes only. |
| RBP-T14 | Replay/read model. | Reads redacted envelopes only and never reconstructs raw values. |
| RBP-T15 | Concurrent candidates. | Evidence cannot be swapped across candidates. |
| RBP-T16 | Static no-registration scan. | No service registration/runtime/product wiring unless separately authorized. |
| RBP-T17 | Static no-external-side-effect scan. | No DB/cloud/provider/network/Browser/CDP/WCU/OCR/Recipes live access. |
| RBP-T18 | Product ledger path attempt. | Rejected; no product path persistence. |
| RBP-T19 | Release/commercial flags. | Remain false/NO-GO. |

## Evidence Schema Requirements

Future evidence must include:

- policy id;
- policy version;
- classifier version;
- redactor version;
- candidate hash over canonical safe candidate shape;
- decision: rejected or redacted;
- classification categories;
- redacted field map or omitted field map;
- safe reason codes;
- created timestamp or monotonic test marker for stale-evidence tests.

Future evidence must not include:

- raw payload values;
- raw secret/PII/path fragments;
- dereferenced reference content;
- stack traces with sensitive values;
- product path material;
- external service response data.

## Static Scan Requirements

Every future implementation commit must include static scans for:

- service registration and hosted runtime wiring;
- command handlers and command bus wiring;
- UI product actions;
- product ledger path strings;
- DB/migration/provider/cloud/network calls;
- Browser/CDP/WCU/OCR/Recipes live access;
- release/commercial readiness claims;
- raw-value logging and exception echoing.

## Findings

| Severity | Finding |
| --- | --- |
| P0 | None. No implementation or runtime/product authority introduced. |
| P1 | None. No product enablement or release/commercial claim. |
| P2 | None for test-plan design scope. Implementation remains blocked. |
| P3 | The plan is not executable until future tests and implementation are explicitly authorized. |
| P3 | Corpus ownership/cadence is defined as a future requirement, not yet assigned to code owners. |
| P3 | Product/runtime adoption remains blocked after any implementation until post-implementation audit and manual GO. |
| P4 | Percentages are planning estimates only. |

## Percentages

| Track | Conservative status |
| --- | --- |
| Redaction-before-persistence service design | 76-82% design-only with test-plan packet |
| Pre-implementation test-plan design | 70-76% design-only |
| Product redaction service implementation | 0% / NO-GO |
| Runtime/live product enablement | 0% / NO-GO |
| Release/commercial readiness | 0% / NO-GO |
| Proyecto usable end-to-end | 24-34% |

## Decision

`GO_WITH_FINDINGS_REDACTION_BEFORE_PERSISTENCE_SERVICE_TEST_PLAN_DESIGN_ONLY_READY`

Next step requiring pause: any implementation of the service, new tests, runtime/product enablement, service registration or product ledger path requires explicit manual GO.
