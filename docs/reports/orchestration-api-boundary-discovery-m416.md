# Orchestration API Boundary Discovery M416

Project: NODAL OS

Milestone: M416-M418

Base commit: c2bea3d

## Scope

This discovery report audits existing NODAL OS concepts that would feed a future internal Orchestration API. It does not implement an API, endpoint, scheduler, orchestration engine, worker runtime, recipe execution, skill execution, step execution, UI, or persistence.

## Existing Concepts

| Area | Current assets | Current meaning |
| --- | --- | --- |
| Mission and task domain | `NexaMission`, `NexaAgentTask`, workboard validation | Internal Agent Operations planning and task-state metadata. |
| Run reporting | `NexaRunReport`, run steps, failure taxonomy | Reporting and troubleshooting output, not an execution authority. |
| Progress reporting | `NodalOsAgentProgressReport`, ready-to-close validation | Operator-facing progress metadata with canonical completion gate alignment. |
| Verification before done | `NodalOsVerificationBeforeDoneGate` | Canonical close/completion decision source. |
| Recipe manifest | `NodalOsRecipeManifest` | Manifest-policy validation only; runtime execution remains deferred. |
| Step library | `NodalOsStepDefinition` and step validation | Catalog/governance metadata only; step execution remains deferred. |
| Package and skill manifest | `NodalOsPackageManifest`, `NodalOsSkillManifest` | Internal package/skill catalog metadata; no registry, install, or runtime. |
| Internal skill registry | `NodalOsInternalSkillRegistrySnapshot` | In-memory/catalog snapshot metadata; visibility does not grant execution permission. |
| Worker boundary | `NodalOsWorkerBoundaryManifest`, worker request/response envelopes | Contract-only future worker boundary; worker runtime is not implemented. |
| Evidence bridge | `NodalOsEvidenceBridgeRef`, `NodalOsEvidenceRefBridge` | No-authority evidence mapping and validation. Evidence cannot approve actions. |
| Common redaction | `NodalOsRedactionService`, `NodalOsSensitiveContentClassifier` | Shared secret detection/redaction for Agent Operations contracts and services. |
| Browser adapter boundary | `OneBrain.BrowserExecutor.Cdp` consuming Agent Operations Core | Temporary browser adapter host. AgentOperations.Core and Contracts remain Browser/CDP-free. |

## What Already Exists For Future Orchestration

- Agent Operations contracts are physically separated in `OneBrain.AgentOperations.Contracts`.
- Agent Operations pure services are physically separated in `OneBrain.AgentOperations.Core`.
- Browser/CDP runtime classes remain in `OneBrain.BrowserExecutor.Cdp`.
- Package, registry, and worker metadata consistently preserve `RuntimeExecutionAllowed=false`, `RuntimeExecutionDeferred=true`, and `RequiresGlobalPolicyEvaluation=true`.
- Worker responses can carry evidence bridge refs and failure kinds, while `Executed=false` remains required in V1.
- Verification-before-done is canonical for completion semantics.
- Evidence bridge and redaction services provide reusable safety gates for future orchestration contracts.

## What Is Missing

- Internal orchestration command contracts.
- Orchestration state model contracts.
- In-process orchestration facade.
- Policy-gate integration contract.
- Human approval request/decision contract.
- Evidence attachment flow contract.
- Dry-run-only orchestration model.
- Persistence model for orchestration state.
- Scheduler.
- Worker runtime.
- HTTP, gRPC, or other API surface.
- UI integration.

## Risks

1. A future consumer could misread registry visibility, recipe approval, skill approval, worker health, or catalog-policy pass as runtime permission.
2. Evidence refs could be treated as authority if no-authority semantics are not restated at the orchestration boundary.
3. An API boundary could drift into execution before policy, approval, evidence, and verification gates are formalized.
4. A UI could be allowed to decide instead of display/request human decisions.
5. Browser/CDP coupling could re-enter AgentOperations.Core if the orchestration boundary is not explicit.
6. Scheduler semantics could imply unattended execution before the project approves worker/runtime gates.

## Orchestration API Versus Runtime Execution

A future Orchestration API is a coordination and state-boundary layer. It can validate, prepare, query, request decisions, attach evidence, and expose reports. It cannot click, type, submit, upload, download, execute workers, execute skills, run recipes, run steps, schedule real runs, or grant runtime permission.

Runtime execution remains deferred until a future supervised implementation explicitly passes global policy, human approval where required, evidence requirements, and verification gates.

## Orchestration API Versus UI

A future UI may display orchestration state, evidence, blockers, and human-decision prompts. UI must not become the authority. Core policy, approval, evidence, and verification gates remain authoritative.

## Orchestration API Versus Worker Runtime

The existing worker boundary is declarative. Worker health and registration can support future readiness checks, but they do not imply executable commands. A worker runtime is a separate future system and is not implemented in this milestone.

## What Not To Implement Now

- No API implementation.
- No HTTP endpoint.
- No gRPC endpoint.
- No controller.
- No scheduler.
- No orchestration engine.
- No worker runtime.
- No recipe execution.
- No skill execution.
- No step execution.
- No UI.
- No persistence DB.
- No cloud runtime.
- No browser or desktop actions.
- No namespace move.
- No broad rename.
- No policy behavior changes.

## Discovery Decision

The existing Agent Operations layer is ready for an Orchestration API Architecture Decision Record. Implementation must remain deferred. The next safe step is ADR plus tests/artifact asserting that policy, approval, evidence, and verification gates remain authoritative and that visibility/health/approval metadata cannot grant runtime permission.
