# ADR — OCR Runtime Strategy Pivot to ONNX Runtime .NET (M194-M196)

## Status

Accepted. Primary local OCR path is now ONNX Runtime .NET with PaddleOCR ONNX models.

## Context

M191-M193 prepared a production-grade Python PaddleOCR runtime but the environment has Python 3.13, which is unsupported by PaddlePaddle/PaddleOCR wheels. Forcing Python 3.10/3.11 adds operational fragility to a .NET/Windows product.

## Decision

1. **Primary:** ONNX Runtime .NET (`Microsoft.ML.OnnxRuntime`) + PaddleOCR ONNX models.
2. **Secondary:** C++ local worker (Paddle Inference or ONNX Runtime C++).
3. **Fallback:** Python PaddleOCR legacy/experimental only.
4. **SaaS:** disabled-by-default.
5. **Production public OCR:** remains blocked until final gate.

## Why ONNX Runtime .NET

- Native .NET integration.
- No Python runtime dependency in production.
- Better Windows packaging and deployment.
- Better memory/process control.
- Better CI/testing.
- Better commercial operational profile.

## Risks accepted

- Model size.
- Pre/post-processing complexity.
- Performance unknown until measured.
- Memory usage.

## Risks blocked

- Python runtime fragility (primary path).
- SaaS data leak.

## Constraints preserved

- Crop-only.
- Redacted-only.
- No raw persistence.
- No full-screen.
- No sensitive data.
- No authority.
- No API keys.

## Next steps

1. Acquire/convert PaddleOCR ONNX models (M195).
2. Build ONNX Runtime .NET worker skeleton (M196).
3. Implement pre/post-processing.
4. Run synthetic redacted crop OCR.
5. Commercial/legal/license review.
