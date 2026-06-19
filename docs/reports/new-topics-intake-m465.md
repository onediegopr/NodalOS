# NODAL OS New Topics Intake M465

## Base State After Pause Closure

NODAL OS closed the Agent Operations / Orchestration / Scheduled Read-Only / Automation Layer pause with:

- Orchestration no-execution ready.
- Scheduled Read-Only contracts ready.
- Automation Layer ADR ready.
- Automation Event/Evidence contracts ready.
- Selector Safety ready.
- Human Handoff ready.
- Recipe Risk Classifier ready as contract-only.
- Recipe DSL ADR ready.
- Cross-layer no-divergence tested.
- Dependency-direction tested.
- NODAL OS naming restored.

Current estimated state:

- Agent Operations / Automation Layer: 97%.
- NODAL OS global: 95%.
- Browser Runtime / Chrome: 90%.
- OCR/perception: 97%.

## Inputs Considered

1. Visual / UX-UI direction for HOTEP MVP.
2. Business / GTM master document.
3. Workflow Architecture roadmap.
4. Open-source RPA additions plan.

These are treated as planning inputs only. They do not authorize implementation.

## Naming Separation

- NODAL OS is the operational project name in this repository and branch.
- NODRIX is treated only as external/historical input wording when it appears in source documents.
- HOTEP is treated only as external visual/UX input wording.
- NEXA is historical compatibility debt, not an operational name.

All applicable ideas are translated into NODAL OS terminology.

## Ideas Applicable To NODAL OS

Visual / UX ideas applicable to NODAL OS Mission Control:

- Dark-first visual direction.
- Mission Control shell.
- Central vertical timeline.
- Right panel for agents, models, fallback, and assignment.
- Bottom panel for logs, events, and evidence.
- Minimal sidebar.
- Top bar with mission, status, and progress.

Business / GTM ideas applicable to NODAL OS:

- Desktop-first and local-first default.
- BYOK provider configuration.
- Evidence-first product trust.
- Approval-first safety model.
- Optional cloud track only after privacy, policy, and local-first foundations.
- Future pricing/packaging planning.
- Future landing/download/licensing track.

Workflow architecture ideas applicable to NODAL OS:

- Execution Registry + EventBus.
- Redaction Foundation finalization.
- Approval Center UX v1.
- Controlled File Operation v2.
- Workspace v1.
- Assignment Engine v1.
- Expert Advisor as observer/reviewer.
- Export Audit/Handoff.
- Optional cloud later.

RPA plan ideas applicable to NODAL OS:

- Recorder as future log-only capture.
- Replay as future dry-run only.
- Work Queue as local, policy-bound future capability.
- Read-only browser automation only after classifier hardening and dedicated runtime audit.
- Template packs as future productization, not runtime authority.

## Already Covered

The RPA plan is already partially absorbed through:

- Automation Layer ADR.
- Automation Event/Evidence contracts.
- Selector Safety Policy.
- Human Handoff Contract.
- Recipe Risk Classifier.
- Recipe DSL ADR.
- No classic RPA guardrails.
- No copied external RPA code.
- No external RPA dependency.

Scheduled read-only and orchestration no-execution are also covered as contract-only foundations.

## Deferred

The following remain deferred:

- Real recorder.
- Real replay.
- Work Queue runtime.
- Scheduler.
- Browser automation.
- DSL parser.
- UI implementation.
- Cloud runtime.
- Billing/licensing implementation.
- Package/template marketplace.
- Recipe/step execution.
- Recipe Risk Classifier hardening.

## Rejected / Not Convenient Now

Do not pursue:

- Classic RPA identity.
- Heavy workflow designer as product center.
- Unattended automation first.
- Recorder/replay as first feature.
- Browser automation runtime before classifier hardening.
- Cloud-first architecture.
- Billing/licensing before local core and privacy policy.
- Direct execution from imported recipe/DSL.

## Risks

- Naming drift from external documents.
- UI implementation before core runtime/guardrails are coherent.
- Automation runtime pressure before classifier hardening.
- Cloud/commercial scope pulling attention away from local-first foundation.
- BYOK/provider configuration without budget, privacy, and prompt governance.

## Recommendation

Proceed with a unified post-pause roadmap.

Recommended next block:

`M468-M470 Core Runtime Registry EventBus Redaction Planning or Execution Registry + EventBus`.

Do not implement UI, browser automation, recorder/replay, queue, scheduler, cloud, or runtime execution in this intake block.
