# NODAL OS Core Legacy Reference Graph M376

Milestone: M374-M376
Base commit: 4d6db4f
Decision target: CORE_LEGACY_REFERENCE_GRAPH_READY_WITH_CLEANUP_BACKLOG

## 1. Executive Summary

The core reference graph is clear enough to proceed. NODAL OS currently has a stable set of active paths for browser runtime, core FSM/safe execution, evidence/audit, OCR perception, and the new Agent Operations platform layer.

The main architectural risk is not an unclear active path. The main risk is accumulated location and semantic debt before future orchestration, UI, recipe execution, scheduled runs, or package/skill surfaces are connected.

This block intentionally did not move projects, rename symbols, delete legacy, change OCR, change policy, or alter runtime behavior.

## 2. Current Active Paths

| Area | Active path | Classification | Reason |
| --- | --- | --- | --- |
| Core execution | `OneBrain.Core/Execution` | ActivePath | Contains safe FSM, evidence ledger, executor authorization policy, legacy execution guard, safe click/read/type verification. |
| Desktop observation/action support | `OneBrain.Observation`, `OneBrain.Actions` | ActiveSupport | UIA/window/visual observation and gated UIA action executors. |
| Browser runtime | `OneBrain.BrowserExecutor.Cdp` | ActivePath | Chrome/CDP executor, browser profiles, runtime smoke, phase services, audit ledger. |
| Browser contracts | `OneBrain.BrowserExecutor.Contracts` | ActivePath | Browser runtime contracts and current Agent Operations contracts. |
| Evidence/audit | `EvidenceLedger`, `BrowserPersistentAuditLedger`, OCR evidence integration | ActivePath | Core and browser audit ledgers are active evidence stores. |
| OCR perception | ONNX .NET services, controlled capture, OCR envelope/audit/FSM observation, assisted verification | ActivePath | OCR line has active local ONNX .NET path and no-authority policy. |
| Agent Operations | Mission/Task, RunReport, RecipeManifest, VerificationBeforeDone, ProgressReport, StepLibrary | ActiveSupport | Contract/governance layer ready; execution deferred. |
| Safety tests | `tests/OneBrain.Safety.Tests` | ActiveSupport | Main safety regression suite. |
| Recipe tests | `tests/OneBrain.Recipes.Tests` | ActiveSupport | Existing recipe/core regression suite. |

## 3. Agent Operations Location Assessment

Agent Operations currently lives in:

- `src/OneBrain.BrowserExecutor.Contracts`
- `src/OneBrain.BrowserExecutor.Cdp`

This is acceptable as a temporary implementation location because the new Agent Operations layer is non-runtime and test-covered. It is not ideal as a long-term boundary because Agent Operations is not browser-specific.

Classification: CandidateForExtraction.

Recommended future boundary:

- `OneBrain.AgentOperations.Contracts`
- `OneBrain.AgentOperations.Core`
- optional compatibility shims from `OneBrain.BrowserExecutor.*` during migration

Earliest safe milestone to touch: after this reference graph, before Orchestration API, Workboard UI, recipe execution, or package registry.

## 4. Browser Runtime Active Path

Active files and modules:

- `ChromeCdpBrowserExecutor`
- `BrowserRuntimeSmoke`
- `BrowserRuntimePhaseServices`
- `BrowserProfileSessionManager`
- `BrowserPersistentAuditLedger`
- browser credential/secret/sensitive/download/upload/vault services

Classification: ActivePath.

Do not touch yet:

- process/profile cleanup semantics
- owned-process-only cleanup
- structured diagnostics
- CDP/browser runtime behavior

Risk if removed or changed: browser smoke and CDP runtime regressions.

Recommended action: keep stable; only change through browser runtime-specific milestones.

## 5. OCR Active Path

Active files and modules:

- ONNX .NET OCR runtime and pre/post-processing services
- PaddleOCR ONNX model catalog/readiness/integrity/verifier services
- QA window and controlled region capture services
- OCR evidence policy/evaluation/integration services
- `NodalOsOcrEvidenceAuditConsumer`
- `NodalOsOcrFsmObservationConsumer`
- assisted verification policy and fixtures

Classification: ActivePath.

Do not touch yet:

- PaddleOCR class semantics
- space token semantics
- ONNX .NET active path
- OCR no-authority policy
- OCR evidence mapping into auxiliary/diagnostic/excluded categories

Risk if removed or changed: regression in audited OCR line.

Recommended action: keep stable until a dedicated OCR maintenance milestone.

## 6. Evidence Active Path

Active files and modules:

- `src/OneBrain.Core/Execution/EvidenceLedger.cs`
- `src/OneBrain.BrowserExecutor.Cdp/BrowserPersistentAuditLedger.cs`
- `src/OneBrain.BrowserExecutor.Contracts/BrowserPersistentAuditLedgerContracts.cs`
- OCR evidence ledger/integration contracts and services
- private preview evidence verifier/freeze services

Classification: ActivePath.

Gap:

- Agent Operations uses `NexaEvidenceRef` as a lightweight reference.
- `NexaEvidenceRef` is not yet a first-class bridge to `EvidenceLedger` or `BrowserPersistentAuditLedger`.

Recommended action: create an EvidenceRef-to-ledger bridge design before UI/orchestration persistence.

## 7. FSM / Runtime / Policy Active Path

Active files and modules:

- `SafeExecutionFsm`
- `SafeExecutorAuthorizationPolicy`
- `SafeClickPlanner`
- `SafeClickStepVerifier`
- `SafeReadStepVerifier`
- `SafeTypeStepVerifier`
- `LegacyExecutionGuard`
- `NodalOsSafeActionEvaluator`
- `SensitiveSitePolicyEvaluator`

Classification: ActivePath.

Do not touch yet:

- SafeAction authorization semantics
- FSM state transitions
- policy fail-closed gates
- sensitive site restrictions

Recommended action: future audit should focus on low-risk action policy only after evidence and verification consolidation.

## 8. Recipe / Step / Run Reporting Active Path

Active files and modules:

- `NodalOsRecipeManifestContracts.cs`
- `NodalOsRecipeManifestServices.cs`
- `NodalOsStepLibraryContracts.cs`
- `NodalOsStepLibraryServices.cs`
- `NodalOsRunReportContracts.cs`
- `NodalOsRunReportingServices.cs`
- `NodalOsFailureTaxonomyContracts.cs`

Classification: ActiveSupport / CandidateForRefactor.

Risks:

- `CanExecute` in recipe validation can be misread as runtime permission.
- `IsAllowedInV1` in Step Library can be misread as runtime permission.
- RunReport builder validation overlaps with VerificationBeforeDone.

Recommended action:

- split manifest policy acceptance from runtime permission before recipe execution.
- make VerificationBeforeDone canonical for completion.
- keep Step Library governance-only until execution design.

## 9. Legacy Modules

| Path | Classification | Reason | Risk if removed | Recommended action | Earliest safe milestone |
| --- | --- | --- | --- | --- | --- |
| `tools/ocr-worker/paddleocr_worker.py` | DeprecatedRetained | Historical Python OCR worker. | May break historical tests/docs if removed blindly. | Candidate cleanup after reference checks. | OCR legacy cleanup follow-up. |
| `src/OneBrain.BrowserExecutor.Cdp/NodalOsPaddleOcrWorkerServices.cs` | DeprecatedRetained | Python worker adapter remains. | Could break M192 tests/history. | Deprecate or remove only with tests updated. | OCR legacy cleanup follow-up. |
| `src/OneBrain.BrowserExecutor.Contracts/NodalOsPaddleOcrWorkerContracts.cs` | DeprecatedRetained | Python worker contracts remain. | Could break historical test coverage. | Candidate cleanup. | OCR legacy cleanup follow-up. |
| `tools/ocr-worker/check-paddleocr.ps1` | HistoricalLegacy | Python setup/check utility. | Low, but historical docs may reference. | Candidate cleanup. | OCR legacy cleanup follow-up. |
| `tools/ocr-worker/setup-paddleocr.ps1` | HistoricalLegacy | Python setup utility. | Low, but historical docs may reference. | Candidate cleanup. | OCR legacy cleanup follow-up. |
| `tools/ocr-worker/rollback-paddleocr.ps1` | HistoricalLegacy | Python rollback utility. | Low, but historical docs may reference. | Candidate cleanup. | OCR legacy cleanup follow-up. |
| `src/OneBrain.BrowserExecutor.Cdp/Nexa*` | HistoricalLegacy / ActiveSupport mixed | Historical product naming. | High if broad rename is blind. | Compatibility ADR, then scoped rename. | Naming cleanup milestone. |
| `src/OneBrain.BrowserExecutor.Contracts/Nexa*` | HistoricalLegacy / ActiveSupport mixed | Historical product naming. | High if broad rename is blind. | Compatibility ADR, then scoped rename. | Naming cleanup milestone. |

## 10. Diagnostic-Only Modules

| Path | Classification | Reason | Recommended action |
| --- | --- | --- | --- |
| `NodalOsFullOcrHandoffRecognitionRuntimeServices.cs` | DiagnosticOnly | ONNX runtime experiment/planner services. | Keep until OCR diagnostic cleanup. |
| `NodalOsOnnxNativeRuntimeCrashProbeMatrixBuilder.cs` | DiagnosticOnly | Native runtime crash probe matrix. | Keep as diagnostic. |
| `NodalOsOnnxNativeRuntimeCrashReadinessReview.cs` | DiagnosticOnly | Native runtime readiness review. | Keep as diagnostic. |
| `NodalOsGuardedSyntheticTextOcrProbeServices.cs` | DiagnosticOnly | Guarded OCR probe services. | Keep as diagnostic. |
| `tools/onnx-ocr-probe-runner` | DiagnosticOnly | Probe runner. | Keep untracked build output ignored. |
| `tools/recipes/*-negative.json` | DiagnosticOnly | Negative fixtures. | Keep as regression fixtures. |
| historical artifacts under `artifacts/ocr-vision-*` | HistoricalTrace | Milestone evidence. | Retain for traceability. |

## 11. Deprecated-But-Retained Modules

Deprecated-retained means the code or tool is not the preferred active path, but remains for traceability, historical tests, or diagnostic replay.

| Module | Reason retained |
| --- | --- |
| Python OCR worker and contracts | Historical OCR path, tests, docs, and rollback traceability. |
| ONNX runtime experiment services | Diagnostic decision history and crash containment evidence. |
| `Nexa*` symbols | Compatibility and old milestone history. |
| old external proof/product/admin readiness services | Some may be historical, but active status requires focused product/admin graph. |

## 12. Candidate Cleanup Backlog

| Priority | Candidate | Action | Blocker |
| --- | --- | --- | --- |
| High | Completion gate duplication | Make `NodalOsVerificationBeforeDoneGate` canonical. | Needs focused refactor tests. |
| High | Sanitizer duplication | Extract common sanitizer/redaction classifier. | Needs policy for false positives/false negatives. |
| High | EvidenceRef ledger gap | Design and implement ledger bridge. | Needs schema decision. |
| Medium | Agent Operations location | Extract to Agent Operations projects. | Needs namespace/project boundary decision. |
| Medium | Recipe `CanExecute` ambiguity | Split manifest acceptance from runtime execution permission. | Needs contract change. |
| Medium | Step Library `IsAllowedInV1` ambiguity | Clarify catalog availability vs runtime permission. | Needs contract change. |
| Medium | `Nexa*` technical naming | Compatibility ADR and scoped rename plan. | Needs API compatibility decision. |
| Low | Python OCR scripts | Remove or archive after reference checks. | Needs OCR cleanup milestone. |
| Low | Empty `OneBrain.SemanticState` | Document future use or remove. | Needs project reference check. |
| Low | local tools `bin/obj` clutter | Ensure ignored and not tracked. | None; local hygiene. |

## 13. Candidate Future Module Boundaries

| Boundary | Contents | Why |
| --- | --- | --- |
| `OneBrain.AgentOperations.Contracts` | Mission, Task, EvidenceRef, RunReport, FailureTaxonomy, RecipeManifest, StepLibrary, ProgressReport, VerificationBeforeDone contracts | Agent Operations is not browser-specific. |
| `OneBrain.AgentOperations.Core` | validators, builders, mappers, gate, report sanitizer adapters | Keeps non-runtime governance services out of CDP. |
| `OneBrain.Redaction` or `OneBrain.Core.Redaction` | common sanitizer/redaction service | Removes duplicated marker-based sanitizers. |
| `OneBrain.Evidence` or Core evidence extension | ledger bridge, evidence refs, provenance mapping | Unifies Agent Operations refs with ledgers. |
| browser-specific adapter project | recipe/step/run adapters for CDP when execution is designed | Keeps execution separate from contracts. |

## 14. What Not To Touch Yet

- OCR active path.
- Browser runtime active path.
- Evidence ledger active path.
- SafeAction/FSM/policy behavior.
- VerificationBeforeDone gate behavior until canonicalization milestone.
- RunReport/ProgressReport behavior until canonicalization milestone.
- Agent Operations project relocation until boundary decision.
- `Nexa*` broad rename until compatibility ADR.
- Python OCR worker deletion until OCR legacy follow-up.
- Recipe execution.
- Orchestration API.
- Workboard UI.
- Scheduled runs.
- Package registry.
- Cloud runtime.

## 15. Recommended Next 6 Hitos

1. M377-M379 Completion Gate Canonicalization.
2. M380-M382 Common Redaction / Sanitizer Service.
3. M383-M385 EvidenceRef to Evidence Ledger Bridge Design.
4. M386-M388 Agent Operations Namespace / Project Boundary ADR.
5. M389-M391 Recipe / Step Runtime Permission Semantics Cleanup.
6. M392-M394 OCR Legacy Retained Cleanup Follow-Up or Product/Admin Legacy Graph.

## Decision

M374-M376 is ready with cleanup backlog.

Recommended readiness decision:

`M374+M375+M376 CERRADO / CORE_LEGACY_REFERENCE_GRAPH_READY_WITH_CLEANUP_BACKLOG`
