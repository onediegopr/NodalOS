# M513-M515 - BYOK Provider Settings Contract + Secret Storage Policy ADR + Provider Test Connection UX Contract

## Executive Summary

M513-M515 prepares future BYOK provider configuration for NODAL OS without implementing real BYOK, secure storage, provider calls, network access, prompt creation, LLM routing, cloud, or runtime execution.

Decision target:

`M513+M514+M515 CERRADO / BYOK_PROVIDER_SETTINGS_SECRET_POLICY_READY`

## Initial Git State

- Worktree: `C:\DESARROLLO\NodalOS\Codigo-m12-audit`
- Branch: `chrome-lab-001-extension-local-ai-bridge`
- Initial HEAD: `7e82a15099ead0c48c107415c449c84e9da65857`
- Initial origin branch HEAD: `7e82a15099ead0c48c107415c449c84e9da65857`
- Remote: `https://github.com/onediegopr/NodalOS.git`
- Forbidden path not used: `C:\Users\diego\OneDrive\PERSONAL\ONE Brain\Codigo`

## Objective

- M513: BYOK Provider Settings Contract.
- M514: Secret Storage Policy ADR.
- M515: Provider Test Connection UX Contract.

## M513 - BYOK Provider Settings Contract

Implemented:

- Reference-only provider settings contract for future BYOK configuration.
- Future scopes: global, workspace, mission, and user profile.
- Future provider kinds: OpenAI, Anthropic, Gemini, local model, Ollama, LM Studio, custom OpenAI-compatible, and unknown.
- Key status model: not configured, reference-only configured, requires secret storage policy, requires user consent, disabled, and blocked by policy.
- Future capability declarations and disabled capability explanations.
- Budget policy, prompt governance, consent, redaction policy, Safe Context Boundary, evidence, timeline, and guardrail refs.
- Explicit no-provider-call, no-network, no-prompt, no-routing, no-cloud, and no-execution flags.

## M514 - Secret Storage Policy ADR

Implemented:

- Formal ADR defining NODAL OS secret storage policy.
- Decision: `NODAL_OS_SECRET_STORAGE_POLICY_DEFINED`.
- Declares no raw secrets in JSON, logs, artifacts, reports, screenshots, observability, handoff/export, timeline, evidence, prompt context, or provider settings serialization.
- Defines future secure store/vault requirement.
- Lists future OS keychain/credential manager and local encrypted vault options.
- Blocks cloud secret storage by default.
- Defines reference-only settings and redacted errors.
- Declares this milestone does not implement secure storage, OS keychain, vault, provider calls, network, BYOK runtime, prompts, LLM routing, or cloud.

## M515 - Provider Test Connection UX Contract

Implemented:

- Future provider test connection request preview model.
- States for not available, mock-only, future preflight eligibility, missing credential ref, policy blockers, network disabled, provider policy blocked, missing consent, missing budget policy, missing prompt governance, and unknown review.
- Preflight check descriptions, user consent requirement, network disabled status, mock-only status, expected safe result, error redaction policy, evidence refs, timeline refs, observability refs, and guardrail refs.
- Explicit action disabled/mock-only flags.
- Explicit no-network, no-provider SDK, no-env-var-read, no-prompt, no-LLM-call, no raw credential, and no-execution flags.

## No-BYOK-Real / No-Secret-Storage / No-Provider-Call / No-Network Confirmation

- No real BYOK.
- No secure store implementation.
- No OS keychain implementation.
- No local encrypted vault implementation.
- No provider SDK.
- No provider call.
- No network or HTTP.
- No raw credential persistence.
- No environment variable reading.
- No prompt creation.
- No LLM routing.
- No cloud.
- No runtime.
- No positive execution gate implementation.

## Files Created

- `docs/architecture/nodal-os-secret-storage-policy-decision-record.md`
- `src/OneBrain.AgentOperations.Contracts/NodalOsByokProviderContracts.cs`
- `src/OneBrain.AgentOperations.Core/NodalOsByokProviderServices.cs`
- `tests/OneBrain.Safety.Tests/NodalOsByokProviderM513M515Tests.cs`
- `docs/reports/byok-provider-settings-m513-m515.md`
- `artifacts/agent-operations/m515/byok-provider-settings-summary.json`

## Files Modified

- `docs/roadmap/nodal-os-roadmap-vnext.md`
- `docs/roadmap/nodal-os-unified-roadmap-post-pause.md`

## Tests Added

- BYOK provider settings states and reference-only guardrails.
- Future provider kinds and capabilities.
- Adversarial credential/endpoint redaction checks.
- Secret Storage Policy ADR coverage.
- Provider test connection states and disabled/mock-only behavior.
- Boundary checks against runtime, provider, network, cloud, filesystem, prompt, and telemetry primitives.
- Existing safety continuity through M510-M512.
- Artifact guardrail flags.

## Validations

- `dotnet restore .\OneBrain.slnx`: passed.
- `dotnet build .\OneBrain.slnx`: passed with 0 warnings and 0 errors.
- Filtered test run for `ByokProvider|ProjectUnderstandingPolicy|ContextIntakePreview|UserContext|WorkspaceReadinessContext|WorkspaceMetadataHealth|WorkspaceStorageMissionSwitcher|WorkspaceLocalModel|MissionControlVisualPolish|MissionControlGuidance|MissionControlInteractionNoOp|MissionControlShellReadOnly|AuditAPreUiBoundaryNaming|ApprovalUxHandoffObservability|ApprovalTimelineEvidence|CoreRuntimeRegistryEventBusRedaction|NewTopicsIntake|NamingCleanup`: 369 passed, 0 skipped, 0 failed.
- Full suite: 4002 passed, 37 skipped, 0 failed.
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
- No LLM routing.
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

- Real BYOK.
- Secure store.
- OS keychain.
- Local encrypted vault.
- Provider calls.
- Provider SDK.
- Network or HTTP.
- Prompt creation.
- LLM routing.
- Cloud.
- Runtime or execution gate.

## Flaky

- Known previous flaky if observed: `BrowserRuntimeSmokeRunnerExecutesAllGatesOnFixture`, Gate 9 WebSocket aborted.
- Not observed in this block.

## Risks And Pending Items

- Future BYOK still requires secure store/vault implementation, consent, prompt governance, budget guardrails, provider policy, redacted errors, and audit/evidence refs.
- Future provider test connection remains blocked until real network/provider policy and secure credential handling exist.
- Future LLM usage remains blocked until Safe Context Boundary, Context-to-LLM Governance, and user consent are satisfied.

## Updated Percentages

If validation remains clean:

- NODAL OS global: 98.0%.
- Agent Operations / Automation Layer: 97.5%.
- Core Runtime: 76%.
- Evidence/Timeline foundation: 85%.
- Approval foundation: 81%.
- Redaction/Safety foundation: 90%.
- Productization foundation: 65%.
- Mission Control UX: 69%.
- Workspace Local: 68%.
- Project Understanding foundation: 55%.
- LLM/Assignment: 40%.
- Cloud optional: 10%.
- Automation future: 35%.

## Next Recommended Milestone

`M516+M517+M518 - Prompt Governance Contract + Budget Guardrails Draft + Model Capability Matrix`

## Final Decision

`M513+M514+M515 CERRADO / BYOK_PROVIDER_SETTINGS_SECRET_POLICY_READY`
