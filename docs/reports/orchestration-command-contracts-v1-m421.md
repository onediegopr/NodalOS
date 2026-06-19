# Orchestration Command Contracts V1 M421

Project: NODAL OS

Milestone: M419-M421

Base commit: 266dad9

## Problem Solved

M416-M418 defined an Orchestration API ADR with conceptual commands and states, but there were no typed internal command contracts. M419-M421 adds V1 command contracts that can be validated and serialized without implementing an API, engine, scheduler, worker runtime, UI, or execution.

## Command Contracts V1

The V1 command envelope is `NodalOsOrchestrationCommandEnvelope`. It captures command identity, command kind, optional mission/task/run/recipe/package/skill/worker IDs, risk level, runtime-deferred flags, policy requirement, human approval requirement, evidence refs, evidence requirements, summary, and creation timestamp.

The result model is `NodalOsOrchestrationCommandResult`. It captures command acceptance and contract-handling state while explicitly requiring `Executed=false` and `RuntimeExecutionDeferred=true` in V1.

## Command Kinds

V1 defines internal command kinds for mission/task creation, run preparation, manifest/skill validation, package snapshot registration, skill registry query, worker request preparation, run status, pause/resume/cancel, human decision request, evidence attachment, report retrieval, and verification-before-done evaluation.

These are internal command contracts only. They are not HTTP endpoints and do not execute actions.

## State Model

The state model mirrors the ADR: `Draft`, `Prepared`, `AwaitingPolicy`, `AwaitingApproval`, `ReadyForDryRun`, `DryRunPrepared`, `RunningFuture`, `PausedFuture`, `Blocked`, `Completed`, `Failed`, and `Cancelled`.

`RunningFuture` and `PausedFuture` remain reserved conceptual states. They do not represent runtime behavior in V1.

## Runtime Execution Deferred

Command validation enforces:

- `RuntimeExecutionAllowed=false`.
- `RuntimeExecutionDeferred=true`.
- `RequiresGlobalPolicyEvaluation=true`.
- command results must have `Executed=false`.

## Policy, Approval, And Evidence Requirements

- Every command requires global policy evaluation.
- High and Critical risk commands require human approval.
- Evidence attachment requires `NodalOsEvidenceBridgeRef` entries.
- Evidence refs are validated through `NodalOsEvidenceRefBridge`.
- Common redaction scans command text, evidence requirements, result errors, warnings, and evidence metadata.

## Result Semantics

- `Accepted=true` means the command contract was accepted for metadata handling only.
- `Accepted=true` does not mean execution.
- `Completed` means contract handling completed only.
- `Completed` does not mean runtime execution completed.
- Failure kinds are reporting metadata only.

## Relationship With M416-M418 ADR

This milestone implements Phase 1 from the ADR: internal command contracts only. It does not implement Phase 2 facade, Phase 3 dry-run, or any future runtime phase.

## Relationship With Agent Operations

The contracts reuse Agent Operations evidence bridge, redaction, failure taxonomy, mission/task identifiers, run/progress report concepts, package/skill registry concepts, worker boundary concepts, and verification-before-done semantics. The command layer coordinates these references without replacing existing validators or creating runtime authority.

## What Is Not Implemented

- No API.
- No HTTP.
- No gRPC.
- No controller.
- No endpoint.
- No scheduler.
- No orchestration engine.
- No worker runtime.
- No recipe execution.
- No skill execution.
- No step execution.
- No UI.
- No persistence DB.

## Next Steps

Recommended next milestone: `M422-M424 Orchestration In-Process Facade Decision or M422-M424 Agent Operations Adapter Project Skeleton`.
