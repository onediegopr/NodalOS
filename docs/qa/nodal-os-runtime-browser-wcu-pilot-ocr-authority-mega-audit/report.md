# NODAL OS - Runtime / Browser / WCU / Pilot / OCR Authority Mega Audit

Decision: `GO_WITH_FINDINGS_PILOT_NEXA_OCR_AUTHORITY_BOUNDARY_READY`

Date: 2026-07-03

## Scope

This macro-block performs a docs-only mega audit and claim hardening pass for:

1. The runtime/browser/WCU claim freeze already versioned.
2. `OneBrain.Pilot` authority boundaries.
3. Nexa admin handler authority boundaries.
4. OCR authority boundaries.
5. Cross-boundary risk between Browser/CDP/ChromeLab, WCU/OCR, Recipes and Durable Audit Trail.

No source code, tests, `Program.cs`, endpoints, service registrations, command handlers, product actions, CDP live activation, browser automation, WCU/OCR live action, Recipes live execution, Durable Stage 2, product ledger path, DB/migration, provider/cloud/network, release/commercial readiness or stash state was changed.

## Repo Guard

| Field | Value |
| --- | --- |
| Repo | `C:/DESARROLLO/NodalOS/Codigo-m12-audit` |
| Branch | `chrome-lab-001-extension-local-ai-bridge` |
| Input HEAD | `7f7ddf64bbd564ecb4f02c90d5b3fa7398e6cbc8` |
| Input commit | `7f7ddf64 docs(audit): freeze runtime browser wcu authority claims` |
| Worktree initial | `clean` |
| Origin sync initial | `0 0` |
| Stash | listed only, not touched: `stash@{0}: On chrome-lab-001-extension-local-ai-bridge: pre-m11-legacy-state` |

## Files And Code Audited

| Area | Files / evidence |
| --- | --- |
| Claim freeze | `docs/adr/runtime-browser-wcu-claim-freeze-design-only.md`, QA report MD/JSON, handoff, `docs/decision-log.md` |
| Pilot | `src/OneBrain.Pilot/Program.cs`, `src/OneBrain.Pilot/PilotRecipeExecutor.cs`, related Pilot route/store evidence from static scans |
| Nexa admin | `src/OneBrain.BrowserExecutor.Cdp/NexaAdminConsoleServices.cs`, `src/OneBrain.BrowserExecutor.Cdp/NexaAdminRuntimeServices.cs`, related Nexa policy/profile/service evidence from static scans |
| OCR/WCU | `src/OneBrain.WindowsComputerUse/*`, OCR/ONNX/PaddleOCR readiness and worker services under `src/OneBrain.BrowserExecutor.Cdp`, related Safety tests |
| Cross-boundary | Browser/CDP/ChromeLab docs, Recipes docs/tests, Durable Audit Trail docs/tests, decision-log chain |

## Claim Freeze External Audit Result

| Check | Result |
| --- | --- |
| Claim Freeze Matrix covers Durable, Browser/CDP/ChromeLab, runtime/service/handlers, WCU/OCR, Recipes and release/commercial | PASS |
| Current authority percentages remain conservative | PASS |
| Product runtime authority claim introduced | NO |
| Product Browser/CDP claim introduced | NO |
| Product WCU/OCR claim introduced | NO |
| Durable Stage 2 claim introduced | NO |
| Release/commercial claim introduced | NO |
| Result | PASS_WITH_FINDINGS; Pilot, Nexa and OCR information-insufficient items now have a dedicated boundary ADR. |

## OneBrain.Pilot Authority Result

| Evidence | Classification | Result |
| --- | --- | --- |
| `src/OneBrain.Pilot/Program.cs` maps local ASP.NET endpoints with `MapGet`, `MapPost` and `app.Run()` | `PILOT_SEPARATE_LOCAL_RUNTIME_FOOTPRINT` | P2 boundary; not current NODAL OS product authority. |
| Pilot local stores and file reads/writes for flows, playback, harness evidence, approvals, run history, recipes and memory | `PILOT_LOCAL_IO_FOOTPRINT_REQUIRES_DEDICATED_AUDIT` | P2; not upgraded to product authority by this block. |
| `/executor-harness/click` creates an approval decision with `executionAllowed: true` and uses `PilotUiaHarnessClickExecutor` | `PILOT_SUPERVISED_HARNESS_REQUIRES_DEDICATED_AUTHORITY_AUDIT` | P2/P3; no product action authority granted. |
| `PilotRecipeExecutor` is wired by the pilot app | `INFORMATION_INSUFFICIENT_FOR_AUTHORITY_UPGRADE` | Requires dedicated Pilot/Recipes authority audit before any claim. |
| Pilot service registration into current NODAL OS product runtime | `NOT_CONFIRMED_BY_THIS_AUDIT` | No authority upgrade allowed. |

Conclusion: `OneBrain.Pilot` is a real separate local pilot footprint, not a current NODAL OS product runtime authority. Any roadmap claim using Pilot must first pass a dedicated authority audit.

## Nexa Admin Handlers Result

| Evidence | Classification | Result |
| --- | --- | --- |
| `NexaAdminCommandHandler` and `NexaAdminQueryHandler` exist | `SEPARATE_ADMIN_HANDLER_FOOTPRINT` | P2 boundary; not accepted as current NODAL OS command authority. |
| `NexaAdminConsoleService.Execute` mutates admin state in its own boundary | `ADMIN_STATE_MUTATION_FOOTPRINT_REQUIRES_AUDIT` | P2; current product command authority remains `0%`. |
| Productive replay/recorder feature flags are denied | `NEGATIVE_ASSERTION` | PASS boundary evidence. |
| Sensitive real pilot and productive vault paths require compliance/entitlement gates | `CONTROLLED_ADMIN_GATE` | PASS_WITH_FINDINGS; still requires dedicated audit before any upgrade. |
| Integration into current NODAL OS command bus | `INFORMATION_INSUFFICIENT_FOR_AUTHORITY_UPGRADE` | No command authority claim allowed. |

Conclusion: Nexa admin handlers are real handler-style/admin services in a separate boundary. They do not grant current NODAL OS command bus or product action authority.

## OCR Authority Result

| Evidence | Classification | Result |
| --- | --- | --- |
| `WindowsUiAutomationReadOnlyCollector` blocks screenshots, invoke, click, keyboard, mouse, clipboard and set-value paths | `WCU_READ_ONLY_BOUNDARY` | PASS. |
| `WindowsUiAutomationEventStream` keeps `ActionAuthority=false` and live event subscription disabled by default | `WCU_FIXTURE_OR_READ_ONLY_BOUNDARY` | PASS. |
| `Win32ReadOnlyContext` and WCU locator/evidence tests assert action authority false | `WCU_NO_ACTION_AUTHORITY` | PASS. |
| OCR/ONNX/PaddleOCR readiness, synthetic and worker services exist | `OCR_TECHNICAL_FOOTPRINT_NO_PRODUCT_AUTHORITY` | P2 boundary, not product authority. |
| Obsolete or historical OCR worker paths mention real invocation while blocked/policy gated | `HISTORICAL_OR_BLOCKED_OCR_FOOTPRINT` | P3; dedicated OCR audit required before broad claims. |
| OCR-to-action authority | `NOT_AUTHORIZED` | OCR product authority remains `0%`. |

Conclusion: OCR has a technical runtime/model footprint but does not grant product action authority. WCU/OCR product authority remains `0%`.

## Cross-Boundary Result

| Boundary | Classification | Result |
| --- | --- | --- |
| Pilot -> Browser/CDP | `INFORMATION_INSUFFICIENT_FOR_AUTHORITY_UPGRADE` | No product Browser/CDP authority granted. |
| Pilot -> WCU/OCR | `PILOT_SUPERVISED_HARNESS_BOUNDARY` | No WCU/OCR product authority granted. |
| Pilot -> Durable Audit Trail | `NO_PRODUCT_LEDGER_AUTHORITY` | Durable remains Stage 1 local/test-safe only. |
| Nexa handlers -> current NODAL OS runtime | `SEPARATE_ADMIN_BOUNDARY` | No command bus authority granted. |
| OCR -> WCU actions | `NO_ACTION_AUTHORITY` | OCR does not authorize actions. |
| Browser/CDP -> WCU/OCR | `NO_CROSS_ENABLE_AUTHORIZED` | PASS. |
| Recipes -> Browser/WCU/OCR | `NO_LIVE_RUNNER_AUTHORITY` | PASS. |
| Durable -> runtime/product ledger | `LOCAL_TEST_SAFE_ONLY` | Stage 2 remains blocked. |

## Findings

| Severity | Count | Details |
| --- | ---: | --- |
| P0 | 0 | No product authority or release/commercial readiness was introduced. |
| P1 | 0 | No code, tests or runtime behavior changed. |
| P2 | 4 | Pilot real local runtime/local IO/supervised harness footprint; Nexa handler-style/admin mutation footprint; OCR mixed technical runtime/model footprint; cross-boundary claim hardening needed for Pilot/Nexa/OCR with Browser/WCU/Recipes/Durable. |
| P3 | 4 | Pilot recipe execution needs dedicated audit; Nexa command-bus integration remains information insufficient; broad OCR authority needs dedicated audit; historical/obsolete OCR and legacy admin wording must stay bounded. |
| P4 | 1 | Historical docs remain traceability records under latest decision-log canon. |

## Corrections Applied

- Added `docs/adr/pilot-nexa-ocr-authority-boundary-design-only.md`.
- Added this QA report and JSON report.
- Added `docs/handoff/nodal-os-runtime-browser-wcu-pilot-ocr-authority-mega-audit-handoff.md`.
- Updated `docs/decision-log.md`.

No source, tests, runtime, endpoints, service registrations, command handlers or product actions were changed.

## Information Insufficient Items

| Item | Classification | Required follow-up |
| --- | --- | --- |
| `OneBrain.Pilot` current product authority | `INFORMATION_INSUFFICIENT_FOR_AUTHORITY_UPGRADE` | Dedicated Pilot runtime/endpoint/local-write/execution authority audit. |
| `PilotRecipeExecutor` and recipe bridge authority | `INFORMATION_INSUFFICIENT_FOR_AUTHORITY_UPGRADE` | Dedicated Pilot/Recipes authority audit. |
| Nexa admin handler integration into current product command bus | `INFORMATION_INSUFFICIENT_FOR_AUTHORITY_UPGRADE` | Dedicated Nexa admin/handler authority audit. |
| OCR broad authority | `INFORMATION_INSUFFICIENT_FOR_AUTHORITY_UPGRADE` | Dedicated OCR authority audit before broad claims. |

## What Remains Blocked

- Runtime/live product enablement.
- Browser/CDP live product automation.
- ChromeLab as product runtime authority.
- Pilot as product runtime authority.
- Nexa admin handlers as current product command authority.
- WCU/OCR live product action authority.
- Recipes live runner/scheduler/trigger execution.
- Durable Audit Trail Stage 2 and product ledger path.
- Service registration or command handler authority upgrades.
- Product UI actions.
- DB/migration.
- Provider/cloud/network.
- Release/commercial readiness.

## Overclaim Scan Classification

| Category | Result |
| --- | --- |
| TRUE_RISK | 0 in changed docs after hardening |
| Negative assertions | Present and expected |
| Prohibited boundaries | Present and expected |
| Design-only mentions | Present and expected |
| Historical references | Present and expected |
| Accepted local/test-safe wording | Present for Durable Stage 1 |
| Lab runtime wording | Present for ChromeLab and separated from product authority |
| Fixture-safe wording | Present for WCU/OCR and tests |
| Information insufficient | Present for Pilot, Nexa and OCR upgrade claims |

## Validations

| Validation | Result |
| --- | --- |
| Repo guard | PASS |
| Claim Freeze external audit | PASS_WITH_FINDINGS |
| OneBrain.Pilot dedicated authority audit | PASS_WITH_FINDINGS |
| Nexa admin handlers dedicated audit | PASS_WITH_FINDINGS |
| OCR authority dedicated audit | PASS_WITH_FINDINGS |
| Cross-boundary audit | PASS_WITH_FINDINGS |
| JSON validation | PASS |
| `git diff --check` | PASS |
| Static scan changed files | PASS; no TRUE_RISK, only negative assertions, prohibited boundaries, design-only mentions, historical references, accepted local/test-safe wording, lab runtime wording, fixture-safe wording and information-insufficient classifications |
| Tests/build | NOT_RUN_BY_DESIGN; docs-only authority audit and no code/test changes |

## Percentages

| Track | Conservative status |
| --- | --- |
| Durable Audit Trail local/test-safe append/write candidate | 92-95% |
| Durable Stage 1 test-only enablement safety | 88-92% |
| Browser/CDP/ChromeLab boundary hardening | 85-90% |
| Runtime/Browser/WCU authority claim freeze | 85-90% |
| Pilot/Nexa/OCR authority boundary hardening | 75-85% |
| Browser/CDP current product authority | 0% |
| OneBrain.Pilot current product authority | 0% |
| Nexa current product command authority | 0% |
| WCU/OCR product authority | 0% |
| Runtime/live product enablement | 0% |
| Execution/mutation broad | 0% |
| Release/commercial readiness | 0% / NO-GO |
| Proyecto usable end-to-end | 20-30% |

## Recommended Next Macro-Block

`NODAL_OS_RUNTIME_BROWSER_WCU_PILOT_OCR_AUTHORITY_BOUNDARY_EXTERNAL_AUDIT_READ_ONLY`

Reason: this macro-block turns the prior information-insufficient items into explicit docs-only boundaries. The next safe step is an external/read-only audit of the new ADR, QA report, handoff and decision-log entry before any Stage 2, Pilot, Nexa, OCR, Browser/CDP, WCU/OCR or runtime planning.
