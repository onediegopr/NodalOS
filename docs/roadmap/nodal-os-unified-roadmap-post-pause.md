# NODAL OS Unified Roadmap Post-Pause

## Scope

This roadmap converts new planning inputs into a NODAL OS post-pause implementation order.

NODAL OS is the operational project name. External names from source documents are treated as input-only and are not adopted.

## Current Baseline

- Agent Operations / Automation Layer: 97%.
- NODAL OS global: 95%.
- Browser Runtime / Chrome: 90%.
- OCR/perception: 97%.
- Runtime execution: deferred.
- Browser automation runtime: deferred.
- UI implementation: deferred.

## Subphase 1 - Core Mandatory Immediate

Objective:

- Build the next core substrate before UI or automation runtime.

Includes:

- Execution Registry + EventBus.
- Redaction Foundation final.
- Approval Center UX v1 planning.
- Controlled File Operation v2.
- Workspace v1.

Already exists:

- Contract-only orchestration.
- Evidence bridge.
- Common redaction patterns.
- Approval/evidence guardrails.

Missing:

- Unified execution registry.
- Core event bus.
- Final redaction foundation.
- Approval UX contract.
- Workspace model.
- Controlled file operation v2.

Risks:

- Treating registry visibility as execution permission.
- Adding UI before event/state semantics are stable.
- File operations without policy and evidence gates.

Guardrails:

- No execution permission from registry/event visibility.
- Evidence-first.
- Approval-first.
- Redaction-before-persistence.
- Local-first.

Estimated milestones:

- M468-M470: Core Runtime Registry + EventBus + Redaction Foundation.
- M471-M473: Approval Center Data Model + Timeline Projection + Evidence Registry Integration.
- M474-M476: Approval Center UX Contract Preview + Export/Handoff Data Pack + Runtime Observability Report.
- AUDIT-A: Claude Full Project Architecture & Safety Audit before UI real / Mission Control Shell.
- M477-M479: AUDIT-A Pre-UI Boundary & Naming Hardening.
- M480-M482: Mission Control Shell V1 Read-Only + Approval Display + Timeline/Evidence Views.
- M483-M485: Mission Control Interaction No-Op Events + Approval Decision Drafting + UI State Persistence Mock.
- M486-M488: Mission Control Empty States + Contextual Onboarding + Guardrail Explainers.
- M489-M491: Mission Control Visual Polish + Responsive Desktop Layout + Static UX Acceptance Pack.
- M492-M494: Controlled File Operation v2.
- M495-M497: Workspace v1.

Current percentage after M486-M488: 82%.

Target percentage after subphase: 80%.

M468-M470 status:

- Execution Registry foundation is ready as contract-only lifecycle bookkeeping.
- Core EventBus foundation is ready as in-memory canonical event capture without side effects.
- Redaction Foundation is strengthened through common redaction and safe serializers.
- Runtime execution remains deferred and unauthorized.

M471-M473 status:

- Approval Center data model is ready as contracts/services for approval cards and decisions.
- Timeline projection is ready as a derivable view from canonical core events.
- Evidence Registry integration is ready as metadata/ref-only linking across registry, event, approval and timeline surfaces.
- Approval, timeline and evidence visibility do not grant runtime authority.
- UI, scheduler, worker runtime, browser automation, cloud and execution remain deferred.

M474-M476 status:

- Approval Center UX Contract Preview is ready as a redacted future-UI data contract.
- Export/Handoff Data Pack is ready as JSON/Markdown contract-first output with registry, approval, timeline, evidence, guardrails and redaction summaries.
- Runtime Observability Report is ready as a future LOG/copy-report foundation with correlation ids, summaries, blocked actions, warnings/failures and handoff requirements.
- Evidence remains ref-only and raw screenshot, DOM, network, header, cookie, body and secret payloads remain forbidden.
- No real UI, frontend, cloud, LLM calls, scheduler, worker, browser automation, recorder/replay, queue, DSL parser runtime or execution was introduced.
- AUDIT-A is the next required architecture and safety review before UI real / Mission Control Shell.

M477-M479 status:

- AUDIT-A pre-UI boundary hardening is ready.
- AgentOperations and future UI project fitness tests guard against direct `OneBrain.BrowserExecutor.Cdp` references.
- Evidence model consolidation ADR names `NodalOsEvidenceBridgeRef` as canonical and marks `NexaEvidenceRef` legacy/compatibility only.
- Execution authorization gate ADR requires a future positive gate before runtime and forbids UI/AgentOperations direct runtime calls.
- Legacy `Nexa*` quarantine plan blocks cloud/licensing/BYOK until sensitive legacy subsystems are deleted, archived, isolated or migrated with tests.
- Naming serialization guards cover Approval UX preview, Handoff Data Pack, Runtime Observability Report, Timeline and Evidence outputs.
- No UI real, cloud, LLM provider calls, scheduler, worker, browser automation, recorder/replay, queue, DSL parser runtime or execution was introduced.

M480-M482 status:

- Mission Control Shell V1 read-only preview is ready as a contract-first visual shell.
- Approval Display renders approval previews and disabled/no-authority options.
- Timeline/Evidence/Observability views are read-only and redacted.
- Static HTML preview renderer exists for Mission Control direction without frontend app, JavaScript runtime, cloud, LLM calls, browser automation or execution wiring.
- Evidence remains ref-only and raw screenshot, DOM, network, header, cookie, body and secret payloads remain forbidden.
- UI boundary guards continue to block direct `OneBrain.BrowserExecutor.Cdp` references from new AgentOperations/Mission Control surfaces.
- Recommended next: M483-M485 Mission Control Interaction No-Op Events + Approval Decision Drafting + UI State Persistence Mock.

M483-M485 status:

- Mission Control no-op UI intent contracts are ready.
- Approval decision drafting is available as non-authoritative UI/local state only.
- UI state persistence is mock-only and in-memory.
- All interactions keep `CanAuthorizeExecution=false` and `RuntimeExecutionAllowed=false`.
- No positive execution gate, runtime, browser automation, cloud, LLM provider call, scheduler/worker, recorder/replay, queue, DSL parser runtime, shell/subprocess, productive filesystem persistence or productive DB was introduced.
- Recommended next: M486-M488 Mission Control Empty States + Contextual Onboarding + Guardrail Explainers.

M486-M488 status:

- Mission Control empty states are ready for no mission selected, no timeline, no approvals, no evidence, no observability report, no workspace, no interaction history, no approval draft, runtime unavailable, LLM not configured, cloud disabled and browser automation deferred.
- Contextual onboarding explains Mission Control, Timeline, Approvals, Evidence, Observability/LOG, Guardrails, runtime blocked, LLM/BYOK future, cloud disabled and what must exist before real execution.
- Guardrail explainers cover read-only/no-runtime/no-browser/no-cloud/no-LLM/no-filesystem/no-shell, approval no-authority, evidence ref-only, redaction, positive gate missing, recipe-risk hardening, browser runtime disconnection, legacy sensitive subsystem quarantine, human handoff and disabled button rationale.
- Guidance is read-only/no-op and cannot unlock execution, change policy, mutate registry or create exceptions.
- No runtime, positive gate, cloud, LLM provider call, telemetry/analytics, productive persistence, browser automation, scheduler/worker, recorder/replay, queue, DSL parser runtime, shell/subprocess or filesystem mutation was introduced.
- Recommended next: M489-M491 Mission Control Visual Polish + Responsive Desktop Layout + Static UX Acceptance Pack.

## Subphase 2 - Mission Control UX

Objective:

- Translate visual/UX inputs into NODAL OS Mission Control planning before productive UI implementation.

Includes:

- Mission Control shell.
- Vertical timeline.
- Evidence viewer.
- Logs/report panel.
- Human approval cards.
- Contextual onboarding.

Already exists:

- Evidence/reporting contracts.
- Human handoff contracts.
- Run/progress semantics.
- Automation event/evidence schema.

Missing:

- UI decision record.
- Information architecture.
- Component inventory.
- Accessibility and keyboard model.
- Local-only state model for UI.

Risks:

- Shipping UI before backend state/event model is stable.
- Visual polish masking missing governance.
- Confusing timeline/evidence view with execution authority.

Guardrails:

- UI observes and requests approval; it does not grant authority.
- Timeline is audit/evidence surface, not execution engine.
- Human approval cards must expose exact blocker and options.

Estimated milestones:

- M480-M482: Mission Control Shell V1 Read-Only + Approval Display + Timeline/Evidence Views. Completed.
- M483-M485: Mission Control Interaction No-Op Events + Approval Decision Drafting + UI State Persistence Mock. Completed.
- M486-M488: Mission Control Empty States + Contextual Onboarding + Guardrail Explainers. Completed.
- M489-M491: Mission Control Visual Polish + Responsive Desktop Layout + Static UX Acceptance Pack.
- M492-M494: Mission Control UX Decision Record and Visual QA.
- M495-M497: Timeline/Evidence interaction contracts.
- M498-M500: Approval cards and handoff UX refinement.

Current percentage: 54%.

Target percentage after subphase: 60%.

## Subphase 3 - LLM / Assignment

Objective:

- Prepare provider configuration, prompt governance, task assignment, and cost controls.

Includes:

- BYOK Provider Config.
- Prompt Governance.
- Assignment Engine v1.
- TaskGraph.
- Budget/cost guardrails.

Already exists:

- Agent Operations contracts.
- Worker boundary contracts.
- Safety and policy guardrails.

Missing:

- Provider credential model.
- BYOK storage/redaction policy.
- Assignment scoring.
- Prompt templates/governance.
- Cost budget model.
- TaskGraph contracts.

Risks:

- Provider secrets leaking into logs/evidence.
- Assignment engine acting as execution authority.
- Prompt drift without audit trail.
- Cost controls after the fact.

Guardrails:

- BYOK secrets never persist raw.
- Assignment recommends; policy/approval remains authoritative.
- Prompt changes are auditable.
- Budget limits fail closed.

Estimated milestones:

- M495-M497: BYOK Provider Config ADR/contracts.
- M498-M500: Prompt Governance contracts.
- M501-M503: Assignment Engine v1 contracts.
- M504-M506: TaskGraph planning/contracts.
- M507-M509: Budget/cost guardrails.

Current percentage: 25%.

Target percentage after subphase: 70%.

## Subphase 4 - Productization

Objective:

- Package the local-first product experience after core and governance are stable.

Includes:

- Export Audit/Handoff.
- Expert Advisor AI v1.
- Settings/models/fallback.
- Local workspace management.

Already exists:

- Evidence/ref bridge.
- Reports.
- Human handoff contracts.
- Recipe risk and selector safety foundations.

Missing:

- Export packages.
- Advisor review contracts.
- Settings model.
- Model fallback policy.
- Local workspace lifecycle.

Risks:

- Expert Advisor becoming executor.
- Export leaking raw secrets.
- Settings bypassing policy.

Guardrails:

- Expert Advisor observes/reviews, not executes.
- Export is redacted and evidence-bound.
- Settings cannot grant runtime permission.

Estimated milestones:

- M510-M512: Export Audit/Handoff contracts.
- M513-M515: Expert Advisor AI v1 ADR/contracts.
- M516-M518: Settings/models/fallback contracts.
- M519-M521: Local workspace management.

Current percentage: 30%.

Target percentage after subphase: 70%.

## Subphase 5 - Cloud Optional / Commercial

Objective:

- Plan commercial surfaces only after local-first privacy and governance are coherent.

Includes:

- Landing/download.
- Licensing/device activation.
- Updates.
- Templates.
- Optional sync only after privacy policy.

Already exists:

- Local-first strategic decision.
- Evidence-first and approval-first posture.

Missing:

- Commercial packaging decision.
- Licensing model.
- Update policy.
- Privacy policy.
- Optional sync policy.

Risks:

- Cloud-first drift.
- Billing/licensing before product safety.
- Sync leaking private workspace/evidence.

Guardrails:

- Local-first remains default.
- Optional sync requires privacy policy and explicit consent.
- Licensing cannot affect safety gates.

Estimated milestones:

- M522-M524: Commercial packaging ADR.
- M525-M527: Landing/download planning.
- M528-M530: Licensing/device activation ADR.
- M531-M533: Update policy.
- M534-M536: Optional sync/privacy decision.

Current percentage: 10%.

Target percentage after subphase: 45%.

## Subphase 6 - Automation Future

Objective:

- Defer automation runtime until preconditions are met.

Includes:

- Recipe DSL parser.
- Recorder log-only.
- Replay dry-run.
- Work Queue local.
- Read-only browser automation.
- Classifier hardening before any runtime.

Already exists:

- Automation Layer ADR.
- Automation Event/Evidence.
- Selector Safety.
- Human Handoff.
- Recipe Risk Classifier.
- DSL ADR.
- Cross-layer no-divergence tests.
- Dependency-direction tests.

Missing:

- Recipe Risk Classifier hardening.
- Dedicated Claude runtime audit.
- DSL parser decision for implementation.
- Recorder log-only contracts.
- Replay dry-run contracts.
- Work Queue local contracts.
- Read-only browser automation boundary proof.

Risks:

- Runtime introduced before classifier hardening.
- Classic RPA drift.
- Imported DSL treated as executable.
- Browser automation bypassing approval/evidence gates.

Guardrails:

- Automation runtime remains deferred.
- Classifier hardening is runtime-gated.
- Import never means direct execution.
- Browser automation requires dedicated audit and no-authority evidence semantics.

Estimated milestones:

- M537-M539: Recipe Risk Classifier hardening.
- M540-M542: Claude runtime-readiness audit.
- M543-M545: DSL parser contracts only.
- M546-M548: Recorder log-only contracts.
- M549-M551: Replay dry-run contracts.
- M552-M554: Work Queue local contracts.
- M555-M557: Read-only browser automation decision/proof.

Current percentage: 35%.

Target percentage after subphase: 65%.

## M489-M491 Status - Static UX Acceptance

M489-M491 closes the static Mission Control visual polish and UX acceptance pack.

Completed:

- Static visual preview polish for Mission Control.
- Responsive desktop layout contract for compact, standard, wide, and ultrawide/control-room modes.
- Static UX acceptance checklist for visual direction, content, guardrails, naming, and accessibility basics.
- Read-only/no-runtime/no-cloud/no-LLM indicators remain visible.
- Approval display remains disabled/no-authority.
- Evidence remains ref-only.
- Observability/log preview remains redacted and read-only.

Current percentages after M489-M491:

- NODAL OS global: 97.2%.
- Agent Operations / Automation Layer: 97.5%.
- Core Runtime: 75%.
- Evidence/Timeline foundation: 82%.
- Approval foundation: 81%.
- Redaction/Safety foundation: 83%.
- Productization foundation: 54%.
- Mission Control UX: 61%.

## Recommended Next Milestone

`M495-M497 Workspace Storage Mock + Mission Binding + Workspace Switcher Contract`.

Do not proceed to UI implementation, cloud implementation, browser automation runtime with execution authority, recorder/replay, queue, scheduler, DSL parser, or execution before the workspace/path-jail model and positive execution authorization gate are planned and audited.

After M489-M491, Mission Control UI remains read-only/no-op until the positive execution authorization gate exists.

## M492-M494 Status - Workspace Local Contract

M492-M494 closes the local workspace model, path jail binding, and import wizard contract.

Completed:

- Workspace local model as read-only contract.
- Path jail binding as contract-only boundary.
- Textual/mock-safe path validation for traversal, absolute paths, drive mismatch, UNC paths, mixed separators, empty paths, and sensitive path values.
- Project import wizard as mock-only/read-only flow.
- No real filesystem scan, no filesystem mutation, no file picker, no import, and no productive persistence.

Current percentages after M492-M494:

- NODAL OS global: 97.3%.
- Agent Operations / Automation Layer: 97.5%.
- Core Runtime: 76%.
- Evidence/Timeline foundation: 82%.
- Approval foundation: 81%.
- Redaction/Safety foundation: 84%.
- Productization foundation: 56%.
- Mission Control UX: 62%.
- Workspace Local: 28%.

Recommended next:

- `M495-M497 Workspace Storage Mock + Mission Binding + Workspace Switcher Contract`.
