# NODAL OS Core Legacy Reference Graph Discovery M374

Milestone: M374-M376
Base commit: 4d6db4f
Scope: discovery-only reference graph for core active paths, legacy, diagnostic-only assets, duplication risks, and extraction candidates.

## Constraints

- No runtime behavior change.
- No UI change.
- No recipe execution.
- No orchestration API.
- No namespace move.
- No legacy deletion.
- No broad rename.
- No OCR behavior change.
- No policy behavior change.

## Commands And Searches Used

`git status --short`

`git rev-parse --short HEAD`

`git branch --show-current`

`git remote -v`

`Get-ChildItem -Name`

`Get-ChildItem -Name src`

`Get-ChildItem -Name tests`

`Get-ChildItem -Name tools`

`Get-ChildItem -Name docs`

`Get-ChildItem -Name artifacts`

`Get-ChildItem -Path src,tests,tools,docs,artifacts -Recurse -File | Where-Object { $_.FullName -notmatch '\\(bin|obj)\\' } | Measure-Object`

`Select-String` searches were used because `rg` is not available on this machine.

Search terms:

- `Nexa`
- `OneBrain`
- `NodalOs`
- `Legacy`
- `Deprecated`
- `Obsolete`
- `SafeAction`
- `EvidenceLedger`
- `BrowserPersistentAuditLedger`
- `Fsm`
- `FSM`
- `Runtime`
- `Executor`
- `Recipe`
- `StepLibrary`
- `RunReport`
- `VerificationBeforeDone`
- `AgentProgress`
- `AgentWorkboard`
- `Ocr`
- `Python`
- `Worker`
- `Orchestration`
- `Approval`
- `Policy`
- `Sanitizer`
- `Redact`
- `Sensitive`

Additional targeted searches:

- tracked ONNX/model/dictionary/build output paths using `git ls-files`
- Agent Operations contracts/services
- OCR worker/probe/runtime symbols
- Evidence ledger and audit ledger symbols
- SafeAction/FSM/policy symbols
- sanitizer/redaction symbols

## Folders Reviewed

| Folder | Role | Notes |
| --- | --- | --- |
| `src/OneBrain.Core` | Active core execution, FSM, evidence, safety primitives, recipes, recording, history | ActivePath / ActiveSupport. |
| `src/OneBrain.Actions` | Desktop UIA action/read/type executors | ActiveSupport with legacy/quarantine comments. |
| `src/OneBrain.Observation` | UIA/window/visual observation | ActiveSupport. |
| `src/OneBrain.BrowserExecutor.Contracts` | Browser contracts plus Agent Operations contracts | ActivePath, CandidateForExtraction for Agent Operations. |
| `src/OneBrain.BrowserExecutor.Cdp` | Browser/CDP runtime, OCR, audit ledger, Agent Operations services | ActivePath with extraction and legacy cleanup candidates. |
| `src/OneBrain.Verification` | Basic action verification reports | ActiveSupport / CandidateForReview. |
| `src/OneBrain.Safety` | Minimal safety policy primitives | ActiveSupport. |
| `src/OneBrain.Pilot` | Pilot/demo execution surface | DiagnosticOnly / CandidateForReview before product use. |
| `tools/ocr-worker` | Historical Python OCR worker plus ONNX model acquisition area | DeprecatedRetained / DiagnosticOnly. |
| `tools/onnx-ocr-probe-runner` | OCR diagnostic runner | DiagnosticOnly. |
| `tools/qa-window-host` | QA window host for controlled OCR/capture fixtures | ActiveSupport for tests/diagnostics. |
| `tools/recipes` | Historical and active fixture recipes/reports | DiagnosticOnly unless explicitly wired. |
| `tests/OneBrain.Safety.Tests` | Main safety/regression suite | ActiveSupport. |
| `tests/OneBrain.Recipes.Tests` | Recipe regression suite | ActiveSupport. |
| `docs` | ADR/report/audit/roadmap history | ActiveSupport / HistoricalTrace. |
| `artifacts` | Historical milestone artifacts | HistoricalTrace / DiagnosticOnly. |

## Module Discovery Table

| Module / Path | Classification | Evidence | Notes |
| --- | --- | --- | --- |
| `src/OneBrain.Core/Execution/SafeExecutionFsm.cs` | ActivePath | FSM implementation exists in Core execution. | Keep as core authority surface. |
| `src/OneBrain.Core/Execution/EvidenceLedger.cs` | ActivePath | Core evidence ledger exists. | Needs future bridge with Agent Operations evidence refs. |
| `src/OneBrain.Core/Execution/SafeExecutorAuthorizationPolicy.cs` | ActivePath | Authorization policy exists in Core. | Do not bypass from recipes/steps. |
| `src/OneBrain.BrowserExecutor.Cdp/ChromeCdpBrowserExecutor.cs` | ActivePath | Browser/CDP executor exists. | Browser runtime active path. |
| `src/OneBrain.BrowserExecutor.Cdp/BrowserRuntimeSmoke.cs` | ActiveSupport | Smoke runner and cleanup probe exist. | M359-M361 hardened flake path. |
| `src/OneBrain.BrowserExecutor.Cdp/BrowserPersistentAuditLedger.cs` | ActivePath | Persistent browser audit ledger exists. | Active audit/evidence trail. |
| `src/OneBrain.BrowserExecutor.Cdp/NodalOsOcrEvidenceIntegrationServices.cs` | ActivePath | `NodalOsOcrEvidenceAuditConsumer` exists. | OCR audit consumer. |
| `src/OneBrain.BrowserExecutor.Cdp/NodalOsOcrFsmObservationServices.cs` | ActivePath | `NodalOsOcrFsmObservationConsumer` exists. | OCR observation read-only path. |
| `src/OneBrain.BrowserExecutor.Cdp/NodalOsAssistedVerificationServices.cs` | ActivePath | Assisted verification policy exists. | Read-only/evidence-only. |
| `src/OneBrain.BrowserExecutor.Cdp/NodalOsAgentWorkboardServices.cs` | ActiveSupport | Workboard validator and fixtures exist. | CandidateForExtraction. |
| `src/OneBrain.BrowserExecutor.Cdp/NodalOsRunReportingServices.cs` | ActiveSupport | Run report builder/sanitizer/fixtures exist. | CandidateForExtraction and sanitizer consolidation. |
| `src/OneBrain.BrowserExecutor.Cdp/NodalOsRecipeManifestServices.cs` | ActiveSupport | Recipe manifest serializer/validator exists. | Execution deferred. |
| `src/OneBrain.BrowserExecutor.Cdp/NodalOsVerificationBeforeDoneGate.cs` | ActiveSupport | Formal gate exists. | Should become canonical close gate. |
| `src/OneBrain.BrowserExecutor.Cdp/NodalOsAgentProgressReportingServices.cs` | ActiveSupport | Progress/blocker reporting exists. | Ready-to-close duplication candidate. |
| `src/OneBrain.BrowserExecutor.Cdp/NodalOsStepLibraryServices.cs` | ActiveSupport | Step Library V1 catalog exists. | Governance-only, execution deferred. |
| `src/OneBrain.BrowserExecutor.Cdp/NodalOsPaddleOcrWorkerServices.cs` | DeprecatedRetained | Python worker adapter remains. | Not active OCR path; retain until planned cleanup. |
| `tools/ocr-worker/paddleocr_worker.py` | DeprecatedRetained | Python worker script remains. | Historical legacy. |
| `src/OneBrain.BrowserExecutor.Cdp/NodalOsFullOcrHandoffRecognitionRuntimeServices.cs` | DiagnosticOnly | Runtime experiment/planner symbols remain. | Keep as historical diagnostic until cleanup milestone. |
| `src/OneBrain.BrowserExecutor.Cdp/NodalOsOnnxNativeRuntimeCrash*` | DiagnosticOnly | Crash probe/readiness symbols remain. | Retain for runtime diagnostics. |
| `tools/onnx-ocr-probe-runner` | DiagnosticOnly | Tool runner exists, build output untracked. | No deletion in this block. |
| `src/OneBrain.Core/Recording/SensitiveTextSanitizer.cs` | ActiveSupport | Existing sanitizer exists in Core. | Candidate common sanitizer source. |
| `src/OneBrain.Core/History/HistorySanitizer.cs` | ActiveSupport | Existing history sanitizer exists. | Candidate common sanitizer source. |
| `src/OneBrain.BrowserExecutor.Cdp/Nexa*` services | HistoricalLegacy / ActiveSupport mixed | Many historical `Nexa*` services remain. | Naming debt; do not rename broadly yet. |
| `src/OneBrain.BrowserExecutor.Contracts/Nexa*` contracts | HistoricalLegacy / ActiveSupport mixed | Many historical contracts remain. | Naming debt; document compatibility first. |

## Active Path Candidates

### Browser/CDP Runtime

Active candidate path:

1. `ChromeCdpBrowserExecutor`
2. `BrowserRuntimeSmoke`
3. `BrowserProfileSessionManager`
4. `BrowserRuntimePhaseServices`
5. `BrowserPersistentAuditLedger`
6. browser policy/sensitive/download/upload/vault services

Classification: ActivePath / ActiveSupport.

Risk note: keep browser flake cleanup, owned-process-only semantics, and profile cleanup intact.

### Core Safe Execution / FSM / Policy

Active candidate path:

1. `SafeExecutionFsm`
2. `SafeExecutorAuthorizationPolicy`
3. `EvidenceLedger`
4. `SafeClickPlanner`
5. `SafeClickStepVerifier`
6. `SafeReadStepVerifier`
7. `SafeTypeStepVerifier`
8. `LegacyExecutionGuard`
9. UIA read/type/pattern executors gated by policies

Classification: ActivePath / ActiveSupport.

Risk note: no Agent Operations validator should become an authorization source.

### OCR Active Path

Active candidate path:

1. ONNX .NET runtime services
2. PaddleOCR ONNX model catalog/readiness/verifier
3. QA window and controlled region capture
4. OCR evidence envelope/policy evaluation
5. `NodalOsOcrEvidenceAuditConsumer`
6. `BrowserPersistentAuditLedger`
7. `NodalOsOcrFsmObservationConsumer`
8. `NodalOsAssistedVerificationPolicy`

Classification: ActivePath / ActiveSupport.

Risk note: Python worker is retained but should remain out of active path.

### Evidence/Audit Path

Active candidate path:

1. `EvidenceLedger`
2. `BrowserPersistentAuditLedger`
3. OCR evidence ledger contracts/services
4. browser external proof evidence pack builders
5. private preview evidence verifier/freeze services

Classification: ActivePath / ActiveSupport.

Risk note: Agent Operations `NexaEvidenceRef` is not yet ledger-integrated.

### Agent Operations Platform Layer

Active candidate path:

1. Agent Workboard contracts/services
2. Failure taxonomy and Run Report V1
3. Recipe Manifest V1
4. Verification Before Done Gate
5. Blocker/Progress Reporting Contract
6. Step Library V1

Classification: ActiveSupport / CandidateForExtraction.

Risk note: current location inside `BrowserExecutor.*` is acceptable temporarily, but not a final boundary.

## Legacy Candidates

| Path | Classification | Evidence | Recommended action |
| --- | --- | --- | --- |
| `tools/ocr-worker/paddleocr_worker.py` | DeprecatedRetained | Python worker still present. | Keep until dedicated OCR legacy cleanup follow-up. |
| `src/OneBrain.BrowserExecutor.Cdp/NodalOsPaddleOcrWorkerServices.cs` | DeprecatedRetained | Worker adapter remains. | Deprecate/guard; no removal without reference graph follow-up. |
| `src/OneBrain.BrowserExecutor.Contracts/NodalOsPaddleOcrWorkerContracts.cs` | DeprecatedRetained | Worker contracts remain. | Keep for historical tests until cleanup. |
| `tools/ocr-worker/check-paddleocr.ps1` | HistoricalLegacy | Python OCR utility. | Cleanup candidate. |
| `tools/ocr-worker/setup-paddleocr.ps1` | HistoricalLegacy | Python OCR setup. | Cleanup candidate. |
| `tools/ocr-worker/rollback-paddleocr.ps1` | HistoricalLegacy | Python OCR rollback. | Cleanup candidate. |
| `src/OneBrain.BrowserExecutor.Cdp/Nexa*` | HistoricalLegacy / ActiveSupport mixed | Old product naming remains. | No broad rename yet. |
| `src/OneBrain.BrowserExecutor.Contracts/Nexa*` | HistoricalLegacy / ActiveSupport mixed | Old contract naming remains. | Compatibility ADR first. |

## Diagnostic-Only Candidates

| Path | Classification | Reason |
| --- | --- | --- |
| `src/OneBrain.BrowserExecutor.Cdp/NodalOsFullOcrHandoffRecognitionRuntimeServices.cs` | DiagnosticOnly | Runtime version experiment/planner area. |
| `src/OneBrain.BrowserExecutor.Cdp/NodalOsOnnxNativeRuntimeCrashProbeMatrixBuilder.cs` | DiagnosticOnly | Crash probe matrix. |
| `src/OneBrain.BrowserExecutor.Cdp/NodalOsOnnxNativeRuntimeCrashReadinessReview.cs` | DiagnosticOnly | Crash readiness review. |
| `src/OneBrain.BrowserExecutor.Cdp/NodalOsGuardedSyntheticTextOcrProbeServices.cs` | DiagnosticOnly | Guarded synthetic OCR probes. |
| `tools/onnx-ocr-probe-runner` | DiagnosticOnly | Out-of-process OCR probe runner. |
| `tools/qa-window-host` | ActiveSupport / DiagnosticOnly | Controlled QA capture host. |
| `tools/recipes/*-negative.json` | DiagnosticOnly | Negative fixture recipes. |
| `artifacts/ocr-vision-*` | HistoricalTrace | Milestone artifacts. |

## Duplication Notes

| Duplication | Files | Risk |
| --- | --- | --- |
| Completion gate semantics | `NodalOsAgentWorkboardServices.cs`, `NodalOsVerificationBeforeDoneGate.cs`, `NodalOsAgentProgressReportingServices.cs`, `NodalOsRunReportingServices.cs` | Medium-high before UI/orchestration. |
| Sanitizer marker logic | `NodalOsRunReportingServices.cs`, `NodalOsRecipeManifestServices.cs`, `NodalOsAgentProgressReportingServices.cs`, `NodalOsStepLibraryServices.cs`, Core sanitizers | High before persistence/runtime reports. |
| Evidence refs vs ledgers | `NexaEvidenceRef`, `EvidenceLedger`, `BrowserPersistentAuditLedger` | Medium-high audit fragmentation risk. |
| Recipe action policy vs Step Library policy metadata | `NodalOsRecipeManifestValidator`, `NodalOsStepLibraryValidator` | Medium future runtime ambiguity. |
| Run report vs progress report | `NexaRunReport`, `NodalOsAgentProgressReport` | Medium duplicate reporting risk. |
| Legacy naming | `Nexa*`, `OneBrain*` | Medium naming debt, low functional risk. |

## Unknown / Needs Review

| Area | Reason |
| --- | --- |
| `src/OneBrain.Pilot` | Pilot executor/catalog exists; classify before product use. |
| old `Nexa*` product/admin/private preview services | Some may still be active support, some historical. Requires focused product/admin reference graph. |
| `src/OneBrain.SemanticState` | Empty project folder; candidate for cleanup or future use documentation. |
| tools build output under `tools/*/bin` and `tools/*/obj` | Untracked in `git ls-files`, but visible locally; ensure ignored/clean in CI. |

## Risk Notes

- No active path ambiguity blocks this milestone.
- No evidence supports immediate deletion in this block.
- Agent Operations location risk is real but not a blocker before Core Legacy Reference Graph completion.
- Sanitizer duplication is not acceptable for future persistence/runtime integration.
- `Nexa*` naming should be handled as compatibility debt, not as an urgent broad rename.
