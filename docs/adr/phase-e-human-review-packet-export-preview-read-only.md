# ADR: Phase E Human Review Packet Export Preview Read Only

Decision target: `GO_PHASE_E_HUMAN_REVIEW_PACKET_EXPORT_PREVIEW_READ_ONLY_READY`

## Status

Accepted as read-only export preview/presenter.

## Context

Phase E already has a read-only approval/human review foundation, risk/decision guards, evidence/context link guards and an approval packet surface. This milestone adds an in-memory export preview for audit handoff. The preview is copy-ready text/DTO data only; it is not physical export, clipboard use, browser download, approval execution or state mutation.

## Decision

Add `HumanReviewPacketExportReadOnlyPresenter` and related DTOs:

- `HumanReviewPacketExportReadOnlyPreview`;
- `HumanReviewPacketExportManifest`;
- `HumanReviewPacketExportSection`;
- preview status/kind enums.

The presenter composes `ApprovalPacketReadOnlySurfacePresenter.CreateFixture()` and carries forward the Phase E no-side-effect proof. The manifest explicitly declares:

- physical file created: false;
- clipboard used: false;
- download started: false;
- product actions count: 0;
- state mutation count: 0;
- export actions count: 0;
- approval execution occurred: false;
- approval state mutation occurred: false;
- raw payload content: false;
- sensitive-value-like content: false;
- durable memory content: false.

## Preview Rules

- Preview is not export.
- Preview content is in-memory only.
- No file, clipboard, download, PDF, DOCX, JSON, MD or ZIP artifact is created.
- Decision labels remain labels, not commands.
- Approve/reject labels are not actions.
- No approval execution.
- No approval state mutation.
- No product actions.
- No export actions.
- No writer/policy/runtime execution path reference.
- Blockers and warnings remain visible.

## Invariants

- No `ApprovalArtifactWriter`.
- No `ApprovalPolicy`, `Pilot` or `AgentOperations` execution path.
- No service registration.
- No filesystem product read/write.
- No DB/dependency.
- No provider/cloud/network.
- No semantic/vector backend.
- No LLM live.
- No durable memory.
- No runtime/live.
- No approval state mutation.

## No Goals

- No physical export.
- No clipboard or browser download.
- No UI mount.
- No action buttons.
- No approve/reject product flow.
- No filesystem, workspace scan, DB, provider/cloud, semantic/vector, LLM live, durable memory or runtime capability.

## Future Unlock Requirements

Any future physical export or approval execution must be handled in a separate protected milestone with writer/path audit, action-control review, state mutation design, durable audit trail design, manual QA and explicit release gate review.
