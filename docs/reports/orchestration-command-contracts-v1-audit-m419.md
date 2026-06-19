# Orchestration Command Contracts V1 Audit M419

Project: NODAL OS

Milestone: M419-M421

Base commit: 266dad9

## Scope

This audit maps existing Agent Operations contracts and core services into internal Orchestration Command Contracts V1. It does not implement an API, HTTP endpoint, gRPC endpoint, controller, scheduler, orchestration engine, worker runtime, recipe execution, skill execution, step execution, UI, persistence DB, browser action, or desktop action.

## Existing Concepts

| Existing concept | Current contract/service | Reuse decision |
| --- | --- | --- |
| Mission/task | `NexaMission`, `NexaAgentTask`, workboard validator | Referenced by IDs in command envelopes. No execution semantics added. |
| Run report | `NexaRunReport`, failure taxonomy | Reused conceptually for `GetRunReport` and command result failure kinds. |
| Progress report | `NodalOsAgentProgressReport` | Reused conceptually for `GetProgressReport`. |
| Verification before done | `NodalOsVerificationBeforeDoneGate` | Referenced by `EvaluateVerificationBeforeDone`; no duplicated close logic. |
| Recipe manifest | `NodalOsRecipeManifest` | Referenced by `ValidateRecipeManifest` and `PrepareRun`; approval remains manifest-policy only. |
| Step library | `NodalOsStepDefinition` | Referenced as governance metadata only; no step execution. |
| Package/Skill manifest | `NodalOsPackageManifest`, `NodalOsSkillManifest` | Referenced by package/skill IDs and validation commands. |
| Internal skill registry | `NodalOsInternalSkillRegistrySnapshot` | Referenced by `RegisterPackageSnapshot` and `QuerySkillRegistry`; visibility is metadata only. |
| Worker boundary | `NodalOsWorkerBoundaryManifest`, request/response envelopes | Referenced by `PrepareWorkerRequest`; worker runtime remains absent. |
| Evidence bridge | `NodalOsEvidenceBridgeRef`, `NodalOsEvidenceRefBridge` | Used directly for command and result evidence refs. |
| Redaction | `NodalOsRedactionService` | Used by validator and serializer to detect/sanitize secret-like content. |

## Command Candidates

- `CreateMission`
- `CreateTask`
- `PrepareRun`
- `ValidateRecipeManifest`
- `ValidateSkill`
- `RegisterPackageSnapshot`
- `QuerySkillRegistry`
- `PrepareWorkerRequest`
- `GetRunStatus`
- `PauseRun`
- `ResumeRun`
- `CancelRun`
- `RequestHumanDecision`
- `AttachEvidence`
- `GetRunReport`
- `GetProgressReport`
- `EvaluateVerificationBeforeDone`

## Reusable Contracts

- `NodalOsEvidenceBridgeRef` for evidence crossing the command boundary.
- `NexaFailureKind` for non-executing command result failure metadata.
- Runtime-deferred flags already established in package, registry, worker, recipe, and step contracts.
- Common redaction and evidence bridge validation from AgentOperations.Core.

## Added In This Milestone

- `NodalOsOrchestrationCommandKind`.
- `NodalOsOrchestrationState`.
- `NodalOsOrchestrationCommandRiskLevel`.
- `NodalOsOrchestrationCommandEnvelope`.
- `NodalOsOrchestrationCommandResult`.
- `NodalOsOrchestrationCommandValidationResult`.
- Command validator.
- JSON serializer with redaction.
- Fixtures.
- No-divergence tests.

## What Is Not Implemented

- No API implementation.
- No HTTP/gRPC.
- No controller or endpoint.
- No scheduler.
- No orchestration engine.
- No runtime executor.
- No worker runtime.
- No recipe, skill, or step execution.
- No UI.
- No persistence DB.

## Risks

1. Consumers could misread a command envelope as executable work. The validator rejects `RuntimeExecutionAllowed=true` and requires `RuntimeExecutionDeferred=true`.
2. Consumers could misread `Accepted` or `Completed` as runtime execution. Result validation keeps `Executed=false` and documents completed-as-contract-handling-only.
3. `PauseRun`, `ResumeRun`, and `CancelRun` could imply an active runtime state machine. V1 emits contract-only warnings and does not implement a transition engine.
4. Evidence could cross the command boundary with authority or sensitive content. The validator delegates to `NodalOsEvidenceRefBridge` and scans evidence fields through common redaction.

## Location Decision

Contracts are placed in `src/OneBrain.AgentOperations.Contracts/NodalOsOrchestrationCommandContracts.cs`. Services are placed in `src/OneBrain.AgentOperations.Core/NodalOsOrchestrationCommandServices.cs`.

The physical project boundary follows the Agent Operations extraction. The namespace remains the compatibility namespace currently used by extracted Agent Operations types.

## Naming

All new public types use the `NodalOs*` prefix. No new `Nexa*` types are introduced.

## Audit Decision

No boundary conflict was found. Orchestration Command Contracts V1 can be added as internal contract-only metadata with implementation deferred and without runtime behavior change.
