# NODAL OS Scheduled Read-Only Integration No-Divergence Audit M440

## Scope

M440 reviewed the Claude pre-runtime audit findings for Scheduled Read-Only contracts, orchestration facade integration, evidence/redaction, and Agent Operations dependency direction. This milestone addresses MEDIUM-1 and MEDIUM-3 only.

No scheduler, timer, background worker, API, UI, worker runtime, browser action, desktop action, recipe execution, skill execution, step execution, persistence DB, namespace migration, broad rename, or Chrome/CDP move is implemented.

## Claude Findings Reviewed

- MEDIUM-1: forbidden-action screening was applied to preview `PlannedReadOnlyOperations`, but not to schedule `AllowedTargets` or `Summary`.
- MEDIUM-2: namespace shim debt remains. This is intentionally deferred to M443-M445 or a later scoped namespace migration milestone.
- MEDIUM-3: cross-layer no-divergence and dependency-direction tests were missing.

## ContainsForbiddenAction Location

`ContainsForbiddenAction` lives in `src/OneBrain.AgentOperations.Core/NodalOsScheduledReadOnlyRunServices.cs` inside `NodalOsScheduledReadOnlyRunValidator`.

Before M440-M442, it was used by `ValidatePreview` for `PlannedReadOnlyOperations` only.

## InvalidMutableActionSchedule Location

`InvalidMutableActionSchedule` lives in `NodalOsScheduledReadOnlyRunFixtures` in `src/OneBrain.AgentOperations.Core/NodalOsScheduledReadOnlyRunServices.cs`.

It includes mutable intent in summary and allowed target metadata. It was not previously covered by tests.

## Schedule Validator Before Fix

`ValidateSchedule` enforced read-only flags, runtime deferred, policy required, evidence redaction required, secret-like content, lifecycle policy states, and evidence bridge validation.

It did not reject forbidden action markers in `AllowedTargets` or `Summary`.

## Preview Validator Before Fix

`ValidatePreview` already rejected forbidden action markers in `PlannedReadOnlyOperations`, required `DryRunOnly=true`, required `Executed=false`, required runtime deferred, and validated evidence refs.

## Fix Strategy

- Apply forbidden-action screening to schedule `AllowedTargets`.
- Apply forbidden-action screening to schedule `Summary`.
- Keep error messages generic and do not echo raw target/summary content.
- Preserve existing common redaction and EvidenceRef bridge validation.
- Keep preview semantics unchanged.

## Cross-Layer Test Strategy

- Validate schedule, request, and preview together.
- Compose a compatible orchestration command from scheduled read-only metadata.
- Dispatch through the in-process facade.
- Assert `Executed=false`, runtime deferred, runtime allowed false on command, global policy required, manual trigger required, dry-run only, and read-only preserved.
- Verify evidence refs remain no-authority and bridge-valid.
- Verify RunReport/ProgressReport composition remains reporting metadata only.

## Dependency-Direction Test Strategy

- Assert AgentOperations.Contracts project does not reference BrowserExecutor.Cdp.
- Assert AgentOperations.Core project does not reference BrowserExecutor.Cdp.
- Assert AgentOperations.Adapters.Browser project does not reference BrowserExecutor.Cdp.
- Assert AgentOperations projects do not reference Chrome/CDP automation packages.
- Confirm BrowserExecutor.Cdp remains temporary host.

## Not Touched

- Namespace migration.
- Browser/CDP runtime.
- Scheduler/timers/background workers.
- HTTP/gRPC/API.
- UI.
- Worker runtime.
- Execution engines.
- Persistence DB.

## Decision

Proceed with a surgical defensive validator fix and no-divergence tests.
