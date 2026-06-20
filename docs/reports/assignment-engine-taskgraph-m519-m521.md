# M519-M521 - Assignment Engine v1 Contracts + TaskGraph Draft + Planner Readiness Gate

## Executive Summary

M519-M521 prepares Assignment Engine v1 for NODAL OS as draft-only contracts. It does not implement a real planner, prompt creation, LLM calls, task execution, runtime wiring, routing, cloud, or filesystem scan.

Decision target:

`M519+M520+M521 CERRADO / ASSIGNMENT_ENGINE_TASKGRAPH_DRAFT_READY`

## Initial Git State

- Worktree: `C:\DESARROLLO\NodalOS\Codigo-m12-audit`
- Branch: `chrome-lab-001-extension-local-ai-bridge`
- Initial HEAD: `295d9177d8039049e638ba72949a70680e02f67e`
- Initial origin branch HEAD: `295d9177d8039049e638ba72949a70680e02f67e`
- Remote: `https://github.com/onediegopr/NodalOS.git`
- Forbidden path not used: `C:\Users\diego\OneDrive\PERSONAL\ONE Brain\Codigo`

## Objective

- M519: Assignment Engine v1 Contracts.
- M520: TaskGraph Draft.
- M521: Planner Readiness Gate.

## M519 - Assignment Engine v1 Contracts

Implemented:

- Assignment request draft model with workspace, mission, user-provided context refs, Safe Context Boundary refs, Project Understanding readiness refs, prompt governance refs, budget policy refs, model capability refs, planner readiness refs, evidence refs, timeline refs, and guardrail refs.
- Assignment purposes for mission planning, task breakdown, risk review, next steps, handoff planning, expert advisor suggestion, and unknown.
- Assignment states for draft/manual/future LLM eligibility and all required blockers.
- Explicit disclosures that the Assignment Engine runtime is not implemented, no model was called, no prompt was generated, no task is executable, and TaskGraph is draft-only.

## M520 - TaskGraph Draft

Implemented:

- Non-authoritative TaskGraph draft model.
- Draft tasks for analysis, documentation, planning, risk assessment, handoff, advisor suggestion, and future execution placeholder.
- Dependency ids, blocked-by ids, risk levels, disabled capabilities, suggested assignee type, evidence refs, timeline refs, approval refs, context refs, guardrail refs, human review requirement, and readiness gate result.
- Every task has `CanExecute=false`.
- FutureExecutionPlaceholder is blocked.

## M521 - Planner Readiness Gate

Implemented:

- Planner readiness gate model with workspace readiness, project understanding readiness, context validation, prompt governance, budget guardrails, BYOK provider settings, model capability matrix, Safe Context Boundary, evidence refs, timeline refs, and guardrail refs.
- Readiness states for manual draft, context review, future LLM planning with consent, required blockers, positive execution gate blocker, and unknown review.
- Planning modes for manual draft, mock draft, future LLM draft with consent, advisor suggestion, assignment planner, future runtime execution, and not allowed.
- FutureRuntimeExecution is always disabled/blocked.

## No-Planner / No-Prompt / No-LLM / No-Runtime Confirmation

- No real planner.
- No prompt creation.
- No final prompt text generation.
- No LLM provider calls.
- No provider SDK.
- No provider calls.
- No network or HTTP.
- No executable TaskGraph.
- No task execution.
- No runtime.
- No productive dependency dispatch.
- No scheduling or queue.
- No cloud.
- No filesystem scan or mutation.
- No positive execution gate implementation.

## Files Created

- `src/OneBrain.AgentOperations.Contracts/NodalOsAssignmentEngineContracts.cs`
- `src/OneBrain.AgentOperations.Core/NodalOsAssignmentEngineServices.cs`
- `tests/OneBrain.Safety.Tests/NodalOsAssignmentEngineM519M521Tests.cs`
- `docs/reports/assignment-engine-taskgraph-m519-m521.md`
- `artifacts/agent-operations/m521/assignment-engine-taskgraph-summary.json`

## Files Modified

- `docs/roadmap/nodal-os-roadmap-vnext.md`
- `docs/roadmap/nodal-os-unified-roadmap-post-pause.md`

## Tests Added

- Assignment Engine states and no-authority flags.
- Assignment disclosures and refs.
- TaskGraph draft task kinds, dependency refs, approval refs, future LLM/runtime flags, and all tasks non-executable.
- Planner readiness states, modes, and future runtime execution blocker.
- Adversarial redaction checks for assignment request and TaskGraph.
- Boundary checks against runtime, provider, network, prompt, routing, model/cost lookup, real planner, executable graph, filesystem, and telemetry primitives.
- Existing safety continuity through M516-M518.
- Artifact guardrail flags.

## Validations

- `dotnet restore .\OneBrain.slnx`: passed.
- `dotnet build .\OneBrain.slnx`: passed with 0 warnings and 0 errors.
- Filtered test run for `AssignmentEngine|PromptGovernance|ByokProvider|ProjectUnderstandingPolicy|ContextIntakePreview|UserContext|WorkspaceReadinessContext|WorkspaceMetadataHealth|WorkspaceStorageMissionSwitcher|WorkspaceLocalModel|MissionControlVisualPolish|MissionControlGuidance|MissionControlInteractionNoOp|MissionControlShellReadOnly|AuditAPreUiBoundaryNaming|ApprovalUxHandoffObservability|ApprovalTimelineEvidence|CoreRuntimeRegistryEventBusRedaction|NewTopicsIntake|NamingCleanup`: 389 passed, 0 skipped, 0 failed.
- Full suite: 4022 passed, 37 skipped, 0 failed.
- Frontend/Tauri/Rust checks: not applicable; no `package.json` or `Cargo.toml` found in expected repo zones.

## Guardrails Confirmed

- No runtime execution.
- No positive execution gate implementation.
- No browser automation.
- No recorder/replay.
- No queue/scheduler/worker.
- No DSL parser runtime.
- No provider SDK.
- No provider call.
- No network or HTTP.
- No prompt creation.
- No final prompt text generation.
- No LLM routing.
- No token counting real.
- No pricing lookup.
- No model availability lookup.
- No cloud.
- No productive persistence.
- No filesystem mutation.
- No filesystem scan.
- No directory listing.
- No file read/write/delete.
- No file hashing.
- No git command.
- No embeddings.
- No project understanding real.
- No real planner.
- No executable task graph.
- No telemetry or analytics.
- NODAL OS remains the operational project name.

## What Was Not Implemented

- Real planner.
- Real assignment engine runtime.
- Executable TaskGraph.
- Prompt creation.
- LLM provider calls.
- Provider SDK/calls.
- Routing.
- Runtime execution.
- Dependency scheduler or queue.
- Cloud.
- Filesystem scan/mutation.

## Flaky

- Known previous flaky if observed: `BrowserRuntimeSmokeRunnerExecutesAllGatesOnFixture`, Gate 9 WebSocket aborted.
- Not observed in this block.

## Risks And Pending Items

- Future Assignment Engine still requires prompt governance, BYOK, budget guardrails, Safe Context Boundary, human review, and explicit positive execution gate before any runtime path.
- TaskGraph remains draft-only and must not be interpreted as execution authority.
- Planner readiness can classify future eligibility but cannot execute, prompt, call models, or dispatch work.

## Updated Percentages

If validation remains clean:

- NODAL OS global: 98.2%.
- Agent Operations / Automation Layer: 97.5%.
- Core Runtime: 76%.
- Evidence/Timeline foundation: 86%.
- Approval foundation: 81%.
- Redaction/Safety foundation: 91%.
- Productization foundation: 67%.
- Mission Control UX: 70%.
- Workspace Local: 68%.
- Project Understanding foundation: 56%.
- LLM/Assignment: 60%.
- Cloud optional: 10%.
- Automation future: 35%.

## Next Recommended Milestone

`M522+M523+M524 - Mission Plan Draft Preview + TaskGraph Review Cards + Assignment Evidence Linking`

## Final Decision

`M519+M520+M521 CERRADO / ASSIGNMENT_ENGINE_TASKGRAPH_DRAFT_READY`
