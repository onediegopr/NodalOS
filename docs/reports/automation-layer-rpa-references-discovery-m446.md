# NODAL OS - Automation Layer RPA References Discovery M446

## Source Material

This discovery uses the internal direction provided for M446-M448 and the existing NODAL OS architecture record. The referenced file `NODAL---_Plan_Agregados_RPA_OpenSource_UIVision_TagUI_OpenRPA.docx` was not present in the repository worktree, so it is treated as non-versioned product input summarized by the operator.

Internal sources considered:

- Documento Maestro Roadmap/Arquitectura/Workflow, represented by current NODAL OS roadmap and architecture docs.
- Documento Maestro Negocio/GTM, represented by the NODAL OS product direction in this milestone.
- Direccion Visual/UX, represented as Mission Control-first UX guidance.
- Plan Agregados RPA Open Source UI.Vision/TagUI/OpenRPA, represented by the summarized product decision: references only, no copied code, no dependencies, no classic RPA identity.

## External References As Inspiration Only

- UI.Vision RPA.
- TagUI.
- OpenRPA/OpenIAP.

These references are not dependencies, are not imported, and are not a code base for NODAL OS.

## UI.Vision Patterns

Accepted as inspiration:

- Conceptual recorder as a future log-only capture source.
- Selenium-like command taxonomy as conceptual vocabulary.
- Visual checkpoints as future fallback or evidence, not the primary strategy.
- CSV/data-driven concept for future templates.
- CLI/import/export concept for future artifact exchange.
- Local-first automation posture.

Rejected:

- Copied code.
- AGPL/commercial dependency.
- Extension UX as product identity.
- Isolated macro identity.
- OCR/visual matching as primary control mechanism.
- Execution without Mission Control, Approval, Evidence, and Timeline.

## TagUI Patterns

Accepted as inspiration:

- Readable DSL.
- Text workflows.
- Simple commands as language-design input.
- Conceptual templates.

Rejected:

- TagUI runtime dependency.
- DSL as runtime.
- Discontinued or weakly maintained runtime as foundation.
- Early productive desktop/OCR automation.

## OpenRPA/OpenIAP Patterns

Accepted as inspiration:

- Work queues as a future governed local-first concept.
- Lifecycle taxonomy.
- Attended/unattended as future classification, not permission.
- Governed triggers.
- Central orchestration as conceptual reference.
- Robot lifecycle as future operational taxonomy.

Rejected:

- Heavy classic RPA designer.
- Visual workflow identity.
- Enterprise RPA identity.
- Windows/RPA dependency.
- Mandatory cloud orchestrator.
- Broad unattended automation early.

## Legal And License Risks

NODAL OS must not copy source code, data files, tests, assets, command implementations, or licensed runtime components from UI.Vision, TagUI, OpenRPA, or OpenIAP. Patterns can be described at architectural level only. No AGPL or commercial dependency is included in this milestone.

## Product Risks

The main risk is turning NODAL OS into classic RPA: macro-first, designer-first, unattended-first, and execution-first. This conflicts with the intended Mission Control model. The accepted scope keeps automation subordinate to policy, approval, evidence, and timeline.

## Scheduler / Unattended / Recorder Risks

Scheduler, unattended execution, recorder, replay, browser automation, DSL parser, queue, and triggers remain future-only. Implementing them now would bypass the staged safety work already established by Agent Operations, Orchestration Facade, and Scheduled Read-Only contracts.

## Covered By Current Architecture

Already covered:

- AgentOperations contracts/core/adapters boundaries.
- Orchestration command contracts.
- Orchestration in-process facade with no execution.
- Scheduled Read-Only contracts.
- Evidence bridge.
- Common redaction.
- Dependency-direction tests.

## Missing Future Designs

Future designs needed before implementation:

- Automation Event Contract.
- Automation Evidence Schema.
- Selector Safety Policy.
- Human Handoff Contract.
- Recipe Risk Classifier.
- Recipe DSL decision.
- Recorder log-only design.
- Work Queue local-first design.
- Trigger Policy.

## Recommendation

Proceed with an Automation Layer ADR only. Do not implement recorder, replay, browser automation, DSL parser, queue, scheduler, UI, or execution in M446-M448.
