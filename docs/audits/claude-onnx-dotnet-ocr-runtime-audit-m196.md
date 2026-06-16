# Claude Audit Prompt — ONNX Runtime .NET OCR Runtime (M196)

## Request

Please audit the NODAL OS M194-M196 ONNX Runtime .NET OCR pivot.

## Scope

1. **Strategy pivot (M194)**
   - `NodalOsOcrRuntimeStrategyService` primary = `OnnxRuntimeDotNet`.
   - Python PaddleOCR demoted to fallback/legacy.
   - SaaS disabled-by-default.

2. **Model acquisition plan (M195)**
   - `NodalOsPaddleOcrOnnxModelManifest` and placeholder manifest JSON.
   - `paddleocr-onnx-model-acquisition-plan-m195.md`.
   - No automatic large downloads.

3. **ONNX worker skeleton (M196)**
   - `NodalOsOnnxOcrWorker`.
   - `Microsoft.ML.OnnxRuntime` 1.18.1 package reference.
   - Pre/post-processing placeholders.
   - Readiness gate.

## Questions for Claude

1. Is ONNX Runtime .NET a sound primary path for a .NET/Windows OCR module?
2. Are the model acquisition constraints reasonable (manual download, checksum, license review)?
3. Does the worker correctly block real inference when models are missing?
4. Are all no-authority / no raw / no full-screen / no sensitive gates preserved?
5. What remains before the first real ONNX OCR run?
6. Should pre/post-processing be implemented before or after model acquisition?
7. Are there any licensing concerns with PaddleOCR ONNX models (Apache-2.0)?

## Context

- No real OCR executed.
- No models downloaded.
- Production public OCR remains disabled.
- Models are missing; worker returns `ModelMissing`.
