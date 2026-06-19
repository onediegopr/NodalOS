# Blocker + Progress Reporting Contract Audit M365

## Scope

M365 reviewed existing progress, blocker, handoff, run report, and verification gate surfaces before adding a stable Agent Operations reporting envelope.

## Models Found

- `NexaProgressNote` and `NexaBlockerReport` in `NodalOsAgentWorkboardContracts.cs`.
- `NexaRunReport`, `NexaFailureReport`, and evidence refs in `NodalOsRunReportContracts.cs`.
- `NodalOsVerificationBeforeDoneResult` in `NodalOsVerificationBeforeDoneContracts.cs`.
- Browser human handoff and credential boundary types, but they are browser-specific and not a general Agent Operations reporting contract.

## Reused

- `NexaProgressNote` for progress notes.
- `NexaBlockerReport` for blockers and suggested resolution mode.
- `NexaEvidenceRef` for evidence references.
- `NexaFailureKind` through `NexaFailureReport` to map run failures into blocker reports.
- `NodalOsVerificationBeforeDoneResult` for ready-to-close verification summaries.

## Added

- `NodalOsAgentProgressReport`.
- report kind/status enums.
- `NodalOsHumanDecisionRequest`.
- `NodalOsVerificationSummary`.
- `NodalOsReportingWarning`.
- validator, builder, sanitizer, and fixtures.

## Mission / Task Relationship

Reports can bind to `MissionId` and `TaskId`, preserve task progress notes and blockers, and stay independent of UI or persistence.

## Run Report Relationship

Run report failures can be projected into blocker reports, while run/step/failure evidence refs are preserved.

## Verification Before Done Relationship

Completion candidate reports include `NodalOsVerificationSummary` values derived from `NodalOsVerificationBeforeDoneResult`.

## Future UI / Sidepanel / Timeline Relationship

The contract is UI-usable later, but it does not implement UI, sidepanel, timeline, persistence, or runtime execution.

## Not Touched

- Runtime actions.
- Browser/CDP behavior.
- Recipe execution.
- Orchestration API.
- Scheduled runs.
- Persistence DB.

## Duplication Risk

Run report and workboard models already hold pieces of progress state, but they do not provide a single serializable reporting envelope. The new contract composes those existing types instead of replacing them.

## Decision

No blocking conflict was found. Add a dedicated Agent Progress Reporting contract in `OneBrain.BrowserExecutor.Contracts` and implementation helpers in `OneBrain.BrowserExecutor.Cdp`.
