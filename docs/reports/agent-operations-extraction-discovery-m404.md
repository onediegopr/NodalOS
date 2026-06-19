# Agent Operations Extraction Discovery M404

Project: NODAL OS

Base commit: `ab5a93e`

## Discovery Commands

`Select-String` was used because `rg` was not available reliably in this environment.

- `NodalOsAgent`
- `NodalOsRecipe`
- `NodalOsStep`
- `NodalOsPackage`
- `NodalOsSkill`
- `NodalOsWorker`
- `NodalOsEvidenceRefBridge`
- `NodalOsRedaction`
- `NodalOsVerificationBeforeDone`
- `NodalOsInternalSkillRegistry`
- `NodalOsPackageSkill`
- `NodalOsRunReport`
- `NodalOsFailure`
- `NexaMission`
- `NexaAgentTask`
- `NexaRunReport`
- `NexaEvidenceRef`
- `BrowserExecutor.Contracts`
- `BrowserExecutor.Cdp`
- `ChromeCdp`
- `Cdp`
- `BrowserPersistentAuditLedger`
- `EvidenceLedger`
- `OneBrain.Core`

Reviewed folders:

- `src/OneBrain.BrowserExecutor.Contracts`
- `src/OneBrain.BrowserExecutor.Cdp`
- `tests/OneBrain.Safety.Tests`
- `docs/reports`
- `docs/architecture`
- `artifacts/agent-operations`
- `artifacts/core`

## Current Project References

`OneBrain.BrowserExecutor.Contracts` is currently dependency-light and has no project references.

`OneBrain.BrowserExecutor.Cdp` currently references:

- `OneBrain.Core`
- `OneBrain.BrowserExecutor.Contracts`
- `Microsoft.ML.OnnxRuntime`
- `System.Security.Cryptography.ProtectedData`

This means Agent Operations services hosted in `BrowserExecutor.Cdp` inherit browser/runtime/OCR-adjacent dependencies even when the services are contract-only or core-like.

## Agent Operations Files Detected

### Contracts Candidate

| Path | Classification | Notes |
| --- | --- | --- |
| `src/OneBrain.BrowserExecutor.Contracts/NodalOsAgentWorkboardContracts.cs` | Contracts candidate | Mission/task/progress/blocker/evidence compatibility symbols. |
| `src/OneBrain.BrowserExecutor.Contracts/NodalOsFailureTaxonomyContracts.cs` | Contracts candidate | Failure taxonomy and troubleshooting model. |
| `src/OneBrain.BrowserExecutor.Contracts/NodalOsRunReportContracts.cs` | Contracts candidate | Run report V1 contracts. |
| `src/OneBrain.BrowserExecutor.Contracts/NodalOsRecipeManifestContracts.cs` | Contracts candidate | Recipe manifest V1, execution-deferred. |
| `src/OneBrain.BrowserExecutor.Contracts/NodalOsVerificationBeforeDoneContracts.cs` | Contracts candidate | Canonical completion gate result and subject kinds. |
| `src/OneBrain.BrowserExecutor.Contracts/NodalOsAgentProgressReportingContracts.cs` | Contracts candidate | Progress/blocker/handoff/ready-to-close report contracts. |
| `src/OneBrain.BrowserExecutor.Contracts/NodalOsStepLibraryContracts.cs` | Contracts candidate | Step Library V1 governance metadata. |
| `src/OneBrain.BrowserExecutor.Contracts/NodalOsRedactionContracts.cs` | Contracts candidate | Common redaction result/options/matches. |
| `src/OneBrain.BrowserExecutor.Contracts/NodalOsEvidenceRefBridgeContracts.cs` | Contracts candidate | EvidenceRef bridge model, source/use/authority/sensitivity. |
| `src/OneBrain.BrowserExecutor.Contracts/NodalOsPackageSkillManifestContracts.cs` | Contracts candidate | Package/Skill Manifest V1. |
| `src/OneBrain.BrowserExecutor.Contracts/NodalOsInternalSkillRegistryContracts.cs` | Contracts candidate | Internal Skill Registry V1 snapshot/query/build result. |
| `src/OneBrain.BrowserExecutor.Contracts/NodalOsWorkerBoundaryContracts.cs` | Contracts candidate | Worker Boundary Contract V1. |

### Core Service Candidate

| Path | Classification | Notes |
| --- | --- | --- |
| `src/OneBrain.BrowserExecutor.Cdp/NodalOsAgentWorkboardServices.cs` | Core service candidate | Workboard validation; no browser execution required. |
| `src/OneBrain.BrowserExecutor.Cdp/NodalOsRunReportingServices.cs` | Core service candidate | Run report builder/sanitizer; contract-only reporting. |
| `src/OneBrain.BrowserExecutor.Cdp/NodalOsRecipeManifestServices.cs` | Core service candidate | Manifest validation/serialization; no recipe execution. |
| `src/OneBrain.BrowserExecutor.Cdp/NodalOsVerificationBeforeDoneGate.cs` | Core service candidate | Canonical completion semantics. |
| `src/OneBrain.BrowserExecutor.Cdp/NodalOsAgentProgressReportingServices.cs` | Core service candidate | Progress report validation/builder/sanitizer. |
| `src/OneBrain.BrowserExecutor.Cdp/NodalOsStepLibraryServices.cs` | Core service candidate | Step catalog, policy metadata validation, mapping. |
| `src/OneBrain.BrowserExecutor.Cdp/NodalOsRedactionServices.cs` | Core shared service candidate | Common redaction/classification service. |
| `src/OneBrain.BrowserExecutor.Cdp/NodalOsEvidenceRefBridgeServices.cs` | Core shared service candidate | EvidenceRef bridge adapter/service. |
| `src/OneBrain.BrowserExecutor.Cdp/NodalOsPackageSkillManifestServices.cs` | Core service candidate | Package/Skill manifest validator/serializer/fixtures. |
| `src/OneBrain.BrowserExecutor.Cdp/NodalOsInternalSkillRegistryServices.cs` | Core service candidate | Registry builder/validator/query/serializer. |
| `src/OneBrain.BrowserExecutor.Cdp/NodalOsWorkerBoundaryServices.cs` | Core service candidate plus future adapter seam | Worker boundary validation/serialization and skill-worker capability mapper. |

### Browser Adapter Candidate / Stay in Browser Runtime

| Path | Classification | Notes |
| --- | --- | --- |
| `src/OneBrain.BrowserExecutor.Cdp/ChromeCdpBrowserExecutor.cs` | Browser adapter/stay | Real CDP executor. |
| `src/OneBrain.BrowserExecutor.Cdp/BrowserRuntimeSmoke.cs` | Browser adapter/stay | Browser runtime smoke and cleanup. |
| `src/OneBrain.BrowserExecutor.Cdp/BrowserPersistentAuditLedger.cs` | Browser adapter/stay or bridge later | Browser-specific persistent audit ledger implementation. |
| `src/OneBrain.BrowserExecutor.Cdp/BrowserProfileSessionManager.cs` | Browser adapter/stay | Browser profile/session management. |
| `src/OneBrain.BrowserExecutor.Cdp/BrowserTargetFrameManager.cs` | Browser adapter/stay | CDP frame management. |
| `src/OneBrain.BrowserExecutor.Cdp/ChromeCdpExternalProofServices.cs` | Browser adapter/stay | External proof through CDP/browser. |
| `src/OneBrain.BrowserExecutor.Cdp/BrowserSafeDownloadServices.cs` | Browser adapter/stay | Browser download behavior. |
| `src/OneBrain.BrowserExecutor.Cdp/BrowserSafeUploadServices.cs` | Browser adapter/stay | Browser upload behavior. |

### Core Shared Dependency

| Dependency | Role | Extraction impact |
| --- | --- | --- |
| `NodalOsRedactionService` | Shared sanitizer/classifier. | Should move to AgentOperations.Core initially or a future shared security core. |
| `NodalOsEvidenceRefBridge` | EvidenceRef bridge into audit/evidence semantics. | Should move with Agent Operations core until Evidence Ledger bridge becomes core-owned. |
| `BrowserPersistentAuditLedger` | Browser audit persistence implementation. | Should not move in Phase 1; only adapters should reference it. |
| `OneBrain.Core` | Current shared project dependency for BrowserExecutor.Cdp. | AgentOperations.Core should minimize direct dependency until specific shared abstractions are identified. |
| Safe action / policy / approval / FSM contracts | Global policy/runtime concerns. | Agent Operations must depend on contracts/abstractions only, not browser execution implementations. |

### Test-Only

Tests under `tests/OneBrain.Safety.Tests` for M344-M403 are extraction guardrails. They should move or be split later only after new Agent Operations projects exist.

Representative groups:

- Mission/task/workboard tests.
- Failure taxonomy/run report tests.
- Recipe manifest tests.
- Verification-before-done tests.
- Progress reporting tests.
- Step library tests.
- Common redaction tests.
- EvidenceRef ledger bridge tests.
- Package/Skill manifest tests.
- Internal Skill Registry tests.
- Worker Boundary tests.
- Package/Registry/Worker no-divergence tests.

### Docs / Artifacts

Relevant docs and artifacts live under:

- `docs/reports/*m344-m403*.md`
- `docs/architecture/agent-operations-namespace-naming-adr.md`
- `artifacts/agent-operations/*`
- `artifacts/core/*`

These are historical traceability assets and should not be rewritten during extraction.

## Dependency Findings

### What Depends on Browser/CDP Really

The Agent Operations core-like services use the `OneBrain.BrowserExecutor.Cdp` namespace and project, but most do not depend on `ChromeCdpBrowserExecutor`, CDP sessions, browser profiles, target frames, or browser runtime actions.

Real browser dependencies are concentrated in browser runtime classes:

- `ChromeCdpBrowserExecutor`
- `BrowserRuntimeSmoke`
- browser profile/session/frame/download/upload services
- `BrowserPersistentAuditLedger`

### What Does Not Depend on Browser/CDP

The following are candidates to extract without changing behavior:

- workboard validation
- run reporting
- recipe manifest validation
- verification-before-done
- progress reporting
- step library metadata
- common redaction
- EvidenceRef bridge
- package/skill manifest validation
- internal skill registry
- worker boundary validation

### Evidence / Redaction / Core Usage

Agent Operations relies on:

- `NexaEvidenceRef` as compatibility evidence ref.
- `NodalOsEvidenceBridgeRef` as richer bridge ref.
- `NodalOsRedactionService` for sanitizer convergence.
- failure taxonomy types for run/worker reports.

The bridge to actual evidence ledger remains conceptual/adapter-based. No persistence move should happen in extraction prep.

### `Nexa*` Compatibility Symbols

Detected compatibility symbols include:

- `NexaMission`
- `NexaAgentTask`
- `NexaProgressNote`
- `NexaBlockerReport`
- `NexaVerificationCheck`
- `NexaEvidenceRef`
- `NexaFailureKind`
- `NexaRunReport`
- `NexaRunStepReport`
- `NexaFailureReport`

These should not be renamed during extraction Phase 1. They need compatibility shims or type forwarding strategy when public namespaces move.

## Shims Needed

- Type-forwarding or compatibility aliases for existing `OneBrain.BrowserExecutor.Contracts` consumers.
- Transitional facade for `NodalOsVerificationBeforeDoneGate`.
- Transitional facade for package/registry/worker services currently under `OneBrain.BrowserExecutor.Cdp`.
- Test namespace compatibility while files move.
- Documentation rule that new types use `NodalOs*` and no new `Nexa*` symbols are introduced.

## Risks

| Risk | Severity | Mitigation |
| --- | --- | --- |
| Moving contracts and services together could create project cycles. | High | Extract contracts first, then core services, then browser adapters. |
| BrowserExecutor.Cdp currently bundles ONNX/runtime/browser dependencies with Agent Operations services. | Medium | New AgentOperations.Core should avoid CDP, ONNX, ProtectedData, and browser runtime references. |
| `Nexa*` compatibility symbols may become breaking changes if renamed. | Medium | Keep names initially; introduce shims/type forwarding before any rename. |
| Evidence/redaction may be split incorrectly. | Medium | Keep redaction and EvidenceRef bridge as shared AgentOperations.Core until a dedicated shared security/evidence project exists. |
| Browser audit ledger could be mistaken for generic evidence ledger. | Medium | Keep browser ledger in browser adapter boundary. |

## Discovery Decision

No blocking dependency cycle was found at the documentation/discovery level. M404-M406 should close as extraction prep with shims required.
