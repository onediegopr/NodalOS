# ADR: NODAL OS Controlled Execution Readiness Design Track

Decision target: `GO_NODAL_OS_CONTROLLED_EXECUTION_READINESS_DESIGN_TRACK_READY`

## Status

Accepted as protected design-only macro-track if validation passes.

## Context

NODAL OS has completed the read-only/no-runtime roadmap and paused cleanly after the protected Approval Execution design audit. The current safe question is not how to execute, but how to specify the complete future readiness bridge without opening execution, mutation, runtime/live or product actions.

Control phrase:

`Design readiness may increase. Runtime readiness remains 0%.`

## Decision

Add a deterministic read-only macro-track fixture:

- `ControlledExecutionReadinessDesignTrack`
- `ApprovalExecutionStateMachineDesignOnly`
- `ApprovalMutationBoundaryDesignOnly`
- `ApprovalWriterPolicyIntegrationBoundaryDesignOnly`
- `ApprovalDurableAuditTrailDesignOnly`
- `ApprovalPhysicalExportPolicyDesignOnly`
- `ProductActionControlReadinessDesignOnly`
- `CrossPhaseRuntimeReadinessGateDesignOnly`
- `ControlledExecutionNegativeCapabilityContract`

The fixture is produced by `ControlledExecutionReadinessDesignTrackPresenter.CreateFixture()`. It is an in-memory DTO model and does not expose executors, mutation methods, product services, command bindings, runtime/live, product IO, DB, provider/cloud, LLM live, durable memory, browser/CDP, WCU/OCR, recipe execution or physical export behavior.

## Design Areas

1. Approval execution state machine design-only: conceptual states and transition previews, with future states marked not implemented.
2. Approval mutation boundary design-only: mutation candidates are blocked and require future actor, timestamp, evidence refs, policy decision and durable audit controls.
3. Writer/policy approval integration boundary design-only: approval previews never imply execution, policy previews never write and writer candidates never run.
4. Durable approval audit trail design-only: future event types and fields are modeled without storage, ledger, DB or migration runner.
5. Physical export policy design-only: future PDF/DOCX/JSON/Markdown/clipboard/download targets are named but all export channels remain unavailable.
6. Product action controls disabled-to-enabled design-only: future controls stay preview-only, blocked and unbound.
7. Cross-phase runtime readiness gate design-only: every category blocks runtime/live and release/commercial remains NO-GO.
8. Negative capability contracts: explicit assertions keep execution, mutation, writer, policy, runtime, service registration, IO, DB, provider/cloud, LLM, vector, memory, browser/CDP, WCU/OCR, recipes, export and commercial claims unavailable.

## Non-Goals

- No real approval execution.
- No approval state mutation.
- No productive writer/policy integration.
- No command handler or command binding.
- No product service registration.
- No runtime/live.
- No physical export, clipboard or download.
- No filesystem product IO.
- No DB or migration runner.
- No provider/cloud/network.
- No semantic/vector backend.
- No LLM live.
- No durable memory.
- No browser/CDP live, WCU live or OCR live.
- No recipe execution.
- No release/commercial readiness claim.
- No protected post-M1345 browser/runtime scope change.

## Safety Proof

- Product action count: `0`.
- State mutation count: `0`.
- Export action count: `0`.
- Approval execution implementation readiness: `0%`.
- Approval state mutation readiness: `0%`.
- Runtime/live readiness: `0%`.
- Physical export readiness: `0%`.
- Release/commercial readiness: `NO-GO`.
- All runtime gates: `Blocked`.
- All product controls: preview-only and disabled.
- All negative capability contracts: true.

## Future Requirements

Any move from this macro-track to implementation requires a separate protected sequence, starting with external audit. Future work must cover state machine implementation review, mutation boundary implementation review, writer/policy boundary review, durable audit trail, export policy, runtime gate, product action security and explicit user authorization.
