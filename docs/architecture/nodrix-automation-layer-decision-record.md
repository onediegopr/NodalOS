# NODRIX Automation Layer Decision Record

## Context

NODAL OS / NODRIX has a mature Agent Operations foundation: contracts, core services, browser adapter skeleton, orchestration command contracts, a no-execution in-process facade, and scheduled read-only contracts. The next architecture question is how to absorb useful RPA patterns without becoming classic RPA.

## Problem

Automation can become unsafe when it is recorder-first, designer-first, unattended-first, or execution-first. NODRIX needs governed automation that strengthens Mission Control, approval, evidence, timeline, human handoff, and auditable recipes without importing old RPA product identity.

## Decision

NODRIX Automation Layer is future governed automation, not classic RPA. UI.Vision, TagUI, and OpenRPA/OpenIAP are references only. This milestone creates no runtime implementation.

## Non-goals

- No recorder implementation.
- No replay implementation.
- No browser automation implementation.
- No workflow designer.
- No DSL parser.
- No queue.
- No trigger.
- No scheduler.
- No timer.
- No background worker.
- No API, HTTP, or gRPC.
- No UI.
- No worker runtime.
- No recipe, skill, or step execution.
- No persistence DB.
- No copied code from external RPA projects.
- No dependency on external RPA projects.

## References Considered

- UI.Vision RPA.
- TagUI.
- OpenRPA/OpenIAP.

They inform patterns only. They are not source material for code.

## Legal / Licensing Decision

No code, assets, tests, runtime components, or licensed implementation details are copied from UI.Vision, TagUI, OpenRPA, or OpenIAP. No AGPL or commercial RPA dependency is introduced.

## Product Identity Decision

NODRIX is AI Mission Control local-first. It is not a classic RPA suite, macro recorder, heavy workflow designer, or unattended robot platform.

## Mission Control-first Rule

All future automation enters through Mission Control. The user must understand what is happening, what is blocked, what evidence exists, and what options are available.

## Approval-first Rule

Submit, publish, purchase, delete, payment, credential, mutation, identity-sensitive, and external side-effect actions always require explicit approval and future policy support.

## Evidence-first Rule

Automation output must flow into Evidence and Timeline. Evidence has no authority to execute. Evidence records what happened, what was observed, and what remains blocked.

## Timeline-first Rule

Every future automation step must be explainable as timeline events. Timeline is the operator-facing source of truth for observation, waiting, approval, handoff, and blocked states.

## Local-first Rule

Automation Layer is local-first by default. Cloud orchestration and broad unattended execution are out of scope unless separately approved by a future security review.

## Automation Layer Components

Planned future components:

- NODRIX Recorder.
- NODRIX Recipe DSL.
- Automation Event Contract.
- Automation Evidence Schema.
- Selector Safety Policy.
- Human Handoff Contract.
- Recipe Risk Classifier.
- Work Queue.
- Trigger Policy.
- Templates/import/export.

## Automation Event Contract Future

Future events must describe intent, observation, evidence, policy status, approval status, and handoff state. They must not imply execution permission.

## Automation Evidence Schema Future

Future automation evidence must preserve no-authority semantics and integrate with common redaction, EvidenceRef bridge, and Timeline.

## Selector Safety Policy Future

Selector strategy is semantic/DOM/CDP first. Visual/OCR is only fallback or evidence. Visual matching must not become the primary authority for mutation.

## Human Handoff Contract Future

Human handoff must be explicit and non-generic. It must explain the exact blocker, why automation stopped, what the user can do, and which options remain safe.

## Recipe Risk Classifier Future

Future recipes must classify risk before execution. Read-only, data-entry, file-transfer, navigation, credential, payment, deletion, publish, and external mutation intents require distinct policy handling.

## Recipe DSL Future

The DSL is representation, not runtime. A DSL file or imported recipe never means direct execution.

## Recorder Future

Recorder v0 must be log-only first. It may capture observations and candidate actions for review, but it must not replay or authorize actions.

## Work Queue Future

Work queue is future-only, local-first, policy-bound, and evidence-bound. Queue visibility never grants runtime permission.

## Trigger Policy Future

Triggers are future-only. Trigger configured does not mean execution allowed. Scheduled or event-triggered automation must pass policy, approval, evidence, and timeline gates.

## Templates / Import / Export Future

Templates and imports are drafts. Recipe import never means direct execution. Imported artifacts must be validated, risk-classified, redacted, and approved before any future execution path.

## What Is Forbidden Now

Forbidden in this milestone:

- Recorder.
- Replay.
- Browser automation.
- Workflow designer.
- DSL parser.
- Queue.
- Trigger.
- Scheduler.
- Timer.
- Background worker.
- API.
- UI.
- Worker runtime.
- Execution.
- External RPA dependencies.
- Copied RPA code.

## Implementation Phases

Phase 0 - ADR and guardrails.

Phase 1 - Automation Event, Evidence, Handoff, and Risk contracts.

Phase 2 - Recipe DSL decision and JSON canonical model.

Phase 3 - Recorder log-only.

Phase 4 - Replay dry-run.

Phase 5 - Work Queue local.

Phase 6 - Read-only browser automation.

Phase 7 - Mutable automation only after dedicated security audit.

## Acceptance Criteria

- UI.Vision, TagUI, and OpenRPA/OpenIAP remain references only.
- No code copied.
- No dependency introduced.
- No runtime implementation.
- Mission Control-first, approval-first, evidence-first, timeline-first, and local-first rules are explicit.
- Expert Advisor audits recipes but does not execute.
- Advisor remains observer, not executor.
- Browser/RPA productive implementation remains deferred.
