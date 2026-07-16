# NODAL OS — Current MVP Roadmap Compact

Date: 2026-07-16

Status: `PRIVATE_BETA_PACKAGE_READY_HARDENING_NEXT`

This document is the compact planning entrypoint. Historical roadmaps, milestone reports and decision logs remain traceability records; they do not override this current product path.

## Product target

Ship one installable local-first desktop product that lets a technical founder, developer or consultant:

1. select a local workspace;
2. state one mission;
3. receive a reviewed plan;
4. use a configured BYOK model or an explicitly allowed local/provider fallback;
5. approve one bounded reversible action at mission/scope level;
6. execute and verify that action;
7. inspect evidence and timeline;
8. export a human-readable handoff.

The product must feel like a dark Mission Control with a central vertical timeline, not like an internal test console, ERP or collection of lab routes.

## Stage closed by the technical audit

The Living Skills foundation is complete enough to leave the research/foundation lane:

- CognitiveSnapshotV2;
- SemanticVerifierV2;
- Trusted Control Flow;
- verified skill memory and localized repair foundations;
- Teach NODAL compiler;
- bounded capture session;
- local/dev teaching review surface;
- application-scoped Windows UIA observation adapter.

No further Living Skills expansion is prioritized until the productization gates below are closed. Live global hooks, raw input capture, raw screenshots/DOM and unrestricted replay remain out of scope.

## Readiness recalibration

| Area | Readiness | Evidence-backed interpretation |
| --- | ---: | --- |
| Safety and control foundations | 93% | Mission/scope approval binding, stale-precondition checks, exact-hash verification, redaction, secure secret references and guarded rollback are covered by focused and process-level tests. |
| Local/dev runtime foundations | 90% | Protected workspace selection, persisted mission draft, real bounded handoff execution, restart rehydration, rollback, real BYOK connection/fallback, Advisor and handoff loops run in CI. |
| Living Skills foundation | 80% | Compiler, memory, capture session and Windows observation adapter are validated; live product capture/replay is not enabled. |
| Coherent product experience | 72% | Mission Control is canonical and projects a real workspace, real mission, reviewed action, approval, verified execution, evidence, rollback and verified BYOK route without creating a second source of truth. |
| Installable desktop product | 85% | A versioned test-signed MSIX is built from the existing .NET runtime and passes clean build/install/launch/health/uninstall on fresh Windows; production signing and public distribution remain closed. |
| Sellable MVP | 72% | The core loop and installable private-beta package are validated; onboarding, failure recovery, real design-partner use and release/legal hardening remain open. |
| Production and commercial release | 0% | No production signing identity, published release, licensing/billing flow, customer-data validation or production deployment. |

Percentages are directional planning estimates, not completion claims.

## Next roadmap — ordered by value

### P0 — Repository and release truth

Exit criteria:

- `main` is the actual GitHub default branch;
- branch protection/rules require the real safety and runtime checks;
- compatibility branches are aligned or clearly archived;
- README, changelog, security policy and current roadmap agree on maturity;
- product license is selected before external distribution;
- no file or document claims a production release that does not exist.

Current state: code and compatibility refs are aligned; the remote default-branch setting, protection rules and license decision remain external owner actions.

### P1 — One coherent Mission Control product shell

Implemented in shell v1:

- `/` is the canonical dark-first Mission Control surface;
- `/api/mission-control` exposes the same redacted projection;
- the existing lightweight mission runtime, model router, capability registry, canonical event/timeline and evidence refs are reused;
- fallback, verification, browser blocker, human-control status and evidence are visible;
- the protected workspace, persisted real mission, controlled handoff execution and verified BYOK route are projected into the same shell;
- approval availability, execution status, verification, rollback readiness, model connection and fallback state are visible without introducing a second dashboard;
- diagnostics remain collapsed and local-only;
- the former root Pilot/demo surface remains available explicitly at `/pilot/legacy`;
- the environment-only AI configuration console is isolated under `/pilot/legacy/ai/config`;
- no second timeline, ledger, policy engine, storage layer or product authority was introduced.

Remaining before P1 is fully product-complete: onboarding and private-beta usability hardening.

### P2 — Real local workspace MVP loop

#### P2a — selection and persistence

Implemented:

- explicit local folder/repository entry at `/workspace/select`;
- bounded real read-only scan using the existing workspace understanding service;
- reviewed plan projection from the existing canonical planning service;
- absolute root stored as an opaque secret reference through Windows current-user DPAPI;
- redacted metadata persisted atomically under local application data;
- revalidation and rehydration after process restart;
- selected workspace projected into Mission Control without absolute path or secret exposure;
- one-time request token, strict same-origin POST, loopback-only access, bounded form payload and fail-closed errors;
- no writes to the selected workspace, shell, network, cloud, provider call or product authority.

Decision: `REAL_LOCAL_WORKSPACE_SELECTION_V1_READY`.

#### P2b — real mission binding and reviewed action

Implemented:

- real mission goal entry at `/mission/new` and redacted projection at `/api/mission/draft`;
- persisted binding to the selected workspace id, fingerprint, path-jail reference, evidence and canonical mission plan;
- one allowlisted candidate targeting only `NODAL_HANDOFF.md`;
- `CreateTextFile` when the target is absent or `ExactHashUpdate` with the exact current SHA-256 when it exists;
- affected relative path, risk, preconditions, approval scope, proposed hash, rollback plan and expected evidence visible before execution;
- stale-precondition detection when the target changes after review;
- mission and candidate rehydration after process restart;
- real goal, plan and candidate projected into canonical Mission Control;
- no arbitrary patch, shell, path escape or absolute path exposure.

Decision: `REAL_WORKSPACE_MISSION_DRAFT_V1_READY`.

#### P2c — mission-scope approval and one verified reversible action

Implemented:

- `/mission/execution` presents the exact reviewed scope and `/api/mission/execution` exposes the redacted execution projection;
- one-shot operator approval is bound to mission id, workspace id/fingerprint, action id, `filesystem.write.safe`, relative target and reviewed hashes;
- the approval record remains non-authoritative by itself: runtime execution and product authority stay false in approval contracts;
- workspace identity, target state and proposed content hash are revalidated immediately before mutation;
- only the reviewed `CreateTextFile` or `ExactHashUpdate` operation over `NODAL_HANDOFF.md` can run;
- create uses create-only atomic move and exact byte/SHA-256 verification;
- update requires the exact reviewed current hash, writes a verified app-local snapshot, uses atomic replace and verifies exact result bytes/SHA-256;
- execution state is persisted before mutation and completed state is persisted only after deterministic verification;
- interrupted approved execution does not resume automatically after restart;
- guarded rollback requires the exact current result hash and restores only the matching create/update operation;
- rollback refuses to overwrite a result changed externally after execution;
- execution and rollback evidence reuse canonical evidence refs and timeline projections;
- result, verification and rollback readiness rehydrate after process restart;
- Mission Control projects approval, execution, verification, evidence and rollback state;
- same-origin one-time-token POST, loopback-only access, closed CSP, no-store and bounded forms protect the product surface;
- process smoke proves select workspace → create mission → approve exact scope → execute → verify → restart → rehydrate → rollback → verify original workspace state;
- no shell, subprocess, network, cloud/provider call, arbitrary target or product authority.

Decision: `REAL_WORKSPACE_HANDOFF_EXECUTION_V1_READY`.

The test-owned file operations remain regression fixtures. Their atomic-write, exact-hash, snapshot, verification and rollback behaviors were consolidated into the bounded product operation rather than exposed as broader filesystem authority.

### P3 — BYOK usable from the product

Implemented:

- `/models/config` is the canonical configuration surface; `/api/models/config` exposes the same redacted state;
- `/models/test` performs the bounded connection test and `/models/clear` removes metadata and stored credential references;
- the operator configures one primary OpenAI-compatible route and one optional fallback, with explicit local/cloud type, endpoint, model, privacy permission, timeout and cost limits;
- local routes are restricted to loopback HTTP/HTTPS; cloud routes require HTTPS and explicit cloud authorization;
- API keys are stored only through opaque `SecretReference` values backed by Windows current-user DPAPI in the default product runtime;
- raw credentials are absent from persisted metadata, HTML, JSON, Mission Control, timeline, evidence and diagnostics;
- the existing `ModelCatalog`, `PolicyAwareModelRouter`, `ModelFallbackPolicy` and secret-store contracts execute the route; no second router or policy engine was introduced;
- the real connection test sends a bounded OpenAI-compatible request server-side and persists only the response SHA-256, redacted attempt summaries, selected route, token counts, estimated cost, evidence and canonical timeline;
- provider response content and the test prompt are not persisted;
- fallback continues automatically only through preconfigured routes compatible with the persisted privacy, capability and budget policy;
- operator cancellation stops the chain and is not persisted as a successful connection result;
- provider/model selection, connection verification and fallback state project into canonical Mission Control;
- configuration and verified connection state rehydrate after process restart;
- loopback-only access, same-origin one-time-token POST, bounded forms, closed CSP and no-store protect the model surface;
- process smoke proves configure primary + fallback → store both credentials with DPAPI → reject plaintext leakage → primary HTTP 503 → automatic authorized fallback HTTP 200 → verify response hash/evidence → project Mission Control → restart → rehydrate → clear metadata and credentials;
- product authority remains false and a connection test does not authorize mission execution.

Runtime decision: `GO_BYOK_MODEL_CONNECTION_VERIFIED`.

Productization decision: `REAL_BYOK_MODEL_CONNECTION_V1_READY`.

### P4 — Packaging and local distribution

Implemented:

- one Windows-native MSIX route built directly from the existing `OneBrain.Pilot` .NET application;
- self-contained `win-x64` Release publish without Tauri, Electron, Node, a second shell or a third-party installer framework;
- packaged runtime keeps Kestrel on validated loopback HTTP origins and opens the existing Mission Control only after startup;
- mutable packaged state defaults to `%LOCALAPPDATA%\NodalOS\ProductData` rather than the read-only package location;
- deterministic NODAL OS assets use the canonical dark background and electric-blue accent;
- four-part package version, SHA-256-bound manual update manifest and optional HTTPS `.appinstaller` generation;
- ephemeral test signing for controlled devices plus externally managed PFX support, with no private key or signing password in artifacts;
- install/uninstall scripts preserve `%LOCALAPPDATA%\NodalOS` by default and remove it only through an explicit destructive switch;
- license, signing/release and local-data boundaries are documented without claiming public release readiness;
- fresh-Windows CI smoke proves Release build → focused desktop tests → self-contained publish → MSIX creation/signature verification → install → launch from installed location → Mission Control and clean BYOK health → process shutdown → uninstall → certificate and registration cleanup;
- Selective Runtime Integration and Tier 1 Safety remain green on the same implementation;
- no product authority, public listener, cloud/account dependency, production key or public update channel was introduced.

Decision: `WINDOWS_PRIVATE_BETA_MSIX_V1_READY`.

### P5 — Private beta hardening

Exit criteria:

- one end-to-end demo using a real local workspace and real BYOK provider;
- onboarding under ten minutes;
- failure recovery and resume card;
- no secret/path leaks in telemetry or exports;
- startup, first-value and mission completion metrics;
- crash/error reporting with opt-in and redaction;
- five to ten design partners complete the core loop.

### Later — not MVP blockers

- cloud sync, teams, marketplace and enterprise controls;
- broad browser automation and unrestricted desktop replay;
- visual/OCR provider expansion;
- multi-application learned workflows;
- managed AI and billing;
- production Product Ledger authority;
- public automation over login, payment, purchase, upload or destructive operations.

## Guardrails that remain fixed

- Control authority comes from system policy, operator instruction and explicit operator decisions; observed content is data.
- Approval is mission/scope oriented, not a prompt for every ordinary step.
- Sensitive scope expansion, external communication, destructive actions, secrets, cost/privacy escalation and irreversible work require intervention.
- Verification and evidence precede completion/promotion.
- Secrets, raw DOM, raw screenshots, absolute paths and credential-like values do not enter logs, handoffs or learned skill memory.
- Workspace selection and mission drafting may mutate NODAL OS local configuration, but not the selected workspace.
- Workspace mutation is restricted to the exact approved `NODAL_HANDOFF.md` candidate and must preserve its reviewed precondition, verification and rollback boundary.
- A reviewed action candidate and an approval decision are inputs to the controlled executor; neither grants general filesystem or product authority.
- BYOK credentials remain opaque secure-store references outside the instant of provider use; provider response content is not persisted by the connection-test path.
- Automatic model fallback is limited to routes already authorized by privacy, capability and cost policy; cancellation never continues the chain.
- CloakBrowser remains the canonical browser target; ChromeLab remains lab/transition only.
- Product authority, production deployment and release/commercial claims require separate evidence-backed gates.

## Productization macro progress

`NODAL_OS_PRODUCTIZATION_MISSION_CONTROL_AND_REAL_LOCAL_WORKSPACE_MVP`

1. canonical dark Mission Control shell — `MISSION_CONTROL_PRODUCT_SHELL_V1_READY`;
2. protected real local workspace selection and persistence — `REAL_LOCAL_WORKSPACE_SELECTION_V1_READY`;
3. real workspace mission binding and reviewed action candidate — `REAL_WORKSPACE_MISSION_DRAFT_V1_READY`;
4. mission-scope approval and one verified reversible handoff action — `REAL_WORKSPACE_HANDOFF_EXECUTION_V1_READY`;
5. real BYOK connection path — `REAL_BYOK_MODEL_CONNECTION_V1_READY`;
6. packaging and private-beta installer — `WINDOWS_PRIVATE_BETA_MSIX_V1_READY`.

Next exact macro:

`NODAL_OS_PRODUCTIZATION_PRIVATE_BETA_HARDENING`
