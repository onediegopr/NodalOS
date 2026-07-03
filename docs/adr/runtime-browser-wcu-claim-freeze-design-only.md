# Runtime / Browser / WCU Authority Claim Freeze Design-Only

Status: `DESIGN_ONLY / CLAIM_FREEZE / PRODUCT_AUTHORITY_NOT_GRANTED`

Baseline HEAD: `08254288934e69252330f7b52fddc90ca2bfc7d6`

Decision: freeze all runtime, Browser/CDP/ChromeLab, WCU/OCR, Recipes and release/commercial claims at their current audited authority levels. Existing lab, fixture, test, historical or design footprints must not be upgraded into product authority without a dedicated scope proposal, external audit and explicit user GO.

## Purpose

This ADR consolidates the claim boundary after the Browser/CDP/ChromeLab boundary hardening block. It prevents real-looking source footprints from being cited as product readiness:

- ChromeLab has real lab runtime registrations, endpoints and WebSockets.
- BrowserRuntime has live CDP healthcheck capability behind guard/artifact requirements.
- WCU/OCR has fixture, read-only, design and mixed OCR model/runtime footprints.
- Durable Audit Trail Stage 1 has local/test-safe append/write only.
- Recipes have design/test/readiness footprints but no live runner authority in the current canon.

This ADR is documentation-only. It does not modify code, tests, runtime behavior, service registrations, command handlers, product actions, UI, endpoints, DB/migration, provider/cloud/network, Browser/CDP live paths, WCU/OCR live actions, Recipe live execution, Durable Stage 2 or release/commercial readiness.

## Claim Freeze Matrix

| Area | Claim allowed now | Claim prohibited now | Current authority | Evidence required before upgrade | Required audit before upgrade |
| --- | --- | --- | --- | --- | --- |
| Durable Audit Trail | Stage 1 local/test-safe append/write candidate exists; temp/local-test JSONL writes are covered by tests. | Product audit trail enabled; product ledger path; Stage 2 enabled; release/commercial audit guarantee. | `LOCAL_TEST_SAFE_ONLY / IMPLEMENTED_NOT_ENABLED` | Stage 2 scope, redaction-before-persistence, runtime flag plan, external audit, manual GO. | Durable Stage 2 pre-implementation and post-implementation external audits. |
| Browser/CDP/ChromeLab | Lab/separate/historical runtime footprint exists; ChromeLab endpoints and WebSockets are real lab evidence. | Product browser automation ready/enabled; CDP live product authority; ChromeLab as current NODAL OS product runtime. | `LAB_RUNTIME_ONLY / NOT_PRODUCT_AUTHORITY` | Browser/CDP scope proposal, endpoint/handler review, no-cross-enable proof, negative tests, manual GO. | Browser/CDP/ChromeLab product authority external audit. |
| Runtime/service/handlers | Separate or historical service/endpoint/handler footprints exist and must be classified before use. | Global runtime/live product enablement; service registration authority; command bus or command handler authority. | `FROZEN / NO_GLOBAL_PRODUCT_AUTHORITY` | Inventory of every registration, handler, endpoint, UI action and feature flag in target scope. | Runtime/service/handler authority external audit. |
| WCU/OCR | WCU fixture-safe/read-only/design-only evidence exists; OCR model/test/readiness footprints exist with no product authority. | WCU live action authority; OCR action authority; UIA/Win32 action execution; product PC Commander authority. | `FIXTURE_SAFE_OR_DESIGN_ONLY / PRODUCT_AUTHORITY_0` | Dedicated WCU/OCR authority proposal, live-action exclusion proof, OCR data policy, negative tests. | WCU/OCR product authority external audit. |
| Recipes | Recipe design/test/readiness artifacts exist; live execution remains blocked. | Recipe live runner, scheduler, trigger, retry loop or product execution authority. | `DESIGN_TEST_ONLY / LIVE_AUTHORITY_0` | Explicit Recipes live scope proposal, no Browser/WCU cross-enable proof, negative tests. | Recipes execution authority external audit. |
| Release/commercial | Release/commercial remains `NO-GO`; current percentages are internal readiness only. | Production ready, release ready, commercial ready, paid beta ready, MVP ready. | `NO-GO` | Full product authority audits, manual QA evidence, runtime/enablement audits, release checklist. | Release/commercial external audit. |

## Runtime / Service / Handler Classification Rules

| Evidence | Classification |
| --- | --- |
| `src/OneBrain.ChromeLab.Bridge/Program.cs` registrations/endpoints/WebSockets | `LAB_RUNTIME_FOOTPRINT` |
| `src/OneBrain.Pilot/Program.cs` mapped demo/pilot endpoints | `PILOT_OR_HISTORICAL_RUNTIME_FOOTPRINT_REQUIRES_DEDICATED_AUDIT` |
| `NexaAdminCommandHandler` / admin runtime service names | `HISTORICAL_OR_SEPARATE_ADMIN_HANDLER_FOOTPRINT_REQUIRES_DEDICATED_AUDIT` |
| Durable Audit Trail result flags with `CommandHandlerRegistered=false` and `ProductActionAllowed=false` | `NEGATIVE_ASSERTION` |
| Approval/EIL/Workspace/Recipe tests asserting no product actions or no command handlers | `TEST_GUARD` |
| Any new claim that global runtime, product service registration or product command handler is ready now | `TRUE_RISK` |

## WCU/OCR Classification Rules

| Evidence | Classification |
| --- | --- |
| `WindowsUiAutomationReadOnlyCollector` with screenshots/invoke blocked | `READ_ONLY_DESIGN_BOUNDARY` |
| `WindowsUiAutomationEventStream` with live subscription disabled and `ActionAuthority=false` | `READ_ONLY_OR_FIXTURE_SAFE_BOUNDARY` |
| `WindowsComputerUseControlPlane` fixtures, classifiers and safe action planner | `FIXTURE_SAFE_CONTROL_PLANE` |
| OCR model catalog, ONNX/PaddleOCR readiness or synthetic run services | `OCR_TECHNICAL_FOOTPRINT_NO_PRODUCT_AUTHORITY` |
| OCR comments mentioning real worker/runtime while blocked/obsolete | `HISTORICAL_OR_BLOCKED_OCR_FOOTPRINT` |
| Any claim that OCR or WCU can execute product actions now | `TRUE_RISK` |

## Cross-Boundary Freeze

| Boundary | Current result |
| --- | --- |
| Durable Audit Trail -> product runtime ledger | Frozen. Stage 1 local/test-safe only. |
| Browser/CDP -> Durable Audit Trail | No authorized cross-enable. |
| Browser/CDP -> WCU/OCR | No authorized cross-enable. |
| WCU/OCR -> Recipes live | No authorized cross-enable. |
| Recipes -> product execution | No live runner authority. |
| ChromeLab endpoints -> product authority | Lab runtime footprint only. |

## Findings

| Severity | Finding |
| --- | --- |
| P0 | None. No product authority or release/commercial readiness is authorized by this ADR. |
| P1 | None. No code, tests or runtime behavior changed. |
| P2 | ChromeLab real lab runtime footprint, BrowserRuntime live CDP healthcheck capability, WCU/OCR mixed technical footprint and separate/historical runtime or handler names require claim freeze discipline. |
| P3 | `OneBrain.Pilot` and `NexaAdminCommandHandler` footprints need a future dedicated authority audit before any roadmap relies on them. |
| P4 | Historical docs remain useful but must be read through latest decision-log and QA reports. |

## Non-Goals

This ADR does not implement or enable Stage 2, runtime/live product behavior, Browser/CDP live automation, WCU/OCR live action, Recipes live execution, service registration, command handlers, product UI actions, DB/migration, provider/cloud/network, release/commercial readiness or stash changes.
