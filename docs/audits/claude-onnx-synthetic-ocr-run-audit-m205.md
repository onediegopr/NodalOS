# Claude Audit Prompt — ONNX Synthetic OCR Run (M205)

## Scope
Audit the first end-to-end synthetic ONNX OCR run implemented in M203–M205.

## Files to Review
- `src/OneBrain.BrowserExecutor.Contracts/NodalOsOnnxOcrSyntheticInferenceContracts.cs`
- `src/OneBrain.BrowserExecutor.Cdp/NodalOsOnnxOcrSyntheticInferencePipeline.cs`
- `src/OneBrain.BrowserExecutor.Contracts/NodalOsOnnxOcrResultNormalizationContracts.cs`
- `src/OneBrain.BrowserExecutor.Cdp/NodalOsOnnxOcrResultNormalizer.cs`
- `src/OneBrain.BrowserExecutor.Contracts/NodalOsOnnxSyntheticOcrReadinessContracts.cs`
- `src/OneBrain.BrowserExecutor.Cdp/NodalOsOnnxSyntheticOcrReadinessReview.cs`
- `tests/OneBrain.Safety.Tests/NodalOsOnnxOcrSyntheticInferenceM203Tests.cs`
- `tests/OneBrain.Safety.Tests/NodalOsOnnxOcrResultNormalizationM204Tests.cs`
- `tests/OneBrain.Safety.Tests/NodalOsOnnxSyntheticOcrReadinessM205Tests.cs`
- `docs/reports/onnx-synthetic-redacted-crop-ocr-run-m205.md`
- `artifacts/ocr-vision-onnx/m205/onnx-synthetic-ocr-run-summary.json`

## Audit Questions
1. Did the pipeline execute real ONNX Runtime inference only on a synthetic/redacted/non-sensitive crop?
2. Are model source URLs, hashes, and sizes correctly pinned to RapidOCR/ModelScope v3.8.0?
3. Does the code block production mode, full-screen, sensitive surfaces, raw persistence, and SaaS?
4. Is no-authority preserved in the normalized result, evidence, and timeline card?
5. Does the detector honestly report `NoTextDetected` when no text is found?
6. Is low confidence mapped to `RequiresHumanReview`?
7. Does the evidence contain raw image bytes or only redacted hashes/refs?
8. Does the readiness review gate advancement honestly (`ReadyForMoreSyntheticFixtures` vs `ReadyForRedactedCropShadow`)?
9. Are the required TestCategory filters present?
10. Is the full test suite flaky failure triaged as unrelated?

## Expected Decision
Confirm whether M203–M205 should close as `ReadyForMoreSyntheticFixtures` or if additional work is required before shadow mode.
