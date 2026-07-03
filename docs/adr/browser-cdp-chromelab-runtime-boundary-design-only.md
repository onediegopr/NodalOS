# Browser/CDP/ChromeLab Runtime Boundary Design-Only

Status: `DESIGN_ONLY / BOUNDARY_HARDENING / PRODUCT_AUTHORITY_NOT_GRANTED`

Baseline HEAD: `588457d65fc883dc4c215d9ad99098d1d8db80f5`

Decision: `ChromeLab` and the Browser/CDP runtime footprint are classified as a separate lab and historical runtime boundary. They are not current NODAL OS product runtime authority, and they do not authorize Browser/CDP product automation, release readiness or commercial readiness.

## Purpose

This ADR resolves the P2 boundary finding from the global Stage 1/runtime claim reconciliation: Browser/CDP/ChromeLab contains real runtime-shaped code, including service registrations, HTTP endpoints, WebSocket endpoints and CDP healthcheck paths. That footprint must remain explicit and bounded so it cannot be cited as current NODAL OS product runtime enablement.

This ADR is documentation-only. It does not modify runtime code, service wiring, endpoints, handlers, UI, command bus, provider/cloud/network, Browser/CDP live execution, WCU/OCR, Recipes or Durable Audit Trail behavior.

## Canonical Classification Rule

Use the narrowest truthful classification supported by current evidence:

| Evidence | Classification |
| --- | --- |
| Real ASP.NET bridge service, DI registration, HTTP endpoint or WebSocket endpoint in `OneBrain.ChromeLab.Bridge` | `LAB_RUNTIME_ONLY / SEPARATE_BOUNDARY` |
| Chrome extension or stealth WebSocket protocol handling under ChromeLab | `LAB_HANDLER_NOT_PRODUCT_COMMAND_HANDLER` |
| CloakBrowser/CDP status or live healthcheck code requiring guard, lock and runtime artifact | `BROWSER_RUNTIME_CAPABILITY_FOOTPRINT_NOT_PRODUCT_AUTHORITY` |
| Safety test, fixture, negative assertion or scan guard | `TEST_GUARD` |
| Legacy release/manual QA evidence under older reports | `HISTORICAL_REFERENCE` |
| Recent current-line docs and QA reports saying runtime/product authority is blocked | `CURRENT_CANONICAL_NO_GO` |
| Any claim that Browser/CDP is product-enabled, release-ready or commercial-ready now | `TRUE_RISK` |

If evidence is insufficient, classify as `UNKNOWN_REQUIRES_AUDIT`. Do not upgrade any Browser/CDP or ChromeLab footprint into product authority by implication.

## Runtime Footprint Inventory

| Area | Evidence | Boundary Classification | Risk |
| --- | --- | --- | --- |
| `src/OneBrain.ChromeLab.Bridge/Program.cs` | `AddSingleton`, `AddHttpClient`, CORS, `UseWebSockets`, `MapGet`, `MapPost`, `/ws/extension`, `/ws/stealth` | `LAB_RUNTIME_ONLY / SEPARATE_BOUNDARY` | P2: real lab runtime footprint can be mistaken for product authority. |
| `src/OneBrain.ChromeLab.Bridge/Sessions/ExtensionMessageHandler.cs` | Handles `extension.hello`, `extension.ping`, `tool.result`; validates token; may emit tool requests and human-pause messages | `LAB_HANDLER_NOT_PRODUCT_COMMAND_HANDLER` | P2: handler-like name and tool relay require boundary wording. |
| `src/OneBrain.ChromeLab.Bridge/Sessions/WebSocketSession.cs` | WebSocket session loop for lab messages | `LAB_TRANSPORT` | P3: real transport exists but remains ChromeLab boundary. |
| `src/OneBrain.ChromeLab.Bridge/Stealth/*` | Stealth runner registry, handoff gateway and protocol messages | `HISTORICAL_LAB_STEALTH_BOUNDARY` | P2: high-risk historical stealth language must not become product authority. |
| `src/OneBrain.BrowserRuntime/CloakBrowserRuntimeProvider.cs` | `RunLiveHealthcheckAsync`, `READY_CDP_DIRECT`, runtime artifact checks | `BROWSER_RUNTIME_CAPABILITY_FOOTPRINT_NOT_PRODUCT_AUTHORITY` | P2: runtime-capable code exists behind guard/artifact requirements. |
| `src/OneBrain.BrowserRuntime/CloakBrowserCdpHealthcheck.cs` | CDP launch, WebSocket, evidence paths, controlled page checks | `CONTROLLED_RUNTIME_PROOF / NOT_PRODUCT_AUTHORITY` | P2: live-capable healthcheck must remain outside product enablement. |
| `tests/OneBrain.Safety.Tests/ChromeLabBridgeTests.cs` | Static assertions for bridge endpoints and diagnostics | `TEST_GUARD / LAB_BOUNDARY_EVIDENCE` | P3: proves footprint exists, not product readiness. |
| `tests/OneBrain.Safety.Tests/CloakBrowserRuntimeLiveTests.cs` | Live tests under controlled runtime assumptions | `TEST_GUARD / OPT_IN_RUNTIME_EVIDENCE` | P2: live test naming needs current-canon boundary. |
| Browser/CDP historical reports under `docs/reports` and `docs/browser-runtime` | Manual QA, extension, runtime, CDP, release-gate history | `HISTORICAL_REFERENCE` | P3: legacy wording must not override current decision-log/QA canon. |

## Allowed

- Static audit of Browser/CDP and ChromeLab source, tests and docs.
- Lab-boundary documentation and read-only external audit.
- Test guards proving no cross-enable into current product authority.
- Historical traceability that is clearly marked as historical, lab, fixture, test-only or blocked.
- Future planning for Browser/CDP only after a dedicated scope proposal, external audit and explicit user GO.

## Prohibited

- Product Browser/CDP automation authority.
- Product runtime enablement.
- New or modified service registration, command handler, command bus binding, UI product action or live button.
- Treating `/ws/extension`, `/ws/stealth`, `MapGet`, `MapPost`, `AddSingleton` or `RunLiveHealthcheckAsync` as current NODAL OS product readiness.
- Cross-enabling Browser/CDP with Durable Audit Trail, WCU/OCR or Recipes.
- Product ledger path, DB/migration, cloud/provider/network expansion.
- Release, commercial, production, paid beta or MVP-ready claims.

## Boundary To Other Tracks

| Track | Boundary |
| --- | --- |
| Durable Audit Trail Stage 1 | Remains local/test-safe and implemented-not-enabled. ChromeLab does not enable Durable product append, Stage 2 or runtime. |
| WCU/OCR | No live Windows/UIA/OCR product authority is granted by Browser/CDP/ChromeLab footprints. |
| Recipes | No recipe live write, scheduler, action runner or trigger is authorized by Browser/CDP/ChromeLab footprints. |
| Approval/Human Review | Browser/CDP lab handlers are not approval command handlers and do not create product execution authority. |
| Release/commercial | Remains `NO-GO`. Historical release reports do not override current canon. |

## Required Future Audits

Before any Browser/CDP authority can move beyond lab/historical/test boundary, a future block must provide:

1. Dedicated Browser/CDP scope proposal.
2. External audit of all service registrations, command handlers, endpoints and UI actions.
3. Negative tests proving no product authority until explicit GO.
4. Explicit user GO for any implementation or enablement.
5. Fresh overclaim scan across docs/source/tests.
6. Evidence that Durable Audit Trail, WCU/OCR and Recipes remain un-cross-enabled.
7. Release/commercial lock confirmation.

## Findings

| Severity | Finding |
| --- | --- |
| P0 | None. This ADR opens no runtime/product authority. |
| P1 | None. No source/test/runtime mutation is made by this block. |
| P2 | ChromeLab has real lab runtime service registrations and endpoints; BrowserRuntime has live CDP healthcheck capability behind guard/artifact checks; historical Browser/CDP docs contain broad runtime wording. |
| P3 | Some older reports use release/manual-QA/runtime terms that require current-canon reading discipline. |
| P4 | Static tests and reports prove the lab footprint exists, which is useful evidence but easy to misread without this boundary. |

## Non-Goals

This ADR does not start ChromeLab, open Browser/CDP live paths, change `Program.cs`, register services, create command handlers, expose product actions, connect UI, run CDP, run Browser automation, create DB/migrations, use provider/cloud/network, touch stash, authorize release/commercial readiness or change Durable Audit Trail Stage 1.
