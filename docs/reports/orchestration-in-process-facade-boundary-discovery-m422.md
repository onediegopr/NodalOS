# Orchestration In-Process Facade Boundary Discovery M422

Project: NODAL OS

Milestone: M422-M424

Base commit: 46bb260

## Scope

This discovery report audits existing NODAL OS concepts that would feed a future internal Orchestration In-Process Facade. It does not implement a facade, command dispatcher, runtime engine, state machine, execution engine, HTTP/gRPC API, scheduler, worker runtime, recipe/skill/step execution, browser/desktop action, UI, or persistence. It is a precondition document for the Orchestration In-Process Facade Decision Record.

## 1. Existing Concepts

| Area | Current assets | Current meaning |
| --- | --- | --- |
| Orchestration command contracts | `NodalOsOrchestrationCommandEnvelope`, `NodalOsOrchestrationCommandResult`, `NodalOsOrchestrationCommandValidator` | Contract-only command/result envelopes with no-execution invariant baked in. |
| Orchestration command validation | `ValidateCommand`, `ValidateResult`, `ValidateNoRuntimeExecution`, `ValidateEvidenceRefs` | Pure validation; forces `RuntimeExecutionAllowed=false`, `RuntimeExecutionDeferred=true`, `RequiresGlobalPolicyEvaluation=true`. |
| Command serialization/redaction | `NodalOsOrchestrationCommandJsonSerializer` | Redacts every command/result/evidence field on serialize via `NodalOsRedactionService`. |
| Mission and task domain | mission/task contracts, workboard validation | Internal Agent Operations planning and task-state metadata. |
| Run reporting | `NodalOsRunReport` family, failure taxonomy | Reporting/troubleshooting output, not an execution authority. |
| Progress reporting | `NodalOsAgentProgressReport`, ready-to-close validation | Operator-facing progress metadata aligned to the canonical completion gate. |
| Verification before done | `NodalOsVerificationBeforeDoneGate` | Canonical close/completion decision source. |
| Recipe manifest | `NodalOsRecipeManifest` | Manifest-policy validation only; runtime execution deferred. |
| Step library | `NodalOsStepDefinition` and validation | Catalog/governance metadata only; step execution deferred. |
| Package and skill manifest | `NodalOsPackageManifest`, `NodalOsSkillManifest` | Internal catalog metadata; no install or runtime. |
| Internal skill registry | `NodalOsInternalSkillRegistrySnapshot`, validator, query | Catalog snapshot metadata; visibility does not grant execution permission. |
| Worker boundary | `NodalOsWorkerBoundaryManifest`, request/response envelopes, capability mapper | Contract-only worker boundary; worker runtime not implemented. Response evidence validated via bridge. |
| Evidence bridge | `NodalOsEvidenceBridgeRef`, `NodalOsEvidenceRefBridge.ValidateBridgeRef` | No-authority evidence mapping/validation. Evidence cannot approve actions. |
| Common redaction | `NodalOsRedactionService` | Shared secret detection/redaction for Agent Operations contracts/services. |
| Browser adapter boundary | `OneBrain.BrowserExecutor.Cdp` consuming `AgentOperations.Core` | Temporary browser adapter host. Core and Contracts remain Browser/CDP-free. |

## 2. What Already Exists (Facade Building Blocks)

- A complete set of orchestration command contracts with the no-execution invariant already enforced in validators.
- Per-command shape validation (`PrepareRun`, `PrepareWorkerRequest`, `AttachEvidence`, `QuerySkillRegistry`, pause/resume/cancel warnings).
- Evidence refs validated through the bridge and scanned for sensitive content per field.
- Redaction applied on serialization of every command/result.
- Canonical completion gate (`NodalOsVerificationBeforeDoneGate`) and reporting surfaces (RunReport/ProgressReport).
- Clean module separation: `AgentOperations.Contracts` (leaf) -> `AgentOperations.Core` -> `BrowserExecutor.Cdp` (adapter host). Core/Contracts are Browser/CDP-free.

## 3. What Is Missing (Intentionally, Until A Future Gated Milestone)

- An in-process coordinator that accepts a command, runs the relevant validators/builders, and returns a `NodalOsOrchestrationCommandResult` (no execution).
- A single structural location for the no-execution invariant (today it lives in each validator, not in one dispatch boundary).
- A documented contract for how the facade composes policy -> approval -> evidence -> verification gates.
- A dry-run preparation surface (future phase).
- Any runtime/state-machine/engine, scheduler, API transport, worker runtime, or UI (all out of scope by principle).

## 4. Risks Of A Facade

- **Runtime-shaping drift**: command names (`PrepareRun`, `PauseRun`, `ResumeRun`, `CancelRun`, `GetRunStatus`) and reserved states (`RunningFuture`, `PausedFuture`) read like a runtime engine. A future implementer could wire an engine behind them without re-deriving gates.
- **Gate dispersion**: gates live inside individual validators. A facade that bypasses a validator could skip a gate. The facade must compose the existing gates, never reimplement or shortcut them.
- **Semantic confusion**: `Accepted`/`Completed` could be misread as "executed". The facade must preserve the documented meanings.
- **Authority leakage**: registry `Visible`, worker `Healthy`, skill/recipe `Approved`, `CanPassCommandPolicy`/`CanPassBoundaryPolicy` could be misinterpreted as runtime permission.
- **Module bleed**: browser/CDP/worker-runtime code could leak into `AgentOperations.Core` through the facade. Core must remain Browser/CDP-free.

## 5. Facade vs Runtime Engine

A runtime engine transitions live state and performs actions. The facade coordinates **validators and contracts only**: it accepts a command, validates it, optionally builds a result/report, and returns it with `Executed=false`. No state machine advances; `RunningFuture`/`PausedFuture` remain reserved names with no behavior.

## 6. Facade vs HTTP/gRPC API

An API is a transport/exposure layer (endpoints, controllers, serialization over the wire, authn/z). The facade is an **in-process .NET coordination type** with no transport, no endpoint, no controller, and no network surface. Exposure over any transport is a later, separately gated phase.

## 7. Facade vs Scheduler

A scheduler triggers runs on time/events. The facade has **no timer, no queue, no background worker, no trigger**. It is invoked synchronously and in-process by a caller; it never schedules or self-triggers anything.

## 8. Facade vs UI

UI displays state and requests human approvals; it does not decide. The facade produces no UI and renders nothing. A future UI may display facade outputs and request approvals, but policy decides and the facade never authorizes.

## 9. Invariants To Protect

- `Executed=false` always; `RuntimeExecutionAllowed=false`; `RuntimeExecutionDeferred=true`; `RequiresGlobalPolicyEvaluation=true`.
- No command may trigger an external/browser/desktop/worker action.
- Policy gate before any preparation; approval gate before any future sensitive action; evidence gate over `EvidenceRefBridge`; `NodalOsVerificationBeforeDoneGate` remains the canonical done-success source.
- Common redaction applies to inputs and outputs.
- `Accepted` means accepted for validation/handling; `Completed` means contract handling completed only; neither means executed.
- Registry `Visible`, worker `Healthy`, skill `Approved`, recipe `Approved`, and command `Accepted` do not grant runtime permission.
- `AgentOperations.Core`/`.Contracts` remain Browser/CDP-free; `BrowserExecutor.Cdp` remains the temporary adapter host.
