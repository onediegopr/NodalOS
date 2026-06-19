# Verification Before Done Gate Audit M362

## Scope

M362 reviewed the existing Agent Operations completion and reporting models before adding a formal Verification Before Done gate.

## Models Found

- `NexaMission`, `NexaAgentTask`, `NexaProgressNote`, `NexaBlockerReport`, `NexaVerificationCheck`, and `NexaEvidenceRef` in `NodalOsAgentWorkboardContracts.cs`.
- `NexaRunReport`, `NexaRunStepReport`, `NexaPolicyDecisionReport`, `NexaApprovalReport`, and `NexaFailureReport` in `NodalOsRunReportContracts.cs`.
- `NexaFailureKind` and `NexaFailureSeverity` in `NodalOsFailureTaxonomyContracts.cs`.

## Validators Found

- `NodalOsAgentWorkboardValidator.ValidateTaskCanComplete` already enforces task-level blocker, verification, and evidence/closing reason requirements.
- `NodalOsRunReportBuilder.Validate` validates basic run report shape, terminal timestamps, blocked/failed report requirements, and sensitive field safety.

## Integration Decision

The new gate extends the existing model instead of replacing it:

- task rules mirror the existing validator and add serializable gate output.
- mission rules aggregate task gate failures.
- run report rules add done-success semantics for `Completed` and `CompletedWithWarnings`.

## Reused

- Agent Workboard contracts.
- Run Report V1 contracts.
- Failure taxonomy severity.
- Run report sanitizer for safe completion reasons.

## Added

- `NodalOsVerificationBeforeDoneResult`.
- `NodalOsVerificationBeforeDoneSubjectKind`.
- `NodalOsVerificationBeforeDoneOptions`.
- `NodalOsVerificationBeforeDoneGate`.

## Not Touched

- UI and sidepanel.
- Browser/CDP runtime actions.
- Recipe execution.
- Orchestration API.
- Scheduled runs.
- Persistence.

## Duplication Risk

There is task-level overlap with `NodalOsAgentWorkboardValidator.ValidateTaskCanComplete`. This is intentional and limited: the validator remains model validation, while the new gate returns an explainable, serializable, subject-scoped closure decision for Task, Mission, and RunReport.

## Namespace and Location

- Contracts: `OneBrain.BrowserExecutor.Contracts` in `NodalOsVerificationBeforeDoneContracts.cs`.
- Service: `OneBrain.BrowserExecutor.Cdp` in `NodalOsVerificationBeforeDoneGate.cs`.

## Decision

No blocking conflict was found. Proceed with a dedicated Verification Before Done gate that preserves evidence refs and does not introduce runtime behavior.
