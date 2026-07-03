# Redaction Before Persistence Service Design Only

Status: `DESIGN_ONLY / FUTURE_SERVICE / NO_RUNTIME_NO_PRODUCT_NO_REGISTRATION`

Baseline HEAD: `9f57da54f9a0975c7fdaf6fdfdccbf0e2ad2d3f7`

Decision: define the future redaction-before-persistence service boundary and required evidence without implementing it. Runtime/live product enablement, service registration, command handlers, UI product actions, product ledger paths, DB/migration, provider/cloud/network, Browser/CDP, WCU/OCR, Recipes live execution, product redaction service activation and release/commercial readiness remain prohibited.

## Context

The post-Stage 2 global audit selected `NODAL_OS_REDACTION_BEFORE_PERSISTENCE_SERVICE_DESIGN_ONLY` as the next safe scope. The current Stage 2 implementation contains a deterministic test-only gate:

- `AppendStage2TestOnly(policy, request, gate)` is additive and delegates to Stage 1 `Append` only after checks pass.
- `DurableAuditTrailAppendOnlyMinimalRedactionProof` is caller-attested evidence, not a service.
- `ValidateStage2SensitiveData` rejects secret-like content, email-like PII, Windows absolute paths and UNC-like paths before persistence.
- Product ledger paths, product-like feature flags and missing redaction proof are rejected.

This ADR designs a future service that could replace caller attestation with deterministic classification, redaction/rejection and non-leaking evidence. It does not wire or register that service.

## Future Service Boundary

Conceptual name: `NodalOsRedactionBeforePersistenceService`.

Responsibility: classify, reject or redact candidate event content before any append, log, durable write, replay materialization or read-model persistence boundary receives it.

The future append layer must accept only a successful redaction result or an explicit rejection result. It must never accept a raw candidate envelope directly.

## Inputs

The future service may receive only an in-memory candidate and policy context:

- candidate event kind;
- actor reference;
- approval reference;
- evidence references;
- metadata map, including nested conceptual maps/lists when supported by policy;
- raw payload presence indicator and raw payload value if the caller has not already forbidden it;
- policy id and policy version;
- caller context such as test-only, dev-sandbox candidate or product-candidate review state;
- target persistence boundary, such as local temp test ledger, dev sandbox ledger or product-candidate ledger review.

Inputs are not persisted until the service returns a safe result. Error paths must not echo raw values.

## Outputs

The service has three conceptual result families:

| Result | Meaning | Persistence eligibility |
| --- | --- | --- |
| `Rejected` | The candidate contains unsupported, unclassified or forbidden content. | Only safe reason codes and non-leaking evidence may be recorded. Raw values are discarded. |
| `Redacted` | The candidate is transformed into a safe envelope under policy. | Only the redacted envelope, classifications, policy version and safe evidence may be passed onward. |
| `Evidence` | Non-leaking proof that classification/redaction ran before append. | May accompany a redacted envelope or a rejection record; must not contain secrets, PII, paths or raw payload fragments. |

## Reject Versus Redact Policy

Reject by default when policy is absent, unknown or ambiguous.

The future service must reject:

- raw payloads when no explicit policy permits canonical safe summarization;
- credentials, tokens, API keys, cookies, authorization headers and private keys;
- redaction failures or classifier failures;
- unclassified nested values;
- unsupported nested metadata shapes;
- product ledger path attempts without an approved product runtime gate;
- unreviewed PII when policy has no explicit PII rule;
- path-like data that can disclose local/user/system structure;
- policy conflicts or unknown policy versions;
- any attempted bypass where append is called without prior service evidence.

The future service may redact or omit only when policy explicitly defines a deterministic safe replacement. Examples: preserve safe references, replace a value with a classification marker, or produce a canonical safe summary.

## Metadata, References And Nested Values

Every key and value must be classified. Safe keys may be preserved only if the key itself is not sensitive. Unknown keys, unknown value types and unsupported nested structures fail closed.

Nested data has two allowed designs for a future implementation:

- canonical flattening into safe classified paths, with raw leaves omitted or replaced;
- full rejection when the policy cannot prove every leaf is classified and safe.

Evidence references are references only. The service must not dereference files, URLs, DB rows, cloud objects, Browser/CDP state, WCU/OCR captures or Recipes output in this design-only scope.

## Ordering

Required future ordering:

1. Caller builds an in-memory candidate.
2. Caller invokes the redaction-before-persistence service.
3. Service returns `Rejected`, `Redacted` or failure.
4. Append receives only the redaction result/evidence, never the raw candidate.
5. Append persists only the safe envelope or safe rejection evidence.
6. Replay/read-model/checkpoint consumers use only the persisted safe envelope and evidence.

Any code path that can append or persist without service evidence is a P0/P1 scope leak for product/runtime work and a P2 blocker for test-only expansion.

## Failure Modes

The future service must fail closed for:

- null input;
- malformed candidate;
- missing policy id;
- unknown policy version;
- classifier exception;
- redactor exception;
- unsupported nested shape;
- forbidden raw payload;
- secret-like hit;
- PII-like hit;
- path-like hit;
- unsafe evidence reference;
- policy conflict;
- evidence generation failure;
- error-message redaction failure;
- attempted bypass/order violation.

Safe failures return reason codes only. They must not log, persist or serialize raw rejected values.

## Future Integration Points

Durable Audit Trail Stage 2 test-only may later integrate this design only after a separate external audit and explicit manual GO. Product/runtime integration remains a separate future decision and is not authorized by this ADR.

The current Stage 2 caller-attested redaction proof is acceptable only as test-only evidence. A future implementation should replace caller attestation with service-produced evidence before any broader enablement claim.

## Anti-Capabilities

This ADR does not:

- implement the service;
- register a service or hosted runtime;
- add command handlers, command bus wiring or UI product actions;
- create product ledger paths;
- add DB/migration, provider, cloud or network behavior;
- connect to Browser/CDP, WCU/OCR, Recipes live execution, Pilot or Nexa;
- enable runtime/live/product behavior;
- store or log raw sensitive values;
- bypass redaction for product paths;
- claim WORM, KMS, compliance-grade checkpoint trust or release/commercial readiness.

## Required Future Test Matrix

Before implementation, a future macro-block must materialize tests or equivalent evidence for:

| Area | Required evidence |
| --- | --- |
| Secret-like corpus | Tokens, API keys, private-key markers, JWT-like strings and authorization headers reject before persistence. |
| PII-like corpus | Email-like PII and future policy-defined PII reject or redact deterministically. |
| Path-like corpus | Windows absolute paths, UNC-like paths and user/system path fragments reject before persistence. |
| Null/malformed metadata | Null, malformed, unknown and unsupported metadata shapes fail closed. |
| References | Null/malformed/unsafe evidence references do not leak and do not trigger dereference side effects. |
| Nested metadata | Every nested key/value is classified; unsupported shapes reject. |
| Evidence safety | Redaction evidence contains policy/version/classes/hashes/safe summaries only; no raw values. |
| Append ordering | Append is blocked when redaction is missing, failed, stale, after-the-fact or mismatched to candidate hash. |
| Positive append | Append succeeds only after successful service result under local/temp or explicitly authorized boundary. |
| No registration | Static scans prove no service registration, handlers, UI actions or runtime product wiring. |
| No external side effects | Static scans prove no DB/cloud/provider/network/Browser/CDP/WCU/OCR/Recipes live access. |
| Replay/read model | Replay and read-model paths consume only redacted envelopes and do not leak raw values. |
| Concurrency | Concurrent candidates cannot mix redaction evidence across events. |
| Logs/errors | Logs, exceptions and QA reports contain only safe reason codes and summaries. |

## Findings

| Severity | Finding |
| --- | --- |
| P0 | None. No runtime/product/live authority introduced. |
| P1 | None. No service registration, command handler, UI action, DB/cloud/network or product ledger path introduced. |
| P2 | None for this design-only scope. Product/runtime implementation remains blocked until external audit and manual GO. |
| P3 | The service is design-only; current Stage 2 still uses caller-attested test-only redaction proof. |
| P3 | The future classifier/redactor corpus must be expanded before implementation to reduce false negatives and false positives. |
| P3 | Ordering, evidence leakage and error-message leakage remain future implementation risks. |
| P4 | Historical redaction and privacy docs remain traceability records under current decision-log canon. |

## Percentages

| Track | Conservative status |
| --- | --- |
| Redaction-before-persistence service design | 72-78% design-only |
| Product redaction service implementation | 0% / NO-GO |
| Durable Stage 2 test-only implementation | 88-92% |
| Runtime/live product enablement | 0% / NO-GO |
| Release/commercial readiness | 0% / NO-GO |
| Proyecto usable end-to-end | 24-34% |

## Decision

`GO_WITH_FINDINGS_REDACTION_BEFORE_PERSISTENCE_SERVICE_DESIGN_ONLY_READY`

The next safe macro-block is `NODAL_OS_REDACTION_BEFORE_PERSISTENCE_SERVICE_DESIGN_EXTERNAL_AUDIT_READ_ONLY`. Implementation, runtime/product enablement, service registration and product ledger paths still require a separate manual GO.
