# Verification Before Done Gate M364

## Problem

Mission, task, and run completion need a formal gate so that "done" cannot be declared by UI, runner, service worker, or recipe metadata alone.

## Implemented

- `NodalOsVerificationBeforeDoneResult`
- `NodalOsVerificationBeforeDoneSubjectKind`
- `NodalOsVerificationBeforeDoneOptions`
- `NodalOsVerificationBeforeDoneGate`

## Agent Workboard Relationship

The gate evaluates `NexaAgentTask` and `NexaMission` using the Agent Workboard model. It preserves `NexaEvidenceRef` values and lists verification labels used during the done decision.

## Run Report V1 Relationship

The gate evaluates `NexaRunReport` completion semantics:

- `Completed` requires completed or reason-skipped steps, no blocking/critical failures, no blocked policy decisions, no pending approvals, `CompletedAt`, and evidence or final summary.
- `CompletedWithWarnings` is allowed only for non-critical failures or explicit warning notes.
- `Failed`, `Blocked`, `Cancelled`, `Running`, `Paused`, `AwaitingApproval`, and `Planned` are not done-success.

## Task Completion Rules

- Task identity and human owner are required.
- Blocking or critical blockers prevent done.
- Pending or failed required verification prevents done.
- Skipped required verification requires a reason.
- Evidence refs or explicit completion reason are required.

## Mission Completion Rules

- Mission identity and human owner are required.
- Empty mission without evidence cannot be marked done.
- Every mission task must pass the task gate.
- Task errors are aggregated with task IDs.

## Non-Goals

- No UI.
- No runtime action behavior changes.
- No recipe execution.
- No orchestration API.
- No scheduled runs.
- No persistence DB.

## Next Step

M365-M367 should add the Blocker + Progress Reporting Contract so gate failures can become structured workboard progress and blocker reports.
