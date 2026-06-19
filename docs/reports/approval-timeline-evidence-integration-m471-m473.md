# Approval Center + Timeline + Evidence Registry Integration M471-M473

## Executive Summary

M471-M473 connects the M468-M470 core foundation with product-facing data models for approval, timeline and integrated evidence references.

This block is model/service/test only. It does not implement visual UI, runtime execution, scheduler, worker runtime, browser automation, recorder/replay, queue, cloud, LLM provider calls, DSL parser runtime, persistence DB, or external side effects.

Decision:

`M471+M472+M473 CERRADO / APPROVAL_TIMELINE_EVIDENCE_INTEGRATION_READY`

## Initial Git State

- Worktree: `C:\DESARROLLO\NodalOS\Codigo-m12-audit`.
- Branch: `chrome-lab-001-extension-local-ai-bridge`.
- Base commit: `488346994a8086bb891f8c4eedc8b6fc53978c57`.
- Remote: `https://github.com/onediegopr/NodalOS.git`.
- Forbidden path not used: `C:\Users\diego\OneDrive\PERSONAL\ONE Brain\Codigo`.

## Objective

Create safe core/data-model foundations for:

- Approval Center cards and decisions.
- Timeline projections derived from canonical core events.
- Evidence registry integration across registry, event, approval and timeline surfaces.

The implementation preserves no-execution semantics and redacted serialization.

## M471 - Approval Center Data Model

Created:

- `NodalOsApprovalCard`.
- `NodalOsApprovalDecision`.
- approval status/severity/action/decision/user option enums.
- `NodalOsApprovalCenterService`.
- approval validator and serializer.

Approval cards include:

- execution registry entry ref;
- core event ref;
- mission/task refs;
- severity and requested action;
- human explanation;
- policy gate reason;
- affected resources or explicit no-resource reason;
- rollback/evidence plan text;
- user options;
- evidence refs.

Approval decisions support:

- approve;
- reject;
- request changes;
- request explanation;
- defer;
- human handoff required.

Important semantics:

- Approval card does not execute.
- Approval decision does not authorize execution by itself.
- `RuntimeExecutionAllowed=false`.
- `RuntimeExecutionDeferred=true`.
- `CanAuthorizeExecution=false`.
- `RequiresGlobalPolicyEvaluation=true`.
- `RequiresEvidenceRedaction=true`.

## M472 - Timeline Projection

Created:

- `NodalOsTimelineEntry`.
- timeline severity/status enums.
- `NodalOsTimelineProjectionService`.

Timeline projection maps canonical `NodalOsCoreEvent` events to timeline entries:

- `ExecutionRequestRegistered`.
- `PolicyGateEvaluated`.
- `ApprovalRequired`.
- `ApprovalGranted`.
- `ApprovalRejected`.
- `DryRunPlanCreated`.
- `ExecutionCompleted`.
- `ExecutionFailed`.
- `EvidenceAttached`.
- `WarningRaised`.
- `HumanHandoffRequired`.
- `RedactionApplied`.

Timeline entries preserve:

- event id;
- execution registry id;
- approval card/decision ids when present in event metadata;
- mission/task refs;
- severity/status;
- human-readable title/message;
- human attention flag;
- evidence refs.

Timeline is derived and reconstructible from events. It does not execute or mutate registry/evidence.

## M473 - Evidence Registry Integration

Created:

- `NodalOsEvidenceRegistryAttachment`.
- `NodalOsEvidenceAttachmentKind`.
- `NodalOsEvidenceRegistryIntegrationService`.

Evidence integration supports:

- canonical evidence ref validation via `NodalOsEvidenceRefBridge`;
- attaching evidence refs to registry entries;
- attaching evidence refs to core events;
- attaching evidence refs to approval cards;
- exposing evidence refs in timeline entries;
- metadata-redacted attachment records.

Safety restrictions:

- Raw payload persistence is rejected.
- Raw secret/cookie/header/body content is rejected.
- Screenshot evidence is reference-only.
- Network evidence is metadata-redacted-only.
- DOM evidence is redacted/reference-only.
- Serializers redact before output.

## Files Created

- `src/OneBrain.AgentOperations.Contracts/NodalOsApprovalTimelineEvidenceContracts.cs`.
- `src/OneBrain.AgentOperations.Core/NodalOsApprovalTimelineEvidenceServices.cs`.
- `tests/OneBrain.Safety.Tests/NodalOsApprovalTimelineEvidenceM471M473Tests.cs`.
- `docs/reports/approval-timeline-evidence-integration-m471-m473.md`.
- `artifacts/agent-operations/m473/approval-timeline-evidence-integration-summary.json`.

## Files Modified

- `docs/roadmap/nodal-os-roadmap-vnext.md`.
- `docs/roadmap/nodal-os-unified-roadmap-post-pause.md`.

## New Tests

Added `NodalOsApprovalTimelineEvidenceM471M473Tests`.

Coverage includes:

- approval card creation from registry/event/policy context;
- required human explanation;
- required severity and affected resource/no-resource explanation;
- required user options;
- all decision kinds modelable;
- approval decision no-execution/no-authority;
- redacted approval serialization;
- evidence ref conservation;
- raw secret/cookie/header/body evidence rejection;
- projection of every canonical core event kind;
- timeline ordering;
- timeline registry/approval/evidence linkage;
- human attention flags;
- timeline serialization without secrets;
- evidence registry ref-only attachment;
- screenshot/network/DOM restrictions;
- cross-layer registry -> event -> timeline -> approval/evidence no-divergence;
- dependency direction guard;
- naming and runtime guardrails.

## Validations Executed

Required validation plan:

- `dotnet restore .\OneBrain.slnx`.
- `dotnet build .\OneBrain.slnx`.
- `dotnet test .\OneBrain.slnx --no-build --no-restore --filter "ApprovalTimelineEvidence|CoreRuntimeRegistryEventBusRedaction|NewTopicsIntake|NamingCleanup"`.
- `dotnet test .\OneBrain.slnx --no-build --no-restore`.

## Guardrails Confirmed

- NODAL OS remains the operational project name.
- No NODRIX/HOTEP operational naming introduced.
- No NEXA operational naming introduced.
- No UI introduced.
- No runtime execution introduced.
- No cloud introduced.
- No LLM provider calls introduced.
- No browser automation introduced.
- No scheduler or worker introduced.
- No recorder or replay introduced.
- No queue introduced.
- No DSL parser runtime introduced.
- No shell/subprocess introduced.
- No persistence DB introduced.

## Not Implemented

- Mission Control visual UI.
- Frontend.
- Runtime executor.
- Scheduler.
- Worker runtime.
- Browser automation.
- Recorder/replay.
- Queue.
- DSL parser runtime.
- Cloud/auth/billing/updater.
- LLM/BYOK/provider calls.
- Recipe/step execution.
- Productive evidence persistence DB.

## Risks And Pending Work

- Approval Center is data-model/service-only; UX preview remains next.
- Timeline projection is derived from canonical events but is not a rendered UI.
- Evidence registry integration is ref-only; durable registry/persistence remains future work.
- Runtime-gated Recipe Risk Classifier hardening remains required before any execution use.

## Updated Percentages

- NODAL OS global: 96%.
- Agent Operations / Automation Layer: 97%.
- Core Runtime: 66% -> 71%.
- Evidence/Timeline foundation: 60% -> 70%.
- Approval foundation: 35-40% -> 58%.
- Redaction/Safety foundation: ~70% -> 74%.
- Mission Control UX: 20%.
- LLM/Assignment: 25%.
- Productization: 30%.
- Cloud optional: 10%.
- Automation future: 35%.

## Next Recommended Block

`M474+M475+M476 - Approval Center UX Contract Preview + Export/Handoff Data Pack + Runtime Observability Report`.

## Final Decision

`M471+M472+M473 CERRADO / APPROVAL_TIMELINE_EVIDENCE_INTEGRATION_READY`
