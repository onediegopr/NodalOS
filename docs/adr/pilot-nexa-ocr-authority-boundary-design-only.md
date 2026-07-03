# Pilot / Nexa / OCR Authority Boundary Design-Only

Status: `DESIGN_ONLY / AUTHORITY_BOUNDARY / PRODUCT_AUTHORITY_NOT_GRANTED`

Baseline HEAD: `7f7ddf64bbd564ecb4f02c90d5b3fa7398e6cbc8`

Decision: preserve `OneBrain.Pilot`, Nexa admin handlers and OCR/WCU technical footprints as separate, audited-boundary evidence. None of these footprints grants current NODAL OS product runtime authority, command authority, live action authority, Stage 2 Durable Audit Trail authority or release/commercial readiness.

## Purpose

This ADR hardens the information-insufficient items left by the runtime/browser/WCU claim-freeze block. The audit found real-looking local runtime, admin handler and OCR footprints that can be misread as current product authority if they are referenced without boundary language.

This ADR is documentation-only. It does not modify source code, tests, `Program.cs`, endpoints, service registrations, command handlers, product actions, UI actions, provider/cloud/network paths, DB/migration, Browser/CDP live automation, WCU/OCR live actions, Recipes live execution, Durable Audit Trail Stage 2 or release/commercial status.

## Authority Classification Rules

| Evidence type | Required classification |
| --- | --- |
| Local pilot ASP.NET endpoints, local stores or supervised harness paths | `PILOT_SEPARATE_LOCAL_RUNTIME_FOOTPRINT_REQUIRES_DEDICATED_AUTHORITY_AUDIT` |
| Nexa admin handler or admin console state mutation services | `SEPARATE_ADMIN_HANDLER_FOOTPRINT_REQUIRES_DEDICATED_AUTHORITY_AUDIT` |
| OCR model/session/worker/readiness footprints with blocked or no-authority policy | `OCR_TECHNICAL_FOOTPRINT_NO_PRODUCT_AUTHORITY` |
| WCU fixture/read-only/control-plane evidence with action authority false | `WCU_FIXTURE_OR_READ_ONLY_NO_PRODUCT_AUTHORITY` |
| Browser/CDP/ChromeLab endpoints or WebSockets under ChromeLab | `LAB_RUNTIME_ONLY_NOT_PRODUCT_AUTHORITY` |
| Durable Audit Trail Stage 1 local/temp ledger writes | `LOCAL_TEST_SAFE_ONLY_NOT_PRODUCT_ENABLEMENT` |
| Any claim that these footprints are current product runtime or release-ready authority | `TRUE_RISK` |

## Dedicated Audit Results

| Area | Evidence | Classification | Current product authority |
| --- | --- | --- | --- |
| `OneBrain.Pilot` | `src/OneBrain.Pilot/Program.cs` maps many local endpoints, creates local pilot services and calls local stores. `/executor-harness/click` uses a supervised harness path and local evidence/run writes. | `PILOT_SEPARATE_LOCAL_RUNTIME_FOOTPRINT_REQUIRES_DEDICATED_AUTHORITY_AUDIT` | `0%` for current NODAL OS product authority. |
| Pilot recipe execution | `PilotRecipeExecutor` is wired by the pilot app and must not be cited as current product Recipes authority without a dedicated audit. | `INFORMATION_INSUFFICIENT_FOR_AUTHORITY_UPGRADE` | `0%` for current Recipes live authority. |
| Nexa admin handlers | `NexaAdminCommandHandler`, `NexaAdminQueryHandler` and `NexaAdminConsoleService` exist and mutate admin state in their own boundary; sensitive/productive flags are gated or denied. | `SEPARATE_ADMIN_HANDLER_FOOTPRINT_REQUIRES_DEDICATED_AUTHORITY_AUDIT` | `0%` for current NODAL OS command authority. |
| OCR | ONNX/PaddleOCR/readiness/worker footprints exist, with no-authority, blocked, obsolete or policy-gated wording across the audited files. | `OCR_TECHNICAL_FOOTPRINT_NO_PRODUCT_AUTHORITY` | `0%` for OCR product action authority. |
| WCU | UIA, Win32, visual perception and safe-mode/control-plane evidence keeps action authority false and blocks live action. | `WCU_FIXTURE_OR_READ_ONLY_NO_PRODUCT_AUTHORITY` | `0%` for WCU product authority. |

## Claim Boundary Matrix

| Area | Claim allowed now | Claim prohibited now | Required evidence before upgrade |
| --- | --- | --- | --- |
| Pilot | Separate local pilot runtime footprint exists and requires dedicated audit. | Current NODAL OS product runtime is enabled through Pilot; Pilot grants product execution authority. | Pilot authority proposal, endpoint inventory, local write inventory, execution path review, external audit and explicit user GO. |
| Nexa admin | Separate admin handler footprint exists and requires dedicated audit. | Nexa admin handlers are current NODAL OS command bus/product authority. | Admin command authority proposal, handler inventory, integration proof, negative tests, external audit and explicit user GO. |
| OCR | OCR technical/runtime/model footprints exist with product authority at `0%`. | OCR grants product action authority or WCU live action authority. | OCR authority proposal, action exclusion proof, data policy, negative tests, external audit and explicit user GO. |
| WCU | Fixture-safe/read-only/design-only WCU evidence exists; action authority remains false. | WCU live action/product automation is ready or enabled. | WCU authority proposal, live-action exclusion proof, no cross-enable proof, external audit and explicit user GO. |
| Browser/CDP/ChromeLab | Lab/separate/historical runtime footprint exists. | Product browser automation or CDP live product authority is ready/enabled. | Browser/CDP scope proposal, endpoint/handler review, no-cross-enable proof, external audit and explicit user GO. |
| Durable Audit Trail | Stage 1 local/test-safe append/write candidate exists. | Product audit trail enabled, Stage 2 enabled, product ledger path authorized. | Stage 2 proposal, redaction-before-persistence, runtime flag plan, external audit and explicit user GO. |
| Recipes | Design/test/readiness artifacts exist; live authority is `0%`. | Recipe runner/scheduler/trigger/retry/live execution is enabled. | Recipes execution proposal, no Browser/WCU cross-enable proof, external audit and explicit user GO. |
| Release/commercial | Release/commercial remains `NO-GO`. | Production-ready, release-ready, commercial-ready, paid-beta-ready or MVP-ready. | Full product authority audits, manual QA evidence and release gate external audit. |

## Cross-Boundary Result

| Boundary | Result |
| --- | --- |
| Pilot to Browser/CDP | No product Browser/CDP authority is granted by this ADR. Pilot browser bridges require a dedicated audit before any claim. |
| Pilot to WCU/OCR | No WCU/OCR product action authority is granted. Any supervised harness path remains a separate Pilot audit item. |
| Pilot to Durable Audit Trail | No product ledger path or Durable Stage 2 authority is granted. |
| Nexa admin to current runtime | Nexa admin handlers remain separate boundary evidence, not current NODAL OS command authority. |
| OCR to WCU actions | OCR remains perception/technical evidence only; it does not authorize actions. |
| Browser/CDP to WCU/OCR | No cross-enable is authorized. |
| Recipes to Browser/WCU/OCR | No live recipe runner or action bridge is authorized. |
| Durable to runtime/product ledger | Stage 1 remains local/test-safe only. |

## Findings

| Severity | Finding |
| --- | --- |
| P0 | None. No current product runtime authority, live action authority or release/commercial claim is authorized by this ADR. |
| P1 | None. This block is docs-only and does not alter code, tests or runtime behavior. |
| P2 | `OneBrain.Pilot` has a real local pilot runtime footprint with endpoints, local stores and supervised harness evidence that must not be read as current product authority. |
| P2 | Nexa admin services include handler-style/admin-state mutation footprints that must not be read as current NODAL OS command authority. |
| P2 | OCR has mixed technical/runtime/model footprints, but the current product action authority remains `0%`. |
| P2 | Cross-boundary wording must keep Pilot, Nexa, OCR/WCU, Browser/CDP, Recipes and Durable in separate authority tracks. |
| P3 | Pilot recipe execution and supervised click paths require a dedicated authority audit before roadmap use. |
| P3 | Nexa admin handler integration into current product runtime remains information insufficient for any upgrade claim. |
| P3 | Broad OCR authority requires a dedicated OCR audit before any non-fixture claim. |
| P4 | Historical reports remain traceability records and must be read through the latest decision log. |

## Anti-Capabilities

This ADR does not authorize runtime/live product enablement, service registration, command handler activation, UI product actions, CDP live activation, browser automation, WCU/OCR live action, Recipes live execution, Durable Stage 2, product ledger path, DB/migration, provider/cloud/network, release/commercial readiness or stash changes.

## Required Future Audits

1. `NODAL_OS_RUNTIME_BROWSER_WCU_PILOT_OCR_AUTHORITY_BOUNDARY_EXTERNAL_AUDIT_READ_ONLY`.
2. Dedicated `OneBrain.Pilot` runtime/endpoint/local-write/execution authority audit before any Pilot product claim.
3. Dedicated Nexa admin handler/command authority audit before any admin-command claim.
4. Dedicated OCR authority audit before any broad OCR/product perception claim.
