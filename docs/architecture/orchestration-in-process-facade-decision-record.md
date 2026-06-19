# Orchestration In-Process Facade Decision Record

Project: NODAL OS

Milestone: M422-M424

Status: Accepted for design, implementation deferred (execution deferred).

## 1. Context

NODAL OS has separated Agent Operations contracts and core services. `OneBrain.AgentOperations.Contracts` and `OneBrain.AgentOperations.Core` are Browser/CDP-free, while `OneBrain.BrowserExecutor.Cdp` remains the temporary browser adapter host. Orchestration Command Contracts V1 (`NodalOsOrchestrationCommandEnvelope`, `NodalOsOrchestrationCommandResult`, `NodalOsOrchestrationCommandValidator`) exist with the no-execution invariant enforced in validators. Package/Skill Manifest V1, Internal Skill Registry V1, Worker Boundary Contract V1, EvidenceRef bridge, common redaction, run/progress reporting, and `NodalOsVerificationBeforeDoneGate` are available.

There is still no orchestration facade, command dispatcher, runtime engine, state machine, execution engine, HTTP/gRPC API, scheduler, worker runtime, recipe/skill/step execution, UI, or persistence.

## 2. Problem

A future in-process facade must coordinate the existing validators, builders, and contracts behind a single entry point without becoming a runtime engine, without dispersing or shortcutting safety gates, and without accidentally granting runtime permission. The boundary, invariants, and gate locations must be fixed in design before any facade code is written.

## 3. Decision

Define an internal Orchestration In-Process Facade as a future, design-only concept. The facade, when later implemented under a gated milestone, is an in-process .NET coordination type that accepts an orchestration command, runs the relevant existing validators/builders, and returns a `NodalOsOrchestrationCommandResult` (and optionally RunReport/ProgressReport) with `Executed=false`. It coordinates; it does not execute, authorize, schedule, or expose a transport.

The facade does not govern policy, does not replace gates, does not execute or authorize actions, does not skip approval, evidence, or verification-before-done, and does not convert worker `Healthy`, registry `Visible`, skill `Approved`, recipe `Approved`, or command `Accepted` into runtime permission. `NodalOsVerificationBeforeDoneGate` remains the canonical completion gate; policy decides; evidence registers; verification closes; human approval unlocks sensitive actions only when policy and evidence also allow.

## 4. Scope

- Define the future facade purpose, boundary, and invariants.
- Define where policy/approval/evidence/verification gates live relative to the facade.
- Define what the facade may coordinate and what it must never do.
- Define facade relationships with command contracts, AgentOperations.Core, Browser Adapter, Worker Boundary, Registry/Recipe/Skill/Step, RunReport/ProgressReport, EvidenceRefBridge, and CommonRedaction.
- Define state semantics and the meaning of `Accepted`/`Completed`/`Executed`.
- Define future implementation phases and acceptance criteria.
- Preserve internal-only scope.

## 5. Non-Goals

- No facade implementation.
- No command dispatcher implementation.
- No runtime/state-machine/execution engine.
- No HTTP endpoint, gRPC endpoint, or controller.
- No scheduler.
- No worker runtime.
- No recipe execution, skill execution, or step execution.
- No browser actions or desktop actions.
- No UI.
- No persistence DB.
- No cloud runtime.
- No package install or uninstall.
- No namespace migration.
- No broad rename.
- No OCR changes.
- No policy behavior changes.

## 6. No-Execution Invariant

- The future facade V1 must return `Executed=false` always.
- `RuntimeExecutionAllowed=false` must be maintained.
- `RuntimeExecutionDeferred=true` must be maintained.
- `RequiresGlobalPolicyEvaluation=true` must be maintained.
- No command may trigger any external, browser, desktop, or worker action.
- The invariant must be enforced structurally at the facade boundary (a single chokepoint), not only inside each validator. The facade must refuse to emit any result with `Executed=true` or `RuntimeExecutionAllowed=true`.

## 7. Gate Location

- **Policy gate** lives before any preparation. No command may advance toward a future action-capable phase without global policy evaluation.
- **Approval gate** lives before any future sensitive action. High/Critical risk commands require `RequiresHumanApproval`. UI may display/request a decision; UI does not decide.
- **Evidence gate** lives over `NodalOsEvidenceRefBridge`. Every evidence ref crossing the facade is validated via `ValidateBridgeRef` and scanned for sensitive content; evidence is no-authority.
- **Verification-before-done gate**: `NodalOsVerificationBeforeDoneGate` remains the canonical done-success source. The facade calls or reflects it; it never duplicates completion rules.
- **Common redaction** is applied to facade inputs and outputs.

The facade composes these existing gates in order; it never reimplements, weakens, or bypasses them.

## 8. Dispatch Boundary

- The future facade may coordinate existing validators and builders.
- The future facade may produce a `NodalOsOrchestrationCommandResult`.
- The future facade may produce RunReport/ProgressReport surfaces.
- The future facade may not execute browser, desktop, or worker actions.
- The future facade may not open or invoke a scheduler.
- The future facade may not perform network/transport I/O.
- The future facade may not persist to a database.

## 9. State Semantics

- Permitted contractual states: `Draft`, `Prepared`, `AwaitingPolicy`, `AwaitingApproval`, `ReadyForDryRun`, `DryRunPrepared`.
- `RunningFuture` and `PausedFuture` remain future-only reserved names with no runtime behavior.
- `Completed` means "contract handling completed", not "runtime action completed".
- `Accepted` means "accepted for validation/handling", not "executed".
- `Executed=false` is required in V1 and remains false at the facade boundary.

## 10. Relationship With Command Contracts

The facade consumes `NodalOsOrchestrationCommandEnvelope` and emits `NodalOsOrchestrationCommandResult`. It relies on `NodalOsOrchestrationCommandValidator` for command/result/evidence validation and the no-runtime-execution checks. The facade adds coordination, never new authority.

## 11. Relationship With AgentOperations.Core

`AgentOperations.Core` provides the validators and builders the facade coordinates. The facade lives in (or above) Core and must remain Browser/CDP-free. The facade does not replace Core validation semantics; it sequences them.

## 12. Relationship With Browser Adapter

`OneBrain.BrowserExecutor.Cdp` remains the temporary browser adapter host. Browser/CDP code must not move into AgentOperations.Core via the facade. A future browser adapter may consume facade outputs, but browser-specific code stays outside contracts and core services.

## 13. Relationship With Worker Boundary

Worker Boundary Contract V1 remains declarative and contract-only. Worker status, health, capabilities, and request/response envelopes do not imply runtime execution. The facade may prepare a worker request contract (`PrepareWorkerRequest`) but never dispatches a worker. Worker runtime is a separate future phase requiring a dedicated security audit.

## 14. Relationship With Registry/Recipe/Skill/Step

- Recipe manifest approval means manifest-policy pass only.
- Step library availability means catalog/governance availability only.
- Skill approval means approved for catalog only.
- Package approval means approved for catalog only.
- Registry visibility means discoverable metadata only.
- None of these grant runtime permission, and the facade never converts them into permission.

## 15. Relationship With RunReport/ProgressReport

RunReport and ProgressReport are reporting surfaces for orchestration state and outcomes. The facade may produce or update them consistently with failure taxonomy, evidence bridge, common redaction, blocker/progress reporting, and verification-before-done. Reporting cannot create authority, approve actions, or bypass policy.

## 16. Relationship With EvidenceRefBridge/CommonRedaction

Evidence refs crossing the facade must pass `NodalOsEvidenceRefBridge.ValidateBridgeRef` and preserve no-authority semantics. Sensitivity and redaction state are validated before crossing the boundary. Common redaction is applied to facade inputs and outputs. Raw secrets, cookies, tokens, private bodies, and unredacted sensitive content are not allowed in facade payloads.

## 17. UI Implications

The facade produces no UI. A future UI may display facade outputs and request human approvals. UI shows, it does not decide. UI must never interpret `Accepted`, `Completed`, `CanPassCommandPolicy`, registry `Visible`, or worker `Healthy` as runtime permission.

## 18. Future Implementation Phases

- Phase 0 — ADR (this milestone).
- Phase 1 — Facade contracts only, if needed.
- Phase 2 — In-process facade V1, no execution.
- Phase 3 — Dry-run preparation only.
- Phase 4 — Supervised low-risk internal execution, only after a Claude pre-runtime audit.
- Phase 5 — Scheduled read-only runs, only after a separate ADR.
- Phase 6 — UI exposure, display/approval only.
- Phase 7 — Worker runtime, only after a dedicated security audit.

## 19. Acceptance Criteria

- Facade boundary discovery report exists.
- Facade ADR exists.
- No facade, dispatcher, runtime engine, API, HTTP/gRPC, scheduler, worker runtime, recipe/skill/step execution, UI, or persistence is implemented.
- No-execution invariant is defined and structural at the facade boundary.
- Policy gate location is defined.
- Approval gate location is defined.
- Evidence gate location is defined.
- Verification-before-done gate is preserved as canonical.
- `Accepted` does not mean executed; `Completed` means contract handling only.
- `RunningFuture` and `PausedFuture` remain future-only.
- Registry visibility, worker health, skill approval, and recipe approval do not grant runtime permission.
- No-authority evidence semantics and internal-only scope are preserved.
- The document uses the NODAL OS name.
