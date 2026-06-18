# CLAUDE — NODAL OS OCR FULL AUDIT (M340)
## M197-M339 Architecture, Safety, Simplification & Readiness Review

This document contains the senior technical audit of the OCR offline integration pipeline developed under milestones M197 to M339 within NODAL OS.

---

## A. Git State & Workspace Verification
- **Worktree:** `C:\DESARROLLO\NodalOS\Codigo-m12-audit`
- **Branch:** `chrome-lab-001-extension-local-ai-bridge`
- **HEAD Commit:** `b9e7f56`
- **Remote URL:** `https://github.com/onediegopr/NodalOS.git`
- **Path Protection:** Confirmed that the forbidden path `C:\Users\diego\OneDrive\PERSONAL\ONE Brain\Codigo` was NOT used or referenced during this audit.
- **Git Status:** 100% clean. The dirty files under `m177` and `m183` were discarded using `git checkout` to restore the repository to a clean state.

---

## B. Status of M177 & M183 Summaries
The modified statuses of `artifacts/ocr-vision-evaluation/m177/ocr-vision-evaluation-summary.json` and `artifacts/ocr-vision-worker/m183/synthetic-worker-run-summary.json` were investigated. 
- **Root Cause of Dirtiness:** Running local tests automatically updates the `CreatedAtUtc` timestamp and regenerates random GUID-based `DecisionId` and `AuditId` fields within these documents. No functional content or policy logic was modified.
- **Resolution:** Discarded these changes to keep the repository clean. It is recommended to configure the test runner to output temporary run outcomes to gitignored directories (e.g., `artifacts/temp/` or `scratch/`) instead of overwriting historical milestone summaries.

---

## C. Executive Summary
The offline OCR integration line (M197-M339) is **fully coherent, safety-first, and technically sound**. It has achieved the primary goal: local-only OCR capabilities integrated into the NODAL OS CDP/FSM runtime without SaaS dependencies, without exposing API keys, and with **absolute sandboxing of authority**.

The entire test suite of 285 tests (with 284 passing and 1 recipe skipped by design) runs cleanly. The system enforces policy restrictions at every boundary (pre-processing, evaluation, audit ledger, FSM observation, and assisted verification). 

Our final audit decision is **`OCR_LINE_AUDIT_PASS_WITH_MINOR_CLEANUP`**. The codebase is ready to proceed to the main roadmap once minor legacy/experimental paths are removed and artifacts are consolidated.

---

## D. Architectural Audit: Coherence, Overdesign & Duplication

### 1. Architectural Coherence
The data and control flow is well-structured and follows a logical pipeline:
1. **Model & Environment Acquisition:** Environment checks via `NodalOsPaddleOcrRuntimeInspector` verify dependencies.
2. **Preprocessing:** `NodalOsPaddleOcrRecognizerCropPreprocessor` aligns crop sizes and aspect ratios.
3. **Out-of-Process Run:** ONNX inference executes via the isolated runner `OneBrain.Tools.OnnxOcrProbeRunner`.
4. **Decoding:** Greedy CTC decode utilizing the `OfficialSpaceToken` policy resolves character indices.
5. **Observation Isolation:** `NodalOsLowRiskOcrObservationEvaluator` validates foreground state, active window titles, average RGB, and SHA-256 capture fingerprints.
6. **Evidence Integration:** `NodalOsOcrEvidenceAdapter` evaluates constraints and generates a structured evidence entry.
7. **Persistent Audit Ledger:** `NodalOsOcrEvidenceAuditConsumer` writes events to the tamper-evident `BrowserPersistentAuditLedger`.
8. **FSM Observation:** `NodalOsOcrFsmObservationConsumer` filters accepted auxiliary evidence and filters out rejected/uncertain/violation results.
9. **Assisted Verification:** `NodalOsAssistedVerificationPolicy` fuses signals, requiring non-OCR corroboration to reach `VerifiedLowRisk`.

### 2. Identified Overdesign
While essential for debugging and solving runtime crashes, the following experimental harnesses are overdesigned for a production-grade motor:
- **Version Matrix Planners:** Classes like `NodalOsOnnxRuntimeVersionExperimentPlanner` and `NodalOsOnnxRuntimeVersionDecisionService` designed to dynamically switch NuGet versions during testing. Since the project has successfully standardized on ONNX Runtime 1.22.1, these version-probing infrastructures are obsolete.
- **Experiment Builders:** Matrix generators like `NodalOsRecognizerRuntimeExperimentBuilder` that test combinations of graph optimizations, single-threading, and memory patterns on zero/ones/gradient tensors.

### 3. Duplicate Paths & Legacy Code
- **Python Worker vs. ONNX .NET:** `NodalOsPaddleOcrLocalWorkerAdapter` and `paddleocr_worker.py` represent a Python-based path that was demoted to a fallback when in-process/out-of-process ONNX Runtime .NET was chosen. Since .NET native execution is robust, this Python dependency adds fragile complexity (requiring virtualenv setups, permission checking, and script location validation) and should be deprecated.
- **Duplicate Preprocessors:** There are legacy pre-processing classes preserved from earlier milestones (e.g., nearest-neighbor per-axis stretching) that are now superseded by the `RatioPreservingRightPad` bilinear preprocessor.

---

## E. Safety, Privacy & No-Authority Verification

### 1. Absolute Action Block
OCR observations cannot approve any of the following critical user actions. The FSM and assisted verification engines explicitly assert that these values are hardcoded to `false` for OCR evidence:
- `CanAuthorizeAction = false`
- `CanApproveClick = false`
- `CanApproveSubmit = false`
- `CanApproveSend = false`
- `CanApproveDelete = false`
- `CanApprovePay = false`
- `CanApproveSign = false`

### 2. Integrity of Segregation
- **Rejected/Uncertain Segregation:** Under `NodalOsOcrFsmObservationConsumer.CanEnterObservationContext()`, only evidence with `NodalOsOcrObservationAcceptanceState.AcceptedEvidence` is permitted to enter the FSM context. Any rejected or uncertain evidence goes to `DiagnosticOnly` or `Excluded` lists.
- **Assisted Verification Signal Fusion:** The `NodalOsAssistedVerificationPolicy` requires a non-OCR signal (such as `KnownQaFixtureSignal` or `ManualExpectedValueSignal`) to corroborate the OCR findings. OCR alone, regardless of confidence (e.g., 0.99) or exact match status, results in a `RejectedOcrOnly` decision.
- **VerifiedLowRisk Bounds:** Even when successfully corroborated, the outcome is `VerifiedLowRisk`, which inherits `ReadOnlyObservationOnly = true` and `ActionsAllowed = false`.

### 3. Privacy & Raw Persistence
- **No Raw Persistence:** `RawImagePersisted` is hardcoded to `false` for all evaluations.
- **Redaction Gate:** Sensitive, credential, or document-level requests are intercepted at the evaluator level (`NodalOsLowRiskOcrObservationEvaluator`) and immediately rejected.
- **Log Redaction:** Rejection reasons and error logs are wrapped in `BrowserCredentialRedactor.Redact()` to prevent accidental leakage of sensitive UI contents.

---

## F. Deep Technical Audit of the OCR Engine

### 1. ONNX Model Handling & Manifests
- **Tracked Files:** No `.onnx` models or dictionary `.txt` files are checked into Git. The `.gitignore` files are correctly configured.
- **Model Acquisition:** Manifests (`paddleocr-onnx-model-manifest.json` and `paddleocr-recognizer-pair-candidates-manifest.json`) specify integrity checksums (SHA-256) and sizes. The acquisition scripts utilize these to avoid size/hash drift.

### 2. PP-OCRv4 Detector & PP-OCRv5 Recognizer Combination
- **Evaluation:** Using the PP-OCRv4 detector with the PP-OCRv5 English recognizer is highly acceptable. The detector is lightweight and stable, and the recognizer provides superior vocabulary parsing.
- **Space Token Policy:** The mismatch root cause was resolved. PP-OCRv5's output has `N+2` classes where the extra class at index `N+1` corresponds to the space character `" "` introduced by `use_space_char=true` in PaddleOCR. The mapping `blank(0) + dictionary(1..N) + space(N+1)` is correctly implemented.
- **No Softmax Duplication:** Softmax is NOT reapplied during greedy decoding, as the model output node (`fetch_name_0`) already includes a softmax activation, preventing double-activation distortion.

### 3. Preprocessing & Crop Alignment
- **Stretch vs. Padding:** Stretching crops to a fixed `48x320` shape distorted aspect ratios, yielding a high edit distance. The implementation of `RatioPreservingRightPad` (bilinear resizing to height 48, dynamic width, and zero-padding the remaining columns) increased crop exact matches from 0/5 to 3/5 in synthetic tests, and 3/3 on real controlled image fixtures.

### 4. Regional Capture & QA Window Host
- **WinForms Host:** The WinForms QA window host (`tools/qa-window-host`) is a clean testing fixture. It correctly uses structured anti-aliased font rendering (`Arial 92 Bold AntiAliasGridFit`) and DPI scaling to provide deterministic targets.
- **Fingerprinting Diff Gate:** Screen region captures are protected by fingerprint validation: SHA-256 hash, average RGB, dark-pixel ratio, and sample signature comparison. If the target window goes out of focus or is covered, `DiffScore > 0.01` or fingerprint mismatch triggers immediate rejection.

---

## G. Technical Debt & Flaky Tests
- **flaky Browser Tests:** Flakiness in CDP browser tests is unrelated to the OCR line. All OCR tests are unit tests or out-of-process console execution tests, which pass with 100% stability.
- **Temp files:** Temporary image files generated during regional capture are cleaned up immediately via `finally` blocks, avoiding DLL locks or system accumulation.

---

## H. Audit Decisions & Recommendations

### 1. Audit Decision: `OCR_LINE_AUDIT_PASS_WITH_MINOR_CLEANUP`
NODAL OS is fully ready to proceed to the main roadmap. The safety constraints are validated, and the OCR engine operates correctly under strict read-only/no-authority rules.

### 2. Recommended Cleanups before Next Hito (M341+)
- **Discard Dirty Files:** Discard local changes to `m177` and `m183` JSON files (completed).
- **Prune Legacy Python Worker:** Remove the legacy Python worker scripts (`tools/ocr-worker/paddleocr_worker.py`, `setup-paddleocr.ps1`, `rollback-paddleocr.ps1`, `check-paddleocr.ps1`) and the adapter `NodalOsPaddleOcrLocalWorkerAdapter.cs` as they are duplicate, unused paths.
- **Prune Version Matrix Probes:** Remove version-experiment code (`NodalOsOnnxRuntimeVersionExperimentPlanner`, `NodalOsOnnxRuntimeVersionDecisionService`, etc.) to simplify the solution.
- **Consolidate ADRs and Reports:** Archive old milestone-specific reports and maintain this audit report as the single master source of truth.

---

## I. Next Milestones (Y) & Files to Modify (Z)

### 1. List of Recommended Next Milestones
- **Milestone 1:** Prune Python worker dependencies and obsolete version matrix classes.
- **Milestone 2:** Consolidate contracts and services in `OneBrain.BrowserExecutor.Cdp` to simplify maintenance.
- **Milestone 3:** Wire low-risk regional OCR observations to internal tools (e.g. read-only visual grounding for non-critical label assertions).

### 2. Target Files to Modify/Remove in Next Milestone
- **To Remove:**
  - `src/OneBrain.BrowserExecutor.Cdp/NodalOsPaddleOcrWorkerServices.cs` (Python Worker Adapter)
  - `src/OneBrain.BrowserExecutor.Contracts/NodalOsPaddleOcrWorkerContracts.cs`
  - `tools/ocr-worker/paddleocr_worker.py`
  - `tools/ocr-worker/setup-paddleocr.ps1`
  - `tools/ocr-worker/check-paddleocr.ps1`
  - `tools/ocr-worker/rollback-paddleocr.ps1`
  - `src/OneBrain.BrowserExecutor.Cdp/NodalOsOnnxRuntimeVersionExperimentServices.cs` (if any)
  - Obsolete test files: `NodalOsPaddleOcrWorkerM192Tests.cs`, `NodalOsOnnxRuntimeVersionExperimentM232M234Tests.cs`
