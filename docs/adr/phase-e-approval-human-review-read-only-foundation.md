# ADR: Phase E Approval Human Review Read-Only Foundation

Decision target: `GO_PHASE_E_APPROVAL_HUMAN_REVIEW_READ_ONLY_FOUNDATION_READY`

## Status

Accepted for read-only foundation.

## Context

Phase C closed the evidence and persistence line with read-only surfaces, disabled scaffolds, hostile redaction fixtures, dry-run migration planning, schema guards, export preview, audit dashboard and closeout. Phase D closed the context/workspace/memory line with read-only foundation, authority/freshness guards, selection/lock/exclusion guards, memory candidate guards, context packet surface/export preview and closeout.

Phase E opens Approval and Human Review, but it must preserve the same safety posture:

- approval preview is not approval execution;
- human review packet is not state mutation;
- no product action buttons or commands;
- no runtime/live;
- no recipe execution;
- no browser/CDP, WCU or OCR live behavior;
- no real workspace scan;
- no filesystem product read/write;
- no database;
- no durable memory;
- no provider/cloud/network;
- no semantic/vector backend;
- no LLM live calls;
- no migration runner;
- no physical export, clipboard or browser download.

Existing approval code includes artifact writing, policy and binding validation surfaces. Those paths are intentionally not reused for this foundation because they are not the read-only Phase E contract. The foundation is a new deterministic in-memory presenter that references Phase C and Phase D read-only fixtures only.

## Decision

Add `ApprovalHumanReviewReadOnlyFoundation` as a deterministic, fixture-safe Core contract.

The foundation models:

- approval packet identity fixture;
- human review summary;
- candidate action previews;
- candidate action kind;
- risk level and rationale;
- Phase C evidence links;
- Phase D context links;
- authority/freshness summary;
- selection/lock/exclusion summary;
- memory candidate risk/contradiction summary;
- required human decision;
- decision options preview;
- approval blockers and warnings;
- missing evidence, missing context, stale context, unresolved contradiction and critical risk blockers;
- runtime/live, filesystem/DB, provider/cloud and durable memory disabled notices;
- safe next step;
- no-side-effect proof;
- deferred capabilities and debt.

The presenter source is `ApprovalHumanReviewReadOnlyPresenter.CreateFixture()`. It consumes:

- `EvidenceIntelligenceAuditDashboardReadOnlyPresenter.CreateFixture()`;
- `WorkspaceContextPacketExportReadOnlyPresenter.CreateFixture()`.

Both are used only as fixture-safe read-only sources. The presenter does not read or write files, mutate approval state, execute approval decisions, register services or call external systems.

## No Goals

- No approval execution.
- No approval state mutation.
- No product approval UI action.
- No approve/reject button that changes state.
- No recipe execution.
- No runtime/live.
- No filesystem read/write.
- No workspace scan.
- No DB or dependency.
- No provider/cloud/network.
- No semantic/vector backend.
- No LLM live calls.
- No durable memory.
- No physical export, clipboard or browser download.

## Safety Model

Every packet, item, link, candidate action, decision option and risk summary carries `ApprovalReviewNoSideEffectProof`.

Required false flags:

- `FilesystemReadAttempted`
- `FilesystemWriteAttempted`
- `DatabaseTouched`
- `DurablePersistenceActive`
- `DurableMemoryActive`
- `VectorSemanticBackendTouched`
- `LlmProviderTouched`
- `ProviderCloudTouched`
- `MigrationRunnerStarted`
- `MigrationExecuted`
- `RuntimeTouched`
- `BrowserCdpTouched`
- `WcuTouched`
- `OcrTouched`
- `RecipeExecutionStarted`
- `ApprovalExecutionStarted`
- `ApprovalStateMutationAttempted`
- `ProductActionExposed`
- `ProductServiceRegistered`

## Future Unlock Requirements

Any future real approval execution, approval state mutation, approval UI action, persisted review packet, runtime/live execution, provider/cloud call, LLM live call, workspace scan, durable memory, DB/dependency or physical export requires a separate milestone with explicit threat model, contracts, guards, tests, scans and closeout.

The recommended next block is `PHASE_E_APPROVAL_RISK_DECISION_GUARDS`, because risk/decision handling should be hardened before creating a visible approval packet surface or any future execution capability.
