# Blocker + Progress Reporting Contract M367

## Problem

Agent progress, blockers, handoffs, human decisions, and completion readiness need a stable reporting format instead of free-form chat text.

## Implemented

- `NodalOsAgentProgressReport`
- `NodalOsAgentProgressReportKind`
- `NodalOsAgentProgressReportStatus`
- `NodalOsHumanDecisionRequest`
- `NodalOsVerificationSummary`
- `NodalOsReportingWarning`
- `NodalOsAgentProgressReportValidator`
- `NodalOsAgentProgressReportBuilder`
- `NodalOsAgentProgressReportSanitizer`
- fixtures for tests and future adapters

## Agent Workboard Relationship

The report envelope preserves `NexaProgressNote`, `NexaBlockerReport`, and `NexaEvidenceRef`. It binds to Mission/Task/Run IDs and can be consumed by a future workboard UI without giving UI decision authority.

## Verification Before Done Relationship

Completion candidate reports require verification summaries. `ReadyToClose` requires successful verification, evidence/detail/summary, and no blocking blocker or blocking human decision.

## Run Report Relationship

Run report failures can be mapped into blockers. Run, step, and failure evidence references are preserved.

## Report Kinds

- Progress
- Blocker
- Warning
- CompletionCandidate
- Handoff
- Diagnostic

## Report Statuses

- Informational
- InProgress
- WaitingForHuman
- WaitingForApproval
- Blocked
- ReadyForReview
- ReadyToClose
- NotReadyToClose
- Failed
- Cancelled

## Human Decision Requests

Reports support explicit human decision requests for missing context, credentials, policy decisions, approvals, ambiguity, external dependencies, risk acceptance, retry, stop, and unknown cases.

## Ready-To-Close Semantics

Ready-to-close requires:

- `Status=ReadyToClose`
- no blocking/critical blockers
- at least one verification summary
- no failed verification summaries
- no blocking human decision request
- evidence, detail, or summary

## Non-Goals

- No UI.
- No sidepanel.
- No orchestration API.
- No scheduled runs.
- No recipe execution.
- No runtime actions.
- No persistence DB.
- No external service integrations.

## Next Step

Recommended next milestone: `M368-M370 Step Library V1 or Core Legacy Reference Graph`.
