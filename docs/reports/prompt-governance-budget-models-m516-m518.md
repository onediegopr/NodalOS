# M516-M518 - Prompt Governance Contract + Budget Guardrails Draft + Model Capability Matrix

## Executive Summary

M516-M518 prepares future LLM/Assignment policy for NODAL OS without creating prompts, calling providers, implementing BYOK, routing, cloud, runtime, pricing lookup, model lookup, or token counting.

Decision target:

`M516+M517+M518 CERRADO / PROMPT_GOVERNANCE_BUDGET_MODEL_MATRIX_READY`

## Initial Git State

- Worktree: `C:\DESARROLLO\NodalOS\Codigo-m12-audit`
- Branch: `chrome-lab-001-extension-local-ai-bridge`
- Initial HEAD: `e80cbc840708348aff3290144927fca85267f2b4`
- Initial origin branch HEAD: `e80cbc840708348aff3290144927fca85267f2b4`
- Remote: `https://github.com/onediegopr/NodalOS.git`
- Forbidden path not used: `C:\Users\diego\OneDrive\PERSONAL\ONE Brain\Codigo`

## Objective

- M516: Prompt Governance Contract.
- M517: Budget Guardrails Draft.
- M518: Model Capability Matrix.

## M516 - Prompt Governance Contract

Implemented:

- Contract for future prompt governance without final prompt text.
- States for not allowed, preview-only, future prompt eligibility with consent, BYOK missing, blocked sensitive/raw/credential context, missing prompt policy, missing budget policy, human review, and unknown review.
- Required Safe Context Boundary, redaction, consent, provenance, confidence, freshness, BYOK policy, budget guardrails, evidence refs, timeline refs, and guardrail refs.
- Required disclosures that real prompt construction is not implemented, no context was sent to a model, no provider call was made, and future prompt use requires BYOK, consent, policy, and budget guardrails.

## M517 - Budget Guardrails Draft

Implemented:

- Draft-only budget policy model.
- Future scopes for global, workspace, mission, and provider.
- Statuses for not configured, draft-only, required before LLM, missing policy, missing user consent, unknown cost, future eligibility, and unknown review.
- Placeholders for currency, max spend, max tokens, max calls, max retries, max concurrent requests, model tier restrictions, confirmation threshold, cost visibility, stop/cancel, and evidence/timeline requirements.
- Explicit disclosures that BYOK does not mean cost free, managed AI future requires separate policy, and no LLM call can occur without budget guardrails.

## M518 - Model Capability Matrix

Implemented:

- Static model capability profile contract.
- Provider-kind profiles for future OpenAI, Anthropic, Gemini, LocalModel, Ollama, LM Studio, custom OpenAI-compatible, and unknown providers.
- Capability flags for chat, reasoning, summarization, project understanding, assignment planning, expert advisor, code assistance, vision, embeddings, tool use, browser automation, and unknown.
- Explicit defaults: browser automation future disabled, tool use future disabled, embeddings future disabled until policy, expert advisor non-executor, project understanding gated by policy, assignment planning gated by prompt governance/budget/BYOK.
- Explicit no model lookup, no pricing lookup, no routing, no provider call, and no execution authority flags.

## No-Prompt / No-LLM / No-Provider / No-Network Confirmation

- No final prompt text.
- No prompt construction real.
- No LLM provider call.
- No provider SDK.
- No network or HTTP.
- No BYOK real.
- No routing real.
- No token counting real.
- No pricing lookup.
- No model availability lookup.
- No cloud.
- No runtime.
- No positive execution gate implementation.

## Files Created

- `src/OneBrain.AgentOperations.Contracts/NodalOsPromptGovernanceContracts.cs`
- `src/OneBrain.AgentOperations.Core/NodalOsPromptGovernanceServices.cs`
- `tests/OneBrain.Safety.Tests/NodalOsPromptGovernanceM516M518Tests.cs`
- `docs/reports/prompt-governance-budget-models-m516-m518.md`
- `artifacts/agent-operations/m518/prompt-governance-budget-models-summary.json`

## Files Modified

- `docs/roadmap/nodal-os-roadmap-vnext.md`
- `docs/roadmap/nodal-os-unified-roadmap-post-pause.md`

## Tests Added

- Prompt governance states and guardrails.
- Budget guardrail statuses and placeholders.
- Model capability profiles and gated future capabilities.
- Adversarial redaction checks for prompt governance, budget, and model profiles.
- Boundary checks against runtime, provider, network, cloud, prompt, routing, pricing, model lookup, filesystem, and telemetry primitives.
- Existing safety continuity through M513-M515.
- Artifact guardrail flags.

## Validations

- `dotnet restore .\OneBrain.slnx`: passed.
- `dotnet build .\OneBrain.slnx`: passed with 0 warnings and 0 errors.
- Filtered test run for `PromptGovernance|ByokProvider|ProjectUnderstandingPolicy|ContextIntakePreview|UserContext|WorkspaceReadinessContext|WorkspaceMetadataHealth|WorkspaceStorageMissionSwitcher|WorkspaceLocalModel|MissionControlVisualPolish|MissionControlGuidance|MissionControlInteractionNoOp|MissionControlShellReadOnly|AuditAPreUiBoundaryNaming|ApprovalUxHandoffObservability|ApprovalTimelineEvidence|CoreRuntimeRegistryEventBusRedaction|NewTopicsIntake|NamingCleanup`: 379 passed, 0 skipped, 0 failed.
- Full suite: 4012 passed, 37 skipped, 0 failed.
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
- No telemetry or analytics.
- NODAL OS remains the operational project name.

## What Was Not Implemented

- Real prompts.
- Final prompt text.
- LLM provider calls.
- Provider SDK.
- BYOK real.
- Routing real.
- Token counting.
- Pricing lookup.
- Model availability lookup.
- Cloud.
- Runtime or execution gate.

## Flaky

- Known previous flaky if observed: `BrowserRuntimeSmokeRunnerExecutesAllGatesOnFixture`, Gate 9 WebSocket aborted.
- Not observed in this block.

## Risks And Pending Items

- Future prompt execution still requires BYOK, secure store, consent, prompt policy, budget guardrails, Safe Context Boundary, evidence refs, and human review.
- Future Assignment Engine remains blocked until TaskGraph and planner readiness gates exist.
- Model capability matrix is static and must not be treated as live provider availability.

## Updated Percentages

If validation remains clean:

- NODAL OS global: 98.1%.
- Agent Operations / Automation Layer: 97.5%.
- Core Runtime: 76%.
- Evidence/Timeline foundation: 85%.
- Approval foundation: 81%.
- Redaction/Safety foundation: 91%.
- Productization foundation: 66%.
- Mission Control UX: 69%.
- Workspace Local: 68%.
- Project Understanding foundation: 55%.
- LLM/Assignment: 50%.
- Cloud optional: 10%.
- Automation future: 35%.

## Next Recommended Milestone

`M519+M520+M521 - Assignment Engine v1 Contracts + TaskGraph Draft + Planner Readiness Gate`

## Final Decision

`M516+M517+M518 CERRADO / PROMPT_GOVERNANCE_BUDGET_MODEL_MATRIX_READY`
