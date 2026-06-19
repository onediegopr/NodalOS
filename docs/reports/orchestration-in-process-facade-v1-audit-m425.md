# Orchestration In-Process Facade V1 Audit M425

Project: NODAL OS

Milestone: M425

Base commit: 69f0a8a

## 1. Existing Services Reusable

### NodalOsOrchestrationCommandValidator
- File: `src/OneBrain.AgentOperations.Core/NodalOsOrchestrationCommandServices.cs`
- Validates command envelope, result, no-runtime-execution invariant, evidence refs, sensitive content.
- Constructor chain: default -> redaction -> redaction + evidenceBridge.
- Can accept `NodalOsRedactionService` and `NodalOsEvidenceRefBridge` as injected dependencies.
- Return type: `NodalOsOrchestrationCommandValidationResult`.

### NodalOsEvidenceRefBridge
- File: `src/OneBrain.AgentOperations.Core/NodalOsEvidenceRefBridgeServices.cs`
- `ValidateBridgeRef(NodalOsEvidenceBridgeRef, NodalOsEvidenceBridgeOptions?)` validates evidence refs.
- Returns `NodalOsEvidenceBridgeResult` with `Accepted`, `Errors`, `Warnings`.

### NodalOsRedactionService
- File: `src/OneBrain.AgentOperations.Core/NodalOsRedactionServices.cs`
- `RedactValue(string?, NodalOsRedactionOptions?)` redacts sensitive content.
- `RedactField(string, string?, NodalOsRedactionOptions?)` redacts by field name.
- `ContainsSensitiveContent(string?)` detects sensitive content.

### NodalOsOrchestrationCommandFixtures
- File: `src/OneBrain.AgentOperations.Core/NodalOsOrchestrationCommandServices.cs`
- Provides factory methods for all command kinds and result types.
- Used by existing tests for data creation.

### NodalOsOrchestrationCommandJsonSerializer
- File: `src/OneBrain.AgentOperations.Core/NodalOsOrchestrationCommandServices.cs`
- Serializes/deserializes commands and results with redaction.

### NodalOsVerificationBeforeDoneGate
- File: `src/OneBrain.AgentOperations.Core/NodalOsVerificationBeforeDoneGate.cs`
- Evaluates task/mission/run completion.
- Remains canonical done-success source (referenced, not duplicated).

### NodalOsRunReportBuilder
- File: `src/OneBrain.AgentOperations.Core/NodalOsRunReportingServices.cs`
- Builds run reports with steps, failures, policy/approval/evidence.
- Available for future coordination but not required in V1 facade.

### NodalOsAgentProgressReportBuilder
- File: `src/OneBrain.AgentOperations.Core/NodalOsAgentProgressReportingServices.cs`
- Builds progress reports with blockers, decisions, verification summaries.
- Available for future coordination.

## 2. Existing Contracts

All contracts live in `src/OneBrain.AgentOperations.Contracts/`:

| Contract File | Key Types |
|---|---|
| `NodalOsOrchestrationCommandContracts.cs` | `NodalOsOrchestrationCommandEnvelope`, `NodalOsOrchestrationCommandResult`, `NodalOsOrchestrationCommandValidationResult`, `NodalOsOrchestrationCommandKind`, `NodalOsOrchestrationState`, `NodalOsOrchestrationCommandRiskLevel` |
| `NodalOsEvidenceRefBridgeContracts.cs` | `NodalOsEvidenceBridgeRef`, `NodalOsEvidenceBridgeResult`, `NodalOsEvidenceBridgeOptions`, bridge enums |
| `NodalOsAgentProgressReportingContracts.cs` | `NodalOsAgentProgressReport`, `NodalOsAgentProgressReportKind`, `NodalOsAgentProgressReportStatus` |

## 3. Invariants to Preserve

1. `Executed=false` always at facade boundary.
2. `RuntimeExecutionAllowed=false` always.
3. `RuntimeExecutionDeferred=true` always.
4. `RequiresGlobalPolicyEvaluation=true` always.
5. No command may trigger external action, browser, desktop, or worker.
6. `Accepted=true` means contract accepted only; does not mean executed.
7. `Completed` means contract handling only; does not mean runtime completion.
8. `RunningFuture` and `PausedFuture` cannot be returned as active runtime states.
9. Evidence refs must validate via `NodalOsEvidenceRefBridge.ValidateBridgeRef`.
10. Common redaction must be applied to inputs/outputs.
11. Policy gate lives before preparation; approval gate before sensitive action; evidence gate over bridge.
12. `NodalOsVerificationBeforeDoneGate` is canonical done-success source.

## 4. Commands Supported in V1

All 17 `NodalOsOrchestrationCommandKind` values are supported for contract handling:

| Command Kind | V1 Contract Handling |
|---|---|
| `CreateMission` | Accept, validate shape, return Prepared |
| `CreateTask` | Accept, validate shape, return Prepared |
| `PrepareRun` | Accept, validate shape, return Prepared |
| `ValidateRecipeManifest` | Accept, validate shape, return Completed |
| `ValidateSkill` | Accept, validate shape, return Completed |
| `RegisterPackageSnapshot` | Accept, return Completed |
| `QuerySkillRegistry` | Accept, warn catalog-only, return Completed |
| `PrepareWorkerRequest` | Accept, validate shape, return Prepared |
| `GetRunStatus` | Accept, return Completed with contract state |
| `PauseRun` | Accept, warn contract-only, return Completed |
| `ResumeRun` | Accept, warn contract-only, return Completed |
| `CancelRun` | Accept, warn contract-only, return Completed |
| `RequestHumanDecision` | Accept, set AwaitingApproval state |
| `AttachEvidence` | Accept, validate evidence refs, return Completed |
| `GetRunReport` | Accept, return Completed |
| `GetProgressReport` | Accept, return Completed |
| `EvaluateVerificationBeforeDone` | Accept, return Completed (no closure) |

## 5. Commands Treated as Contract-Only

- `PauseRun` / `ResumeRun` / `CancelRun` — no runtime state transition engine exists.
- `QuerySkillRegistry` — catalog lookup only; cannot grant runtime permission.
- `ValidateRecipeManifest` — manifest policy validation only; no recipe execution.
- `ValidateSkill` — skill metadata validation only; no skill execution.
- `PrepareWorkerRequest` — contract preparation only; no worker dispatch.
- `PrepareRun` — contract preparation only; no run start.
- `AttachEvidence` — evidence validation only; no action authorization.
- `EvaluateVerificationBeforeDone` — contract evaluation only; no automatic closure.

## 6. Non-Goals

- No API, HTTP, gRPC, controller, endpoint.
- No scheduler, runtime engine, worker runtime.
- No recipe/skill/step execution.
- No browser actions, desktop actions.
- No UI.
- No persistence DB.
- No policy behavior change.
- No `BrowserExecutor.Cdp` dependency.
- No `RunningFuture`/`PausedFuture` as active state.

## 7. Risks

1. **Over-coordination**: Facade might grow beyond coordination into orchestration logic. Must stay dispatch-only.
2. **Evidence bridge bypass**: Evidence validation must happen at facade level, not skipped.
3. **Redaction leakage**: Errors/warnings from validators might contain unredacted sensitive content.
4. **State creep**: Returning states beyond the contract-handling boundary (e.g., actually signaling runtime readiness).
5. **Test coupling**: Tests must not create actual runtime dependencies or call browser/worker code.

## 8. Implementation Strategy

1. Create `NodalOsOrchestrationInProcessFacade` in `OneBrain.AgentOperations.Core`.
2. Accept `NodalOsOrchestrationCommandEnvelope` as input.
3. Validate via `NodalOsOrchestrationCommandValidator`.
4. Validate evidence refs via `NodalOsEvidenceRefBridge.ValidateBridgeRef`.
5. Apply redaction via `NodalOsRedactionService.RedactValue` on errors/warnings.
6. Return `NodalOsOrchestrationCommandResult` with `Executed=false`, `RuntimeExecutionDeferred=true`.
7. Invalid commands return `Accepted=false`, `State=Blocked`.
8. Valid commands return `Accepted=true`, state per command kind.
9. High/Critical risk without approval returns `Blocked` with policy error.
10. Evidence validation failure returns `Blocked`.
11. Sensitive summary detected by the command validator returns `Accepted=false`, `Executed=false`, and `State=Blocked`; no raw sensitive value is exposed.
12. Pause/Resume/Cancel return `Completed` with contract-only warning.
13. No `RunningFuture`/`PausedFuture` anywhere in V1 facade results.

Architecture: Single `Dispatch` method, pure coordination, no runtime, no side effects, no I/O.
