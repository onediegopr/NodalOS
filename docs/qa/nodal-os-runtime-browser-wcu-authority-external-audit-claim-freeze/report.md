# NODAL OS - Runtime / Browser / WCU Authority External Audit And Claim Freeze

Decision: `GO_WITH_FINDINGS_RUNTIME_BROWSER_WCU_AUTHORITY_CLAIM_FREEZE_READY`

Date: 2026-07-03

## Scope

This block performs a docs-only external-audit style reconciliation and claim freeze for:

1. Browser/CDP/ChromeLab boundary.
2. Runtime/service registration/command handler authority.
3. WCU/OCR product authority.
4. Cross-boundary risks between Browser/CDP, Durable Audit Trail, WCU/OCR and Recipes.
5. Claims allowed and prohibited before any Stage 2 or runtime work.

No source, tests, runtime, `Program.cs`, endpoints, service registrations, command handlers, product actions, CDP live activation, browser automation, WCU/OCR live action, Recipes live execution, Durable Stage 2, product ledger path, DB/migration, provider/cloud/network, release/commercial readiness or stash state was changed.

## Repo Guard

| Field | Value |
| --- | --- |
| Repo | `C:/DESARROLLO/NodalOS/Codigo-m12-audit` |
| Branch | `chrome-lab-001-extension-local-ai-bridge` |
| Input HEAD | `08254288934e69252330f7b52fddc90ca2bfc7d6` |
| Input commit | `08254288 docs(audit): harden browser cdp chromelab runtime boundary` |
| Worktree initial | `clean` |
| Origin sync initial | `0 0` |
| Stash | listed only, not touched: `stash@{0}: On chrome-lab-001-extension-local-ai-bridge: pre-m11-legacy-state` |

## Browser/CDP/ChromeLab External Audit Result

| Check | Result |
| --- | --- |
| Boundary ADR present | PASS |
| QA report and handoff present | PASS |
| ChromeLab classified as lab/separate/historical boundary | PASS |
| Existing `AddSingleton`, `AddHttpClient`, `MapGet`, `MapPost`, `/ws/extension`, `/ws/stealth` classified as lab runtime footprint | PASS_WITH_FINDINGS |
| `/ws/extension` and `/ws/stealth` product automation authority | NOT_AUTHORIZED |
| BrowserRuntime/CDP healthcheck equals product automation authority | NO |
| Product Browser/CDP live automation | 0% |
| Release/commercial | NO-GO |

## Runtime / Service / Handler Audit Result

| Evidence | Classification | Result |
| --- | --- | --- |
| `src/OneBrain.ChromeLab.Bridge/Program.cs` registrations/endpoints | `LAB_RUNTIME_FOOTPRINT` | P2 boundary, not product authority. |
| `src/OneBrain.Pilot/Program.cs` mapped demo/pilot endpoints | `PILOT_OR_HISTORICAL_RUNTIME_FOOTPRINT_REQUIRES_DEDICATED_AUDIT` | P3; not accepted as current product runtime authority in this block. |
| `src/OneBrain.BrowserExecutor.Cdp/NexaAdminConsoleServices.cs` and `NexaAdminRuntimeServices.cs` handler names | `HISTORICAL_OR_SEPARATE_ADMIN_HANDLER_FOOTPRINT_REQUIRES_DEDICATED_AUDIT` | P3; not accepted as current product command authority. |
| Durable Audit Trail Stage 1 flags | `NEGATIVE_ASSERTION` | `ProductActionAllowed=false`, `CommandHandlerRegistered=false`. |
| Approval/EIL/Workspace/Recipe tests with no-action/no-handler assertions | `TEST_GUARD` | PASS. |
| Global runtime/live authority | `NO_GLOBAL_PRODUCT_AUTHORITY` | PASS_NO_ENABLEMENT. |

## WCU/OCR Authority Result

| Evidence | Classification | Result |
| --- | --- | --- |
| `WindowsUiAutomationReadOnlyCollector` | `READ_ONLY_DESIGN_BOUNDARY` | Screenshots and invoke are blocked. |
| `WindowsUiAutomationEventStream` | `READ_ONLY_OR_FIXTURE_SAFE_BOUNDARY` | Live subscription disabled by default; `ActionAuthority=false`. |
| `WindowsComputerUseControlPlane` | `FIXTURE_SAFE_CONTROL_PLANE` | Fixtures/classifiers/planners exist; live execution remains blocked. |
| ONNX/PaddleOCR services under BrowserExecutor.Cdp | `OCR_TECHNICAL_FOOTPRINT_NO_PRODUCT_AUTHORITY` | Mixed OCR model/runtime footprints remain no-authority or blocked. |
| Historical/obsolete OCR worker comments | `HISTORICAL_OR_BLOCKED_OCR_FOOTPRINT` | Requires dedicated OCR audit before broad authority claims. |
| WCU/OCR product action authority | `0%` | PASS. |

## Cross-Boundary Result

| Boundary | Classification | Result |
| --- | --- | --- |
| Durable Audit Trail Stage 1 -> product runtime ledger | `NO_CONNECTION_AUTHORIZED` | Stage 1 remains local/test-safe only. |
| Browser/CDP -> Durable Audit Trail | `NO_CONNECTION_AUTHORIZED` | No cross-enable. |
| Browser/CDP -> WCU/OCR | `NO_CONNECTION_AUTHORIZED` | No cross-enable. |
| WCU/OCR -> Recipes live | `NO_CONNECTION_AUTHORIZED` | No cross-enable. |
| Recipes -> product execution | `NO_LIVE_RUNNER_AUTHORITY` | Live execution remains blocked. |
| ChromeLab endpoints -> product authority | `LAB_ONLY_BRIDGE` | Not product authority. |

## Claim Freeze Matrix

| Area | Allowed claim | Prohibited claim | Current authority | Evidence required before upgrade | Required audit |
| --- | --- | --- | --- | --- | --- |
| Durable Audit Trail | Stage 1 local/test-safe append/write candidate exists. | Product audit trail enabled; product ledger path; Stage 2 enabled. | `LOCAL_TEST_SAFE_ONLY` | Stage 2 scope, redaction-before-persistence, runtime flag, manual GO. | Durable Stage 2 external audit. |
| Browser/CDP/ChromeLab | Lab/separate/historical runtime footprint exists. | Product browser automation ready/enabled. | `LAB_RUNTIME_ONLY` | Browser/CDP scope proposal, no-cross-enable proof, negative tests. | Browser/CDP authority audit. |
| Runtime/service/handlers | Separate/historical footprints exist and require classification. | Global runtime/live product enablement or command authority. | `FROZEN / NO_GLOBAL_PRODUCT_AUTHORITY` | Full inventory of registrations, handlers, endpoints, UI actions. | Runtime authority audit. |
| WCU/OCR | Fixture-safe/read-only/design-only evidence exists. | WCU/OCR product action authority. | `PRODUCT_AUTHORITY_0` | WCU/OCR scope proposal, live-action exclusion proof. | WCU/OCR authority audit. |
| Recipes | Design/test/readiness artifacts exist. | Recipe live runner/scheduler/trigger execution. | `LIVE_AUTHORITY_0` | Recipes live scope proposal and negative tests. | Recipes execution audit. |
| Release/commercial | Release/commercial remains NO-GO. | Production/release/commercial/paid-beta/MVP ready. | `NO-GO` | Full product authority and release evidence. | Release external audit. |

## Overclaim Scan Classification

| Category | Result |
| --- | --- |
| TRUE_RISK | 0 in changed docs after claim freeze wording |
| Negative assertions | Present and expected |
| Prohibited boundaries | Present and expected |
| Design-only mentions | Present and expected |
| Historical references | Present and expected |
| Accepted local/test-safe wording | Present for Durable Stage 1 |
| Lab runtime wording | Present and classified |
| Fixture-safe wording | Present for WCU/OCR and tests |

## Findings

| Severity | Count | Details |
| --- | ---: | --- |
| P0 | 0 | No product authority, runtime enablement or release/commercial claim introduced. |
| P1 | 0 | No source/test/runtime mutation was made. |
| P2 | 4 | ChromeLab lab runtime footprint; BrowserRuntime live CDP healthcheck capability behind guards; WCU/OCR mixed technical footprint; runtime/service/handler claim freeze needed across historical footprints. |
| P3 | 3 | `OneBrain.Pilot` endpoints require future dedicated authority audit; Nexa admin handler names require future dedicated authority audit; OCR model/runtime wording requires dedicated OCR audit before broad claims. |
| P4 | 1 | Historical reports remain useful traceability but require latest-canon discipline. |

## Corrections Applied

- Added `docs/adr/runtime-browser-wcu-claim-freeze-design-only.md`.
- Added this QA report and JSON report.
- Added claim-freeze handoff.
- Updated `docs/decision-log.md`.

No code, tests, runtime, endpoints, service registrations, command handlers or product actions were changed.

## Information Insufficient Items

| Item | Classification | Required follow-up |
| --- | --- | --- |
| `OneBrain.Pilot` current product authority | `INFORMATION_INSUFFICIENT_FOR_AUTHORITY_UPGRADE` | Dedicated pilot/runtime authority audit before any claim. |
| Nexa admin handler current authority | `INFORMATION_INSUFFICIENT_FOR_AUTHORITY_UPGRADE` | Dedicated admin/handler authority audit before any claim. |
| OCR broad authority | `INFORMATION_INSUFFICIENT_FOR_AUTHORITY_UPGRADE` | Dedicated OCR authority audit before any claim. |

## What Remains Blocked

- Runtime/live product enablement.
- Browser/CDP live product automation.
- ChromeLab as product runtime authority.
- WCU/OCR live product action authority.
- Recipes live runner/scheduler/trigger execution.
- Durable Audit Trail Stage 2.
- Product ledger path.
- Service registration or command handler authority.
- Product UI actions.
- DB/migration.
- Provider/cloud/network.
- Release/commercial readiness.

## Validations

| Validation | Result |
| --- | --- |
| Repo guard | PASS |
| Browser/CDP boundary audit | PASS_WITH_FINDINGS |
| Runtime/service/handler audit | PASS_WITH_FINDINGS |
| WCU/OCR authority audit | PASS_WITH_FINDINGS |
| Cross-boundary audit | PASS |
| JSON validation | PASS |
| `git diff --check` | PASS |
| Static scan changed files | PASS; no TRUE_RISK, only negative assertions, prohibited boundaries, design-only mentions, historical references, accepted local/test-safe wording, lab runtime wording and fixture-safe wording |
| Tests/build | NOT_RUN_BY_DESIGN; docs-only claim freeze and no code/test changes |

## Percentages

| Track | Conservative status |
| --- | --- |
| Durable Audit Trail local/test-safe append/write candidate | 92-95% |
| Durable Stage 1 test-only enablement safety | 88-92% |
| Browser/CDP/ChromeLab boundary hardening | 85-90% |
| Runtime/Browser/WCU authority claim freeze | 80-85% |
| Browser/CDP current product authority | 0% |
| WCU/OCR product authority | 0% |
| Runtime/live product enablement | 0% |
| Execution/mutation broad | 0% |
| Release/commercial readiness | 0% / NO-GO |
| Proyecto usable end-to-end | 20-30% |

## Recommended Next Macro-Block

`NODAL_OS_RUNTIME_BROWSER_WCU_AUTHORITY_CLAIM_FREEZE_EXTERNAL_AUDIT_READ_ONLY`

Reason: the claim freeze is now versioned. The next safe step is a read-only external audit of the new ADR, QA report, handoff and decision-log entry before considering any Stage 2, Browser/CDP, WCU/OCR or runtime planning.
