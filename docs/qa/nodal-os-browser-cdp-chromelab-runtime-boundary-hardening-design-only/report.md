# NODAL OS - Browser/CDP/ChromeLab Runtime Boundary Hardening Design-Only

Decision: `GO_WITH_FINDINGS_BROWSER_CDP_CHROMELAB_BOUNDARY_HARDENING_READY`

Date: 2026-07-03

## Scope

This block hardens the canonical boundary between Browser/CDP/ChromeLab runtime footprints and current NODAL OS product runtime authority.

The block is docs-only. It audits existing source/tests/docs and adds boundary documentation. It does not modify code, tests, `Program.cs`, endpoints, service registration, command handlers, product actions, runtime/live behavior, Browser/CDP live automation, WCU/OCR, Recipes, DB/migration, provider/cloud/network, Durable Audit Trail Stage 2, release/commercial readiness or stash state.

## Repo Guard

| Field | Value |
| --- | --- |
| Repo | `C:/DESARROLLO/NodalOS/Codigo-m12-audit` |
| Branch | `chrome-lab-001-extension-local-ai-bridge` |
| Input HEAD | `588457d65fc883dc4c215d9ad99098d1d8db80f5` |
| Input commit | `588457d6 docs(audit): reconcile stage 1 and runtime claims` |
| Worktree initial | `clean` |
| Origin sync initial | `0 0` |
| Stash | listed only, not touched: `stash@{0}: On chrome-lab-001-extension-local-ai-bridge: pre-m11-legacy-state` |

## Runtime Footprint Inventory

| File | Symbols / evidence | Classification | Risk |
| --- | --- | --- | --- |
| `src/OneBrain.ChromeLab.Bridge/Program.cs` | `AddSingleton`, `AddHttpClient`, `UseWebSockets`, `MapGet`, `MapPost`, `/ws/extension`, `/ws/stealth`, `/runtime`, `/debug`, `/api/runs` | `LAB_RUNTIME_ONLY / SEPARATE_BOUNDARY` | P2 |
| `src/OneBrain.ChromeLab.Bridge/Sessions/ExtensionMessageHandler.cs` | `IMessageHandler`, token validation, `extension.hello`, `extension.ping`, `tool.result`, tool relay, human pause | `LAB_HANDLER_NOT_PRODUCT_COMMAND_HANDLER` | P2 |
| `src/OneBrain.ChromeLab.Bridge/Sessions/WebSocketSession.cs` | WebSocket session transport for ChromeLab messages | `LAB_TRANSPORT` | P3 |
| `src/OneBrain.ChromeLab.Bridge/Stealth/*` | stealth runner registry, task protocol, handoff gateway | `HISTORICAL_LAB_STEALTH_BOUNDARY` | P2 |
| `src/OneBrain.BrowserRuntime/CloakBrowserRuntimeProvider.cs` | `RunLiveHealthcheckAsync`, `READY_CDP_DIRECT`, runtime artifact/guard status | `BROWSER_RUNTIME_CAPABILITY_FOOTPRINT_NOT_PRODUCT_AUTHORITY` | P2 |
| `src/OneBrain.BrowserRuntime/CloakBrowserCdpHealthcheck.cs` | CDP launch/healthcheck, WebSocket, local verification evidence paths | `CONTROLLED_RUNTIME_PROOF / NOT_PRODUCT_AUTHORITY` | P2 |
| `tests/OneBrain.Safety.Tests/ChromeLabBridgeTests.cs` | Static endpoint diagnostics assertions | `TEST_GUARD / LAB_BOUNDARY_EVIDENCE` | P3 |
| `tests/OneBrain.Safety.Tests/CloakBrowserRuntimeLiveTests.cs` | Opt-in live runtime healthcheck tests | `TEST_GUARD / OPT_IN_RUNTIME_EVIDENCE` | P2 |
| `docs/browser-runtime/*`, `docs/reports/*` | CDP, extension, runtime, release/manual-QA historical records | `HISTORICAL_REFERENCE` | P3 |

## Authority Classification

| Area | Result |
| --- | --- |
| Current NODAL OS product runtime authority | `NOT_GRANTED` |
| ChromeLab runtime footprint | `REAL_LAB_RUNTIME_ONLY / SEPARATE_BOUNDARY` |
| BrowserRuntime/CDP capability footprint | `CAPABILITY_FOOTPRINT_NOT_PRODUCT_AUTHORITY` |
| Product Browser/CDP automation | `NOT_AUTHORIZED` |
| Product service registration | `NOT_AUTHORIZED_BY_THIS_BLOCK` |
| Product command handler | `NOT_AUTHORIZED_BY_THIS_BLOCK` |
| Product action / UI live button | `NOT_AUTHORIZED` |
| Durable Audit Trail cross-enable | `NO` |
| WCU/OCR/Recipes cross-enable | `NO` |
| Release/commercial readiness | `NO-GO` |

## Service Registration Findings

ChromeLab has real `AddSingleton` and `AddHttpClient` registrations in `src/OneBrain.ChromeLab.Bridge/Program.cs`. These registrations belong to the ChromeLab bridge application and must be treated as `LAB_RUNTIME_ONLY / SEPARATE_BOUNDARY`.

No evidence in this block promotes those registrations to current NODAL OS product runtime authority. No code was changed and no new registration was added.

Durable Audit Trail remains outside ChromeLab registration. This block does not register Durable Audit Trail as a product service.

## Endpoint Findings

ChromeLab exposes real lab endpoints and WebSocket paths:

- `/health`
- `/metrics`
- `/config/public`
- `/pairing/local-token`
- `/runtime`
- `/clients`
- `/last-events`
- `/debug`
- `/api/runs`
- `/api/runs/{runId}/stop`
- `/api/runs/{runId}/pause`
- `/api/runs/{runId}/resume`
- `/ws/extension`
- `/ws/stealth`

These endpoints prove a real lab runtime footprint. They do not prove current NODAL OS product runtime enablement.

## Claims Findings

| Source | Finding | Classification |
| --- | --- | --- |
| Recent decision log and QA chain | Browser/CDP/ChromeLab boundary is an open P2 and product runtime remains blocked | `CURRENT_CANONICAL_NO_GO` |
| `docs/ROADMAP.md` | Already marked legacy/non-authoritative by previous block | `HISTORICAL_REFERENCE_WITH_WARNING` |
| Historical Browser/CDP reports | Manual QA, extension, CDP and runtime terms appear in older reports | `HISTORICAL_REFERENCE` |
| ChromeLab source | Real bridge runtime language and endpoints | `LAB_RUNTIME_CLAIM` |
| Current block docs | Explicitly prohibit product runtime authority and release/commercial readiness | `NEGATIVE_ASSERTION` |

No source inspected in this block authorizes current Browser/CDP product runtime authority for NODAL OS.

## Overclaim Scan

| Category | Result |
| --- | --- |
| TRUE_RISK | 0 in changed docs after boundary wording |
| Negative assertions | Present and expected |
| Design-only mentions | Present and expected |
| Prohibited boundaries | Present and expected |
| Lab runtime claims | Present and explicitly classified |
| Historical references | Present and expected |
| Accepted fixture-safe wording | Present in test/fixture contexts |

## Findings

| Severity | Count | Details |
| --- | ---: | --- |
| P0 | 0 | No product runtime authority, enablement or release/commercial claim was introduced. |
| P1 | 0 | No source/test/runtime mutation was made. |
| P2 | 3 | ChromeLab has real lab service registrations/endpoints; BrowserRuntime has live CDP healthcheck capability behind guard/artifact checks; historical Browser/CDP runtime wording remains broad and must be read through this boundary. |
| P3 | 2 | Older reports use release/manual-QA/runtime language; test names and static assertions can be misread unless classified as test guard or historical evidence. |
| P4 | 1 | The lab footprint is useful traceability but requires ongoing latest-canon discipline. |

## Corrections Applied

- Added `docs/adr/browser-cdp-chromelab-runtime-boundary-design-only.md`.
- Added this QA report and its JSON companion.
- Added handoff for this boundary block.
- Updated `docs/decision-log.md`.

No code, tests, runtime, endpoints, service registrations, command handlers or product actions were changed.

## What Remains Blocked

- Product Browser/CDP automation authority.
- ChromeLab as current NODAL OS product runtime authority.
- CDP live product automation.
- Service registration for product runtime.
- Command handlers.
- Product actions or UI live buttons.
- Cross-enable with Durable Audit Trail, WCU/OCR or Recipes.
- Product ledger path, DB/migration, cloud/provider/network.
- Release/commercial readiness.

## Validations

| Validation | Result |
| --- | --- |
| Repo guard | PASS |
| Runtime footprint scan | PASS_WITH_FINDINGS |
| Claims scan | PASS_WITH_FINDINGS |
| JSON validation | PASS |
| `git diff --check` | PASS |
| Static scan changed files | PASS; no TRUE_RISK, only negative assertions, prohibited boundaries, lab runtime claims and historical references |
| Tests/build | NOT_RUN_BY_DESIGN; docs-only boundary hardening and no code/test changes |

## Percentages

| Track | Conservative status |
| --- | --- |
| Durable audit trail local/test-safe append/write candidate | 92-95% |
| Durable audit trail Stage 1 test-only enablement safety | 88-92% |
| Browser/CDP/ChromeLab boundary hardening | 80-85% |
| Browser/CDP current NODAL OS product authority | 0% |
| Runtime/live product enablement | 0% |
| Execution/mutation broad | 0% |
| WCU/OCR product authority | 0% |
| Recipes live authority | 0% |
| Release/commercial readiness | 0% / NO-GO |
| Proyecto usable end-to-end | 20-30% |

## Recommended Next Macro-Block

`NODAL_OS_BROWSER_CDP_CHROMELAB_RUNTIME_BOUNDARY_EXTERNAL_AUDIT_READ_ONLY`

Reason: this block persists the boundary. The next safe step is an external/read-only audit of the new ADR, QA report, handoff and decision-log entry before any Browser/CDP planning or runtime discussion.
