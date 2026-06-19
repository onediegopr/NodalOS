# Orchestration API Decision Record

Project: NODAL OS

Milestone: M416-M418

Status: Accepted for design, implementation deferred.

## Context

NODAL OS now has separated Agent Operations contracts and core services. `OneBrain.AgentOperations.Contracts` and `OneBrain.AgentOperations.Core` are Browser/CDP-free, while `OneBrain.BrowserExecutor.Cdp` remains the temporary browser adapter host. Package/Skill Manifest V1, Internal Skill Registry V1, Worker Boundary Contract V1, EvidenceRef bridge, common redaction, run reporting, progress reporting, and verification-before-done are available as contract-only or core-service foundations.

There is still no Orchestration API, worker runtime, recipe execution, skill execution, step execution, UI, scheduler, registry persistence DB, or marketplace.

## Problem

The next architectural boundary must define how future orchestration coordinates missions, tasks, manifests, registry metadata, worker boundaries, evidence, approvals, reports, and completion gates without accidentally granting runtime permission or introducing execution before safety gates are approved.

## Decision

Create an internal Orchestration API architecture decision only. The API is conceptual in this milestone. No HTTP endpoint, gRPC endpoint, controller, scheduler, orchestration engine, worker runtime, execution adapter, UI, or persistence is implemented.

The future Orchestration API cannot grant runtime permission. Global policy, approval gates, evidence gates, redaction, no-authority evidence semantics, and `NodalOsVerificationBeforeDoneGate` remain authoritative.

Registry `Visible`, worker `Healthy`, skill `ApprovedForCatalog`, recipe `Approved`, `CanPassCatalogPolicy`, and `CanPassBoundaryPolicy` do not mean executable.

## Scope

- Define future internal orchestration responsibility.
- Define conceptual commands.
- Define a future state model.
- Define policy, approval, evidence, reporting, and verification gates.
- Define relationships with Agent Operations, Browser Adapter, Worker Boundary, Registry, Recipe, Step, Skill, RunReport, and ProgressReport.
- Define implementation phases.
- Preserve internal-only scope.

## Non-Goals

- No API implementation.
- No HTTP endpoint.
- No gRPC endpoint.
- No controller.
- No scheduler.
- No worker runtime.
- No orchestration engine.
- No recipe execution.
- No skill execution.
- No step execution.
- No UI.
- No persistence DB.
- No cloud runtime.
- No marketplace.
- No package install or uninstall.
- No browser actions.
- No desktop actions.
- No namespace move.
- No broad rename.
- No policy behavior changes.

## Future Conceptual Commands

The following names are Conceptual only. They are not implemented in this milestone. They are not HTTP endpoints. They do not imply execution.

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

No execution is authorized by these conceptual commands. Future command contracts must return policy/evidence/approval status explicitly and must preserve runtime execution deferred semantics until a later gated implementation is approved.

## Future State Model

The future state model is conceptual and internal-only:

- `Draft`
- `Prepared`
- `AwaitingPolicy`
- `AwaitingApproval`
- `ReadyForDryRun`
- `DryRunPrepared`
- `RunningFuture`
- `PausedFuture`
- `Blocked`
- `Completed`
- `Failed`
- `Cancelled`

`RunningFuture` and `PausedFuture` are reserved names for a later approved runtime phase. They do not exist as runtime behavior now. Action execution remains deferred.

## Policy Gates

- Global policy evaluation is required before any future action-capable phase.
- Catalog approval is not policy authorization.
- Registry visibility is not policy authorization.
- Worker health is not policy authorization.
- Recipe approval is not policy authorization.
- Skill approval is not policy authorization.
- Policy failures must block progression to any future execution-capable state.

## Approval Gates

- Human approval is required for sensitive, high-risk, critical, file-transfer, data-entry, interaction, credential, payment, signing, deletion, or externally mutating actions.
- Human approval requests must be explicit future orchestration objects.
- UI may display/request a decision, but UI does not decide.
- Human approval does not bypass global policy, evidence requirements, or verification-before-done.

## Evidence Gates

- Evidence requirements must be explicit before future execution-capable phases.
- Evidence cannot authorize actions.
- Evidence bridge refs must preserve no-authority semantics.
- Evidence sensitivity and redaction state must be validated before crossing an orchestration boundary.
- Raw secrets, cookies, tokens, private bodies, and unredacted sensitive content are not allowed in orchestration payloads.

## Verification Before Done

`NodalOsVerificationBeforeDoneGate` remains the canonical close/completion gate.

- Pending required verification blocks completion.
- Failed required verification blocks completion.
- Blocking and critical blockers block completion.
- Completion requires evidence or completion reason according to existing Agent Operations semantics.
- A future Orchestration API must call or reflect this gate rather than duplicate completion rules.

## Relationship With Agent Operations

Agent Operations remains the source for mission/task contracts, run reports, progress reports, recipe/step/package/skill/registry/worker contracts, redaction, evidence bridge, and verification-before-done. A future Orchestration API coordinates these services; it does not replace their validation semantics.

## Relationship With Browser Adapter

`OneBrain.BrowserExecutor.Cdp` remains the temporary browser adapter host. Browser/CDP code must not move into AgentOperations.Core. A future browser adapter may consume orchestration commands, but browser-specific code must remain outside contracts and core services.

## Relationship With Worker Boundary

Worker Boundary Contract V1 is declarative and contract-only. Worker status, health, capabilities, request envelopes, and response envelopes do not imply runtime execution. Worker runtime remains a separate future phase.

## Relationship With Recipe, Step, Skill, Package Registry

- Recipe manifest approval means manifest-policy pass only.
- Step library availability means catalog/governance availability only.
- Skill approval means approved for catalog only.
- Package approval means approved for catalog only.
- Registry visibility means discoverable metadata only.
- None of these grant runtime permission.

## Relationship With RunReport And ProgressReport

RunReport and ProgressReport are reporting surfaces for future orchestration state and outcomes. They must remain consistent with failure taxonomy, evidence bridge, common redaction, blocker/progress reporting, and verification-before-done. Reporting cannot create authority, approve actions, or bypass policy.

## Security And No-Authority Rules

- Internal-only scope is required until a future ADR approves broader exposure.
- Orchestration payloads must use common redaction.
- Evidence remains no-authority.
- Policy decides.
- Verification closes.
- Human approval unlocks only when policy and evidence also allow.
- UI displays and requests decisions; it does not decide.
- Browser and worker adapters execute only in a future approved runtime phase.

## Implementation Phases

Phase 0: ADR and invariants.

Phase 1: Internal command contracts only.

Phase 2: In-process orchestration facade with no execution.

Phase 3: Dry-run only.

Phase 4: Supervised low-risk internal execution.

Phase 5: UI and adapter exposure.

Phase 6: Scheduled read-only runs.

Phase 7: Worker runtime, only if explicitly approved.

## Acceptance Criteria

- Orchestration API ADR exists.
- Boundary discovery report exists.
- No API, HTTP, gRPC, controller, scheduler, worker runtime, execution, UI, or persistence is implemented.
- Policy gate is required.
- Approval gate is required.
- Evidence gate is required.
- Verification-before-done is required.
- Registry visibility does not grant runtime permission.
- Worker health does not grant runtime permission.
- Recipe approval does not grant runtime permission.
- Skill approval does not grant runtime permission.
- No-authority evidence semantics are preserved.
- Internal-only scope is preserved.
