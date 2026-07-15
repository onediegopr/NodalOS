# NODAL OS — Current MVP Roadmap Compact

Date: 2026-07-15

Status: `TECHNICAL_FOUNDATION_STRONG_PRODUCTIZATION_INCOMPLETE`

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

## Stage closed by this audit

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
| Local/dev runtime foundations | 75% | Workspace understanding, test-owned file operations, model routing fixtures, Advisor and handoff loops run in CI. |
| Living Skills foundation | 80% | Compiler, memory, capture session and Windows observation adapter are validated; live product capture/replay is not enabled. |
| Coherent product experience | 30% | Multiple useful surfaces exist, but the root Pilot experience is internal/demo-heavy, visually inconsistent and not the intended Mission Control shell. |
| Installable desktop product | 0% | No Tauri/Cargo workspace, desktop packaging project, signed installer or updater channel exists in the current repository. |
| Sellable MVP | 35% | Technical capabilities are ahead of packaging, onboarding, BYOK live connection, product UX and release operations. |
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

### P1 — One coherent Mission Control product shell

Build one dark-first entry surface that replaces the current collection of demo links as the primary experience.

Required surface:

- top bar: mission, status, progress, active model and pause/approval state;
- left navigation: Mission Control, Timeline, Workspace, Models, Evidence, Settings;
- central vertical timeline: plan, actions, verification, fallbacks, decisions and evidence;
- right context: active capability/model, fallback, Advisor and human intervention;
- bottom or expandable diagnostics: logs/events/evidence, hidden from normal flow unless requested.

The existing local/dev routes may remain diagnostic adapters, but they must not define the customer-facing information architecture.

### P2 — Real local workspace MVP loop

Exit criteria:

- explicit local workspace selection and persistence;
- bounded scan and reviewed plan over a real user-selected workspace;
- one allowlisted reversible file create or exact-hash update in that workspace;
- precondition, approval, snapshot/rollback plan and semantic/post-write verification;
- no arbitrary patch, shell or path escape;
- evidence and handoff use redacted relative identifiers.

The current test-owned file operations remain regression fixtures, not the sellable feature by themselves.

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
- CloakBrowser remains the canonical browser target; ChromeLab remains lab/transition only.
- Product authority, production deployment and release/commercial claims require separate evidence-backed gates.

## Immediate next macro after audit closeout

`NODAL_OS_PRODUCTIZATION_MISSION_CONTROL_AND_REAL_LOCAL_WORKSPACE_MVP`

Order inside that macro:

1. canonical dark Mission Control shell;
2. real local workspace selection and persistence;
3. real BYOK connection path;
4. one reversible user-workspace file operation;
5. packaging and private-beta installer.
