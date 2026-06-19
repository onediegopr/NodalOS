# NODAL OS Scheduled Read-Only Run Contracts V1 M436

## Summary

M434-M436 created Scheduled Read-Only Run Contracts V1 for NODAL OS. The milestone adds schedule, run request, dry-run preview, validation, serialization, and fixtures while keeping scheduler/runtime implementation deferred.

## What Was Created

- `NodalOsScheduledReadOnlySchedule`.
- `NodalOsScheduledReadOnlyRunRequest`.
- `NodalOsScheduledReadOnlyPreview`.
- `NodalOsScheduledReadOnlyScheduleStatus`.
- `NodalOsScheduledReadOnlyFrequencyKind`.
- `NodalOsScheduledReadOnlyValidationResult`.
- `NodalOsScheduledReadOnlyRunValidator`.
- `NodalOsScheduledReadOnlyRunJsonSerializer`.
- `NodalOsScheduledReadOnlyRunFixtures`.

## Schedule Contract

A schedule contract records future read-only intent and governance metadata: owner, status, frequency kind, references to mission/task/recipe/skill/package, allowed targets, evidence requirements, evidence refs, summary, and creation time.

It is not a scheduler. It does not run in the background, create timers, trigger jobs, open browsers, call workers, or mutate state.

## Lifecycle

The lifecycle states are contract states only:

- Draft.
- PolicyReviewRequired.
- EvidenceRequirementsReviewRequired.
- ApprovalRequiredIfSensitive.
- DryRunPreviewPrepared.
- AwaitingManualTrigger.
- ScheduledReadOnlyFuture.
- ReportProduced.
- Blocked.
- Cancelled.

`ScheduledReadOnlyFuture` is a future metadata state, not active runtime.

## Read-Only Invariant

Validation requires:

- `ReadOnly=true`.
- `RuntimeExecutionAllowed=false`.
- `RuntimeExecutionDeferred=true`.
- `RequiresGlobalPolicyEvaluation=true`.
- `RequiresEvidenceRedaction=true` on schedules.
- `ManualTriggerRequired=true` on V1 run requests.
- `DryRunOnly=true` and `Executed=false` on previews.

Valid schedule policy does not mean executable. Ready/healthy/future status does not mean executable.

## Manual Trigger

V1 run requests require manual trigger. This avoids silently converting a schedule contract into background execution. Future scheduling must be approved by a separate milestone and security audit.

## Dry-Run Preview

Preview is planning metadata only. It may list planned read-only operations such as validate manifest, query registry, prepare dry-run, read observation, collect evidence, and produce RunReport/ProgressReport. It cannot execute anything.

## Forbidden Actions

Preview validation rejects planned operations containing:

- click;
- type;
- submit;
- upload;
- download;
- login;
- captcha;
- 2FA;
- payment/pay;
- send;
- delete;
- sign;
- publish;
- mutate;
- write;
- file system mutation.

## Evidence And Redaction

Evidence refs validate through `NodalOsEvidenceRefBridge`. Common redaction checks schedules, allowed targets, evidence requirements, summaries, warnings, and evidence refs. Raw secrets, cookies, headers, tokens, passwords, private bodies, or credential material are rejected or redacted by the serializer.

## Relationship With Orchestration Facade

The contracts can be used by future orchestration layers, but the current in-process facade remains no-execution. No schedule contract can grant runtime permission.

## Relationship With RunReport And ProgressReport

Future scheduled read-only outputs are restricted to reports, evidence, status, and verification. RunReport and ProgressReport remain reporting outputs, not action authority.

## Limitations

- No scheduler.
- No timer.
- No background worker.
- No API/HTTP/gRPC.
- No UI.
- No worker runtime.
- No browser or desktop actions.
- No recipe, skill, or step execution.
- No persistence DB.

## Next Steps

Recommended next milestone: `M437-M439 Claude Pre-Runtime Agent Operations Audit` or `M437-M439 Browser Adapter Extraction Phase 1`.
