# Product Ledger Public Command Action Exposure Test Plan Only

Date: 2026-07-04

Decision: `GO_WITH_FINDINGS_PRODUCT_LEDGER_PUBLIC_COMMAND_ACTION_EXPOSURE_TEST_PLAN_ONLY_READY`

## Context

The public UI read-only disabled mock preview is now available as a Core-only disabled model. The next real frontier is public UI action execution or public/product command handler exposure. This ADR is test-plan-only and does not implement that frontier.

The plan defines the minimum evidence and negative tests required before any future public command/action exposure can be considered.

## Scope

Allowed:

- public command/action exposure test plan;
- negative guard matrix;
- evidence prerequisites;
- launch blocker mapping;
- external audit read-only checklist.

Not allowed in this block:

- public UI action implementation;
- public/product command handler exposure;
- destructive action execution;
- endpoint/controller/route mapping;
- productive DI/service registration;
- physical writer/export authority;
- external/cloud export;
- provider/cloud/network;
- DB/migration;
- KMS/WORM/external trust;
- Browser/CDP/WCU/OCR/Recipes live execution;
- release/commercial readiness.

## Required Preconditions Before Future Exposure

| Gate | Required evidence | Required tests | Current state |
| --- | --- | --- | --- |
| Operator identity | Authenticated local operator identity and scope evidence. | missing/forged/operator-mismatch fails closed. | Not implemented. |
| Fresh public readiness packet | Fresh matrix, threat model and blocker map. | stale/missing/inconsistent packet fails closed. | Design docs exist; no public runtime check. |
| Redaction-before-display | Safe display payload and no raw secret/PII/path leakage. | raw payload, secret-like, PII-like and path-like corpus fails closed. | Required, not public-exposed. |
| Command allowlist | Explicit public-safe command catalog. | unknown/unsupported/corrupt command fails closed. | Not implemented. |
| Disabled action invariant | Disabled state cannot become executable by stale UI state. | disabled action execute attempt fails closed. | Mock only. |
| Router mediation | Public action must route through preview router then handler. | direct handler bypass fails closed. | Test-plan only. |
| Handler exposure contract | No handler id/callback/productive command id in read-only models. | handler/callback/productive id presence rejects render. | Covered in mock tests. |
| Destructive action policy | Separate approval and rollback/non-rollback classification. | destructive command fails closed without explicit destructive GO. | Not implemented. |
| Ledger/checkpoint integrity | Read/verify before any display/action. | missing/corrupt/stale ledger/checkpoint fails closed. | Internal/local-only only. |
| Export boundary | Public surface cannot unbound physical export or external/cloud export. | public export/unbounded path/cloud claim fails closed. | Bounded internal export only. |
| Provider/cloud/network | Explicit separate policy and GO. | provider/cloud/network request fails closed. | Not authorized. |
| DB/migration | Explicit separate policy and GO. | DB/migration request fails closed. | Not authorized. |
| KMS/WORM/external trust | Explicit external custody/key/trust design and audit. | KMS/WORM/external trust claim fails closed. | Not authorized. |
| Live automation | Explicit live Browser/CDP/WCU/OCR/Recipes authority. | live automation request fails closed. | Not authorized. |
| Release/commercial | Business/release decision and release gates. | release/commercial claim fails closed. | Not authorized. |

## Public Command Negative Test Matrix

Every future public command/action exposure must include negative tests for:

- missing request;
- missing operator identity;
- forged operator identity;
- missing explicit public action scope;
- stale UI state;
- stale public readiness packet;
- missing redaction evidence;
- raw payload or secret-like display payload;
- path-like payload;
- PII-like payload;
- unknown command;
- unsupported command kind;
- corrupt command id;
- command id casing/whitespace variants;
- handler id present in read-only model;
- callback name present in read-only model;
- productive command id present in read-only model;
- disabled action execution attempt;
- destructive action attempt;
- direct handler bypass attempt;
- router decision mismatch;
- stale ledger/checkpoint state;
- corrupt ledger/checkpoint state;
- append hash mismatch;
- rollback evidence missing;
- non-rollback classification missing;
- external/cloud export attempt;
- unbounded physical export/write attempt;
- provider/cloud/network request;
- DB/migration request;
- KMS/WORM/external trust claim;
- Browser/CDP/WCU/OCR/Recipes live request;
- telemetry/sync/billing/licensing cloud claim;
- release/commercial claim.

## Required Static Scans

Future exposure work must scan changed files for:

- endpoint/controller/route mapping;
- productive DI/service registration;
- product command handler exposure;
- callback/handler ids in public view models;
- executable public action flags;
- public destructive action labels and handlers;
- provider/cloud/network calls;
- DB/migration references;
- KMS/WORM/external trust references;
- Browser/CDP/WCU/OCR/Recipes live execution references;
- physical write/export outside bounded internal export;
- release/commercial readiness wording.

## Launch Blockers

Hard blockers before real public command/action exposure:

- no public operator identity model;
- no public-safe command allowlist;
- no destructive action approval model;
- no public redaction/display policy;
- no public ledger/checkpoint freshness policy;
- no direct handler bypass tests;
- no stale UI command tests;
- no explicit business/release decision;
- no provider/cloud/network policy;
- no external trust/KMS/WORM policy.

## Stop Packet

`PUBLIC_UI_ACTION_OR_PRODUCT_COMMAND_HANDLER_IMPLEMENTATION_REQUIRES_NEW_EXPLICIT_GO`

The next step after this plan is no longer another safe internal/read-only step if it implements or exposes public UI actions or product command handlers. It requires explicit human GO.

## Decision

`GO_WITH_FINDINGS_PRODUCT_LEDGER_PUBLIC_COMMAND_ACTION_EXPOSURE_TEST_PLAN_ONLY_READY`.
