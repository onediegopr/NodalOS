# NODAL OS Completion Gate Canonicalization Audit M377

Milestone: M377-M379
Base commit: 1612913

## Scope

This audit compares completion and ready-to-close semantics across Agent Operations before refactoring. The goal is to make `NodalOsVerificationBeforeDoneGate` the canonical source for done-success semantics without introducing UI, runtime execution, orchestration, recipe execution, namespace moves, or policy changes.

## Decision Points Found

| Decision point | Path | Existing role | Risk |
| --- | --- | --- | --- |
| `ValidateTaskCanComplete` | `NodalOsAgentWorkboardServices.cs` | Task completion validation. | Duplicated task completion rules. |
| `EvaluateTask` | `NodalOsVerificationBeforeDoneGate.cs` | Canonical task gate candidate. | Should own task done-success semantics. |
| `EvaluateMission` | `NodalOsVerificationBeforeDoneGate.cs` | Mission gate candidate. | Aggregates task gate results. |
| `EvaluateRunReport` | `NodalOsVerificationBeforeDoneGate.cs` | Run done-success gate candidate. | Should own completed run semantics. |
| `ValidateReadyToClose` | `NodalOsAgentProgressReportingServices.cs` | Progress report ready-to-close validation. | Must consume gate summaries rather than invent independent rules. |
| `NodalOsRunReportBuilder.Validate` | `NodalOsRunReportingServices.cs` | Run report validity validation. | Must align completed/completed-with-warnings with gate while still allowing failed/blocked reports to exist. |

## Current Rules By Module

### WorkboardValidator

- Requires TaskId.
- Requires MissionId.
- Requires Title.
- Requires HumanOwner.
- Blocks Blocking/Critical blockers.
- Blocks Pending/Failed required verification.
- Requires skipped required verification reason.
- Requires evidence or explicit completion reason.

### VerificationBeforeDoneGate

- Requires TaskId.
- Requires MissionId.
- Requires HumanOwner.
- Warns when task/mission is evaluated prospectively before completed status.
- Blocks Blocking/Critical blockers.
- Blocks Pending/Failed required verification.
- Requires skipped required verification reason.
- Requires evidence or explicit completion reason.
- Aggregates mission task errors.
- Blocks failed/blocked/cancelled/running/paused/planned runs from done-success.
- Requires completed runs to have CompletedAt.
- Requires completed runs to have completed or skipped-with-reason steps.
- Blocks blocking/critical run failures.
- Blocks pending/denied approvals.
- Allows CompletedWithWarnings only with controlled non-critical failures, notes, or summary.

### ProgressReportValidator

- Requires report identity and summary.
- CompletionCandidate requires at least one verification summary.
- ReadyToClose requires status ReadyToClose.
- ReadyToClose blocks Blocking/Critical blockers.
- ReadyToClose blocks any verification summary where `CanMarkDone=false`.
- ReadyToClose requires at least one positive verification summary.
- ReadyToClose blocks blocking human decision requests.
- ReadyToClose requires evidence, detail, or summary.

### RunReportBuilder / Validator

- Validates reportability: RunId, Goal, terminal CompletedAt, failed run failure report, blocked run failure/policy decision, sensitive fields.
- Previously warned on CompletedWithWarnings independently.
- Builder remains report-focused and can produce Failed/Blocked reports.
- Completed and CompletedWithWarnings are the only statuses that represent done-success candidates.

## Differences Detected

| Difference | Risk | Canonicalization decision |
| --- | --- | --- |
| Workboard duplicated gate rules with different message wording. | Workboard could pass/fail differently from gate. | Workboard delegates to gate and maps errors through compatibility adapter. |
| Gate did not require task title while Workboard did. | Missing title could pass gate but fail Workboard. | Gate now checks task title too. |
| Progress ready-to-close works from summaries, not source objects. | Progress report could invent positive summary unless builder/gate result is used. | ReadyToClose remains summary-based but requires positive gate summary and blocks negative summaries. Tests enforce summary alignment. |
| RunReportBuilder validates report validity, not done-success. | Completed invalid run could be report-valid but gate-invalid. | Builder validator invokes gate for Completed/CompletedWithWarnings only. Failed/Blocked remain valid reports but not done-success. |

## Risk Of Divergence

The highest divergence risk was between task completion validation and gate evaluation. This is now addressed by delegating Workboard completion to the gate through a compatibility adapter.

The second risk was completed run validity. This is addressed by invoking the gate for completed run statuses while preserving failed/blocked report creation.

## Canonicalization Decision

`NodalOsVerificationBeforeDoneGate` is the canonical source for done-success semantics.

Compatibility APIs remain:

- `NodalOsAgentWorkboardValidator.ValidateTaskCanComplete`
- `NodalOsAgentProgressReportValidator.ValidateReadyToClose`
- `NodalOsRunReportBuilder.Validate`

These APIs must align with or reflect gate results.

## APIs Kept For Compatibility

- `NexaTaskValidationResult`
- `ValidateTaskCanComplete`
- `HasBlockingBlockers`
- `HasPendingOrFailedRequiredVerification`
- `HasEvidenceOrCompletionReason`
- `ValidateReadyToClose`
- `NodalOsRunReportBuilder.Build`
- `NodalOsRunReportBuilder.Validate`

## APIs Delegating Or Reflecting Gate Results

- `ValidateTaskCanComplete` delegates to `NodalOsVerificationBeforeDoneGate.EvaluateTask`.
- `ValidateReadyToClose` consumes `NodalOsVerificationSummary` created from gate results.
- `RunReportBuilder.Validate` invokes `EvaluateRunReport` for `Completed` and `CompletedWithWarnings`.

## Not Touched

- UI.
- Sidepanel.
- Runtime action behavior.
- Recipe execution.
- Orchestration API.
- Scheduled runs.
- Browser actions.
- Desktop actions.
- Persistence DB.
- Package registry.
- Evidence Ledger bridge.
- Sanitizer centralization.
- Namespace move.
- Broad `Nexa*` rename.
