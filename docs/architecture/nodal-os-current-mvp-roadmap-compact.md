# NODAL OS — Current MVP Roadmap Compact

Date: 2026-07-16

Status: `TECHNICAL_FOUNDATION_STRONG_PRODUCTIZATION_IN_PROGRESS`

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
| Safety and control foundations | 90% | Strong tests, mission/scope approval, redaction, semantic verification and fail-closed boundaries exist. |
| Local/dev runtime foundations | 78% | Workspace understanding, protected workspace selection, test-owned file operations, model routing fixtures, Advisor and handoff loops run in CI. |
| Living Skills foundation | 80% | Compiler, memory, capture session and Windows observation adapter are validated; live product capture/replay is not enabled. |
| Coherent product experience | 48% | Mission Control is canonical and now exposes a protected, persisted, revalidated real workspace selection; mission creation and action execution remain fixture-backed. |
| Installable desktop product | 0% | No desktop packaging project, signed installer or updater channel exists in the current repository. |
| Sellable MVP | 43% | The product has a coherent entry surface and real local workspace identity, but real mission binding, live BYOK, reversible user-workspace action and packaging remain open. |
| Production and commercial release | 0% | No published release, licensing/billing flow, customer-data validation or production deployment. |

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
- diagnostics remain collapsed and local-only;
- the former root Pilot/demo surface remains available explicitly at `/pilot/legacy`;
- no second timeline, ledger, policy engine, storage layer or product authority was introduced.

Remaining before P1 is fully product-complete: bind the shell to the real selected workspace and a real mission lifecycle rather than a fixture mission.

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

#### P2b — real mission binding and reviewed action

Next exit criteria:

- create one real mission bound to the selected workspace identity;
- produce a reviewed plan from the revalidated workspace context;
- select one allowlisted reversible create or exact-hash update candidate;
- show affected relative path, precondition, risk, rollback and expected evidence before execution;
- keep execution disabled until the existing mission/scope approval boundary authorizes it;
- no arbitrary patch, shell, path escape or absolute path exposure.

#### P2c — one verified reversible action

Later exit criteria:

- execute the approved candidate inside the selected workspace;
- atomic write with precondition and snapshot/restore plan;
- semantic or deterministic post-write verification;
- rollback path validated;
- evidence and handoff use redacted relative identifiers;
- no escalation to broader filesystem authority.

The current test-owned file operations remain regression fixtures; they are reused as implementation evidence but are not the sellable feature by themselves.

### P3 — BYOK usable from the product

Exit criteria:

- provider/model selection from the product shell;
- opaque secret reference stored through the approved local secure store;
- connection test with redacted diagnostics;
- one real provider call under explicit privacy/cost policy;
- automatic fallback only within pre-authorized privacy, capability and budget limits;
- cancellation stops the chain;
- usage/cost/fallback evidence excludes secrets.

### P4 — Packaging and local distribution

Choose and implement one desktop packaging route for the existing .NET runtime. Do not assume Tauri because historical documents mentioned it.

Exit criteria:

- reproducible Release build;
- versioned Windows installer;
- application data directories and uninstall behavior;
- signed artifact strategy;
- update manifest/channel or an explicit manual-update v1;
- clean install smoke on a fresh Windows environment;
- license, security and privacy documentation present.

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
- Workspace selection may mutate NODAL OS local configuration, but never the selected workspace.
- CloakBrowser remains the canonical browser target; ChromeLab remains lab/transition only.
- Product authority, production deployment and release/commercial claims require separate evidence-backed gates.

## Productization macro progress

`NODAL_OS_PRODUCTIZATION_MISSION_CONTROL_AND_REAL_LOCAL_WORKSPACE_MVP`

1. canonical dark Mission Control shell — `MISSION_CONTROL_PRODUCT_SHELL_V1_READY`;
2. protected real local workspace selection and persistence — `REAL_LOCAL_WORKSPACE_SELECTION_V1_READY`;
3. real workspace mission binding and reviewed action candidate — next;
4. real BYOK connection path;
5. one verified reversible user-workspace file operation;
6. packaging and private-beta installer.

Next exact macro:

`NODAL_OS_PRODUCTIZATION_REAL_WORKSPACE_MISSION_BINDING_AND_REVIEWED_ACTION`
