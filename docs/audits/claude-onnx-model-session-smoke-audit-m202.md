# Claude Audit Prompt — ONNX Model Session Smoke (M202)

## Context
NODAL OS selected RapidOCR/ModelScope as the primary source for PaddleOCR ONNX models (M200), prepared controlled acquisition scripts (M201), and ran the first ONNX Runtime .NET session smoke tests (M202).

## Audit Request
Review the following and decide whether the project can advance to M203-M205 (first synthetic redacted-crop ONNX OCR inference):

1. `docs/adr/paddleocr-onnx-model-source-decision-m200-m202.md`
2. `docs/reports/paddleocr-onnx-model-source-decision-m200.md`
3. `tools/ocr-worker/models/onnx/paddleocr-onnx-model-manifest.json`
4. `tools/ocr-worker/models/onnx/download-models.ps1`
5. `tools/ocr-worker/models/onnx/verify-models.ps1`
6. `tools/ocr-worker/models/onnx/rollback-models.ps1`
7. `tools/ocr-worker/models/onnx/convert-models-plan.md`
8. `src/OneBrain.BrowserExecutor.Contracts/NodalOsOnnxModelAcquisitionContracts.cs`
9. `src/OneBrain.BrowserExecutor.Contracts/NodalOsOnnxModelSessionContracts.cs`
10. `src/OneBrain.BrowserExecutor.Cdp/NodalOsOnnxModelAcquisitionService.cs`
11. `src/OneBrain.BrowserExecutor.Cdp/NodalOsOnnxRuntimeSessionFactory.cs`
12. `src/OneBrain.BrowserExecutor.Cdp/NodalOsOnnxModelSessionSmokeTester.cs`
13. `src/OneBrain.BrowserExecutor.Cdp/NodalOsOnnxOcrOfflineReadinessGate.cs`
14. `tests/OneBrain.Safety.Tests/NodalOsOnnxModelSourceM200Tests.cs`
15. `tests/OneBrain.Safety.Tests/NodalOsOnnxModelAcquisitionM201Tests.cs`
16. `tests/OneBrain.Safety.Tests/NodalOsOnnxModelSessionSmokeM202Tests.cs`
17. `docs/reports/onnx-model-session-smoke-m202.md`
18. `artifacts/ocr-vision-onnx/m202/onnx-model-session-smoke-summary.json`

## Checklist
- [ ] Model source decision is documented and reasonable.
- [ ] Source is pinned, hashes are published, license is acceptable.
- [ ] Download/verify/rollback scripts exist and are safe.
- [ ] Models are not committed as large binaries (gitignored).
- [ ] Session factory loads verified models only.
- [ ] Dummy inference does not process real images.
- [ ] Session smoke reports honest status.
- [ ] Offline readiness gate returns `ReadyForOnnxSyntheticRun` only when all gates pass.
- [ ] No OCR is treated as authority.
- [ ] No SaaS, no real customer data, no sensitive documents.

## Constraints
- Do not approve production OCR activation.
- Do not approve use on real documents or sensitive surfaces.
- If models are missing, the only acceptable decision is `MODEL_MISSING_WITH_PRODUCTION_PLAN`.

## Expected Output
A short audit report: PASS / PARTIAL / BLOCKED, with concrete findings and next steps.
