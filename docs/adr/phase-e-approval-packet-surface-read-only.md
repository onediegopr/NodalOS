# ADR: Phase E Approval Packet Surface Read Only

Decision target: `GO_PHASE_E_APPROVAL_PACKET_SURFACE_READ_ONLY_READY`

## Status

Accepted as read-only surface/presenter.

## Context

Phase E already has a read-only approval/human review foundation, risk/decision guards and evidence/context link guards. This milestone adds a surface DTO/presenter that consolidates those pieces for audit and review without exposing approval execution, state mutation, action controls or export.

## Decision

Add `ApprovalPacketReadOnlySurfacePresenter` and related DTOs.

The surface composes:

- `ApprovalHumanReviewReadOnlyPresenter.CreateFixture()`;
- `ApprovalRiskDecisionReadOnlyGuard.EvaluateCatalog()`;
- `HumanReviewEvidenceContextLinkReadOnlyGuard.EvaluateCatalog()`.

The presenter produces 30 deterministic in-memory sections for summary, candidate action previews, guard summaries, evidence/context links, blockers, decision labels, disabled capability notices, no-side-effect proof, debt and next block.

## Surface Rules

- Surface is not approval execution.
- Decision option is not command.
- Approve/reject labels are preview labels only.
- Product action count is always zero.
- State mutation count is always zero.
- Export action count is always zero.
- The surface does not hide blockers.
- Fixture data is not promoted to production trusted.
- The surface does not call or reference writer/policy/runtime execution paths.

## Invariants

- No approval execution.
- No approval state mutation.
- No product action command.
- No action controls.
- No export real.
- No filesystem product read/write.
- No DB/dependency.
- No provider/cloud/network.
- No semantic/vector backend.
- No LLM live.
- No durable memory.
- No runtime/live.
- No service registration.

## No Goals

- No UI mount.
- No approve/reject product flow.
- No `ApprovalArtifactWriter`.
- No `ApprovalPolicy`, `Pilot` or `AgentOperations` execution path.
- No physical export, clipboard or download.
- No runtime/live or browser/CDP.

## Future Unlock Requirements

Any future visible approval UI or executable approval flow must be handled in a separate protected milestone with action controls review, state mutation design, durable audit trail design, writer/policy path audit and manual QA.
