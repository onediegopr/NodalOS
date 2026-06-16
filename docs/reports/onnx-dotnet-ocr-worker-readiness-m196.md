# ONNX Runtime .NET OCR Worker Readiness Report (M196)

**Date:** 2026-06  
**Milestone:** M196  
**Status:** `MODEL_MISSING_WITH_PRODUCTION_PLAN`

## Summary

The ONNX Runtime .NET worker skeleton is production-grade and ready, but real OCR inference is blocked because verified PaddleOCR ONNX models are not yet available.

## Components

| Component | Status |
|-----------|--------|
| ONNX Runtime .NET package (`Microsoft.ML.OnnxRuntime` 1.18.1) | Available |
| Worker skeleton (`NodalOsOnnxOcrWorker`) | Implemented |
| Invocation policy | Fail-closed |
| Pre-processing placeholder | Implemented |
| Post-processing placeholder | Implemented |
| Detection ONNX model | Missing |
| Recognition ONNX model | Missing |
| Classification ONNX model | Missing |

## Policy gates

- Pixel redaction V2 required.
- Crop-only.
- No full-screen.
- No sensitive.
- No raw persistence.
- No SaaS.
- No Python dependency.
- No authority.
- Production public OCR blocked.

## Decision

`NodalOsOnnxOcrWorkerExecutionMode.ModelMissing` — real OCR will not run until models are acquired, verified, and explicitly enabled.

## Next steps

1. Follow `docs/reports/paddleocr-onnx-model-acquisition-plan-m195.md`.
2. Download/convert and verify ONNX models.
3. Update `tools/ocr-worker/models/onnx/paddleocr-onnx-model-manifest.json`.
4. Implement pre/post-processing logic.
5. Run synthetic redacted crop OCR.
