# Claude Audit Prompt — ONNX OCR Offline Readiness (M199)

## Context
NODAL OS is preparing a local OCR pipeline based on PaddleOCR ONNX models + ONNX Runtime .NET. M197 acquired/verified model infrastructure; M198 implemented pre/post-processing; M199 implemented the offline readiness gate.

## Audit Request
Review the following and report whether the system is ready for a first real ONNX OCR run on synthetic redacted crops (M200–M202):

1. `tools/ocr-worker/models/onnx/paddleocr-onnx-model-manifest.json`
2. `src/OneBrain.BrowserExecutor.Cdp/NodalOsPaddleOcrOnnxModelCatalogService.cs`
3. `src/OneBrain.BrowserExecutor.Cdp/NodalOsPaddleOcrOnnxModelVerifierService.cs`
4. `src/OneBrain.BrowserExecutor.Cdp/NodalOsPaddleOcrOnnxModelIntegrityCheckerService.cs`
5. `src/OneBrain.BrowserExecutor.Cdp/NodalOsPaddleOcrOnnxModelReadinessService.cs`
6. `src/OneBrain.BrowserExecutor.Cdp/NodalOsOnnxOcrImagePreProcessor.cs`
7. `src/OneBrain.BrowserExecutor.Cdp/NodalOsOnnxOcrDetectorPreProcessor.cs`
8. `src/OneBrain.BrowserExecutor.Cdp/NodalOsOnnxOcrRecognizerPreProcessor.cs`
9. `src/OneBrain.BrowserExecutor.Cdp/NodalOsOnnxOcrDetectorPostProcessor.cs`
10. `src/OneBrain.BrowserExecutor.Cdp/NodalOsOnnxOcrRecognizerPostProcessor.cs`
11. `src/OneBrain.BrowserExecutor.Cdp/NodalOsOnnxOcrCharacterDictionary.cs`
12. `src/OneBrain.BrowserExecutor.Cdp/NodalOsOnnxOcrOfflineReadinessGate.cs`
13. `tests/OneBrain.Safety.Tests/NodalOsPaddleOcrOnnxModelVerificationM197Tests.cs`
14. `tests/OneBrain.Safety.Tests/NodalOsOnnxOcrPrePostProcessingM198Tests.cs`
15. `tests/OneBrain.Safety.Tests/NodalOsOnnxOcrOfflineReadinessM199Tests.cs`

## Checklist
- [ ] Model manifest is complete: modelId, kind, version, source, license, checksum, size, shape, opset.
- [ ] Required models: TextDetection + TextRecognition.
- [ ] No model files are committed without explicit decision.
- [ ] Verification checks file existence + size + SHA-256.
- [ ] License acceptance gate exists.
- [ ] Pre-processor blocks missing redaction, raw persistence, full-screen, sensitive.
- [ ] Detector post-processor rejects unsupported shapes and does not invent boxes.
- [ ] Recognizer post-processor performs CTC decode and flags low confidence.
- [ ] Offline readiness gate returns an honest decision.
- [ ] No OCR is treated as authority.
- [ ] No SaaS, no real customer data, no sensitive documents.

## Constraints
- Do not approve production OCR activation.
- Do not approve use on real documents or sensitive surfaces.
- If models are missing, the only acceptable decision is `MODEL_MISSING_WITH_PRODUCTION_PLAN` or `ModelMissing`.

## Expected Output
A short audit report: PASS / PARTIAL / BLOCKED, with concrete findings and next steps.
