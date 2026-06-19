# Orchestration In-Process Facade V1 M427

Project: NODAL OS

Milestone: M425-M427

Base commit: 69f0a8a

## What Was Implemented

- `NodalOsOrchestrationInProcessFacade` — in-process facade coordinating validators and gates.
- `Dispatch(NodalOsOrchestrationCommandEnvelope)` → `NodalOsOrchestrationCommandResult`.
- Validation via `NodalOsOrchestrationCommandValidator`.
- Evidence ref validation via `NodalOsEvidenceRefBridge.ValidateBridgeRef`.
- Common redaction via `NodalOsRedactionService.RedactValue`.
- No-execution invariant.
- Policy, approval, evidence, and verification gates.
- Full test suite (28 tests) for facade behavior.
- M425 audit report.
- M427 report and artifact.

## What Was Not Implemented

- No API, HTTP, gRPC, controller, endpoint.
- No scheduler, runtime engine, state machine, worker runtime.
- No recipe execution, skill execution, step execution.
- No browser actions, desktop actions.
- No UI.
- No persistence DB.
- No `BrowserExecutor.Cdp.CrossAppDomain` dependency.
- No `RunningFuture` or `PausedFuture` as active states.
- No new contracts (existing `NodalOsOrchestrationCommandResult` suffices).

## Dispatch Semantics

1. Validate command shape and invariants via validator.
2. Validate evidence refs via bridge.
3. If invalid: return `Accepted=false`, `State=Blocked`, `Executed=false`.
4. If valid: return `Accepted=true`, state per command kind, `Executed=false`.
5. Sanitize errors/warnings via common redaction.
6. Detect sensitive content in summary, policy errors, evidence refs.
7. High/Critical risk without approval is rejected.
8. Invalid evidence refs are rejected.

## No-Execution Invariant

- `Executed=false` always in every result.
- `RuntimeExecutionDeferred=true` always in every result.
- `RuntimeExecutionAllowed=false` always (validated at dispatch).
- No command triggers external, browser, desktop, or worker action.
- The facade is a pure coordination layer with zero side effects.

## Command Handling

All 17 `NodalOsOrchestrationCommandKind` values are dispatched:

| Kind | State | Notes |
|---|---|---|
| CreateMission | Prepared | Contract preparation only |
| CreateTask | Prepared | Contract preparation only |
| PrepareRun | Prepared | Does not start run |
| ValidateRecipeManifest | Completed | Does not execute recipe |
| ValidateSkill | Completed | Does not execute skill |
| RegisterPackageSnapshot | Completed | Contract handling only |
| QuerySkillRegistry | Completed | Catalog lookup, no runtime permission |
| PrepareWorkerRequest | Prepared | No worker dispatch |
| GetRunStatus | Completed | Contract state only |
| PauseRun | Completed | Contract-only, no runtime transition |
| ResumeRun | Completed | Contract-only, no runtime transition |
| CancelRun | Completed | Contract-only, no runtime transition |
| RequestHumanDecision | AwaitingApproval | High/Critical require approval |
| AttachEvidence | Completed | Evidence validated, no authorization |
| GetRunReport | Completed | Contract handling only |
| GetProgressReport | Completed | Contract handling only |
| EvaluateVerificationBeforeDone | Completed | No automatic closure |

## Relationship With Command Contracts

The facade consumes the existing `NodalOsOrchestrationCommandEnvelope` and produces `NodalOsOrchestrationCommandResult`. The facade adds coordination sequencing on top of the existing validator, without replacing or bypassing validation semantics.

## Relationship With Gates

- **Policy gate**: Lives in the validator's `ValidateCommand` before any preparation state. The facade enforces that policy is always evaluated.
- **Approval gate**: Lives in the validator's High/Critical risk check. The facade rejects commands that skip required human approval.
- **Evidence gate**: Lives over `NodalOsEvidenceRefBridge.ValidateBridgeRef`. The facade validates every evidence ref before accepting the command.
- **Verification-before-done gate**: `NodalOsVerificationBeforeDoneGate` remains the canonical done-success source. The facade does not duplicate or replace it.

## Limitations

- No runtime execution in V1.
- No dry-run preparation in V1.
- No worker dispatch in V1.
- No recipe/skill/step execution in V1.
- No persistence between dispatches (in-memory coordination only).
- No rollback or compensation logic.
- No event subscription or notification.

## Next Steps

Recommended next milestone:
`M428-M430 Agent Operations Adapter Project Skeleton` or
`M428-M430 Scheduled Read-Only Runs Decision Record`
