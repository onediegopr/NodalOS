# Core Runtime Registry + EventBus + Redaction Foundation M468-M470

## Executive Summary

M468-M470 returns NODAL OS to the core roadmap after the pause closure and unified roadmap intake.

This block implements foundation-only contracts and services for:

- Execution Registry lifecycle tracking.
- Core canonical EventBus.
- Redaction Foundation reuse and hardening.

No runtime execution, UI, scheduler, worker, browser automation, cloud, LLM provider call, recorder, replay, queue, DSL parser, or external side effect was implemented.

Decision:

`M468+M469+M470 CERRADO / CORE_RUNTIME_REGISTRY_EVENTBUS_REDACTION_FOUNDATION_READY`

## Initial Git State

- Worktree: `C:\DESARROLLO\NodalOS\Codigo-m12-audit`.
- Branch: `chrome-lab-001-extension-local-ai-bridge`.
- Base commit: `1d7fad710aae7ff5508c97bff98f6bb5653dd5fd`.
- Remote: `https://github.com/onediegopr/NodalOS.git`.
- Forbidden path not used: `C:\Users\diego\OneDrive\PERSONAL\ONE Brain\Codigo`.

## Objective

Create a core foundation for execution-state bookkeeping, canonical event capture, timeline-safe projections, and redacted serialization.

The implementation is intentionally contract/service-only. It does not create an executor or any productive runtime.

## M468 - Execution Registry Foundation

Created:

- `NodalOsExecutionRequest`.
- `NodalOsExecutionRegistryEntry`.
- `NodalOsExecutionRegistryTransition`.
- `NodalOsExecutionRegistryState`.
- `NodalOsExecutionRegistry`.
- `NodalOsCoreRuntimeValidator`.

The registry supports:

- request registration;
- initial `Registered` state;
- explicit transition validation;
- transition history;
- actor/source metadata;
- policy, approval, dry-run, verification, snapshot and evidence references;
- redacted failure reason;
- safe JSON serialization;
- no-runtime-authority invariants.

Important semantics:

- Registry visibility does not grant execution permission.
- Registry transitions are state bookkeeping only.
- `RuntimeExecutionAllowed=false`.
- `RuntimeExecutionDeferred=true`.
- `RequiresGlobalPolicyEvaluation=true`.
- `RequiresEvidenceRedaction=true`.

## M469 - Core EventBus

Created:

- `NodalOsCoreEvent`.
- `NodalOsCoreEventKind`.
- `NodalOsCoreTimelineProjection`.
- `NodalOsCoreEventBus`.

Canonical event kinds:

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

The EventBus supports:

- publishing valid canonical events;
- rejecting invalid events;
- in-memory fixture-safe ordering;
- evidence refs;
- execution registry linkage;
- timeline-compatible projections;
- sanitized event metadata and summaries.

The EventBus has no scheduler behavior, no external side effects, no HTTP/API behavior, and no execution authority.

## M470 - Redaction Foundation

Reused and extended the common `NodalOsRedactionService` and `NodalOsSensitiveContentClassifier`.

Coverage now includes:

- bearer tokens;
- basic auth;
- authorization headers;
- cookies;
- set-cookie;
- password/pass/passwd;
- secret;
- api_key/apikey;
- access_token;
- refresh_token;
- id_token;
- private key material;
- connection strings;
- email addresses;
- JWT-like tokens;
- private body markers.

Serializers for registry entries and core events sanitize fields before JSON output. EventBus publishing also sanitizes event metadata and summaries before storing.

Redaction remains idempotent for already-redacted values.

## Files Created

- `src/OneBrain.AgentOperations.Contracts/NodalOsCoreRuntimeContracts.cs`.
- `src/OneBrain.AgentOperations.Core/NodalOsCoreRuntimeServices.cs`.
- `tests/OneBrain.Safety.Tests/NodalOsCoreRuntimeRegistryEventBusRedactionM468M470Tests.cs`.
- `docs/reports/core-runtime-registry-eventbus-redaction-m468-m470.md`.
- `artifacts/agent-operations/m470/core-runtime-registry-eventbus-redaction-summary.json`.

## Files Modified

- `src/OneBrain.AgentOperations.Contracts/NodalOsRedactionContracts.cs`.
- `src/OneBrain.AgentOperations.Core/NodalOsRedactionServices.cs`.
- `docs/roadmap/nodal-os-roadmap-vnext.md`.
- `docs/roadmap/nodal-os-unified-roadmap-post-pause.md`.

## New Tests

Added `NodalOsCoreRuntimeRegistryEventBusRedactionM468M470Tests`.

Coverage includes:

- registry request registration;
- invalid and valid transitions;
- evidence refs;
- verification refs;
- redacted failure reason;
- registry serialization without secrets;
- no execution authority;
- EventBus publish/reject/order/timeline projection;
- EventBus no side effects;
- redaction of bearer tokens, cookies, authorization, password, secret, api keys, access/refresh/id tokens, private key, connection string and email;
- redaction idempotence;
- artifact/report secret guard;
- naming and runtime guardrails.

## Validations Executed

Required validation plan:

- `dotnet restore .\OneBrain.slnx`.
- `dotnet build .\OneBrain.slnx`.
- filtered tests for `CoreRuntimeRegistryEventBusRedaction|NewTopicsIntake|NamingCleanup`.
- full suite: `dotnet test .\OneBrain.slnx --no-build --no-restore`.

## Guardrails Confirmed

- NODAL OS remains the operational project name.
- No NODRIX/HOTEP operational naming introduced.
- No NEXA operational naming introduced in new M468-M470 files.
- No runtime execution introduced.
- No UI introduced.
- No cloud introduced.
- No LLM provider calls introduced.
- No browser automation introduced.
- No scheduler or worker introduced.
- No recorder or replay introduced.
- No DSL parser runtime introduced.
- No shell/subprocess introduced.
- No external RPA dependency introduced.

## Not Implemented

- UI or Mission Control visual implementation.
- Runtime executor.
- Scheduler.
- Worker runtime.
- Browser automation.
- Recorder/replay.
- Queue.
- DSL parser runtime.
- Cloud.
- API/HTTP/gRPC.
- LLM provider/BYOK calls.
- Recipe/step execution.
- Persistence DB.

## Risks And Pending Work

- Execution Registry is in-memory/foundation-only; productive persistence remains future work.
- EventBus is in-memory/foundation-only; durable event store remains future work.
- Timeline projection is contract-compatible but not UI-bound.
- Approval Center data model and evidence registry integration remain next-step work.
- Runtime-gated Recipe Risk Classifier hardening remains blocking before any automation runtime.

## Updated Percentages

- NODAL OS global: 95.5-96%.
- Agent Operations / Automation Layer: 97%.
- Core Runtime subphase: 55% -> 66%.
- Evidence/Timeline foundation: 50% -> 60%.
- Redaction/Safety foundation: ~70%.
- Mission Control UX: 20%.
- LLM/Assignment: 25%.
- Productization: 30%.
- Cloud optional: 10%.
- Automation future: 35%.

## Next Recommended Block

`M471+M472+M473 - Approval Center Data Model + Timeline Projection + Evidence Registry Integration`.

## Final Decision

`M468+M469+M470 CERRADO / CORE_RUNTIME_REGISTRY_EVENTBUS_REDACTION_FOUNDATION_READY`
