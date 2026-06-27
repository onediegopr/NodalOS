# Recipe Approval Narrative 2.0

Phase: 4/9 - Human Intervention + Approval Narrative 2.0.

`RecipeApprovalNarrative` explains why a recipe needs operator review and what can safely happen next.

## Required Narrative Answers

The narrative must answer:

- what the recipe wants to do,
- target system,
- target entity summary when known,
- reason for approval,
- risk level,
- sensitive action categories,
- evidence summary,
- validation summary,
- redaction summary,
- limits summary,
- complete criteria summary,
- terminate criteria summary,
- deterministic action resolution summary,
- what changes if approved,
- what changes if rejected,
- what remains blocked,
- safe next action,
- rollback/restore boundary,
- raw data omitted,
- no-secret guarantee,
- operator-visible explanation.

High and critical risk narratives require evidence, limits, consequence summary, and safe next action.

## Decision Options

Allowed decision options are:

- `ApprovePreviewOnly`,
- `ApproveDryRunOnly`,
- `ApproveFixtureRunOnly`,
- `ApproveManualContinuation`,
- `Reject`,
- `CancelRun`,
- `RequestMoreEvidence`,
- `MarkManuallyResolved`,
- `Escalate`,
- `KeepBlocked`.

There are no usable options for live browser execution, live desktop execution, payment execution, fiscal submission execution, message send execution, delete execution, public posting execution, or external mutation execution.
