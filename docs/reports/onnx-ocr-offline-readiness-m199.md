# ONNX OCR Offline Readiness — M199

## Scope
M199 creates the readiness gate that decides whether M200–M202 can attempt a real ONNX OCR run on synthetic redacted crops.

## Gate Components
- `NodalOsOnnxOcrOfflineReadinessGate`
- `NodalOsOnnxOcrOfflineReadinessReport`
- `NodalOsOnnxOcrOfflineReadinessRequirement`
- `NodalOsOnnxOcrSyntheticFixtureSet`

## Requirements Checked
1. ONNX Runtime .NET package available.
2. Detection model verified.
3. Recognition model verified.
4. Model shapes known (via fixture set).
5. Pre-processor ready.
6. Detector post-processor ready.
7. Recognizer post-processor ready.
8. No raw persistence.
9. No full-screen OCR.
10. No sensitive OCR.
11. No SaaS OCR.
12. OCR is not authority.
13. Production public OCR blocked.

## Current Decision
`MODEL_MISSING_WITH_PRODUCTION_PLAN`

Because the PaddleOCR ONNX model files are not present, the gate correctly returns `ModelMissing` / `ModelUnverified`. The pre/post-processing path is ready for known fixtures.

## Decisions Supported
- `ReadyForOnnxSyntheticRun`
- `ModelMissing`
- `ModelUnverified`
- `UnsupportedModelShape`
- `PreProcessingIncomplete`
- `PostProcessingIncomplete`
- `BlockedByRedaction`
- `BlockedByPolicy`
- `NotReady`

## Compliance
- No real OCR executed.
- No SaaS used.
- No raw persistence.
- No full-screen or sensitive OCR.
- No authority claimed.
- Production public OCR remains blocked.

## Next Step
Acquire and verify PaddleOCR ONNX models (M197), then re-run the M199 gate to transition from `ModelMissing` to `ReadyForOnnxSyntheticRun`.
