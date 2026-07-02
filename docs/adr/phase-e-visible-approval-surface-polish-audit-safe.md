# ADR: Phase E Visible Approval Surface Polish Audit Safe

Decision target: `GO_NODAL_OS_VISIBLE_APPROVAL_SURFACE_POLISH_AUDIT_SAFE_READY`

## Status

Accepted as read-only visible-surface polish if validation passes.

## Context

NODAL OS is paused at `PAUSED_SAFE_READ_ONLY_NO_RUNTIME` with Phase E formally closed as read-only/no-runtime/no-execution. The Approval/Human Review line already has foundation fixtures, risk/decision guards, evidence/context link guards, an approval packet surface and an in-memory human review export preview.

The remaining polish need is presentation clarity: labels, section titles, disabled notices, blocker copy, decision-option preview wording, no-side-effect wording and audit handoff text.

## Decision

Polish the existing read-only presenters without adding a new capability surface:

- `ApprovalPacketReadOnlySurfacePresenter` now uses clearer human-review surface wording.
- Candidate actions and decision options are described as preview labels and copy-safe review content.
- Disabled capability notices explain what remains unavailable without implying product controls.
- `HumanReviewPacketExportReadOnlyPresenter` clarifies that the preview is an in-memory copy surface, not physical export.
- Recipe coverage asserts the visible polish remains read-only, deterministic, actionless and exportless.

## Non-Goals

- No approval execution.
- No approval state mutation.
- No writer/policy integration.
- No physical export, clipboard or browser download.
- No runtime/live.
- No provider/cloud, DB, semantic/vector backend, LLM live or durable memory.
- No product UI action controls.
- No release/commercial readiness claim.

## Safety Notes

This hito changes copy and tests around existing read-only DTO/presenter paths. It does not register services, create files, call providers, start runtime, open browser/CDP, run recipes or mutate approval state.

## Next Safe Step

Recommended next block remains either `NODAL_OS_APPROVAL_EXECUTION_DESIGN_ONLY_PROTECTED` for protected design only, or `NODAL_OS_PAUSE_AFTER_VISIBLE_APPROVAL_POLISH` if the project should pause again.
