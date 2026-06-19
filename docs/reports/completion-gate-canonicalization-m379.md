# NODAL OS Completion Gate Canonicalization M379

Milestone: M377-M379
Base commit: 1612913

## Problem Resolved

Agent Operations had completion and ready-to-close semantics in multiple places:

- Workboard task completion validation.
- Verification Before Done Gate.
- Progress Report ready-to-close validation.
- Run Report builder validation.

This created a risk that a Task, Run, or ProgressReport could be ready-to-close through one path and blocked through another.

## Canonical Source

`NodalOsVerificationBeforeDoneGate` is now the canonical source for done-success semantics.

Canonical subjects:

- Mission.
- AgentTask.
- RunReport.

## APIs That Delegate Or Align

| API | Alignment |
| --- | --- |
| `NodalOsAgentWorkboardValidator.ValidateTaskCanComplete` | Delegates to `NodalOsVerificationBeforeDoneGate.EvaluateTask`. |
| `NodalOsAgentProgressReportValidator.ValidateReadyToClose` | Requires positive `NodalOsVerificationSummary` generated from gate result and blocks negative summaries. |
| `NodalOsRunReportBuilder.Validate` | Uses `EvaluateRunReport` for `Completed` and `CompletedWithWarnings` statuses. |

## Compatibility APIs Kept

- `ValidateTaskCanComplete` remains available.
- `NexaTaskValidationResult` remains available.
- Workboard helper methods remain available for tests and compatibility.
- `ValidateReadyToClose` remains available.
- `NodalOsRunReportBuilder.Build` remains throwing for invalid report builds.
- Failed/Blocked run reports remain valid reports even though they are not done-success.

## Compatibility Shim

`NodalOsCompletionGateCompatibilityAdapter` maps `NodalOsVerificationBeforeDoneResult` into `NexaTaskValidationResult` while preserving legacy Workboard error wording where existing tests and callers expect it.

This is intentionally a compatibility shim, not a second source of truth.

## Consistent Rules

- Blocking/Critical blockers prevent close.
- Pending/Failed required verification prevents close.
- Skipped required verification requires a reason.
- Evidence or explicit completion reason is required.
- Failed/Blocked/Cancelled/Running/Paused/Planned runs cannot be done-success.
- Completed runs require `CompletedAt`.
- Completed runs cannot include failed/blocked/waiting-for-human steps.
- Pending/denied approvals prevent completed done-success.
- CompletedWithWarnings requires non-critical failure, notes, or final summary.

## No-Divergence Tests

The new M377-M379 tests verify:

- Workboard task completion matches the gate.
- ProgressReport ReadyToClose requires gate-positive summaries.
- ProgressReport ReadyToClose fails on gate-negative summaries.
- RunReport completed semantics match the gate.
- Failed and blocked reports remain valid reports but gate-negative.
- Invalid completed runs fail both builder validation and gate evaluation.
- Gate results remain serializable.
- No UI/runtime/orchestration/recipe execution was introduced.

## Limits And Future Work

RunReport still has two concepts:

- report validity.
- done-success eligibility.

This is intentional. Failed and blocked runs must remain reportable even though they cannot be marked done-success.

Future cleanup should address:

- common redaction/sanitizer service.
- EvidenceRef-to-ledger bridge.
- Recipe/Step runtime-permission semantics.
- Agent Operations namespace/project extraction.

## Readiness

Recommended readiness decision:

`M377+M378+M379 CERRADO / COMPLETION_GATE_CANONICALIZED_WITH_COMPATIBILITY_SHIMS`
