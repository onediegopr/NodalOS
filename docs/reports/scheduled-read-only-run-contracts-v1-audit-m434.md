# NODAL OS Scheduled Read-Only Run Contracts V1 Audit M434

## Scope

M434 audited the scheduled read-only ADR, orchestration command contracts, orchestration facade, EvidenceRef bridge, common redaction, RunReport/ProgressReport contracts, and Agent Operations project boundaries before adding V1 schedule contracts.

This audit is contract-only. It does not implement a scheduler, timer, background worker, hosted service, queue, API, UI, worker runtime, browser action, desktop action, recipe execution, skill execution, step execution, persistence DB, cloud runtime, or notification integration.

## Existing Concepts

- `NodalOsOrchestrationCommandEnvelope` and `NodalOsOrchestrationCommandResult` already enforce no-execution semantics for orchestration command handling.
- `NodalOsOrchestrationInProcessFacade` dispatches commands in-process without executing runtime actions.
- `NodalOsEvidenceBridgeRef` and `NodalOsEvidenceRefBridge` provide no-authority evidence validation.
- `NodalOsRedactionService` detects and redacts secret-like content.
- `NexaRunReport` and `NodalOsAgentProgressReport` already exist as reporting outputs.
- `OneBrain.AgentOperations.Adapters.Browser` exists as skeleton only; real browser runtime stays in `OneBrain.BrowserExecutor.Cdp`.

## Reusable Contracts

- Evidence refs: `NodalOsEvidenceBridgeRef`.
- Evidence validation: `NodalOsEvidenceRefBridge.ValidateBridgeRef`.
- Redaction: `NodalOsRedactionService`.
- Orchestration no-execution concepts: runtime allowed false, runtime deferred true, global policy required.
- Reporting concepts: RunReport, ProgressReport, warnings, verification summaries, evidence refs.

## Added By M434-M436

- `NodalOsScheduledReadOnlySchedule`.
- `NodalOsScheduledReadOnlyRunRequest`.
- `NodalOsScheduledReadOnlyPreview`.
- `NodalOsScheduledReadOnlyScheduleStatus`.
- `NodalOsScheduledReadOnlyFrequencyKind`.
- `NodalOsScheduledReadOnlyValidationResult`.
- Validator, JSON serializer, and fixtures.

## Schedule Contract vs Scheduler

A schedule contract describes future intent, policy/evidence requirements, read-only classification, and lifecycle metadata. A scheduler runs timed/background work. M434-M436 creates only contracts and validators. It does not create timers, cron, hosted services, background workers, queue processing, persistence, retries, or concurrent execution.

## Scheduled Read-Only Run vs Runtime Execution

A scheduled read-only run request is a future manual-trigger contract envelope. It cannot start a run, call a worker, open a browser, mutate state, or execute a recipe/skill/step. Readiness or validity means policy metadata is acceptable, not executable.

## Read-Only Invariant

All schedule and request contracts must keep:

- `ReadOnly=true`.
- `RuntimeExecutionAllowed=false`.
- `RuntimeExecutionDeferred=true`.
- `RequiresGlobalPolicyEvaluation=true`.
- `RequiresEvidenceRedaction=true` for schedules.
- `ManualTriggerRequired=true` for V1 run requests.
- Preview `DryRunOnly=true`.
- Preview `Executed=false`.

## Risks

- Treating non-manual frequency enum values as real scheduler behavior.
- Treating `ScheduledReadOnlyFuture` as active runtime.
- Allowing planned preview operations to include click/type/submit/upload/download/login/captcha/2FA/payment/send/delete/sign/publish/mutation verbs.
- Persisting raw secrets in allowed targets, evidence requirements, summaries, warnings, or evidence refs.
- Treating valid schedule policy as runtime permission.

## Not Implemented

- Scheduler.
- Timer.
- Background worker.
- Hosted service.
- Cron.
- Automation queue.
- HTTP/gRPC API.
- UI.
- Worker runtime.
- Browser/desktop actions.
- Recipe/skill/step execution.
- Persistence DB.
- Notifications.

## Decision

Proceed with Scheduled Read-Only Run Contracts V1. Runtime implementation remains deferred.
