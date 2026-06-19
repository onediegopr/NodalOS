# NODAL OS Scheduled Read-Only Runs Decision Record

## Context

NODAL OS now has Agent Operations contracts/core, an Agent Operations Browser Adapter skeleton, Orchestration Command Contracts V1, and an Orchestration In-Process Facade V1. All current orchestration surfaces are contract-only and no-execution. There is no scheduler, no background worker, no HTTP/gRPC API, no UI, no worker runtime, and no recipe/skill/step execution.

## Problem

Future scheduled read-only runs are useful for observation, evidence collection, and reporting, but scheduling can easily be misread as permission to execute. The architecture needs a clear decision before any schedule contracts or runtime work are introduced.

## Decision

Scheduled read-only runs are approved as a future-only design direction. No scheduler is implemented now. Any future scheduled read-only run must remain policy-governed, evidence-backed, redacted, report-only, internal-only, and unable to grant runtime permission.

## Scope

This ADR defines meaning, boundaries, gates, prohibited actions, relationships, lifecycle, and future implementation phases for scheduled read-only runs.

## Non-Goals

- No scheduler.
- No timer.
- No cron.
- No background worker.
- No HTTP/gRPC API.
- No UI.
- No worker runtime.
- No browser action implementation.
- No desktop action implementation.
- No recipe execution.
- No skill execution.
- No step execution.
- No persistence DB.
- No notifications.
- No cloud runtime.

## Definition Of Read-Only

Future read-only scheduled runs may:

- validate manifests;
- query registry;
- prepare dry-run;
- read or extract observation;
- collect evidence;
- produce RunReport and ProgressReport outputs;
- request human decision;
- attach evidence;
- evaluate verification.

Future read-only scheduled runs must not:

- click;
- type;
- submit;
- upload;
- download;
- login;
- solve or bypass captcha;
- complete 2FA;
- pay;
- send;
- delete;
- sign;
- publish;
- mutate external systems;
- mutate the local file system;
- perform network mutation beyond a future explicitly approved read/navigation boundary.

Read-only does not mean "low risk action." It means no action that changes local, browser, remote, account, workflow, financial, legal, or persisted state.

## Future Schedule Model

A future schedule contract may include schedule id, owner, mission or task reference, recipe or skill reference, read-only classification, frequency bounds, allowed observation targets, evidence requirements, policy requirements, redaction requirements, and reporting destination. It must not include raw secrets, cookies, headers, tokens, private bodies, passwords, or credential material.

Frequency must be bounded by policy. The future default should be conservative, opt-in, internal-only, and visible in audit/reporting surfaces.

## Future Run Lifecycle

Future lifecycle states should be contract-first:

- DraftSchedule.
- PolicyReviewRequired.
- EvidenceRequirementsReviewRequired.
- ApprovalRequiredIfSensitive.
- DryRunPreviewPrepared.
- AwaitingManualTrigger.
- ScheduledReadOnlyFuture.
- ReportProduced.
- Blocked.
- Cancelled.

Any state that suggests running remains future-only until a dedicated runtime security audit approves implementation.

## Policy Gates

Scheduled read-only runs cannot bypass policy. Policy must decide whether a schedule exists, whether a target is in scope, whether a frequency is acceptable, whether read-only classification is valid, and whether evidence requirements are sufficient.

Registry Visible, Worker Healthy, Skill Approved, Recipe Approved, CanPassCatalogPolicy, and CanPassBoundaryPolicy are not runtime permission.

## Evidence And Redaction Gates

Evidence must preserve no-authority semantics. Evidence can document, support, or verify; it cannot authorize actions. Evidence refs must use the EvidenceRef bridge model where applicable.

All future schedule metadata, run summaries, errors, warnings, evidence requirements, RunReport, and ProgressReport content must pass common redaction. Raw secrets, cookies, headers, tokens, private bodies, passwords, and credential material must never be persisted.

## Human Approval Rules

Scheduled read-only runs should not require approval merely because they are scheduled if the policy classifies them as internal, read-only, low risk, and bounded. Human approval is required whenever future policy marks the target, data, frequency, identity context, evidence sensitivity, or report destination as sensitive. Human approval cannot be cached into blanket runtime permission.

## Relationship With Orchestration Facade

The Orchestration In-Process Facade remains no-execution. Future schedule contracts may prepare orchestration command envelopes, but the facade must keep `Executed=false`, `RuntimeExecutionAllowed=false`, and `RuntimeExecutionDeferred=true` until a separate gated runtime implementation exists.

## Relationship With Browser Adapter

`OneBrain.AgentOperations.Adapters.Browser` is currently marker-only. Scheduled read-only runs may later depend on a browser adapter for approved read/navigation observation, but this ADR does not move or implement browser runtime. BrowserExecutor.Cdp remains the real browser runtime host.

## Relationship With Worker Boundary

Worker Boundary and Worker Health describe future worker capabilities and status. They do not authorize scheduled execution. Future scheduled read-only runs may reference worker boundary metadata only after policy confirms internal-only, read-only, and evidence requirements.

## Relationship With Recipe, Skill, And Step Library

Recipe Approved, Skill Approved, Step catalog availability, and Registry Visible are descriptive governance states. Future scheduled read-only runs must require explicit recipe/skill/read-only classification. Click, Type, Submit, Upload, Download, Login, Captcha, 2FA, payment, send, delete, sign, and publish capabilities are forbidden in scheduled read-only scope.

## Relationship With RunReport And ProgressReport

Outputs are limited to RunReport, ProgressReport, evidence refs, failure kinds, warnings, and verification summaries. Reports must state that scheduled read-only handling is observation/reporting only and does not imply execution authority.

## Safety Restrictions

- Scheduled read-only cannot grant runtime permission.
- Scheduled read-only cannot bypass policy.
- Scheduled read-only cannot bypass evidence redaction.
- Scheduled read-only cannot bypass human approval when policy requires it.
- Scheduled read-only cannot treat Worker Healthy, Registry Visible, Skill Approved, or Recipe Approved as permission.
- Scheduled read-only outputs are reports and evidence only.
- Scheduled read-only must never persist raw secrets.

## Implementation Phases

Phase 0: ADR.

Phase 1: schedule contracts only.

Phase 2: dry-run schedule preview.

Phase 3: manual-trigger read-only dry-run.

Phase 4: supervised scheduled read-only internal run.

Phase 5: UI display and approval.

Phase 6: broader scheduler only after security audit.

## Acceptance Criteria

- No scheduler implementation exists in this milestone.
- No timer, cron, or background worker exists in this milestone.
- Read-only allowed and forbidden actions are explicit.
- Policy, evidence/redaction, and approval gates remain authoritative.
- Orchestration facade remains no-execution.
- Browser adapter remains boundary-only unless a later milestone moves adapter code.
- RunReport and ProgressReport are the only future scheduled run outputs.
- NODAL OS naming is used for project-forward language.
