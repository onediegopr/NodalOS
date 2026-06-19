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

- M468-M470: Execution Registry + EventBus planning/contracts.
- M471-M473: Redaction Foundation final.
- M474-M476: Controlled File Operation v2.
- M477-M479: Workspace v1.
- M480-M482: Approval Center UX v1 decision/contracts.

Current percentage: 55%.

Target percentage after subphase: 80%.

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

- M483-M485: Mission Control UX Decision Record.
- M486-M488: Timeline/Evidence UI contracts.
- M489-M491: Approval cards and handoff UX contracts.
- M492-M494: Mission Control shell skeleton, if core substrate is ready.

Current percentage: 20%.

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

## Recommended Next Milestone

`M468-M470 Core Runtime Registry EventBus Redaction Planning or Execution Registry + EventBus`.

Do not proceed to UI implementation, cloud implementation, browser automation runtime, recorder/replay, queue, scheduler, DSL parser, or execution before the core mandatory track is planned.
