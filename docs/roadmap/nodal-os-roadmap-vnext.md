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

## Orchestration In-Process Facade V1 No Execution M427

- M425-M427 created the Orchestration In-Process Facade V1 audit, facade service, tests, report, and artifact.
- `NodalOsOrchestrationInProcessFacade` added as in-process coordination service in `OneBrain.AgentOperations.Core`.
- Facade implements `Dispatch(NodalOsOrchestrationCommandEnvelope)` → `NodalOsOrchestrationCommandResult`.
- Validation via `NodalOsOrchestrationCommandValidator`.
- Evidence validation via `NodalOsEvidenceRefBridge.ValidateBridgeRef`.
- Common redaction via `NodalOsRedactionService`.
- No-execution invariant enforced structurally at facade boundary: `Executed=false` always.
- Policy gate, approval gate, evidence gate, and verification-before-done gate locations respected.
- 17 command kinds supported for contract handling.
- High/Critical risk without approval rejected.
- Invalid evidence refs rejected.
- Sensitive content detected and rejected.
- Pause/Resume/Cancel are contract-only.
- `RunningFuture` and `PausedFuture` not returned as active states.
- API/HTTP/gRPC/scheduler/worker/runtime/execution/UI not implemented.
- No `BrowserExecutor.Cdp` dependency.
- New types use `NodalOs*` prefix.
- Decision: `ORCHESTRATION_IN_PROCESS_FACADE_V1_READY_NO_EXECUTION`.

Recommended next milestone: `M428-M430 Agent Operations Adapter Project Skeleton or M428-M430 Scheduled Read-Only Runs Decision Record`.

## Agent Operations Browser Adapter Project Skeleton M430

- M428-M430 created `OneBrain.AgentOperations.Adapters.Browser` as a skeleton project for future browser-specific Agent Operations adapters.
- The adapter project references `OneBrain.AgentOperations.Contracts` and `OneBrain.AgentOperations.Core`.
- The adapter project does not reference `OneBrain.BrowserExecutor.Cdp`.
- `OneBrain.AgentOperations.Contracts` remains Browser/CDP-free.
- `OneBrain.AgentOperations.Core` remains Browser/CDP-free.
- `NodalOsBrowserAgentOperationsAdapterBoundary` was added as a marker-only boundary type.
- Adapter project skeleton only: true.
- Runtime behavior implemented: false.
- Browser runtime moved: false.
- `ChromeCdpBrowserExecutor.cs` stayed in `OneBrain.BrowserExecutor.Cdp`.
- `BrowserRuntimeSmoke.cs` stayed in `OneBrain.BrowserExecutor.Cdp`.
- `BrowserPersistentAuditLedger.cs` stayed in `OneBrain.BrowserExecutor.Cdp`.
- UI implemented: false.
- Orchestration API implemented: false.
- Execution implemented: false.

Recommended next milestone: `M431-M433 Scheduled Read-Only Runs Decision Record or M431-M433 Browser Adapter Extraction Phase 1`.

## Scheduled Read-Only Runs Decision Record M433

- M431-M433 created the Scheduled Read-Only Runs Boundary Discovery report and Architecture Decision Record.
- Scheduled read-only run implementation deferred: true.
- Scheduler implemented: false.
- Timer implemented: false.
- Background worker implemented: false.
- UI implemented: false.
- Execution implemented: false.
- Read-only definition created: future allowed scope is validate manifest, query registry, prepare dry-run, read/extract observation, collect evidence, produce RunReport/ProgressReport, request human decision, attach evidence, and evaluate verification.
- Forbidden scheduled read-only actions: click, type, submit, upload, download, login, captcha, 2FA, payment, send, delete, sign, publish, external mutation, file system mutation, and network mutation beyond a future approved read/navigation boundary.
- Policy gate required: true.
- Evidence/redaction gate required: true.
- Human approval remains required whenever future policy marks target, data, frequency, identity context, evidence sensitivity, or report destination as sensitive.
- Scheduled read-only cannot grant runtime permission and cannot use Registry Visible, Worker Healthy, Skill Approved, or Recipe Approved as permission.
- Future outputs are limited to RunReport, ProgressReport, evidence refs, failure kinds, warnings, and verification summaries.

Recommended next milestone: `M434-M436 Scheduled Read-Only Run Contracts V1 or M434-M436 Browser Adapter Extraction Phase 1`.

## Scheduled Read-Only Run Contracts V1 M436

- M434-M436 created Scheduled Read-Only Run Contracts V1.
- Added schedule model, run request model, dry-run preview model, lifecycle/status enum, frequency enum, validation result, validator, JSON serializer, and fixtures.
- Schedule contract is not a scheduler.
- Scheduler implemented: false.
- Timer implemented: false.
- Background worker implemented: false.
- UI implemented: false.
- Execution implemented: false.
- Read-only required: true.
- Runtime execution allowed: false.
- Runtime execution deferred: true.
- Global policy evaluation required: true.
- Evidence redaction required: true.
- Manual trigger required for V1 run requests: true.
- Dry-run preview only: true.
- Forbidden planned preview actions are rejected: click, type, submit, upload, download, login, captcha, 2FA, payment/pay, send, delete, sign, publish, mutate, write, and file system mutation.
- Evidence refs validate through the EvidenceRef bridge.
- Common redaction is used for schedules, allowed targets, evidence requirements, summaries, warnings, and evidence refs.

Recommended next milestone: `M437-M439 Claude Pre-Runtime Agent Operations Audit or M437-M439 Browser Adapter Extraction Phase 1`.

## Scheduled Read-Only Integration No-Divergence M442

- M440-M442 closed Claude MEDIUM-1 and MEDIUM-3 pre-runtime cleanup.
- Scheduled read-only forbidden-action screening now covers schedule `AllowedTargets`, schedule `Summary`, and preview `PlannedReadOnlyOperations`.
- `InvalidMutableActionSchedule` is covered by tests and rejected by `ValidateSchedule`.
- Cross-layer scheduled read-only to orchestration facade no-divergence tests added.
- RunReport/ProgressReport no-authority/report-only coverage added.
- Dependency-direction tests added for AgentOperations.Contracts, AgentOperations.Core, and AgentOperations.Adapters.Browser.
- Namespace migration implemented: false.
- Scheduler implemented: false.
- Timer implemented: false.
- Background worker implemented: false.
- API implemented: false.
- UI implemented: false.
- Execution implemented: false.

Recommended next milestones:

- `M443-M445 AgentOperations Namespace Migration Scoped With Obsolete Shims`.
- `M446-M448 Automation Layer ADR + RPA References Decision`.
- `M449-M451 Automation Event and Evidence Schema Contracts V1`.

## AgentOperations Namespace Migration M445

- M443-M445 migrated physical AgentOperations.Contracts source files to `OneBrain.AgentOperations.Contracts`.
- M443-M445 migrated physical AgentOperations.Core source files to `OneBrain.AgentOperations.Core`.
- AgentOperations.Adapters.Browser remains canonical under `OneBrain.AgentOperations.Adapters.Browser`.
- Compatibility shims were reviewed; duplicate type shims were not created because they would produce divergent enum/record CLR types.
- Internal tests now import canonical AgentOperations namespaces through explicit global usings.
- AgentOperations.Contracts browser-free: true.
- AgentOperations.Core browser-free: true.
- AgentOperations.Adapters.Browser references BrowserExecutor.Cdp: false.
- BrowserExecutor.Cdp remains temporary browser host: true.
- Broad Nexa rename implemented: false.
- Broad OneBrain rename implemented: false.
- Scheduler implemented: false.
- API implemented: false.
- UI implemented: false.
- Execution implemented: false.

Recommended next milestones:

- `M446-M448 Automation Layer ADR + RPA References Decision`.
- `M449-M451 Automation Event and Evidence Schema Contracts V1`.

## Automation Layer ADR + RPA References Decision M448

- M446-M448 created the NODAL OS Automation Layer ADR.
- UI.Vision, TagUI, and OpenRPA/OpenIAP are references only.
- No copied code, no AGPL/commercial RPA code, and no external RPA dependency introduced.
- NODAL OS remains Mission Control-first, approval-first, evidence-first, timeline-first, and local-first.
- Automation Layer is future governed automation, not classic RPA.
- Recorder future-only: true.
- Recipe DSL future-only: true.
- Work Queue future-only: true.
- Trigger Policy future-only: true.
- Automation Evidence Schema future-only: true.
- Human Handoff Contract planned: true.
- Selector Safety Policy planned: true.
- Recipe Risk Classifier planned: true.
- Recorder implemented: false.
- Replay implemented: false.
- Queue implemented: false.
- Scheduler implemented: false.
- Browser automation implemented: false.
- UI implemented: false.
- Execution implemented: false.

Recommended next milestones:

- `M449-M451 Automation Event and Evidence Schema Contracts V1`.
- `M452-M454 Selector Safety Policy + Human Handoff Contract V1`.
- `M455-M457 Recipe Risk Classifier + DSL Decision Record`.
- `M458-M459 Claude Automation Layer Pre-Implementation Audit`.
- `M460 Core Roadmap Re-Sync and Pause Closure`.

## Automation Event and Evidence Schema Contracts V1 M451

- M449-M451 created NODAL OS Automation Event and Evidence Schema Contracts V1.
- Added automation event kinds, automation evidence kinds, handoff reasons, event/evidence/handoff records, validator, JSON serializer, fixtures, tests, report, and artifact.
- Automation events are contract-only and cannot grant runtime permission.
- Runtime execution allowed: false.
- Runtime execution deferred: true.
- Global policy evaluation required: true.
- Evidence redaction required: true.
- Evidence refs validate through the EvidenceRef bridge.
- Common redaction is used for summaries, selector paths, DOM snippets, step logs, network metadata, and human notes.
- Raw secrets, cookies, headers, and private bodies are rejected.
- Screenshot evidence is future reference-only, not inline binary content.
- Network metadata is redacted-only and cannot include Authorization, Cookie, Set-Cookie, or body content.
- Human handoff requires a clear reason and explicit user options.
- Timeline and Mission Control compatibility are explicit through Mission/Task/Recipe/Step correlation fields.
- Recorder implemented: false.
- Replay implemented: false.
- Queue implemented: false.
- Scheduler implemented: false.
- Browser automation implemented: false.
- UI implemented: false.
- Execution implemented: false.

Recommended next milestones:

- `M452-M454 Selector Safety Policy + Human Handoff Contract V1`.
- `M455-M457 Recipe Risk Classifier + DSL Decision Record`.
- `M458-M459 Claude Automation Layer Pre-Implementation Audit`.
- `M460 Core Roadmap Re-Sync and Pause Closure`.

## Selector Safety Policy + Human Handoff Contract V1 M454

- M452-M454 created Selector Safety Policy + Human Handoff Contract V1.
- Added selector strategy kinds, selector risk kinds, selector safety decisions, selector policy, selector candidate, selector evaluation, human handoff contract, validator, serializer, fixtures, tests, report, and artifact.
- Selector policy is contract-only and observation-only.
- Runtime execution allowed: false.
- Runtime execution deferred: true.
- Selector/handoff can authorize action: false.
- Semantic/DOM/CDP strategies are preferred before Visual/OCR.
- Visual/OCR remains fallback/evidence-only and cannot be first strategy.
- Raw secrets, cookies, headers, tokens, passwords, and private bodies are rejected.
- Mutable intent is rejected.
- Unstable selectors are rejected or require human review.
- Human handoff requires a specific blocker, explicit user options, and rejects generic `blocked`.
- Evidence refs validate through the EvidenceRef bridge.
- Common redaction is used for selector paths, labels, blocker text, technical logs, reasons, and warnings.
- Timeline and Mission Control compatibility are explicit through Mission/Task/Recipe/Step correlation fields.
- Recorder implemented: false.
- Replay implemented: false.
- Queue implemented: false.
- Scheduler implemented: false.
- Browser automation implemented: false.
- UI implemented: false.
- Execution implemented: false.

Recommended next milestones:

- `M455-M457 Recipe Risk Classifier + DSL Decision Record`.
- `M458-M459 Claude Automation Layer Pre-Implementation Audit`.
- `M460 Core Roadmap Re-Sync and Pause Closure`.

## Recipe Risk Classifier + DSL Decision Record M457

- M455-M457 created Recipe Risk Classifier V1 and the Recipe DSL Decision Record.
- Added recipe step risk categories, risk levels, approval requirements, DSL decision enum, step risk input, step classification, recipe risk profile, DSL decision record, validation result, classifier, serializer, fixtures, tests, reports, artifact, and ADR.
- Recipe risk classifier is contract-only and cannot authorize action.
- Runtime execution allowed: false.
- Runtime execution deferred: true.
- Global policy evaluation required: true.
- Evidence redaction required: true.
- Read-only/extraction can classify Low but still cannot execute.
- Form-fill is at least Medium and requires approval before execution.
- Submit and external publish/send are High and require approval.
- Purchase/payment and delete/destructive are Critical and require approval.
- Credential/login/captcha/two-factor require human handoff.
- File system mutation requires approval and evidence.
- Browser automation remains future-only and runtime deferred.
- DSL is representation, not runtime.
- DSL parser deferred: true.
- DSL runtime deferred: true.
- Direct execution forbidden: true.
- Import validation required: true.
- JSON canonical model required: true.
- No TagUI dependency introduced.
- Recorder implemented: false.
- Replay implemented: false.
- Queue implemented: false.
- Scheduler implemented: false.
- Browser automation implemented: false.
- UI implemented: false.
- Execution implemented: false.

Recommended next milestones:

- `M458-M459 Claude Automation Layer Pre-Implementation Audit`.
- `M460-M462 Automation Layer Integration No-Divergence Cleanup`.
- `M463 Core Roadmap Re-Sync and Pause Closure`.

## Automation Layer Integration No-Divergence Cleanup M462

- M460-M462 closed Claude M458-M459 MEDIUM-2 with cross-layer no-divergence tests.
- M460-M462 closed Claude M458-M459 MEDIUM-3 with dependency-direction tests.
- AutomationEvent, SelectorSafety, HumanHandoff, RecipeRisk, RiskProfile, and DSL Decision contracts preserve no-runtime flags across layers.
- Runtime execution allowed remains false.
- Runtime execution deferred remains true.
- Evidence redaction remains required.
- Global policy evaluation remains required.
- Selector, handoff, and recipe risk outputs cannot authorize action.
- Evidence refs still validate through EvidenceBridge.
- Common redaction remains preserved for sensitive text.
- AgentOperations.Contracts remains browser/CDP-free.
- AgentOperations.Core remains browser/CDP-free.
- AgentOperations.Adapters.Browser still does not reference BrowserExecutor.Cdp.
- BrowserExecutor.Cdp remains the temporary browser runtime host.
- Claude MEDIUM-1 classifier keyword hardening is documented as runtime-gated backlog.
- MEDIUM-1 blocks any future recorder/replay, browser automation, DSL parser runtime, recipe/step execution, or approval gate that would use the classifier as runtime authority.
- MEDIUM-1 does not block pause closure, contracts, docs, or no-runtime design.
- DSL parser implemented: false.
- Recorder implemented: false.
- Replay implemented: false.
- Queue implemented: false.
- Scheduler implemented: false.
- Browser automation implemented: false.
- UI implemented: false.
- Execution implemented: false.
- Automation Layer can close pause after M463 if roadmap re-sync confirms no new runtime scope.
- Do not implement runtime until a new dedicated Claude audit approves that scope.

Recommended next milestone:

- `M463 Core Roadmap Re-Sync and Pause Closure`.

## Pause Closure and Core Roadmap Re-Sync M463

- M463 formally closes the Agent Operations / Orchestration / Scheduled Read-Only / Automation Layer pause.
- Pause closed: true.
- Ready for core roadmap return: true.
- Ready for new topics intake: true.
- Automation Layer design closed for this pause: true.
- Runtime implementation deferred: true.
- Recipe Risk Classifier hardening remains runtime-gated.
- The runtime-gated classifier backlog blocks future classifier-backed approval gates, recorder/replay, browser automation, DSL parser runtime, recipe execution, and step execution.
- New documents and topics can be incorporated after this closure, but only through planning intake first.
- New topics pending intake: Mission Control Visual/UX direction, Business/GTM master document, Workflow Architecture roadmap, and consolidated NODAL OS source of truth.
- Do not proceed to real browser automation yet.
- Browser automation runtime remains deferred until classifier hardening and a new dedicated Claude runtime audit.
- Recorder implemented: false.
- Replay implemented: false.
- Queue implemented: false.
- Scheduler implemented: false.
- Browser automation implemented: false.
- DSL parser implemented: false.
- UI implemented: false.
- Execution implemented: false.

Post-pause Track A - Core mandatory:

- Execution Registry + EventBus.
- Redaction Foundation final.
- Approval Center UX v1.
- Controlled File Operation v2.
- Workspace v1.

Post-pause Track B - Product/UX intake:

- Consolidate Mission Control Visual/UX direction.
- Consolidate Business/GTM document.
- Consolidate Master Roadmap/Architecture document.
- Create NODAL OS unified source of truth.

Post-pause Track C - LLM/Assignment:

- BYOK Provider Config.
- Assignment Engine v1.
- Prompt Governance.
- Budget/cost guardrails.

Recommended next milestone:

- `New topics intake / unified roadmap update before implementation`.

## M465-M467 New Topics Intake + Unified Roadmap Update

- M465-M467 performs post-pause intake for new planning inputs without implementation.
- Operational project name remains NODAL OS.
- External names from source documents are input-only and are not operational names.
- HOTEP is treated as external visual/UX input only.
- The RPA source-plan product name from external input is treated as historical wording only.
- New topics intake report: `docs/reports/new-topics-intake-m465.md`.
- Unified post-pause roadmap: `docs/roadmap/nodal-os-unified-roadmap-post-pause.md`.
- Core return recommended: true.
- No runtime implemented.
- No UI implemented.
- No cloud implemented.
- No automation runtime implemented.
- Browser automation runtime remains deferred.
- Recipe Risk Classifier hardening remains runtime-gated before any automation runtime.

Unified roadmap subphases:

- Subphase 1: Core mandatory immediate.
- Subphase 2: Mission Control UX.
- Subphase 3: LLM / Assignment.
- Subphase 4: Productization.
- Subphase 5: Cloud optional / commercial.
- Subphase 6: Automation future.

Recommended next milestone:

- `M468-M470 Core Runtime Registry EventBus Redaction Planning or Execution Registry + EventBus`.

## M468-M470 Core Runtime Registry + EventBus + Redaction Foundation

- M468-M470 creates the first post-pause core foundation implementation block.
- Execution Registry foundation created for contract-only request registration, state transitions, evidence refs, timestamps, actor/source metadata, and redacted failure reasons.
- Core EventBus foundation created for canonical in-memory events, registry linkage, evidence refs, timeline-compatible projections, and safe serialization.
- Redaction Foundation strengthened through the existing common redaction service for bearer tokens, authorization headers, cookies, passwords, secrets, api keys, access/refresh/id tokens, private keys, connection strings, emails, and private body markers.
- Registry and EventBus visibility do not grant execution permission.
- Runtime execution allowed remains false.
- Runtime execution deferred remains true.
- Global policy evaluation and evidence redaction remain required.
- No UI, cloud, LLM provider call, scheduler, worker runtime, browser automation, recorder, replay, queue, DSL parser runtime, shell/subprocess, API, or execution was introduced.
- Report: `docs/reports/core-runtime-registry-eventbus-redaction-m468-m470.md`.
- Artifact: `artifacts/agent-operations/m470/core-runtime-registry-eventbus-redaction-summary.json`.

Recommended next milestone:

- `M471-M473 Approval Center Data Model + Timeline Projection + Evidence Registry Integration`.

## M471-M473 Approval Center Data Model + Timeline Projection + Evidence Registry Integration

- M471-M473 connects the core runtime registry/eventbus/redaction foundation to product-facing approval, timeline and evidence surfaces.
- Approval Center data model created for approval cards, approval decisions, status, severity, requested action, policy reason, affected resources, user options and evidence refs.
- Timeline projection created from canonical core events with ordering, registry refs, approval refs, severity/status, human attention flag and safe evidence refs.
- Evidence Registry integration created as metadata/ref-only attachment across registry entries, core events, approval cards and timeline entries.
- Approval card visibility does not execute.
- Approval decision visibility does not authorize execution by itself.
- Timeline projection does not mutate registry or evidence.
- Evidence integration rejects raw payload persistence and raw secret/cookie/header/body material.
- Screenshot evidence remains reference-only.
- Network evidence remains metadata-redacted-only.
- DOM evidence remains redacted/reference-only.
- Runtime execution allowed remains false.
- Runtime execution deferred remains true.
- Global policy evaluation and evidence redaction remain required.
- No UI, frontend, cloud, LLM provider call, scheduler, worker runtime, browser automation, recorder, replay, queue, DSL parser runtime, shell/subprocess, API, persistence DB, or execution was introduced.
- Report: `docs/reports/approval-timeline-evidence-integration-m471-m473.md`.
- Artifact: `artifacts/agent-operations/m473/approval-timeline-evidence-integration-summary.json`.

Recommended next milestone:

- `M474-M476 Approval Center UX Contract Preview + Export/Handoff Data Pack + Runtime Observability Report`.

## M474-M476 Approval Center UX Contract Preview + Export/Handoff Data Pack + Runtime Observability Report

- M474-M476 prepares the visible/exportable core layer without implementing real UI.
- Approval Center UX Contract Preview created as redacted contracts/services for future UI cards, titles, summaries, full explanations, severity/risk, status, requested action, affected resources, policy reason, user options, rollback/no-rollback information, expected evidence, attention flags, registry refs, event refs, timeline refs and evidence refs.
- Export/Handoff Data Pack created as JSON/Markdown-safe contract for registry entries, approval previews/decisions, timeline entries, evidence refs, warnings, failures, handoff requirements, guardrails, redaction summary and next steps.
- Runtime Observability Report created as a safe technical report contract for future LOG/copy-report flows with registry/event/timeline/approval/evidence summaries, blocked actions, failures/warnings, handoff requirements, correlation ids and next recommended action.
- Evidence remains ref-only. Screenshot inline data, raw DOM, raw network, raw headers, raw cookies, raw body and raw secrets remain forbidden.
- Approval previews, handoff packs and observability reports cannot execute, cannot authorize execution, and cannot mutate registry state.
- No UI, frontend, cloud, LLM provider call, scheduler, worker runtime, browser automation, recorder, replay, queue, DSL parser runtime, shell/subprocess, API, persistence DB, PDF/DOCX production export, or execution was introduced.
- Report: `docs/reports/approval-ux-handoff-observability-m474-m476.md`.
- Artifact: `artifacts/agent-operations/m476/approval-ux-handoff-observability-summary.json`.

Recommended next milestone:

- `AUDIT-A - Claude Full Project Architecture & Safety Audit before UI real / Mission Control Shell`.

## AUDIT-A Claude Full Project Architecture & Safety Audit

- Timing: after M474-M476 and before UI real / Mission Control Shell.
- Objective: independent full-project architecture and safety review.
- Scope: architecture, naming, guardrails, dependency direction, duplication, evidence/timeline, approval, redaction, export/handoff, observability, runtime deferral, roadmap coherence and readiness for future UI/LLM/cloud.
- Runtime remains blocked during the audit. UI real, cloud, LLM provider calls, browser automation, scheduler/worker, recorder/replay, queue, DSL parser runtime and execution remain deferred.

## M477-M479 AUDIT-A Pre-UI Boundary & Naming Hardening

- M477-M479 closes the minimum fixes from `AUDIT_A_CONDITIONAL_PASS_FIXES_REQUIRED_BEFORE_UI`.
- Dependency-direction fitness tests now guard AgentOperations and future UI projects from direct `OneBrain.BrowserExecutor.Cdp` references.
- BrowserExecutor.Cdp remains a temporary runtime host but stays disconnected from new AgentOperations core/product-facing foundations.
- Evidence model consolidation ADR decides `NodalOsEvidenceBridgeRef` as the canonical future NODAL OS evidence ref model and marks `NexaEvidenceRef` legacy/compatibility only.
- Execution authorization gate ADR requires a future positive gate before any real runtime execution and forbids UI/AgentOperations direct runtime calls.
- Legacy `Nexa*` subsystem quarantine plan blocks cloud/licensing/BYOK until sensitive legacy billing/email/credentials/admin/config surfaces are deleted, archived, isolated or migrated with tests.
- Naming serialization guards protect Approval UX preview, Handoff Data Pack, Runtime Observability Report, Timeline and Evidence serialized outputs from operational `Nexa`, `NEXA`, `NODRIX` or `HOTEP`.
- No UI, cloud, LLM provider call, browser automation, scheduler, worker runtime, recorder, replay, queue, DSL parser runtime, shell/subprocess, persistence DB, broad rename, legacy deletion or execution was introduced.
- Report: `docs/reports/audit-a-pre-ui-boundary-naming-m477-m479.md`.
- Artifact: `artifacts/agent-operations/m479/audit-a-pre-ui-boundary-naming-summary.json`.

Recommended next milestone:

- `M480-M482 Mission Control Shell V1 Read-Only + Approval Display + Timeline/Evidence Views`.

## M480-M482 Mission Control Shell V1 Read-Only + Approval Display + Timeline/Evidence Views

- M480-M482 creates the first NODAL OS Mission Control visual shell as a read-only, contract-first preview.
- The shell exposes mission title, overall status, progress, read-only state, no-runtime state, no-browser-automation state, no-cloud-sync state and no-LLM-provider-calls state.
- Approval Display renders approval previews, risk/severity, requested action, affected resources, policy gate reason, rollback/evidence context, registry/event/timeline/evidence refs and disabled user options.
- Timeline view renders ordered timeline entries with status/severity, registry refs, approval refs, evidence refs and human attention flags.
- Evidence view remains ref-only and does not expose raw screenshot, DOM, network, header, cookie, body or secret payloads.
- Observability/LOG preview renders runtime summary, event bus summary, timeline summary, approval summary, evidence summary, redaction summary, blocked actions and next recommended action without clipboard/runtime wiring.
- UI boundary remains guarded: no direct `OneBrain.BrowserExecutor.Cdp` reference, no runtime primitives, no scheduler/worker, no browser automation, no cloud and no LLM calls.
- This is not real runtime UI. It is a static/read-only display contract and HTML preview renderer over safe fixtures/contracts.
- Report: `docs/reports/mission-control-shell-readonly-m480-m482.md`.
- Artifact: `artifacts/agent-operations/m482/mission-control-shell-readonly-summary.json`.

Recommended next milestone:

- `M483-M485 Mission Control Interaction No-Op Events + Approval Decision Drafting + UI State Persistence Mock`.

## M483-M485 Mission Control Interaction No-Op Events + Approval Decision Drafting + UI State Persistence Mock

- M483-M485 adds controlled Mission Control interactions as no-op UI intent contracts.
- UI intents cover selecting timeline entries, approval cards and evidence refs; expanding/collapsing timeline entries; switching navigation; opening observability/log preview; requesting explanation; requesting changes; deferring approval; copy technical log intent; acknowledging warnings; and opening guardrails summary.
- Every UI intent and UI event is no-op, non-authoritative and keeps `RuntimeExecutionAllowed=false`.
- Approval decision drafts can represent approve, reject, request changes, request explanation, defer and human handoff compatible decisions, but drafts do not update real approvals, mutate registry state or authorize execution.
- UI state persistence is mock-only and in-memory: active section, selected mission/timeline/approval/evidence refs, expanded timeline entries, filters, panel state and log preview state.
- No productive DB, cloud persistence, runtime execution, BrowserExecutor.Cdp reference, browser automation, scheduler/worker, recorder/replay, queue, DSL parser runtime, shell/subprocess or LLM provider call was introduced.
- Report: `docs/reports/mission-control-interaction-noop-m483-m485.md`.
- Artifact: `artifacts/agent-operations/m485/mission-control-interaction-noop-summary.json`.

Recommended next milestone:

- `M486-M488 Mission Control Empty States + Contextual Onboarding + Guardrail Explainers`.

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
