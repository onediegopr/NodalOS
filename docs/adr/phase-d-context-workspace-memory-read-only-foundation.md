# ADR: Phase D Context Workspace Memory Read-Only Foundation

Decision target: `GO_PHASE_D_CONTEXT_WORKSPACE_MEMORY_READ_ONLY_FOUNDATION_READY`

## Status

Accepted for read-only foundation.

## Context

Fase C closed with EIL persistence and evidence contracts in a design-only, disabled, fail-closed state. Phase D opens a new line for Context, Workspace and Memory, but it must preserve the same safety posture:

- no real workspace scan;
- no durable memory;
- no filesystem product read/write;
- no database;
- no provider/cloud/network;
- no semantic/vector backend;
- no LLM live calls;
- no runtime/live actions;
- no browser/CDP, WCU or OCR live behavior.

Existing historical workspace and process memory code includes mock storage and artifact read/write paths. Those paths are intentionally not reused as the Phase D foundation implementation. The foundation is a new in-memory contract and presenter that can reference EIL read-only evidence labels without activating EIL persistence stores.

## Decision

Add `WorkspaceContextReadOnlyFoundation` as a deterministic, fixture-safe Core contract.

The foundation models:

- workspace identity fixture;
- workspace boundary descriptor;
- context packet summary;
- selected, locked and excluded context;
- evidence-linked context references;
- authority levels;
- freshness/staleness;
- contradiction, risk, decision, claim and action memory previews;
- missing and stale context warnings;
- sensitive/unsafe context blockers;
- provider/cloud disabled notice;
- semantic/vector disabled notice;
- safe next step;
- no-side-effect proof;
- documented debt.

The presenter source is `WorkspaceContextReadOnlyPresenter.CreateFixture()`. It consumes `EvidenceIntelligenceAuditDashboardReadOnlyPresenter.CreateFixture()` only as an in-memory read-only source label and evidence-ref source. It does not read the workspace, query a store, register services or call external systems.

## No Goals

- No real workspace filesystem reads.
- No filesystem writes.
- No durable memory.
- No EIL read/write store activation.
- No DB or dependency.
- No semantic/vector backend.
- No embeddings.
- No provider/cloud/network.
- No LLM live calls.
- No runtime/live.
- No migration runner.
- No UI action surface.

## Safety Model

Every source, item and memory candidate carries `WorkspaceContextNoSideEffectProof`.

Required false flags:

- `WorkspaceFilesystemReadAttempted`
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
- `ProductActionExposed`
- `ProductServiceRegistered`

## Future Unlock Requirements

Any future real workspace scan, durable memory, semantic/vector backend, provider/cloud call, LLM integration, UI action, or persistence activation requires a separate milestone with explicit threat model, guards, tests, scans and closeout.

The recommended next block is `PHASE_D_CONTEXT_AUTHORITY_FRESHNESS_GUARDS`, because authority and freshness should be hardened before expanding memory candidates or mounting a visible surface.
