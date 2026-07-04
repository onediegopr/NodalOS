# NODAL OS Roadmap vNext

## Canonical Status Hardening Note

This roadmap contains historical percentages and legacy runtime/browser planning notes. Those values are not current implementation readiness for runtime/live, execution, mutation, physical export, redaction runtime, secret/PII scan, retention/deletion runtime or release/commercial use.

Current canonical status after the final privacy/export/controlled-execution closeout is:

- `PAUSED_READ_ONLY_NO_RUNTIME_NO_EXECUTION_NO_MUTATION_NO_PHYSICAL_EXPORT_NO_REDACTION_RUNTIME`.
- Runtime/live real readiness: 0%.
- Execution real readiness: 0%.
- Mutation real readiness: 0%.
- Physical export real readiness: 0%.
- Redaction runtime readiness: 0%.
- Secret/PII scan real readiness: 0%.
- Retention/deletion runtime readiness: 0%.
- Release/commercial readiness: NO-GO.

Any historical `Core Runtime` percentage in this document is roadmap/foundation context only and must not be read as live runtime authority or implementation readiness.

## Durable Runtime Safety Scaffold Status

Latest Durable Runtime scaffold status: `GO_WITH_FINDINGS_DURABLE_RUNTIME_SCAFFOLD_READ_MODEL_EVIDENCE_PACK_TEST_ONLY_READY`.

- Durable runtime design readiness: 74-82%.
- Durable runtime test-only scaffold: 50-60%.
- Property/corpus hardening: 38-48%.
- Symlink/junction readiness: 15-25%.
- Product ledger path product readiness: 10-15%.
- Redaction product wiring readiness: 22-32%.
- Runtime feature flag product readiness: 18-28%.
- Authority wiring readiness: 18-28%.
- Replay/failure evidence readiness: 32-42%.
- Runtime/live product enablement: 0% / NO-GO.
- Release/commercial readiness: 0% / NO-GO.
- Proyecto usable end-to-end for runtime product use: 0%.
- Latest property-corpus block: `GO_WITH_FINDINGS_DURABLE_RUNTIME_SCAFFOLD_PROPERTY_CORPUS_EXPANSION_TEST_ONLY_READY`.
- Latest read-model/evidence-pack block: `GO_WITH_FINDINGS_DURABLE_RUNTIME_SCAFFOLD_READ_MODEL_EVIDENCE_PACK_TEST_ONLY_READY`.
- Latest external audit block: `GO_WITH_FINDINGS_DURABLE_RUNTIME_SCAFFOLD_EXTERNAL_AUDIT_AFTER_READ_MODEL_EVIDENCE_PACK_READY`.
- Latest stop packet: `PAUSE_FOR_MANUAL_GO_BEFORE_DURABLE_RUNTIME_PRODUCT_ENABLEMENT`.
- Latest premortem decision packet: `GO_WITH_FINDINGS_DURABLE_RUNTIME_PRODUCT_ENABLEMENT_PREMORTEM_AND_DECISION_PACKET_READ_ONLY_READY`.
- Latest product ledger path test plan: `GO_WITH_FINDINGS_PRODUCT_LEDGER_PATH_CANONICALIZATION_REPARSE_AND_AUTHORITY_TEST_PLAN_ONLY_READY`.
- Latest product ledger path test-plan external audit: `GO_WITH_FINDINGS_PRODUCT_LEDGER_PATH_CANONICALIZATION_REPARSE_AUTHORITY_TEST_PLAN_EXTERNAL_AUDIT_READY`.
- Recommended next safe macro-block before any product enablement: `NODAL_OS_PRODUCT_LEDGER_PATH_CANONICALIZATION_REPARSE_AUTHORITY_IMPLEMENTATION_SCAFFOLD_DISABLED_TEST_ONLY`.
- Durable runtime product enablement still requires explicit manual GO; autonomous safe continuation may only proceed through docs/design/audit/test-plan/test-only blocks.

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

## M486-M488 Mission Control Empty States + Contextual Onboarding + Guardrail Explainers

- M486-M488 adds Mission Control guidance contracts and services for safe empty states, contextual onboarding and guardrail explainers.
- Empty states cover no mission selected, no active mission, no timeline events, no approvals pending, no evidence, no observability report, no workspace, no UI interaction history, no approval draft, no selected evidence ref, no guardrail warnings, runtime unavailable, LLM not configured, cloud disabled and browser automation deferred.
- Contextual onboarding explains missions, timeline, approvals, approval no-authority, evidence ref-only, Observability/LOG, runtime blocked, LLM/BYOK future, cloud disabled and what must exist before real execution.
- Guardrail explainers cover read-only mode, no runtime, no browser automation, no cloud sync, no LLM provider calls, no filesystem mutation, no shell/subprocess, approval no-authority, evidence ref-only, redaction, missing positive gate, recipe-risk hardening, browser runtime disconnection, legacy sensitive subsystem quarantine, human handoff and disabled button rationale.
- Guidance remains read-only/no-op: `CanExecuteAction=false`, `CanUnlockExecution=false`, `CanAuthorizeExecution=false`, `RuntimeExecutionAllowed=false`.
- No runtime execution, positive execution gate, BrowserExecutor.Cdp reference, browser automation, scheduler/worker, recorder/replay, queue, DSL parser runtime, cloud, LLM provider call, productive persistence, telemetry/analytics, shell/subprocess or filesystem mutation was introduced.
- Report: `docs/reports/mission-control-guidance-m486-m488.md`.
- Artifact: `artifacts/agent-operations/m488/mission-control-guidance-summary.json`.

Recommended next milestone:

- `M489-M491 Mission Control Visual Polish + Responsive Desktop Layout + Static UX Acceptance Pack`.

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

## M489-M491 Mission Control Visual Polish + Responsive Desktop Layout + Static UX Acceptance Pack

Decision:

- `M489+M490+M491 CERRADO / MISSION_CONTROL_STATIC_UX_ACCEPTANCE_READY`

What changed:

- Added static Mission Control visual polish as contract-first preview output.
- Added responsive desktop layout contract for compact, standard, wide, and ultrawide/control-room modes.
- Added static UX acceptance pack for visual, content, naming, accessibility, and guardrail review.
- Kept approvals visible as disabled/no-authority.
- Kept evidence visible as ref-only.
- Kept observability/log preview redacted and read-only.

Guardrails:

- No runtime execution.
- No positive execution gate implementation.
- No browser automation.
- No productive frontend app.
- No cloud.
- No LLM provider calls.
- No scheduler, worker, queue, recorder, replay, or DSL parser runtime.
- No telemetry or analytics.
- NODAL OS remains the operational name.

Updated percentages:

- NODAL OS global: 97.2%.
- Agent Operations / Automation Layer: 97.5%.
- Core Runtime: 75%.
- Evidence/Timeline foundation: 82%.
- Approval foundation: 81%.
- Redaction/Safety foundation: 83%.
- Productization foundation: 54%.
- Mission Control UX: 61%.

Recommended next milestone:

- `M492-M494 Workspace Local Model + Path Jail Binding + Project Import Wizard Contract`.

## M492-M494 Workspace Local Model + Path Jail Binding + Project Import Wizard Contract

Decision:

- `M492+M493+M494 CERRADO / WORKSPACE_LOCAL_MODEL_PATH_JAIL_IMPORT_CONTRACT_READY`

What changed:

- Added contract-first local workspace model.
- Added path jail binding model with textual/mock-safe relative path validation.
- Added project import wizard contract with read-only/mock-only eight-step flow.
- Preserved evidence refs, timeline refs, UI state refs, guardrail summaries, and next safe steps.

Guardrails:

- No real filesystem scan.
- No filesystem mutation.
- No real file picker.
- No productive persistence.
- No runtime execution.
- No positive execution gate implementation.
- No cloud.
- No LLM provider calls.
- No browser automation.
- NODAL OS remains the operational name.

Updated percentages:

- NODAL OS global: 97.3%.
- Agent Operations / Automation Layer: 97.5%.
- Core Runtime: 76%.
- Evidence/Timeline foundation: 82%.
- Approval foundation: 81%.
- Redaction/Safety foundation: 84%.
- Productization foundation: 56%.
- Mission Control UX: 62%.
- Workspace Local: 28%.

Recommended next milestone:

- `M495-M497 Workspace Storage Mock + Mission Binding + Workspace Switcher Contract`.

## M495-M497 Workspace Storage Mock + Mission Binding + Workspace Switcher Contract

Decision:

- `M495+M496+M497 CERRADO / WORKSPACE_STORAGE_MISSION_SWITCHER_MOCK_READY`

What changed:

- Added in-memory, fixture-safe Workspace Storage Mock.
- Added Workspace to Mission Binding as read-only/no-runtime contract state.
- Added Workspace Switcher Contract with local-first/privacy badges, path jail status, mock counts, no-op switch intents, and preview-only switch results.
- Preserved workspace path jail refs, mission refs, timeline refs, evidence refs, UI state refs, and import wizard refs.

Guardrails:

- No real filesystem scan.
- No directory listing.
- No file read/write/delete.
- No filesystem mutation.
- No real file picker.
- No productive persistence.
- No database.
- No cloud.
- No LLM provider calls.
- No runtime execution.
- No positive execution gate implementation.
- No browser automation.
- No scheduler, worker, queue, recorder, replay, or DSL parser runtime.
- NODAL OS remains the operational name.

Updated percentages:

- NODAL OS global: 97.4%.
- Agent Operations / Automation Layer: 97.5%.
- Core Runtime: 76%.
- Evidence/Timeline foundation: 82%.
- Approval foundation: 81%.
- Redaction/Safety foundation: 85%.
- Productization foundation: 58%.
- Mission Control UX: 64%.
- Workspace Local: 43%.

Recommended next milestone:

- `M498-M500 Workspace Metadata Index Mock + Safe Project Summary Contract + Workspace Health Report`.

## M498-M500 Workspace Metadata Index Mock + Safe Project Summary Contract + Workspace Health Report

Decision:

- `M498+M499+M500 CERRADO / WORKSPACE_METADATA_HEALTH_MOCK_READY`

What changed:

- Added Workspace Metadata Index Mock.
- Added Safe Project Summary Contract.
- Added Workspace Health Report.
- Preserved evidence refs, timeline refs, mission refs, workspace refs, redaction summaries, guardrails, blockers, warnings, and next safe steps.
- Explicitly disclosed that the project summary is based on safe/mock metadata, not real scan output.

Guardrails:

- No real filesystem scan.
- No directory listing.
- No file read/write/delete.
- No file hashing.
- No git command.
- No embeddings.
- No real project understanding.
- No filesystem mutation.
- No productive persistence.
- No runtime execution.
- No positive execution gate implementation.
- No cloud.
- No LLM provider calls.
- No browser automation.
- No scheduler, worker, queue, recorder, replay, or DSL parser runtime.
- NODAL OS remains the operational name.

Updated percentages:

- NODAL OS global: 97.5%.
- Agent Operations / Automation Layer: 97.5%.
- Core Runtime: 76%.
- Evidence/Timeline foundation: 82%.
- Approval foundation: 81%.
- Redaction/Safety foundation: 85%.
- Productization foundation: 60%.
- Mission Control UX: 65%.
- Workspace Local: 54%.

Recommended next milestone:

- `M501-M503 Workspace Readiness Gate + Project Understanding Intake Contract + Safe Context Boundary`.

## M501-M503 Workspace Readiness Gate + Project Understanding Intake Contract + Safe Context Boundary

Decision:

- `M501+M502+M503 CERRADO / WORKSPACE_READINESS_CONTEXT_BOUNDARY_READY`

What changed:

- Added Workspace Readiness Gate.
- Added Project Understanding Intake Contract for user-provided/mock-safe context only.
- Added Safe Context Boundary for display/export/future-use classification.
- Preserved no-runtime, no-filesystem-scan, no-LLM, no-cloud, no-authorization semantics.
- Added explicit future LLM/BYOK policy requirement for any future provider context use.

Guardrails:

- No real filesystem scan.
- No directory listing.
- No file read/write/delete.
- No file hashing.
- No git command.
- No embeddings.
- No real project understanding.
- No filesystem mutation.
- No productive persistence.
- No runtime execution.
- No positive execution gate implementation.
- No cloud.
- No LLM provider calls.
- No browser automation.
- No scheduler, worker, queue, recorder, replay, or DSL parser runtime.
- NODAL OS remains the operational name.

Updated percentages:

- NODAL OS global: 97.6%.
- Agent Operations / Automation Layer: 97.5%.
- Core Runtime: 76%.
- Evidence/Timeline foundation: 83%.
- Approval foundation: 81%.
- Redaction/Safety foundation: 86%.
- Productization foundation: 61%.
- Mission Control UX: 66%.
- Workspace Local: 64%.
- Project Understanding foundation: 18%.

Recommended next milestone:

- `M504-M506 User-Provided Context Capture + Context Review Cards + Context Evidence Linking`.

## M504-M506 User-Provided Context Capture + Context Review Cards + Context Evidence Linking

Decision:

- `M504+M505+M506 CERRADO / USER_PROVIDED_CONTEXT_REVIEW_LINKING_READY`

What changed:

- Added User-Provided Context Capture.
- Added Context Review Cards for Mission Control-safe review.
- Added Context Evidence Linking as ref-only.
- Preserved user-provided/not-verified semantics.
- Preserved Safe Context Boundary blocking for sensitive, secret, raw payload, and unknown context.

Guardrails:

- No real filesystem scan.
- No directory listing.
- No file read/write/delete.
- No file hashing.
- No git command.
- No embeddings.
- No real project understanding.
- No prompt creation.
- No filesystem mutation.
- No productive persistence.
- No runtime execution.
- No positive execution gate implementation.
- No cloud.
- No LLM provider calls.
- No browser automation.
- No scheduler, worker, queue, recorder, replay, or DSL parser runtime.
- NODAL OS remains the operational name.

Updated percentages:

- NODAL OS global: 97.7%.
- Agent Operations / Automation Layer: 97.5%.
- Core Runtime: 76%.
- Evidence/Timeline foundation: 84%.
- Approval foundation: 81%.
- Redaction/Safety foundation: 87%.
- Productization foundation: 62%.
- Mission Control UX: 67%.
- Workspace Local: 67%.
- Project Understanding foundation: 30%.

Recommended next milestone:

- `M507-M509 Context Intake UI Preview + Context Validation Summary + Project Understanding Readiness Report`.

## M507-M509 Context Intake UI Preview + Context Validation Summary + Project Understanding Readiness Report

Decision:

- `M507+M508+M509 CERRADO / CONTEXT_INTAKE_PREVIEW_READINESS_READY`

What changed:

- Added Context Intake UI Preview as static/read-only Mission Control renderer and contract.
- Added Context Validation Summary for non-authoritative context safety aggregation.
- Added Project Understanding Readiness Report for future governance and preconditions.
- Preserved user-provided/unverified context semantics.
- Preserved no real Project Understanding, no filesystem access, no provider calls and no prompt creation.

Guardrails:

- No real filesystem scan.
- No directory listing.
- No file read/write/delete.
- No file hashing.
- No git command.
- No embeddings.
- No real project understanding.
- No prompt creation.
- No filesystem mutation.
- No productive persistence.
- No runtime execution.
- No positive execution gate implementation.
- No cloud.
- No LLM provider calls.
- No browser automation.
- No scheduler, worker, queue, recorder, replay, or DSL parser runtime.
- NODAL OS remains the operational name.

Updated percentages:

- NODAL OS global: 97.8%.
- Agent Operations / Automation Layer: 97.5%.
- Core Runtime: 76%.
- Evidence/Timeline foundation: 85%.
- Approval foundation: 81%.
- Redaction/Safety foundation: 88%.
- Productization foundation: 63%.
- Mission Control UX: 69%.
- Workspace Local: 68%.
- Project Understanding foundation: 40%.

Recommended next milestone:

- `M510-M512 Project Understanding Policy ADR + Real Scan Preconditions + Context-to-LLM Governance Draft`.

## M510-M512 Project Understanding Policy ADR + Real Scan Preconditions + Context-to-LLM Governance Draft

Decision:

- `M510+M511+M512 CERRADO / PROJECT_UNDERSTANDING_POLICY_GOVERNANCE_READY`

What changed:

- Added Project Understanding Policy ADR.
- Added Real Scan Preconditions as contract-first, policy-only model.
- Added Context-to-LLM Governance Draft as future BYOK/LLM policy model.
- Preserved no real Project Understanding, no scan, no LLM, no prompt, no BYOK, no cloud, and no runtime.

Guardrails:

- No real filesystem scan.
- No directory listing.
- No file read/write/delete.
- No file hashing.
- No git command.
- No embeddings.
- No real project understanding.
- No prompt creation.
- No BYOK implementation.
- No LLM provider calls.
- No filesystem mutation.
- No productive persistence.
- No runtime execution.
- No positive execution gate implementation.
- No cloud.
- No browser automation.
- No scheduler, worker, queue, recorder, replay, or DSL parser runtime.
- NODAL OS remains the operational name.

Updated percentages:

- NODAL OS global: 97.9%.
- Agent Operations / Automation Layer: 97.5%.
- Core Runtime: 76%.
- Evidence/Timeline foundation: 85%.
- Approval foundation: 81%.
- Redaction/Safety foundation: 89%.
- Productization foundation: 64%.
- Mission Control UX: 69%.
- Workspace Local: 68%.
- Project Understanding foundation: 55%.
- LLM/Assignment: 30%.

Recommended next milestone:

- `M513-M515 BYOK Provider Settings Contract + Secret Storage Policy ADR + Provider Test Connection UX Contract`.

## M513-M515 BYOK Provider Settings Contract + Secret Storage Policy ADR + Provider Test Connection UX Contract

Decision:

- `M513+M514+M515 CERRADO / BYOK_PROVIDER_SETTINGS_SECRET_POLICY_READY`

What changed:

- Added BYOK Provider Settings Contract as reference-only future configuration.
- Added Secret Storage Policy ADR defining no raw secrets in serializable product surfaces and future secure-store/vault requirements.
- Added Provider Test Connection UX Contract as disabled/mock-only future preflight preview.
- Preserved no real BYOK, no secure store, no provider calls, no network, no prompts, no cloud, and no runtime.

Guardrails:

- No real BYOK implementation.
- No secure store implementation.
- No OS keychain implementation.
- No local encrypted vault implementation.
- No provider SDK.
- No provider calls.
- No network or HTTP.
- No prompt creation.
- No LLM routing.
- No raw credential persistence.
- No environment variable reading.
- No cloud.
- No runtime execution.
- No positive execution gate implementation.
- No browser automation.
- No scheduler, worker, queue, recorder, replay, or DSL parser runtime.
- NODAL OS remains the operational name.

Updated percentages:

- NODAL OS global: 98.0%.
- Agent Operations / Automation Layer: 97.5%.
- Core Runtime: 76%.
- Evidence/Timeline foundation: 85%.
- Approval foundation: 81%.
- Redaction/Safety foundation: 90%.
- Productization foundation: 65%.
- Mission Control UX: 69%.
- Workspace Local: 68%.
- Project Understanding foundation: 55%.
- LLM/Assignment: 40%.

Recommended next milestone:

- `M516-M518 Prompt Governance Contract + Budget Guardrails Draft + Model Capability Matrix`.

## M516-M518 Prompt Governance Contract + Budget Guardrails Draft + Model Capability Matrix

Decision:

- `M516+M517+M518 CERRADO / PROMPT_GOVERNANCE_BUDGET_MODEL_MATRIX_READY`

What changed:

- Added Prompt Governance Contract for future prompt policy without final prompt text generation.
- Added Budget Guardrails Draft for future cost/call/token/retry/concurrency policy without live counting or pricing lookup.
- Added static Model Capability Matrix for future provider/model capability classification without provider calls, model lookup, pricing lookup, or routing.
- Preserved no prompt creation, no LLM calls, no provider calls, no network, no BYOK real, no routing, no cloud, and no runtime.

Guardrails:

- No final prompt text generation.
- No LLM provider calls.
- No provider SDK.
- No provider calls.
- No network or HTTP.
- No BYOK implementation.
- No token counting real.
- No pricing lookup.
- No model availability lookup.
- No routing real.
- No cloud.
- No runtime execution.
- No positive execution gate implementation.
- No browser automation.
- No scheduler, worker, queue, recorder, replay, or DSL parser runtime.
- NODAL OS remains the operational name.

Updated percentages:

- NODAL OS global: 98.1%.
- Agent Operations / Automation Layer: 97.5%.
- Core Runtime: 76%.
- Evidence/Timeline foundation: 85%.
- Approval foundation: 81%.
- Redaction/Safety foundation: 91%.
- Productization foundation: 66%.
- Mission Control UX: 69%.
- Workspace Local: 68%.
- Project Understanding foundation: 55%.
- LLM/Assignment: 50%.

Recommended next milestone:

- `M519-M521 Assignment Engine v1 Contracts + TaskGraph Draft + Planner Readiness Gate`.

## M519-M521 Assignment Engine v1 Contracts + TaskGraph Draft + Planner Readiness Gate

Decision:

- `M519+M520+M521 CERRADO / ASSIGNMENT_ENGINE_TASKGRAPH_DRAFT_READY`

What changed:

- Added Assignment Engine v1 draft contracts for assignment requests and planning eligibility.
- Added non-authoritative TaskGraph Draft with all tasks non-executable.
- Added Planner Readiness Gate for manual draft, context review, and future LLM planning eligibility checks.
- Preserved no planner runtime, no prompt creation, no model calls, no executable task graph, no dispatch, no cloud, and no runtime.

Guardrails:

- No real planner.
- No executable TaskGraph.
- No final prompt text generation.
- No LLM provider calls.
- No provider SDK.
- No provider calls.
- No network or HTTP.
- No BYOK implementation.
- No token counting real.
- No pricing lookup.
- No model availability lookup.
- No routing real.
- No cloud.
- No runtime execution.
- No positive execution gate implementation.
- No browser automation.
- No scheduler, worker, queue, recorder, replay, or DSL parser runtime.
- NODAL OS remains the operational name.

Updated percentages:

- NODAL OS global: 98.2%.
- Agent Operations / Automation Layer: 97.5%.
- Core Runtime: 76%.
- Evidence/Timeline foundation: 86%.
- Approval foundation: 81%.
- Redaction/Safety foundation: 91%.
- Productization foundation: 67%.
- Mission Control UX: 70%.
- Workspace Local: 68%.
- Project Understanding foundation: 56%.
- LLM/Assignment: 60%.

Recommended next milestone:

- `M522-M524 Mission Plan Draft Preview + TaskGraph Review Cards + Assignment Evidence Linking`.

## M522-M524 Mission Plan Draft Preview + TaskGraph Review Cards + Assignment Evidence Linking

Decision:

- `M522+M523+M524 CERRADO / MISSION_PLAN_DRAFT_REVIEW_READY`

What changed:

- Added Mission Plan Draft Preview for non-authoritative assignment outputs with explicit draft-only, no-task-executable, no-model, no-prompt, no-runtime, and human-review disclosures.
- Added TaskGraph Review Cards for draft work items with no-op user options, non-authoritative state, disabled future runtime/model/filesystem capabilities, dependencies, blockers, evidence refs, timeline refs, and guardrails.
- Added Assignment Evidence Linking as ref-only links between assignment request, TaskGraph, work items, context refs, evidence refs, and timeline refs without raw evidence payloads or real verification.

Guardrails:

- No real planner.
- No executable TaskGraph.
- No final prompt text generation.
- No prompt creation.
- No LLM provider calls.
- No provider SDK.
- No provider calls.
- No network or HTTP.
- No BYOK implementation.
- No token counting real.
- No pricing lookup.
- No model availability lookup.
- No routing real.
- No real evidence verification.
- No cloud.
- No runtime execution.
- No positive execution gate implementation.
- No browser automation.
- No scheduler, worker, queue, recorder, replay, or DSL parser runtime.
- NODAL OS remains the operational name.

Updated percentages:

- NODAL OS global: 98.3%.
- Agent Operations / Automation Layer: 97.5%.
- Core Runtime: 76%.
- Evidence/Timeline foundation: 87%.
- Approval foundation: 81%.
- Redaction/Safety foundation: 92%.
- Productization foundation: 68%.
- Mission Control UX: 71%.
- Workspace Local: 68%.
- Project Understanding foundation: 56%.
- LLM/Assignment: 68%.

Recommended next milestone:

- `M525-M527 Assignment UI Preview + TaskGraph Interaction No-Op + Planner UX Acceptance Pack`.

## M525-M527 Assignment UI Preview + TaskGraph Interaction No-Op + Planner UX Acceptance Pack

Decision:

- `M525+M526+M527 CERRADO / ASSIGNMENT_UI_NOOP_ACCEPTANCE_READY`

What changed:

- Added Assignment UI Preview contracts and static rendering for Mission Control style review of draft assignment and TaskGraph state.
- Added TaskGraph Interaction No-Op contracts for select, expand, collapse, filter, sort, explanation, needs-review marking, draft note, revise-draft intent, compare, refs display, guardrails display, and technical report preview intent.
- Added Planner UX Acceptance Pack with acceptance criteria and UX states for draft, blocked readiness, missing refs, context review, dependency block, and all-work-items-draft-only.

Guardrails:

- No real planner.
- No executable TaskGraph.
- No prompt creation.
- No LLM provider calls.
- No provider SDK.
- No provider calls.
- No network or HTTP.
- No BYOK implementation.
- No routing real.
- No real evidence verification.
- No cloud.
- No runtime execution.
- No positive execution gate implementation.
- No browser automation.
- No scheduler, worker, queue, recorder, replay, or DSL parser runtime.
- No clipboard real.
- NODAL OS remains the operational name.

Updated percentages:

- NODAL OS global: 98.4%.
- Agent Operations / Automation Layer: 97.6%.
- Core Runtime: 76%.
- Evidence/Timeline foundation: 87%.
- Approval foundation: 82%.
- Redaction/Safety foundation: 92%.
- Productization foundation: 69%.
- Mission Control UX: 72%.
- Workspace Local: 68%.
- Project Understanding foundation: 56%.
- LLM/Assignment: 70%.

Recommended next milestone:

- `M528-M530 Assignment Review Persistence Mock + Planner Handoff Contract + Assignment Safety Audit Pack`.

## M528-M530 Assignment Review Persistence Mock + Planner Handoff Contract + Assignment Safety Audit Pack

Decision:

- `M528+M529+M530 CERRADO / ASSIGNMENT_REVIEW_HANDOFF_SAFETY_READY`

What changed:

- Added Assignment Review Persistence Mock with in-memory fixture-safe snapshots, deterministic serialization, and rehydration that preserves draft-only/no-authority/no-execution boundaries.
- Added Planner Handoff Contract with mission, assignment, TaskGraph draft, review session, blockers, open questions, missing readiness gates, evidence refs, timeline refs, context refs, guardrails, and user-facing sections.
- Added Assignment Safety Audit Pack with pass/fail audit dimensions for draft-only integrity, no-op integrity, no planner runtime, no prompt/model/provider, no runtime, no filesystem, no network, no productive persistence, redaction safety, deterministic serialization, and human-readable explanations.

Guardrails:

- No planner runtime.
- No executable TaskGraph.
- No prompt creation.
- No LLM provider calls.
- No provider SDK.
- No provider calls.
- No network or HTTP.
- No BYOK implementation.
- No real evidence verification.
- No cloud.
- No runtime execution.
- No positive execution gate implementation.
- No browser automation.
- No async dispatch runtime.
- No clipboard real.
- No productive persistence.
- NODAL OS remains the operational name.

Updated percentages:

- NODAL OS global: 98.5%.
- Agent Operations / Automation Layer: 97.7%.
- Core Runtime: 76%.
- Evidence/Timeline foundation: 88%.
- Approval foundation: 82%.
- Redaction/Safety foundation: 93%.
- Productization foundation: 70%.
- Mission Control UX: 72%.
- Workspace Local: 68%.
- Project Understanding foundation: 56%.
- LLM/Assignment: 72%.

Recommended next milestone:

- `M531-M533 Assignment Review History Mock + Handoff Compare Preview + Planner Governance Closeout`.

## M531-M533 Assignment Review History Mock + Handoff Compare Preview + Planner Governance Closeout

Decision:

- `M531+M532+M533 CERRADO / ASSIGNMENT_PLANNER_GOVERNANCE_CLOSEOUT_READY`

What changed:

- Added Assignment Review History Mock with in-memory fixture-safe history entries, visible labels, diff candidate refs, and visual/mock-only restore.
- Added Handoff Compare Preview that compares refs and metadata only, reports changed blockers, questions, readiness gates, evidence refs, timeline refs, context refs, guardrails, and unverified claims.
- Added Planner Governance Closeout covering Assignment contracts, TaskGraph draft, mission plan preview, review cards, UI preview, no-op interactions, mock persistence, handoff, safety audit, history mock, and compare preview.

Guardrails:

- No planner runtime.
- No executable TaskGraph.
- No prompt creation.
- No model/provider activity.
- No provider SDK.
- No HTTP/network.
- No BYOK implementation.
- No evidence content verification.
- No cloud.
- No runtime execution.
- No positive execution gate implementation.
- No browser automation.
- No async dispatch runtime.
- No clipboard integration.
- No productive persistence.
- NODAL OS remains the operational name.

Updated percentages:

- NODAL OS global: 98.6%.
- Agent Operations / Automation Layer: 97.8%.
- Core Runtime: 76%.
- Evidence/Timeline foundation: 88%.
- Approval foundation: 82%.
- Redaction/Safety foundation: 93%.
- Productization foundation: 71%.
- Mission Control UX: 73%.
- Workspace Local: 68%.
- Project Understanding foundation: 56%.
- LLM/Assignment: 74%.

Recommended next milestone:

- `M534-M536 Governed Project Understanding Preconditions + Assignment Archive Review + Next Phase ADR`.

## M534-M536 Governed Project Understanding Preconditions + Assignment Archive Review + Next Phase ADR

Closed: 2026-06-20

Decision: `M534+M535+M536 CERRADO / PROJECT_UNDERSTANDING_PRECONDITIONS_READY`

Scope:

- M534: Project Understanding Preconditions contracts/services/readiness result. All real capabilities blocked (filesystem, LLM, embeddings, indexing, cloud).
- M535: Assignment Archive Review covering M519-M533. CanArchiveAsGovernanceBaseline=true. All operational archive flags false.
- M536: Formal ADR — no direct move to real Project Understanding; all real scan/LLM/runtime capabilities blocked until future governed milestones.

Updated percentages:

- NODAL OS global: 98.7%
- Agent Operations / Automation Layer: 97.9%
- Core Runtime: 76%
- Evidence/Timeline foundation: 88%
- Approval foundation: 82%
- Redaction/Safety foundation: 94%
- Productization foundation: 72%
- Mission Control UX: 73%
- Workspace Local: 69%
- Project Understanding foundation: 60%
- LLM/Assignment: 74%
- Cloud optional: 10%

## M620 - Sidepanel Patch 5 - Visual QA Fixes / Contrast / Dead-Style Cleanup

Status: implemented in branch, pending validation at block close.

Adds:

- CSS-only corrective patch based on M619 visual QA.
- Stronger Research OS focus ring.
- Contrast refinements for active tab, primary action, recording state, and governance surfaces.
- `status-running` remap away from operational blue into Research OS risk treatment.
- Timeline dark-island softening toward warm Research OS research archive styling.
- Log and `pre` visual treatment as secondary research-note surfaces.
- M620 summary and post-QA risk register.
- Guardrail tests confirming no HTML, JS, or manifest mutation.

Boundaries:

- CSS-only patch.
- No HTML modification.
- No JS modification.
- No manifest modification.
- No behavior or structural layout changes.
- No Provider/cloud coupling, filesystem feature, productive consent, capability enablement, or product state persistence.

Updated progress estimate after closeout:

- NODAL OS global: 99.98%
- Agent Operations / Automation Layer: 99.4%
- Core Runtime: 76%
- Evidence/Timeline foundation: 96%
- Approval foundation: 95%
- Redaction/Safety foundation: 98%
- Productization foundation: 99.0%
- Mission Control UX: 99.0%
- Workspace Local: 84%
- Project Understanding foundation: 93%
- LLM/Assignment: 75%
- Cloud optional: 10%

## M624 - Installed Extension Interactive QA / Live Sidepanel State Audit

Status: implemented in branch, pending validation at block close.

Adds:

- Audit-only installed extension QA artifact set.
- Explicit environment limitation: Chrome connector was not available for live sidepanel control.
- Manual QA checklist for Operar, Aprender, Recetas, Runtime, focus, STOP responsive state, governance surfaces, runtime/model blocked perception, logs, and responsive widths.
- Readiness summary that keeps HTML, manifest, and JS changes blocked.

Decision target: `INSTALLED_EXTENSION_INTERACTIVE_QA_READY`.

Boundaries:

- No CSS, HTML, JS, or manifest changes.
- No runtime, provider/cloud integration, filesystem feature, productive consent, capability enablement, or source-of-truth promotion.
- Installed extension live pass/fail remains unknown until manual QA or connector repair.

Updated progress estimate after closeout:

- NODAL OS global: 99.98%
- Agent Operations / Automation Layer: 99.4%
- Core Runtime: 76%
- Evidence/Timeline foundation: 96%
- Approval foundation: 95%
- Redaction/Safety foundation: 98%
- Productization foundation: 99.4%
- Mission Control UX: 99.4%
- Workspace Local: 84%
- Project Understanding foundation: 93%
- LLM/Assignment: 75%
- Cloud optional: 10%

## M625 - Installed Extension QA Enablement / Manual Runbook

Status: implemented in branch, pending validation at block close.

Adds:

- QA enablement report documenting the M624 Chrome connector limitation.
- Manual Windows runbook for loading the unpacked extension from `browser-extension/onebrain-chrome-lab`.
- Screenshot checklist for Operar, Aprender, Recetas, Runtime, STOP narrow width, focus ring, governance surfaces, status badges, and console review.
- Readiness summary with user evidence required before product UI file changes.

Decision target: `INSTALLED_EXTENSION_QA_ENABLEMENT_READY`.

Boundaries:

- No CSS, HTML, JS, or manifest changes.
- No runtime, provider/cloud integration, filesystem feature, productive consent, capability enablement, or source-of-truth promotion.
- HTML, manifest, and JS remain NO-GO until installed-extension evidence is captured.

Updated progress estimate after closeout:

- NODAL OS global: 99.98%
- Agent Operations / Automation Layer: 99.4%
- Core Runtime: 76%
- Evidence/Timeline foundation: 96%
- Approval foundation: 95%
- Redaction/Safety foundation: 98%
- Productization foundation: 99.45%
- Mission Control UX: 99.45%
- Workspace Local: 84%
- Project Understanding foundation: 93%
- LLM/Assignment: 75%
- Cloud optional: 10%

## M626+M627 - Manual QA Evidence Contract + HTML/Manifest Readiness Gate

Status: implemented in branch, pending validation at block close.

Adds:

- Manual QA evidence intake contract for installed-extension screenshots and result fields.
- Manual QA result template for user-provided evidence.
- HTML/manifest readiness decision gate with all product changes still blocked by default.
- Readiness summary preserving JS, runtime, provider/cloud, filesystem, productive consent, and capability NO-GO status.

Decision target: `INSTALLED_EXTENSION_QA_EVIDENCE_GATE_READY`.

Boundaries:

- No CSS, HTML, JS, or manifest changes.
- No runtime, provider/cloud integration, filesystem feature, productive consent, capability enablement, or source-of-truth promotion.
- Manual QA evidence is required before any future HTML minimum patch candidate.
- Manifest/naming cleanup requires separate extension review and explicit approval.
- JS remains NO-GO.

Updated progress estimate after closeout:

- NODAL OS global: 99.98%
- Agent Operations / Automation Layer: 99.4%
- Core Runtime: 76%
- Evidence/Timeline foundation: 96%
- Approval foundation: 95%
- Redaction/Safety foundation: 98%
- Productization foundation: 99.5%
- Mission Control UX: 99.5%
- Workspace Local: 84%
- Project Understanding foundation: 93%
- LLM/Assignment: 75%
- Cloud optional: 10%

## M628 - Installed Extension Manual QA Evidence Capture

Status: blocked in branch, pending validation at block close.

Adds:

- Evidence capture report for the attempted installed-extension QA.
- Filled manual QA result with `manualQaCompleted=false`.
- Screenshot index documenting all 13 required scenarios as missing.
- Blocker report identifying Chrome connector setup as the failed step.
- HTML/manifest/JS go/no-go summary after incomplete evidence.

Decision target if blocked: `MANUAL_QA_EVIDENCE_CAPTURE_REQUIRES_USER_ACTION`.

Boundaries:

- No CSS, HTML, JS, or manifest changes.
- No runtime, provider/cloud integration, filesystem feature, productive consent, capability enablement, or source-of-truth promotion.
- HTML minimum patch remains NO-GO.
- Manifest/naming cleanup remains NO-GO.
- JS remains NO-GO.

Updated progress estimate after blocked closeout:

- NODAL OS global: 99.98%
- Agent Operations / Automation Layer: 99.4%
- Core Runtime: 76%
- Evidence/Timeline foundation: 96%
- Approval foundation: 95%
- Redaction/Safety foundation: 98%
- Productization foundation: 99.5%
- Mission Control UX: 99.5%
- Workspace Local: 84%
- Project Understanding foundation: 93%
- LLM/Assignment: 75%
- Cloud optional: 10%

## M619 - Sidepanel Visual QA / Accessibility / Contrast / Dead-Style Audit

Status: implemented in branch, pending validation at block close.

Adds:

- Audit-only visual QA report for sidepanel token patches M615-M618.
- CSS risk register with contrast, focus, semantics, and dead-style candidates.
- Guardrail tests for M615-M618 token/remap preservation and unchanged HTML/JS/manifest hashes.
- Recommendation for M620 or Claude audit before additional CSS migration.

Boundaries:

- Audit-only checkpoint.
- No CSS micro-fix in this block.
- No HTML modification.
- No JS modification.
- No manifest modification.
- No runtime behavior.
- No provider/cloud coupling, filesystem feature, productive consent, capability enablement, or product state persistence.

Updated progress estimate after closeout:

- NODAL OS global: 99.98%
- Agent Operations / Automation Layer: 99.4%
- Core Runtime: 76%
- Evidence/Timeline foundation: 96%
- Approval foundation: 95%
- Redaction/Safety foundation: 98%
- Productization foundation: 98.9%
- Mission Control UX: 98.9%
- Workspace Local: 84%
- Project Understanding foundation: 93%
- LLM/Assignment: 75%
- Cloud optional: 10%

Recommended next milestone:

- `M537+ Path Jail Implementation` (blocks real scan).
- `M537+ Consent UI and Scope Preview` (blocks real scan).
- `M537+ BYOK and Provider Policy` (blocks LLM context build).
- `M537+ Real Scan Implementation Audit` (required before enabling any real scan).

## M537-M539 Path Jail Preconditions + Consent Scope Preview + Real Scan Audit Gate

Closed: 2026-06-20

Decision: `M537+M538+M539 CERRADO / PROJECT_UNDERSTANDING_SCAN_GATE_READY`

Scope:

- M537: Path Jail Implementation Preconditions with symbolic root refs, required policies, no-mutation guarantee, cancellation, evidence/timeline plan, and audit requirement.
- M538: Consent UI and Scope Preview Contract with draft-only/no-op consent options and estimated-only scope preview.
- M539: Real Scan Audit Gate that blocks future scan, folder enumeration, content handling, content fingerprinting, indexing, vectorization, LLM context build, cloud sync, and runtime until a future governed milestone.

Updated percentages:

- NODAL OS global: 98.8%
- Agent Operations / Automation Layer: 98.0%
- Core Runtime: 76%
- Evidence/Timeline foundation: 89%
- Approval foundation: 83%
- Redaction/Safety foundation: 94%
- Productization foundation: 73%
- Mission Control UX: 74%
- Workspace Local: 71%
- Project Understanding foundation: 63%
- LLM/Assignment: 74%
- Cloud optional: 10%

Recommended next milestone:

- `M540+M541+M542 - Secret Detection Policy Preview + Exclusion Policy Pack + Scan Dry Run Contract`.

## M540-M542 Secret Detection Policy Preview + Exclusion Policy Pack + Scan Dry Run Contract

Closed: 2026-06-20

Decision: `M540+M541+M542 CERRADO / PROJECT_UNDERSTANDING_DRY_RUN_POLICY_READY`

Scope:

- M540: Secret Detection Policy Preview with policy categories, strategy refs, redaction requirement, user review requirement, and readiness blocked for real sensitive-data detection, real scan, and LLM context build.
- M541: Exclusion Policy Pack with default exclusion groups and preview-only rules. The pack does not apply rules to a real filesystem and cannot create an index or build LLM context.
- M542: Scan Dry Run Contract referencing Path Jail Preconditions, Consent Scope Preview, Secret Detection Policy Preview, Exclusion Policy Pack, and the M539 Real Scan Audit Gate. The gate remains blocked.

Updated percentages:

- NODAL OS global: 98.9%
- Agent Operations / Automation Layer: 98.1%
- Core Runtime: 76%
- Evidence/Timeline foundation: 89%
- Approval foundation: 83%
- Redaction/Safety foundation: 95%
- Productization foundation: 74%
- Mission Control UX: 74%
- Workspace Local: 72%
- Project Understanding foundation: 66%
- LLM/Assignment: 74%
- Cloud optional: 10%

Recommended next milestone:

- `M543+M544+M545 - Project Understanding Dry Run UI Preview + Scan Consent Review Cards + Dry Run Evidence Plan`.

## M543-M545 Project Understanding Dry Run UI Preview + Scan Consent Review Cards + Dry Run Evidence Plan

Closed: 2026-06-20

Decision: `M543+M544+M545 CERRADO / PROJECT_UNDERSTANDING_DRY_RUN_REVIEW_READY`

Scope:

- M543: Static dry-run UI preview with refs to dry-run contract, path jail preconditions, consent scope preview, sensitive-data policy preview, exclusion policy pack, and real scan audit gate.
- M544: Scan consent review cards with draft-only states, no-op options, non-authorizing results, and future-gate requirement.
- M545: Dry Run Evidence Plan with planned evidence refs, planned timeline previews, planned audit refs, redaction refs, and readiness blocked for real evidence emission and verification.

Updated percentages:

- NODAL OS global: 99.0%
- Agent Operations / Automation Layer: 98.2%
- Core Runtime: 76%
- Evidence/Timeline foundation: 90%
- Approval foundation: 84%
- Redaction/Safety foundation: 95%
- Productization foundation: 75%
- Mission Control UX: 75%
- Workspace Local: 72%
- Project Understanding foundation: 69%
- LLM/Assignment: 74%
- Cloud optional: 10%

Recommended next milestone:

- `M546+M547+M548 - Project Understanding Implementation ADR + Path Jail Prototype Contract + Scan Fixture Matrix`.

## M546-M548 Project Understanding Implementation ADR + Path Jail Prototype Contract + Scan Fixture Matrix

Closed: 2026-06-20

Decision: `M546+M547+M548 CERRADO / PROJECT_UNDERSTANDING_IMPLEMENTATION_BOUNDARY_READY`

Scope:

- M546: Formal implementation boundary ADR. Future Project Understanding implementation requires path jail prototype, fixture matrix, synthetic-only tests, dry-run simulator contract, audit checkpoint, explicit consent, no-mutation, cancellation, evidence/timeline, and redaction/sensitive-data/exclusion policies.
- M547: Path Jail Prototype Contract with synthetic root only, symbolic candidates, preview-only policy decisions, and readiness blocked for real operational use.
- M548: Scan Fixture Matrix with synthetic fixtures covering empty/source/dependency/generated/hidden/environment/sensitive-name/media/symlink-like/outside-jail/case/deep/limit/cancellation/no-mutation cases.

Updated percentages:

- NODAL OS global: 99.1%
- Agent Operations / Automation Layer: 98.3%
- Core Runtime: 76%
- Evidence/Timeline foundation: 90%
- Approval foundation: 84%
- Redaction/Safety foundation: 95%
- Productization foundation: 76%
- Mission Control UX: 75%
- Workspace Local: 73%
- Project Understanding foundation: 72%
- LLM/Assignment: 74%
- Cloud optional: 10%

Recommended next milestone:

- `M549+M550+M551 - Synthetic Dry Run Simulator Contract + Fixture Result Review + Scan Boundary Audit`.

## M549-M551 Synthetic Dry Run Simulator Contract + Fixture Result Review + Scan Boundary Audit

Closed: 2026-06-20

Decision: `M549+M550+M551 CERRADO / SYNTHETIC_DRY_RUN_SIMULATOR_READY`

Scope:

- M549: Synthetic Dry Run Simulator Contract that evaluates declared fixture metadata only and keeps all operational flags false.
- M550: Fixture Result Review with no-op, non-authorizing review options and local-only result review.
- M551: Scan Boundary Audit with synthetic-layer pass decision and all real readiness blocked.

Updated percentages:

- NODAL OS global: 99.2%
- Agent Operations / Automation Layer: 98.4%
- Core Runtime: 76%
- Evidence/Timeline foundation: 91%
- Approval foundation: 84%
- Redaction/Safety foundation: 95%
- Productization foundation: 77%
- Mission Control UX: 75%
- Workspace Local: 74%
- Project Understanding foundation: 75%
- LLM/Assignment: 74%
- Cloud optional: 10%

Recommended next milestone:

- `M552+M553+M554 - Synthetic Dry Run UI Results + Fixture Coverage Report + Real Scan Readiness ADR`.

## M552-M554 Synthetic Dry Run UI Results + Fixture Coverage Report + Real Scan Readiness ADR

Closed: 2026-06-20

Decision: `M552+M553+M554 CERRADO / REAL_SCAN_READINESS_ADR_READY`

Scope:

- M552: Synthetic Dry Run UI Results preview with static read-only/no-op result sections.
- M553: Fixture Coverage Report with 16/16 synthetic fixture categories covered.
- M554: Real Scan Readiness ADR declaring synthetic baseline ready and operational scan behavior not ready.

Updated percentages:

- NODAL OS global: 99.3%
- Agent Operations / Automation Layer: 98.5%
- Core Runtime: 76%
- Evidence/Timeline foundation: 91%
- Approval foundation: 84%
- Redaction/Safety foundation: 95%
- Productization foundation: 78%
- Mission Control UX: 76%
- Workspace Local: 74%
- Project Understanding foundation: 78%
- LLM/Assignment: 74%
- Cloud optional: 10%

Recommended next milestone:

- `M555+M556+M557 - Disabled Path Jail Prototype Gate + Synthetic Canonicalization Cases + No-Mutation Proof Contract`.

## M555-M557 Disabled Path Jail Prototype Gate + Synthetic Canonicalization Cases + No-Mutation Proof Contract

Closed: 2026-06-20

Decision: `M555+M556+M557 CERRADO / DISABLED_PATH_JAIL_PROTOTYPE_GATE_READY`

Scope:

- M555: Disabled Path Jail Prototype Gate with prototype disabled by default and all operational readiness blocked.
- M556: Synthetic Canonicalization Cases with 16/16 declared case groups represented.
- M557: No-Mutation Proof Contract with all mutation capability flags false.

Updated percentages:

- NODAL OS global: 99.4%
- Agent Operations / Automation Layer: 98.6%
- Core Runtime: 76%
- Evidence/Timeline foundation: 91%
- Approval foundation: 85%
- Redaction/Safety foundation: 96%
- Productization foundation: 79%
- Mission Control UX: 76%
- Workspace Local: 76%
- Project Understanding foundation: 80%
- LLM/Assignment: 74%
- Cloud optional: 10%

Recommended next milestone:

- `M558+M559+M560 - Disabled Path Jail UI Preview + Operational Access Audit ADR + Synthetic Policy Regression Pack`.

## M558-M560 Disabled Path Jail UI Preview + Operational Access Audit ADR + Synthetic Policy Regression Pack

Closed: 2026-06-20

Decision: `M558+M559+M560 CERRADO / OPERATIONAL_ACCESS_AUDIT_READY`

Scope:

- M558: Disabled Path Jail UI Preview with static read-only/no-op review options.
- M559: Operational Access Audit ADR declaring operational filesystem access not ready.
- M560: Synthetic Policy Regression Pack with all declared synthetic categories represented.

Updated percentages:

- NODAL OS global: 99.5%
- Agent Operations / Automation Layer: 98.7%
- Core Runtime: 76%
- Evidence/Timeline foundation: 91%
- Approval foundation: 85%
- Redaction/Safety foundation: 96%
- Productization foundation: 80%
- Mission Control UX: 77%
- Workspace Local: 77%
- Project Understanding foundation: 82%
- LLM/Assignment: 74%
- Cloud optional: 10%

Recommended next milestone:

- `M561+M562+M563 - Per-Capability Access Gate Contracts + Synthetic Failure Modes + Consent Enforcement Preview`.

## M561-M563 Per-Capability Access Gate Contracts + Synthetic Failure Modes + Consent Enforcement Preview

Closed: 2026-06-20

Decision: `M561+M562+M563 CERRADO / PER_CAPABILITY_ACCESS_GATES_READY`

Scope:

- M561: Per-capability access gates, all disabled by default and contract-only.
- M562: Synthetic failure modes with fail-closed behavior and no runtime escalation.
- M563: Consent enforcement preview with no-op, non-authorizing review behavior.

Updated percentages:

- NODAL OS global: 99.6%
- Agent Operations / Automation Layer: 98.8%
- Core Runtime: 76%
- Evidence/Timeline foundation: 92%
- Approval foundation: 86%
- Redaction/Safety foundation: 96%
- Productization foundation: 81%
- Mission Control UX: 77%
- Workspace Local: 78%
- Project Understanding foundation: 84%
- LLM/Assignment: 74%
- Cloud optional: 10%

Recommended next milestone:

- `M564+M565+M566 - Capability Gate UI Review + Consent Scope Ledger Mock + Fail-Closed Acceptance Pack`.
## M564+M565+M566 - Capability Gate Review Acceptance

Status: implemented in branch, pending validation at block close.

Adds:

- Static capability gate UI review with no enablement authority.
- Mock consent scope ledger with no productive persistence.
- Fail-closed acceptance pack for per-capability gate behavior.
- Static redacted artifacts under `artifacts/agent-operations/m566`.

Boundaries:

- Capability gates remain disabled by default.
- Consent entries remain mock-only and non-authoritative.
- Operational access, content access, content fingerprinting, indexing, LLM context, cloud, provider activity, and runtime remain blocked.

Updated progress estimate after closeout:

- NODAL OS global: 99.7%
- Agent Operations / Automation Layer: 98.9%
- Core Runtime: 76%
- Evidence/Timeline foundation: 92%
- Approval foundation: 87%
- Redaction/Safety foundation: 96%
- Productization foundation: 82%
- Mission Control UX: 78%
- Workspace Local: 79%
- Project Understanding foundation: 86%
- LLM/Assignment: 74%
- Cloud optional: 10%
## M567+M568+M569 - Real Access Blocker Closeout

Status: implemented in branch, pending validation at block close.

Adds:

- Static consent ledger UI preview over the mock ledger.
- Capability audit checklist with required pre-enable categories.
- Real access blocker closeout as governance baseline only.
- Static redacted artifacts under `artifacts/agent-operations/m569`.

Boundaries:

- Ledger remains mock-only.
- Checklist remains contract-only.
- Closeout does not recommend direct operational implementation.
- Operational access, content access, content fingerprinting, indexing, LLM context, cloud, provider activity, and runtime remain blocked.

Updated progress estimate after closeout:

- NODAL OS global: 99.8%
- Agent Operations / Automation Layer: 99.0%
- Core Runtime: 76%
- Evidence/Timeline foundation: 93%
- Approval foundation: 88%
- Redaction/Safety foundation: 96%
- Productization foundation: 83%
- Mission Control UX: 79%
- Workspace Local: 80%
- Project Understanding foundation: 88%
- LLM/Assignment: 74%
- Cloud optional: 10%
## M570+M571+M572 - Access Implementation Checkpoint

Status: implemented in branch, pending validation at block close.

Adds:

- Audit checkpoint review covering M534-M569.
- Productive consent design draft with no persistence or enforcement.
- Disabled access roadmap with all phases disabled by default.
- Static redacted artifacts under `artifacts/agent-operations/m572`.

Boundaries:

- Checkpoint cannot authorize implementation.
- Consent design cannot persist or enforce consent.
- Roadmap cannot enable operational access.
- Operational access, content access, content fingerprinting, indexing, LLM context, cloud, provider activity, and runtime remain blocked.

Updated progress estimate after closeout:

- NODAL OS global: 99.85%
- Agent Operations / Automation Layer: 99.1%
- Core Runtime: 76%
- Evidence/Timeline foundation: 93%
- Approval foundation: 88%
- Redaction/Safety foundation: 97%
- Productization foundation: 84%
- Mission Control UX: 79%
- Workspace Local: 81%
- Project Understanding foundation: 90%
- LLM/Assignment: 74%
- Cloud optional: 10%
## M573+M574+M575 - Productive Consent Design Review

Status: implemented in branch, pending validation at block close.

Adds:

- Productive consent design review as review-only and no-op.
- Disabled consent storage contract with no productive persistence.
- Consent audit acceptance as governance baseline only.
- Static redacted artifacts under `artifacts/agent-operations/m575`.

Boundaries:

- The review cannot approve implementation.
- The storage contract cannot persist or enforce consent.
- Acceptance cannot enable productive consent.
- Operational access, content access, content fingerprinting, indexing, LLM context, cloud, provider activity, and runtime remain blocked.

Updated progress estimate after closeout:

- NODAL OS global: 99.9%
- Agent Operations / Automation Layer: 99.2%
- Core Runtime: 76%
- Evidence/Timeline foundation: 93%
- Approval foundation: 89%
- Redaction/Safety foundation: 97%
- Productization foundation: 85%
- Mission Control UX: 80%
- Workspace Local: 82%
- Project Understanding foundation: 91%
- LLM/Assignment: 74%
- Cloud optional: 10%
## M576+M577+M578 - Consent Governance Closeout

Status: implemented in branch, pending validation at block close.

Adds:

- Consent adversarial test matrix as synthetic-only.
- Productive consent storage implementation ADR.
- Consent governance closeout as baseline only.
- Static redacted artifacts under `artifacts/agent-operations/m578`.

Boundaries:

- The matrix cannot persist or enforce consent.
- The ADR does not implement storage.
- The closeout cannot authorize productive consent.
- Operational access, content access, content fingerprinting, indexing, LLM context, cloud, provider activity, and runtime remain blocked.

Updated progress estimate after closeout:

- NODAL OS global: 99.95%
- Agent Operations / Automation Layer: 99.3%
- Core Runtime: 76%
- Evidence/Timeline foundation: 94%
- Approval foundation: 90%
- Redaction/Safety foundation: 97%
- Productization foundation: 86%
- Mission Control UX: 81%
- Workspace Local: 83%
- Project Understanding foundation: 92%
- LLM/Assignment: 74%
- Cloud optional: 10%
## M579+M580+M581 - Consent Storage Boundary

Status: implemented in branch, pending validation at block close.

Adds:

- Consent storage boundary test pack as synthetic-only.
- Disabled storage UI preview as static/read-only/no-op.
- Storage audit readiness as planning-only.
- Static redacted artifacts under `artifacts/agent-operations/m581`.

Boundaries:

- The boundary pack cannot use productive persistence.
- The preview cannot enable storage.
- The readiness pack cannot authorize implementation.
- Operational access, content access, content fingerprinting, indexing, LLM context, cloud, provider activity, and runtime remain blocked.

Updated progress estimate after closeout:

- NODAL OS global: 99.97%
- Agent Operations / Automation Layer: 99.4%
- Core Runtime: 76%
- Evidence/Timeline foundation: 94%
- Approval foundation: 91%
- Redaction/Safety foundation: 98%
- Productization foundation: 87%
- Mission Control UX: 82%
- Workspace Local: 84%
- Project Understanding foundation: 93%
- LLM/Assignment: 74%
- Cloud optional: 10%

## M582+M583+M584 - Visual Reengineering Foundation

Status: implemented in branch, pending validation at block close.

Adds:

- Visual audit of current sidepanel and static preview surfaces.
- Research OS design system foundation.
- Screen map, textual wireframes, and phased migration plan.
- Static Research Mission Control concept preview under `artifacts/agent-operations/m584`.

Boundaries:

- No massive productive redesign.
- No runtime changes.
- No operational access.
- No LLM context, cloud, provider activity, or productive consent changes.
- This is audit/design/plan-first.

Updated progress estimate after closeout:

- NODAL OS global: 99.97%
- Agent Operations / Automation Layer: 99.4%
- Core Runtime: 76%
- Evidence/Timeline foundation: 94%
- Approval foundation: 91%
- Redaction/Safety foundation: 98%
- Productization foundation: 89%
- Mission Control UX: 88%
- Workspace Local: 84%
- Project Understanding foundation: 93%
- LLM/Assignment: 74%
- Cloud optional: 10%
## M585+M586+M587 - Visual Foundation Phase 1

Status: implemented in branch, pending validation at block close.

Adds:

- Visual foundation tokens for Research OS direction.
- Static Research OS shell preview.
- Static visual QA pack and phase summary.
- Static redacted artifacts under `artifacts/agent-operations/m587`.

Boundaries:

- No broad productive redesign.
- No runtime changes.
- No operational access.
- No LLM context, cloud, provider activity, or productive consent changes.
- Phase 1 visual foundation only.

Updated progress estimate after closeout:

- NODAL OS global: 99.97%
- Agent Operations / Automation Layer: 99.4%
- Core Runtime: 76%
- Evidence/Timeline foundation: 94%
- Approval foundation: 91%
- Redaction/Safety foundation: 98%
- Productization foundation: 90%
- Mission Control UX: 90%
- Workspace Local: 84%
- Project Understanding foundation: 93%
- LLM/Assignment: 74%
- Cloud optional: 10%

## M588+M589+M590 - Mission Control Research OS Layout

Status: implemented in branch, pending validation at block close.

Adds:

- Mission Control Research OS layout artifact and static preview.
- Current Mission Hero centered on Build Local AI Workspace.
- Mission status panels for Consent / Capability, Evidence Summary, and Advisor Note.
- Activity Feed as compact mission journal.
- Static redacted artifacts under `artifacts/agent-operations/m590`.

Boundaries:

- Visual Mission Control only.
- Preview actions remain no-op and non-authorizing.
- No broad productive redesign.
- No operational access.
- No productive consent changes.
- No capability enablement.
- No LLM context, cloud, provider activity, or runtime behavior changes.

Updated progress estimate after closeout:

- NODAL OS global: 99.98%
- Agent Operations / Automation Layer: 99.4%
- Core Runtime: 76%
- Evidence/Timeline foundation: 94%
- Approval foundation: 91%
- Redaction/Safety foundation: 98%
- Productization foundation: 91%
- Mission Control UX: 92%
- Workspace Local: 84%
- Project Understanding foundation: 93%
- LLM/Assignment: 74%
- Cloud optional: 10%

## M591+M592+M593 - Timeline / Evidence Research OS Visual Implementation

Status: implemented in branch, pending validation at block close.

Adds:

- Timeline Research Journal as mission log, technical journal, living roadmap, evidence line, and governance record.
- Evidence Archive Visual System as research archive and governance binder.
- Static Traceability QA pack.
- Combined static preview under `artifacts/agent-operations/m593`.

Boundaries:

- Timeline and Evidence visuals only.
- Static fixture data only.
- No productive source-of-truth promotion.
- No evidence verification.
- No operational access.
- No productive consent changes.
- No capability enablement.
- No LLM context, cloud, provider activity, or runtime behavior changes.

Updated progress estimate after closeout:

- NODAL OS global: 99.98%
- Agent Operations / Automation Layer: 99.4%
- Core Runtime: 76%
- Evidence/Timeline foundation: 96%
- Approval foundation: 91%
- Redaction/Safety foundation: 98%
- Productization foundation: 92%
- Mission Control UX: 93%
- Workspace Local: 84%
- Project Understanding foundation: 93%
- LLM/Assignment: 74%
- Cloud optional: 10%

## M594+M595+M596 - Decisions / Advisor Research OS Visual Implementation

Status: implemented in branch, pending validation at block close.

Adds:

- Decision Room Research OS as executive governance and risk review surface.
- Advisor Notes Visual System as editorial technical advisory surface, not chat.
- Static Decision / Advisor QA pack.
- Combined static preview under `artifacts/agent-operations/m596`.

Boundaries:

- Decisions and Advisor visuals only.
- Static fixture data only.
- No real decision authorization.
- No approval mutation.
- No Advisor runtime.
- No productive source-of-truth promotion.
- No evidence verification.
- No operational access.
- No productive consent changes.
- No capability enablement.
- No LLM context, cloud, provider activity, or runtime behavior changes.

Updated progress estimate after closeout:

- NODAL OS global: 99.98%
- Agent Operations / Automation Layer: 99.4%
- Core Runtime: 76%
- Evidence/Timeline foundation: 96%
- Approval foundation: 94%
- Redaction/Safety foundation: 98%
- Productization foundation: 93%
- Mission Control UX: 94%
- Workspace Local: 84%
- Project Understanding foundation: 93%
- LLM/Assignment: 74%
- Cloud optional: 10%

## M597+M598+M599 - Consent / Runtime / Models Research OS Visual Implementation

Status: implemented in branch, pending validation at block close.

Adds:

- Consent Governance Console as capability governance and scope review surface.
- Runtime Local-First Safety Visual System with local-only, disabled network, blocked content access, disabled indexing, disabled Embeddings, disabled provider activity, and blocked execution.
- Models Policy Visual System as governance and policy surface, not a provider selector.
- Static Consent / Runtime / Models QA pack.
- Combined static preview under `artifacts/agent-operations/m599`.

Boundaries:

- Consent, Runtime, and Models visuals only.
- Static fixture data only.
- No real consent authorization.
- No productive consent persistence.
- No capability enablement.
- No runtime behavior.
- No evidence verification.
- No operational access.
- No provider activity, cloud, LLM context, model execution, or source-of-truth promotion.

Updated progress estimate after closeout:

- NODAL OS global: 99.98%
- Agent Operations / Automation Layer: 99.4%
- Core Runtime: 76%
- Evidence/Timeline foundation: 96%
- Approval foundation: 95%
- Redaction/Safety foundation: 98%
- Productization foundation: 94%
- Mission Control UX: 95%
- Workspace Local: 84%
- Project Understanding foundation: 93%
- LLM/Assignment: 75%
- Cloud optional: 10%

## M600+M601+M602 - Agents / Settings / Activity Feed Research OS Visual Implementation

Status: implemented in branch, pending validation at block close.

Adds:

- Agents Research OS Visual System as supervised mission roles, not autonomous bots.
- Settings Governance Visual System as policy configuration, not generic forms.
- Mission Activity Feed Visual System as mission-readable activity, not raw logs.
- Static Agents / Settings / Activity Feed QA pack.
- Combined static preview under `artifacts/agent-operations/m602`.

Boundaries:

- Agents, Settings, and Activity Feed visuals only.
- Static fixture data only.
- No real agents.
- No productive settings persistence.
- No capability enablement.
- No runtime behavior.
- No evidence verification.
- No operational access.
- No Provider Calls, cloud, LLM context, model execution, or source-of-truth promotion.

Updated progress estimate after closeout:

- NODAL OS global: 99.98%
- Agent Operations / Automation Layer: 99.4%
- Core Runtime: 76%
- Evidence/Timeline foundation: 96%
- Approval foundation: 95%
- Redaction/Safety foundation: 98%
- Productization foundation: 95%
- Mission Control UX: 96%
- Workspace Local: 84%
- Project Understanding foundation: 93%
- LLM/Assignment: 75%
- Cloud optional: 10%

## M603+M604+M605 - Research OS Visual Consolidation / Acceptance / Migration Readiness

Status: implemented in branch, pending validation at block close.

Adds:

- Research OS Visual Consolidation across Mission Control, Timeline, Evidence, Decisions, Advisor, Consent, Runtime, Models, Agents, Settings, Activity Feed, Blocked States, Readiness Gates, and Empty States.
- Cross-Surface Acceptance Pack for mission clarity, governance clarity, evidence traceability, decision authority, advisor pattern, consent pattern, runtime safety, model policy, supervised agents, settings governance, activity feed readability, and blocked-state explanation.
- Product UI Migration Readiness with phased migration plan and no direct broad UI rewrite recommendation.
- Static previews under `artifacts/agent-operations/m605`.

Boundaries:

- Consolidation, acceptance, and migration readiness only.
- Static fixture data only.
- No product UI migration.
- No product source-of-truth promotion.
- No runtime behavior.
- No evidence verification.
- No operational access.
- No Provider Calls, cloud, productive consent, capability enablement, or product state persistence.

Updated progress estimate after closeout:

- NODAL OS global: 99.98%
- Agent Operations / Automation Layer: 99.4%
- Core Runtime: 76%
- Evidence/Timeline foundation: 96%
- Approval foundation: 95%
- Redaction/Safety foundation: 98%
- Productization foundation: 96%
- Mission Control UX: 97%
- Workspace Local: 84%
- Project Understanding foundation: 93%
- LLM/Assignment: 75%
- Cloud optional: 10%

## M606+M607+M608 - Product UI Entry Point Audit / Sidepanel Migration Boundary / Visual Regression QA Plan

Status: implemented in branch, pending validation at block close.

Adds:

- Product UI Entry Point Audit identifying the Chrome extension sidepanel as the primary high-risk product UI candidate.
- Sidepanel / Mission Control Migration Boundary with allowed visual-only changes and forbidden runtime/source-of-truth couplings.
- Visual Regression QA Plan for future safe static UI migration.
- Boundary preview under `artifacts/agent-operations/m608`.

Boundaries:

- Audit, boundary, and QA plan only.
- No product UI migration.
- No broad UI rewrite.
- No product source-of-truth promotion.
- No runtime behavior.
- No evidence verification.
- No operational access.
- No Provider Calls, cloud, productive consent, capability enablement, or product state persistence.

Updated progress estimate after closeout:

- NODAL OS global: 99.98%
- Agent Operations / Automation Layer: 99.4%
- Core Runtime: 76%
- Evidence/Timeline foundation: 96%
- Approval foundation: 95%
- Redaction/Safety foundation: 98%
- Productization foundation: 97%
- Mission Control UX: 97%
- Workspace Local: 84%
- Project Understanding foundation: 93%
- LLM/Assignment: 75%
- Cloud optional: 10%

## M609+M610+M611 - Sidepanel Research OS Migration Plan / Rollback Strategy / Visual Regression Fixtures

Status: implemented in branch, pending validation at block close.

Adds:

- Sidepanel Research OS Migration Plan for the real Chrome extension sidepanel entrypoints.
- Rollback / Safety Strategy for future scoped sidepanel visual migration.
- Sidepanel Visual Regression Fixtures and Visual QA Baseline.
- Static migration preview under `artifacts/agent-operations/m611`.

Boundaries:

- Plan, rollback, and fixture baseline only.
- No sidepanel product UI migration.
- No product UI migration.
- No broad UI rewrite.
- No product source-of-truth promotion.
- No runtime behavior.
- No evidence verification.
- No operational access.
- No Provider Calls, cloud, productive consent, capability enablement, or product state persistence.

Updated progress estimate after closeout:

- NODAL OS global: 99.98%
- Agent Operations / Automation Layer: 99.4%
- Core Runtime: 76%
- Evidence/Timeline foundation: 96%
- Approval foundation: 95%
- Redaction/Safety foundation: 98%
- Productization foundation: 97.5%
- Mission Control UX: 97.5%
- Workspace Local: 84%
- Project Understanding foundation: 93%
- LLM/Assignment: 75%
- Cloud optional: 10%

## M612+M613+M614 - Sidepanel Visual Token Integration Dry Run / Diff Plan / No-Runtime Coupling Tests

Status: implemented in branch, pending validation at block close.

Adds:

- Sidepanel Visual Token Integration Dry Run mapping Research OS tokens to current sidepanel CSS candidates.
- Sidepanel Diff / Patch Plan split into eight future patch units.
- No-Runtime Coupling Test Plan and Go / No-Go criteria.
- Static token dry-run and patch plan previews under `artifacts/agent-operations/m614`.

Boundaries:

- Dry-run, diff-plan, and test-plan only.
- No sidepanel product UI migration.
- No token integration into product sidepanel files.
- No product UI migration.
- No broad UI rewrite.
- No product source-of-truth promotion.
- No runtime behavior.
- No evidence verification.
- No operational access.
- No Provider Calls, cloud, productive consent, capability enablement, or product state persistence.

Updated progress estimate after closeout:

- NODAL OS global: 99.98%
- Agent Operations / Automation Layer: 99.4%
- Core Runtime: 76%
- Evidence/Timeline foundation: 96%
- Approval foundation: 95%
- Redaction/Safety foundation: 98%
- Productization foundation: 98%
- Mission Control UX: 98%
- Workspace Local: 84%
- Project Understanding foundation: 93%
- LLM/Assignment: 75%
- Cloud optional: 10%


## M615 - Sidepanel Token Patch 1 - CSS Variable Addition

Status: implemented in branch, pending validation at block close.

Adds:

- 17 Research OS CSS variables to `:root` block of `sidepanel.css`.
- Source-boundary tests verifying CSS-only, no-HTML, no-JS, no-manifest changes.
- Governance approval artifact confirming additive-only, no-runtime-coupling operation.
- Patch 1 summary artifact.

Boundaries:

- CSS variable addition only.
- No remapping of existing variables.
- No HTML modification.
- No JS modification.
- No manifest modification.
- No runtime behavior.
- No Provider Calls, cloud, filesystem, productive consent, or capability enablement.

Updated progress estimate after closeout:

- NODAL OS global: 99.98%
- Agent Operations / Automation Layer: 99.4%
- Core Runtime: 76%
- Evidence/Timeline foundation: 96%
- Approval foundation: 95%
- Redaction/Safety foundation: 98%
- Productization foundation: 98.2%
- Mission Control UX: 98.2%
- Workspace Local: 84%
- Project Understanding foundation: 93%
- LLM/Assignment: 75%
- Cloud optional: 10%

## M621 - Sidepanel Manual Screenshot Visual QA

Status: implemented in branch, pending validation at block close.

Adds:

- Static Chrome screenshot QA for the sidepanel Operate surface.
- Screenshot QA report, risk register, and readiness summary.
- Tests proving M621 is audit-only and product sidepanel files remain byte-identical.
- Go / No-Go decision for M622 CSS-only cleanup.

Boundaries:

- Audit-only.
- No CSS modification.
- No HTML modification.
- No JS modification.
- No manifest modification.
- No runtime behavior.
- No evidence verification.
- No operational access.
- No provider/cloud coupling, productive consent, capability enablement, or product source-of-truth promotion.

Updated progress estimate after closeout:

- NODAL OS global: 99.98%
- Agent Operations / Automation Layer: 99.4%
- Core Runtime: 76%
- Evidence/Timeline foundation: 96%
- Approval foundation: 95%
- Redaction/Safety foundation: 98%
- Productization foundation: 99.1%
- Mission Control UX: 99.1%
- Workspace Local: 84%
- Project Understanding foundation: 93%
- LLM/Assignment: 75%
- Cloud optional: 10%

## M622 - Sidepanel Dead-Style Cleanup CSS-Only

Status: implemented in branch, pending validation at block close.

Adds:

- CSS-only cleanup removing orphan legacy root variables from `sidepanel.css`.
- Zero-reference verification for each removed legacy variable.
- Dead-style cleanup summary and cleanup register.
- Tests proving `--nos-*` tokens and M615-M620 remaps remain intact.

Boundaries:

- Removed only legacy root declarations confirmed with zero `var(...)` references.
- No selectors changed.
- No visual remap.
- No HTML modification.
- No JS modification.
- No manifest modification.
- No runtime behavior.
- No evidence verification.
- No operational access.
- No provider/cloud coupling, productive consent, capability enablement, or product source-of-truth promotion.

Updated progress estimate after closeout:

- NODAL OS global: 99.98%
- Agent Operations / Automation Layer: 99.4%
- Core Runtime: 76%
- Evidence/Timeline foundation: 96%
- Approval foundation: 95%
- Redaction/Safety foundation: 98%
- Productization foundation: 99.2%
- Mission Control UX: 99.2%
- Workspace Local: 84%
- Project Understanding foundation: 93%
- LLM/Assignment: 75%
- Cloud optional: 10%

## M623 - Sidepanel Contrast / Responsive Microfix CSS-Only

Status: implemented in branch, pending validation at block close.

Adds:

- CSS-only `.tab.active` contrast microfix.
- CSS-only STOP button narrow viewport microfix.
- Contrast/responsive summary and follow-up register.
- Tests proving HTML, JS, and manifest remain unchanged.

Boundaries:

- CSS-only.
- Touched only `.tab.active`, `.stop-button`, and a narrow viewport stop-button media query.
- No text, IDs, event handlers, or permissions changed.
- No HTML modification.
- No JS modification.
- No manifest modification.
- No runtime behavior.
- No evidence verification.
- No operational access.
- No provider/cloud coupling, productive consent, capability enablement, or product source-of-truth promotion.

Updated progress estimate after closeout:

- NODAL OS global: 99.98%
- Agent Operations / Automation Layer: 99.4%
- Core Runtime: 76%
- Evidence/Timeline foundation: 96%
- Approval foundation: 95%
- Redaction/Safety foundation: 98%
- Productization foundation: 99.3%
- Mission Control UX: 99.3%
- Workspace Local: 84%
- Project Understanding foundation: 93%
- LLM/Assignment: 75%
- Cloud optional: 10%

## M629 - Extension Legacy Naming Inventory + NODAL OS Minimum Naming Cleanup

Status: implemented in branch, pending validation at block close.

Adds:

- Chrome extension legacy naming inventory for `NEXA`, `ONE BRAIN`, `HOTEP`, and `NODRIX`.
- Minimum visible manifest metadata cleanup to `NODAL OS`.
- Visible sidepanel consent mojibake correction.
- Manual reload QA checklist after naming cleanup.
- Guardrail tests proving permissions, host permissions, storage keys, port names, alarm names, protocol, and sidepanel JS behavior remain unchanged.

Boundaries:

- No broad rename.
- No storage key rename.
- No port, alarm, message type, protocol, or socket change.
- No JS functional change.
- No CSS change.
- No runtime behavior.
- No provider/cloud integration, filesystem feature, productive consent, capability enablement, or product source-of-truth promotion.

Updated progress estimate after closeout:

- NODAL OS global: 99.98%
- Agent Operations / Automation Layer: 99.45%
- Core Runtime: 76%
- Evidence/Timeline foundation: 96%
- Approval foundation: 95%
- Redaction/Safety foundation: 98%
- Productization foundation: 99.6%
- Mission Control UX: 99.6%
- Workspace Local: 84%
- Project Understanding foundation: 93%
- LLM/Assignment: 75%
- Cloud optional: 10%
