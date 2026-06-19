# NODAL OS Roadmap vNext

## Current State

- Product name: NODAL OS.
- Historical technical names still present: NEXA, ONE BRAIN.
- Visible rename NEXA to NODAL OS: completed in M97-M99.
- Technical `Nexa*` symbol cleanup: pending compatibility task.
- NODAL OS engineering: 97%.
- Browser Runtime local/sandbox: 97%.
- External HTTP read-only proof readiness: 90-95%.
- External target-owned Chrome/CDP/DOM read-only proof readiness: 85-90%.
- Security/evidence integrity: 92-95%.
- M51: closed with strict HTTP read-only scope.
- M65: closed with limited target-owned Chrome/CDP/DOM read-only scope.
- External Chrome/CDP/DOM proof: completed only for `https://lab.nodalos.com.ar`.
- External CDP general-ready: false.

## Post-Selective Absorption Re-Sync M358

- OCR line: closed for the current roadmap phase.
- Selective absorption immediate scope: 100%.
- Agent Operations / Platform Layer: 68%.
- Browser Runtime / Chrome layer: 88%.
- OCR/perception: 97%.
- NODAL OS global: 85%.
- BotBoard absorption: decision and Mission/Task/Blocker/Verification/Evidence domain ready.
- Axiom absorption: Failure Taxonomy, Troubleshooting, Run Report V1 and Recipe Manifest / Automation JSON V1 ready.
- Robomotion absorption: roadmap note only.
- Recipe execution implemented: false.
- Workboard UI implemented: false.
- Orchestration API implemented: false.
- Scheduled runs implemented: false.
- Package registry implemented: false.
- Cloud runtime implemented: false.
- Captcha solving implemented: false.
- Bot bypassing implemented: false.

Recommended next path: Hybrid priority roadmap.

Recommended next milestone: `M359-M361 Browser Runtime Flake Hardening`.

Hybrid priority sequence:

1. Browser runtime flake hardening.
2. Verification Before Done Gate.
3. Blocker + Progress Reporting Contract.
4. Core legacy reference graph.
5. Step Library V1.
6. Desktop identity/liveness.

## Core Legacy Reference Graph M376

- M374-M376 created the core legacy reference graph.
- Active paths documented: core FSM/safe execution, browser/CDP runtime, evidence/audit ledgers, OCR ONNX .NET path, Agent Operations contracts/services, recipe/step/run reporting.
- Legacy paths documented: retained Python OCR worker, historical OCR scripts, old `Nexa*` technical naming, diagnostic OCR runtime experiments.
- Diagnostic-only paths documented: ONNX crash probes, guarded OCR probe runner, negative fixture recipes, historical artifacts.
- Cleanup backlog created: completion gate canonicalization, common redaction/sanitizer service, EvidenceRef-to-ledger bridge, Agent Operations extraction boundary, recipe/step runtime-permission wording.
- Runtime behavior changed: false.
- UI implemented: false.
- Recipe execution implemented: false.
- Orchestration API implemented: false.
- Namespace move implemented: false.
- Legacy deleted: false.

Recommended next milestone: `M377-M379 Completion Gate Canonicalization`.

## Agent Operations Namespace / Naming ADR M391

- M389-M391 created the Agent Operations namespace/naming ADR.
- Current product name remains NODAL OS.
- `Nexa*` Agent Operations symbols are compatibility debt, not the forward naming pattern.
- `OneBrain.*` remains a historical implementation namespace.
- New Agent Operations types should use `NodalOs*`.
- Current Agent Operations placement in `OneBrain.BrowserExecutor.Contracts` and `OneBrain.BrowserExecutor.Cdp` is tolerated temporarily for compatibility.
- Long-term extraction target: `OneBrain.AgentOperations.Contracts`, `OneBrain.AgentOperations.Core`, and `OneBrain.AgentOperations.Adapters.Browser` or equivalent product-aligned namespaces.
- Namespace move implemented: false.
- Broad rename implemented: false.
- Runtime behavior changed: false.
- UI implemented: false.

## Agent Operations Contracts Extraction M409

- M407-M409 created `OneBrain.AgentOperations.Contracts`.
- Phase 1 extracted Agent Operations contracts out of the BrowserExecutor contracts project boundary.
- Moved contracts: workboard, failure taxonomy, run report, recipe manifest, verification-before-done, progress reporting, step library, redaction, evidence bridge, package/skill manifest, internal skill registry, and worker boundary.
- Compatibility strategy: preserve existing `OneBrain.BrowserExecutor.Contracts` namespace in moved types while the assembly/project boundary changes.
- Compatibility shims required: true, via namespace preservation and direct project references.
- Services moved: false.
- Browser adapters moved: false.
- Runtime behavior changed: false.
- UI implemented: false.
- Orchestration API implemented: false.
- Execution implemented: false.

Recommended next milestone: `M410-M412 Agent Operations Extraction Phase 2 Core Services`.

## Agent Operations Core Services Extraction M412

- M410-M412 created `OneBrain.AgentOperations.Core`.
- Phase 2 extracted pure Agent Operations services out of the BrowserExecutor CDP project boundary.
- Moved services: workboard validator/fixtures, run reporting, recipe manifest, verification-before-done, progress reporting, step library, package/skill manifest, internal skill registry, worker boundary, evidence bridge, and redaction.
- Compatibility strategy: preserve existing `OneBrain.BrowserExecutor.Cdp` namespace in moved services while the assembly/project boundary changes.
- Compatibility shims required: true, via namespace preservation and direct project references.
- Contracts moved in this phase: false.
- Browser adapters moved: false.
- Runtime behavior changed: false.
- UI implemented: false.
- Orchestration API implemented: false.
- Execution implemented: false.

Recommended next milestone: `M413-M415 Agent Operations Browser Adapter Boundary or M413-M415 Orchestration API Decision Record`.

## Agent Operations Browser Adapter Boundary M415

- M413-M415 documented and protected the browser adapter boundary after Agent Operations contracts/core extraction.
- `OneBrain.AgentOperations.Contracts` remains Browser/CDP-free at project-reference level.
- `OneBrain.AgentOperations.Core` remains Browser/CDP-free at project-reference level.
- `OneBrain.BrowserExecutor.Cdp` remains the temporary browser adapter host and may consume Agent Operations Core.
- `ChromeCdpBrowserExecutor.cs` stayed in `OneBrain.BrowserExecutor.Cdp`.
- `BrowserRuntimeSmoke.cs` stayed in `OneBrain.BrowserExecutor.Cdp`.
- `BrowserPersistentAuditLedger.cs` is classified as browser-specific audit infrastructure and was not moved.
- Adapter project created: false.
- Adapter project deferred: true.
- Runtime behavior changed: false.
- UI implemented: false.
- Orchestration API implemented: false.
- Execution implemented: false.

Recommended next milestone: `M416-M418 Orchestration API Decision Record or M416-M418 Agent Operations Extraction Phase 3 Adapter Project`.

## Orchestration API Decision Record M418

- M416-M418 created the Orchestration API Architecture Decision Record.
- The ADR defines future conceptual commands, a future state model, policy gates, approval gates, evidence gates, verification-before-done rules, and relationships with Agent Operations, Browser Adapter, Worker Boundary, Registry, Recipe, Step, Skill, RunReport, and ProgressReport.
- Future conceptual commands are design-only and not implemented as endpoints or runtime behavior.
- Orchestration API implementation deferred: true.
- API implemented: false.
- HTTP/gRPC implemented: false.
- Scheduler implemented: false.
- Worker runtime implemented: false.
- Recipe execution implemented: false.
- Skill execution implemented: false.
- Step execution implemented: false.
- UI implemented: false.
- Registry Visible, Worker Healthy, Recipe Approved, and Skill Approved do not grant runtime permission.
- Policy, approval, evidence, and verification-before-done gates remain authoritative.

Recommended next milestone: `M419-M421 Orchestration Command Contracts V1 or M419-M421 Agent Operations Adapter Project Skeleton`.

## Orchestration Command Contracts V1 M421

- M419-M421 created internal Orchestration Command Contracts V1.
- Added command kinds, command envelope, command result, command state model, risk model, validator, JSON serializer, and fixtures.
- Runtime execution allowed: false.
- Runtime execution deferred: true.
- Global policy evaluation required: true.
- Human approval is required for High and Critical risk command contracts.
- Evidence refs are validated through the EvidenceRef bridge.
- Common redaction is used for command/result sensitive-content validation and serialization.
- Accepted does not mean executed.
- Completed means contract handling only.
- Pause, Resume, and Cancel are contract-only in V1.
- API implemented: false.
- HTTP/gRPC implemented: false.
- Scheduler implemented: false.
- Worker runtime implemented: false.
- Recipe execution implemented: false.
- Skill execution implemented: false.
- Step execution implemented: false.
- UI implemented: false.

Recommended next milestone: `M422-M424 Orchestration In-Process Facade Decision or M422-M424 Agent Operations Adapter Project Skeleton`.

## Orchestration In-Process Facade Decision Record M424

- M422-M424 created the Orchestration In-Process Facade Boundary Discovery report and Decision Record.
- Facade implementation deferred: true.
- Facade implemented: false.
- Command dispatcher implemented: false.
- Runtime/state-machine/execution engine implemented: false.
- API/HTTP/gRPC implemented: false.
- Scheduler implemented: false.
- Worker runtime implemented: false.
- Recipe/skill/step execution implemented: false.
- UI implemented: false.
- No-execution invariant defined and required structural at the facade boundary: true.
- Policy, approval, and evidence gate locations defined: true.
- `NodalOsVerificationBeforeDoneGate` preserved as canonical completion gate: true.
- Accepted does not mean executed; Completed means contract handling only.
- RunningFuture and PausedFuture remain future-only.
- Registry Visible, Worker Healthy, Skill Approved, and Recipe Approved do not grant runtime permission.
- Decision: `ORCHESTRATION_FACADE_ADR_READY_WITH_EXECUTION_DEFERRED`.

Recommended next milestone: `M425-M427 Orchestration In-Process Facade V1 No Execution or M428-M430 Agent Operations Adapter Project Skeleton`.

Recommended next milestone: `M392-M394 Package / Skill Manifest V1 or M392-M394 Agent Operations Extraction Prep`.

## Package / Skill Manifest V1 M394

- M392-M394 created Package / Skill Manifest V1 as internal governed catalog metadata.
- New contracts use the `NodalOs*` prefix.
- Package and skill manifests are internal-only in V1.
- Catalog policy is separated from runtime permission.
- Runtime execution allowed: false.
- Runtime execution deferred: true.
- Global policy evaluation required: true.
- Registry, worker runtime, marketplace, package installation, UI, orchestration, recipe execution, and step execution remain unimplemented.
- Common redaction is used for secret-like manifest validation.

Recommended next milestone: `M395-M397 Internal Skill Registry V1 Design or Agent Operations Extraction Prep`.

## Internal Skill Registry V1 M397

- M395-M397 created Internal Skill Registry V1 as an in-memory/catalog snapshot design over Package / Skill Manifest V1.
- Registry entries preserve package/skill provenance, evidence requirements, capability metadata, risk metadata, internal-only status, and global-policy requirement.
- Registry lookup is catalog metadata only and cannot grant execution permission.
- Runtime execution allowed: false.
- Runtime execution deferred: true.
- Registry persistence DB, worker runtime, marketplace, package installation, UI, orchestration, skill execution, recipe execution, and step execution remain unimplemented.
- Current placement remains temporary under `OneBrain.BrowserExecutor.*` until Agent Operations extraction.

Recommended next milestone: `M398-M400 Worker Boundary Contract V1 or Agent Operations Extraction Prep`.

## Worker Boundary Contract V1 M400

- M398-M400 created Worker Boundary Contract V1 as a governance-only contract for future workers.
- Worker identity, status, health, capability declaration, request envelope, and response envelope are defined.
- Worker health is diagnostic only and does not grant execution permission.
- Worker responses can carry EvidenceBridge refs and FailureKind values for reporting only.
- Runtime execution allowed: false.
- Runtime execution deferred: true.
- Can authorize actions: false.
- Worker runtime, skill execution, recipe execution, step execution, UI, orchestration, persistence, marketplace, and package installation remain unimplemented.

Recommended next milestone: `M401-M403 Agent Operations Extraction Prep or M401-M403 Orchestration API Decision Record`.

## Package / Registry / Worker Integration No-Divergence M403

- M401-M403 closed the contract-only integration edge between Package / Skill Manifest V1, Internal Skill Registry V1, and Worker Boundary Contract V1.
- Worker response envelopes now validate EvidenceBridge refs and scan evidence metadata for sensitive content.
- Registry snapshots can be built through `BuildValidatedSnapshot`; registry entries normalize runtime flags to execution-deferred metadata.
- Registry status is enum-driven and no longer depends on display-name heuristics.
- Skill-to-worker capability mapping is explicit through `NodalOsWorkerSkillCapabilityMapper`.
- Runtime execution allowed remains false across package, registry, and worker layers.
- Runtime execution deferred remains true across package, registry, and worker layers.
- Global policy evaluation remains required across package, registry, and worker layers.
- Visible, Healthy, CanPassCatalogPolicy, and CanPassBoundaryPolicy do not grant runtime permission.
- Worker runtime, skill execution, recipe execution, step execution, UI, orchestration, persistence, marketplace, and package installation remain unimplemented.

Recommended next milestone: `M404-M406 Agent Operations Extraction Prep or M404-M406 Orchestration API Decision Record`.

## Agent Operations Extraction Prep M406

- M404-M406 documented the extraction dependency graph for Agent Operations currently hosted under `OneBrain.BrowserExecutor.*`.
- Recommended target layout: `OneBrain.AgentOperations.Contracts`, `OneBrain.AgentOperations.Core`, and `OneBrain.AgentOperations.Adapters.Browser`.
- Option `NodalOs.AgentOperations.*` remains a future larger naming migration, not Phase 1.
- Contracts extraction candidates include workboard, run report, failure taxonomy, recipe manifest, verification-before-done, progress reporting, step library, redaction, EvidenceRef bridge, Package/Skill manifest, Internal Skill Registry, and Worker Boundary contracts.
- Core service extraction candidates include validators, builders, serializers, redaction service, EvidenceRef bridge, registry services, and worker boundary services.
- Browser-specific runtime classes stay in BrowserExecutor or a future browser adapter boundary.
- Compatibility shims are required; `Nexa*` symbols remain compatibility debt and are not renamed now.
- No namespace move, broad rename, runtime behavior change, UI, orchestration, worker runtime, or execution was implemented.

Recommended next milestone: `M407-M409 Agent Operations Extraction Phase 1 Contracts or M407-M409 Orchestration API Decision Record`.

## M51 Scope

M51 is closed only for:

- external HTTP read-only proof;
- target `https://lab.nodalos.com.ar`;
- `ProbeKind=RealHttpClient`;
- `Tooling=HttpReadOnlyExternal`;
- capabilities `HttpGetReadOnly`, `NetworkMetadataOnly`, `CoreGoverned`;
- redacted evidence persisted to `BrowserPersistentAuditLedger`.

M51 does not prove:

- Chrome/CDP external runtime;
- DOM read-only external proof;
- profile/browser process cleanup against external live target;
- auth target readiness;
- document workflow readiness;
- sensitive site readiness.

## M65 Status

M65 is formally closed only for:

- target-owned external low-risk Chrome/CDP/DOM read-only proof;
- target `https://lab.nodalos.com.ar`;
- isolated temporary browser profile;
- `ProbeKind=RealChromeCdp`;
- `Tooling=ChromeCdpExternalReadOnly`;
- redacted evidence persisted to `BrowserPersistentAuditLedger`;
- `LedgerRef=audit-ledger-edb3e2fbb0a0446788dae17a269c0058`;
- `LedgerHash=61f52af1eebf08d59a24e5fbb72e70acf0038e7a329bff6599a0ac00c757f03e`;
- no credentials, no login real, no submit, no payment, no mutation, no sensitive site.

M65 does not mean external CDP general-ready.

M65 does not unlock third-party sites, sensitive sites, real credentials, submit/pay/sign/delete, production external CDP, SaaS public, public API, billing real, or email real.

## Recommended Next Blocks

### M97/M98/M99: Visible Rename NEXA to NODAL OS

Goal:

- Rename visible product surfaces from historical NEXA to NODAL OS where appropriate.
- Keep compatibility aliases if required.
- Do not mix rename with proof/security-critical changes.

Status:

- Completed for visible/operator-facing surfaces.
- Technical symbol cleanup remains future work.

Rules:

- Start from canonical worktree only.
- Preserve git diff with safeguard patch before commit.
- Validate full suite.

### M100/M101/M102: M65 Dedicated Evidence Plan

Goal:

- Define M65-specific evidence, scenarios, gates, and ledger requirements.
- Keep M65 deferred until this evidence exists.
- Do not use real credentials or sensitive sites.

### M103/M104/M105: External Chrome/CDP/DOM Read-Only Proof

Goal:

- Prove real browser runtime against the test-owned external target if still required.
- Use Chrome/CDP with controlled profile and Core authority.
- Persist evidence to HMAC ledger.
- Do not infer from HttpClient proof.

### M115/M116/M117: Product/Admin Private Preview Hardening

Goal:

- Harden Product/Admin private preview after M51 and M65 limited external evidence.
- Keep local/private authority boundaries.
- Keep SaaS public, public API, billing real, email real, and real credentials blocked.

### M118/M119/M120: Core Audit / External Proof Audit / Release Gate

Goal:

- Audit M51 HTTP evidence and M65 target-owned Chrome/CDP evidence.
- Verify ledger references, redaction and scope locks.
- Decide whether release gates need independent review before broader preview.

### M121/M122/M123: HITO-162 Rewrite / Map

Goal:

- Re-audit the legacy HITO-162 intent.
- Map it to the NODAL OS roadmap or rewrite it as a new block.
- Do not resume it blindly.

### M124+: Embedded Runtime Evaluation If Needed

Goal:

- Evaluate WebView2/CEF/embedded runtime only if a concrete limitation justifies it.
- Chromium fork is not planned unless a hard limitation appears.

### Legacy HITO-162 Reconciliation / Rewrite

Goal:

- Re-audit the legacy HITO-162 intent.
- Decide whether it maps to perception robustness, safe action expansion, or a new NODAL OS hito block.
- Do not resume it blindly.

### Product/Admin Private Preview Hardening

Continue local-only operator readiness, issue triage, private local API, diagnostics, support, and audit hardening.

### SaaS/API/Billing/Email Future Phases

Remain blocked until dedicated phases exist:

- public SaaS;
- public API;
- billing real;
- email real;
- real customer credentials.

## HITO-162 Decision

HITO-162 is paused/not forgotten.

It must be treated as a legacy milestone requiring reconciliation. It should be rewritten or mapped to the new NODAL OS roadmap using `docs/roadmap/nodal-os-legacy-hito-absorption-matrix.md`.

## Advancement Rules

- Use grouped milestones when they reduce coordination overhead.
- Do not mix rename with proof/security-critical changes.
- Do not close external/live broad capability without persisted ledger evidence.
- Do not treat M65 as external CDP general-ready.
- Do not open sensitive surfaces without dedicated evidence and gates.
- Keep Core authority: Core decides, Browser Runtime executes, UI/Companion/Admin observes/transports without authority.
- Keep percentages visible and honest.

## Active Restrictions

- No SaaS public.
- No public API real.
- No billing real.
- No email real.
- No real customer credentials.
- No sensitive sites.
- No AFIP, banks, ERP, fiscal, financial, or government sites.
- No submit/pay/sign/delete.
- No productive recorder/replay.
- No Chrome/CDP general-ready claim from target-owned proof.
- No Chromium fork planned now.

## M145-M147 Update

HITO-162 replacement is stable local fixture-first after M133-M144 and internal audit M145-M147.

Next phase recommendation:

- Product/Admin polish.
- Continue internal local private preview iteration.
- Run a focused Claude audit before broader local preview expansion if scope changes.
- Keep embedded runtime evaluation future-only.
- Keep Chromium fork not planned.

External CDP general-ready remains false. Production, SaaS public, public API real, billing/email real, credentials, sensitive sites, submit/pay/sign/delete, and productive recorder/replay remain blocked.
