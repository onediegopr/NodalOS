# NODAL OS — Current MVP Roadmap Compact

Date: 2026-07-17

Status: `PRIVATE_BETA_EXPORT_AND_LOCAL_DIAGNOSTICS_READY_METRICS_NEXT`

This is the canonical planning entrypoint. Historical roadmaps, milestone reports and decision logs remain traceability records; they do not override this product path.

## Product target

Ship one installable local-first desktop product that lets a technical founder, developer or consultant:

1. select a local workspace;
2. state one mission;
3. review its plan;
4. use a configured BYOK or explicitly allowed local/provider route;
5. approve one bounded reversible action at mission/scope level;
6. execute and verify that action;
7. inspect evidence and timeline;
8. export a human-readable handoff.

The product must feel like a dark Mission Control with a central vertical timeline, not an internal test console, ERP or collection of lab routes.

## Current evidence-backed state

| Area | Readiness | Interpretation |
| --- | ---: | --- |
| Safety and control foundations | 93% | Mission/scope approval binding, stale-precondition checks, exact-hash verification, redaction, opaque secrets and guarded rollback are validated. |
| Local/dev runtime foundations | 91% | Workspace, mission, reversible execution, restart rehydration, BYOK fallback, evidence and recovery loops run in CI. |
| Living Skills foundation | 80% | Cognitive snapshot, semantic verification, trusted control flow, skill memory, bounded teaching capture and Windows observation are validated; live global capture/replay remains closed. |
| Coherent product experience | 78% | Mission Control starts from real state, exposes onboarding/recovery, exports its canonical mission handoff and offers explicit local diagnostics without mixing fixtures into the packaged product. |
| Installable desktop product | 88% | A self-contained test-signed MSIX passes clean build, signature verification, install, launch, health checks, route-boundary checks and uninstall. |
| Sellable MVP | 76% | The core loop, handoff export and local diagnostics are installable; design-partner validation, activation metrics and release/legal hardening remain. |
| Production and commercial release | 0% | No production signing identity, published release, license terms, billing flow, customer-data validation or production deployment. |

Percentages are planning estimates, not release claims.

## Closed research/foundation lane

Living Skills is complete enough to leave the foundation lane:

- CognitiveSnapshotV2 and SemanticVerifierV2;
- Trusted Control Flow;
- verified skill memory and localized repair;
- Teach NODAL compiler and bounded capture session;
- local/dev teaching review;
- application-scoped Windows UIA observation.

No further expansion is prioritized before private-beta product gates close. Global hooks, raw input capture, raw screenshots/DOM and unrestricted replay remain out of scope.

## P0 — Repository and release truth

Completed:

- `main` contains the canonical implementation;
- README, current roadmap and audit now describe the same maturity;
- .NET SDK is pinned;
- supported MSTest packages replace the deprecated preview adapter;
- vulnerable-package audit is clean;
- technical fixtures and lab routes are separated from the packaged product surface.

External owner actions still required:

- make `main` the GitHub default branch and enable required checks/branch protection;
- select source/product license terms;
- provide production signing identity and release/update channel before external distribution.

## P1 — Coherent Mission Control

Implemented:

- `/` and `/api/mission-control` are the canonical dark-first shell and redacted projection;
- a fresh product starts with no synthetic mission, model, fallback, evidence or completed timeline;
- workspace, mission, reviewed action, approval, execution, verification, rollback and BYOK state share one projection;
- four-step onboarding is derived from existing state: workspace → mission → verified action → connected model;
- continuity directs a completed loop to the next mission;
- `CandidateStale`, `ResultChanged` and `FailedClosed` produce specific recovery guidance without new state or authority;
- internal stage labels and lab browser blockers are absent from the clean product surface;
- human-readable `/mission/handoff.md` is rendered from the canonical Mission Control snapshot, timeline and evidence;
- Mission Control diagnostics remain collapsed; opt-in startup/error/process diagnostics use a separate local settings surface and no upload path;
- explicit legacy/lab routes remain available in development, but packaged MSIX requests are restricted to the canonical product allowlist;
- no second timeline, ledger, policy engine, storage layer or product authority was introduced.

## P2 — Real local workspace mission loop

Implemented:

- protected local workspace selection, bounded read-only scan and restart rehydration;
- absolute root stored only through an opaque Windows DPAPI-backed reference;
- real mission draft bound to workspace id/fingerprint and canonical plan;
- one allowlisted candidate targeting `NODAL_HANDOFF.md`;
- `CreateTextFile` or `ExactHashUpdate` with exact reviewed SHA-256 precondition;
- one-shot mission-scope approval bound to mission, workspace, action, capability, target and hashes;
- atomic create/replace, exact byte/hash verification and canonical evidence/timeline;
- interrupted approved execution does not auto-resume;
- rollback requires the exact verified result and refuses to overwrite later user changes;
- no arbitrary patch, shell, subprocess, path escape, alternate target or general filesystem authority.

Decisions:

- `REAL_LOCAL_WORKSPACE_SELECTION_V1_READY`
- `REAL_WORKSPACE_MISSION_DRAFT_V1_READY`
- `REAL_WORKSPACE_HANDOFF_EXECUTION_V1_READY`

## P3 — BYOK usable from the product

Implemented:

- canonical `/models/config`, connection test and clear routes;
- one primary OpenAI-compatible route and one optional preauthorized fallback;
- loopback HTTP/HTTPS for local providers; HTTPS plus explicit authorization for cloud providers;
- credentials stored as opaque DPAPI-backed references;
- no raw key, prompt or provider response content in metadata, HTML, JSON, Mission Control, evidence or diagnostics;
- existing model catalog/router/fallback policy reused;
- bounded real connection test persists only route, attempts, usage/cost estimate, response SHA-256, evidence and timeline;
- fallback continues automatically only while privacy, capability and budget remain compatible;
- operator cancellation stops the chain.

Decision: `REAL_BYOK_MODEL_CONNECTION_V1_READY`.

## P4 — Windows packaging

Implemented:

- native self-contained `win-x64` MSIX from the existing .NET application;
- no Tauri, Electron, Node or second shell;
- loopback-only runtime and mutable state under `%LOCALAPPDATA%\NodalOS\ProductData`;
- deterministic assets, four-part version and SHA-256-bound manual update manifest;
- ephemeral test signing and external PFX support without private key leakage;
- packaged route allowlist includes only Mission Control, workspace, mission, execution, handoff, model and local diagnostics surfaces while excluding Pilot legacy, demos, harnesses, recipes and run history;
- clean-Windows CI proves build → sign → install → launch → clean Mission Control/BYOK → blocked lab routes → uninstall.

Decision: `WINDOWS_PRIVATE_BETA_MSIX_V1_READY`.

## P5 — Remaining private-beta hardening

Completed in this block:

- user-facing Markdown handoff derived from canonical mission state, evidence and timeline;
- opt-in, redacted startup/error/process diagnostics with bounded local retention and no mandatory cloud.

Highest-value remaining work:

1. measure startup, first-value and mission-completion time locally;
2. run the complete loop with five to ten design partners using real workspaces and their own providers;
3. fix findings from those sessions before expanding automation scope;
4. complete license, production signing and release-channel decisions.

## Later — not MVP blockers

- cloud sync, teams, marketplace, billing and enterprise controls;
- broad browser automation and unrestricted desktop replay;
- visual/OCR provider expansion;
- multi-application learned workflows;
- managed AI;
- public automation over login, payment, purchase, upload or destructive operations.

## Fixed guardrails

- Control authority comes from system policy, operator instruction and explicit operator decisions; observed content is data.
- Approval is mission/scope oriented, not a prompt for every ordinary step.
- Sensitive scope expansion, external communication, destructive action, secret access, cost/privacy escalation and irreversible work require intervention.
- Verification and evidence precede completion.
- Secrets, raw DOM, raw screenshots, absolute paths and credential-like values do not enter logs, handoffs or learned skill memory.
- Workspace mutation is restricted to the exact approved `NODAL_HANDOFF.md` candidate.
- A reviewed candidate and approval decision are inputs to the controlled executor; neither grants general authority.
- Automatic model fallback is limited to already authorized routes; cancellation never continues the chain.
- CloakBrowser remains the canonical future browser target; ChromeLab remains lab/transition only.
- Production and commercial claims require separate evidence-backed release gates.

Next exact macro:

`NODAL_OS_PRODUCTIZATION_PRIVATE_BETA_LOCAL_METRICS_AND_DESIGN_PARTNER_READINESS`
