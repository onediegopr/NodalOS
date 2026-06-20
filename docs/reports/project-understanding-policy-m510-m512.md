# M510-M512 - Project Understanding Policy ADR + Real Scan Preconditions + Context-to-LLM Governance Draft

## Executive Summary

M510-M512 defines the policy and preconditions for future Project Understanding in NODAL OS without implementing real scan, provider usage, prompt creation, BYOK, cloud, runtime, or filesystem access.

Decision target:

`M510+M511+M512 CERRADO / PROJECT_UNDERSTANDING_POLICY_GOVERNANCE_READY`

## Initial Git State

- Worktree: `C:\DESARROLLO\NodalOS\Codigo-m12-audit`
- Branch: `chrome-lab-001-extension-local-ai-bridge`
- Initial HEAD: `553668cdb09ebd87e0c57bb894d58402bd59433e`
- Initial origin branch HEAD: `553668cdb09ebd87e0c57bb894d58402bd59433e`
- Remote: `https://github.com/onediegopr/NodalOS.git`
- Forbidden path not used: `C:\Users\diego\OneDrive\PERSONAL\ONE Brain\Codigo`

## Objective

- M510: Project Understanding Policy ADR.
- M511: Real Scan Preconditions.
- M512: Context-to-LLM Governance Draft.

## M510 - Project Understanding Policy ADR

Implemented:

- Formal ADR defining Project Understanding and what it is not.
- Clear separation between user-provided context, mock metadata, safe project summary, future real scan, future real Project Understanding, and future LLM-assisted understanding.
- Policy principles: local-first, explicit consent, path jail first, redaction first, evidence first, approval before sensitive future operations, no cloud by default, BYOK/policy before LLM, and no execution authority.
- Relationships to Safe Context Boundary, Workspace Readiness Gate, Evidence/Timeline, future Assignment Engine, future Expert Advisor, and runtime execution gate.

Decision:

`NODAL_OS_PROJECT_UNDERSTANDING_POLICY_DEFINED`

## M511 - Real Scan Preconditions

Implemented:

- Contract and service for future real scan preconditions.
- States for eligible/blocked/review outcomes.
- Scope, excluded patterns, max file count, max file size, binary policy, secrets policy, redaction policy, symlink policy, case sensitivity policy, evidence requirements, preview-before-scan, cancel/stop, and no-mutation guarantee.
- Explicit flags proving no real scan, listing, read, hash, git, mutation, cloud, LLM, embeddings, or execution authority.

## M512 - Context-to-LLM Governance Draft

Implemented:

- Governance contract for future context-to-LLM/BYOK usage.
- States for display/export only, future LLM eligibility with consent, missing BYOK, missing prompt policy, missing budget policy, secret/raw/sensitive blockers, human review, and unknown review.
- Requirements for redaction, consent, BYOK future, prompt governance future, budget guardrails future, evidence refs, provenance/confidence/freshness, human review, and timeline registration.
- Explicit flags proving no prompt creation, provider call, network send, cloud call, or execution authority.

## No-Runtime / No-Filesystem / No-LLM / No-Prompt Confirmation

- No real Project Understanding.
- No filesystem scan.
- No directory listing.
- No file read/write/delete.
- No file hashing.
- No git command.
- No embeddings.
- No prompt creation.
- No LLM provider calls.
- No BYOK implementation.
- No cloud.
- No runtime.
- No positive execution gate implementation.
- No productive persistence.

## Files Created

- `docs/architecture/nodal-os-project-understanding-policy-decision-record.md`
- `src/OneBrain.AgentOperations.Contracts/NodalOsProjectUnderstandingPolicyContracts.cs`
- `src/OneBrain.AgentOperations.Core/NodalOsProjectUnderstandingPolicyServices.cs`
- `tests/OneBrain.Safety.Tests/NodalOsProjectUnderstandingPolicyM510M512Tests.cs`
- `docs/reports/project-understanding-policy-m510-m512.md`
- `artifacts/agent-operations/m512/project-understanding-policy-summary.json`

## Files Modified

- `docs/roadmap/nodal-os-roadmap-vnext.md`
- `docs/roadmap/nodal-os-unified-roadmap-post-pause.md`

## Tests Added

- ADR existence and required policy coverage.
- Real scan precondition states and guardrails.
- Context-to-LLM governance states and guardrails.
- Source boundary checks against runtime, filesystem, provider, prompt, cloud, and automation primitives.
- Existing safety continuity through M507-M509.
- Artifact guardrail flags.

## Validations

- `dotnet restore .\OneBrain.slnx`: passed.
- `dotnet build .\OneBrain.slnx`: passed with 0 warnings and 0 errors.
- Filtered test run for `ProjectUnderstandingPolicy|ContextIntakePreview|UserContext|WorkspaceReadinessContext|WorkspaceMetadataHealth|WorkspaceStorageMissionSwitcher|WorkspaceLocalModel|MissionControlVisualPolish|MissionControlGuidance|MissionControlInteractionNoOp|MissionControlShellReadOnly|AuditAPreUiBoundaryNaming|ApprovalUxHandoffObservability|ApprovalTimelineEvidence|CoreRuntimeRegistryEventBusRedaction|NewTopicsIntake|NamingCleanup`: 359 passed, 0 skipped, 0 failed.
- Full suite: 3992 passed, 37 skipped, 0 failed.
- Frontend/Tauri/Rust checks: not applicable; no `package.json` or `Cargo.toml` found in expected repo zones.

## Guardrails Confirmed

- No runtime execution.
- No positive execution gate implementation.
- No browser automation.
- No recorder/replay.
- No queue/scheduler/worker.
- No DSL parser runtime.
- No LLM provider calls.
- No BYOK implementation.
- No prompt creation.
- No cloud sync.
- No productive persistence.
- No filesystem mutation.
- No filesystem scan.
- No directory listing.
- No file read/write/delete.
- No file hashing.
- No git command.
- No embeddings.
- No project understanding real.
- No telemetry or analytics.
- NODAL OS remains the operational project name.

## What Was Not Implemented

- Real Project Understanding.
- Real scan.
- Real context extraction.
- Prompt creation.
- LLM/BYOK.
- Provider registry or calls.
- Git integration.
- Embeddings/vector index.
- File/directory verification.
- Runtime or execution gate.
- Cloud.

## Flaky

- Known previous flaky if observed: `BrowserRuntimeSmokeRunnerExecutesAllGatesOnFixture`, Gate 9 WebSocket aborted.
- Not observed in this block.
- First full suite attempt timed out at the command timeout boundary before a test result was returned; rerun with a longer timeout passed cleanly.

## Risks And Pending Items

- This block is policy/precondition only.
- Future real scan still requires explicit consent, path jail validation, scope preview, redaction, evidence, and stop/cancel semantics.
- Future LLM usage still requires BYOK/provider policy, prompt governance, budget guardrails, consent, and human review.

## Updated Percentages

If validation remains clean:

- NODAL OS global: 97.9%.
- Agent Operations / Automation Layer: 97.5%.
- Core Runtime: 76%.
- Evidence/Timeline foundation: 85%.
- Approval foundation: 81%.
- Redaction/Safety foundation: 89%.
- Productization foundation: 64%.
- Mission Control UX: 69%.
- Workspace Local: 68%.
- Project Understanding foundation: 55%.
- LLM/Assignment: 30%.
- Cloud optional: 10%.
- Automation future: 35%.

## Next Recommended Milestone

`M513+M514+M515 - BYOK Provider Settings Contract + Secret Storage Policy ADR + Provider Test Connection UX Contract`

## Final Decision

`M510+M511+M512 CERRADO / PROJECT_UNDERSTANDING_POLICY_GOVERNANCE_READY`
