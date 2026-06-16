# OCR Runtime Strategy Pivot Report (M194)

**Date:** 2026-06  
**Milestone:** M194  
**Decision:** Pivot primary local OCR runtime to ONNX Runtime .NET.

## Previous state

M191-M193 established a Python PaddleOCR production-grade plan but the local environment (Python 3.13) blocks PaddlePaddle/PaddleOCR installation. The module was closed as `BLOCKED_BY_ENVIRONMENT_WITH_PRODUCTION_PLAN`.

## New strategy

| Priority | Runtime | Purpose |
|----------|---------|---------|
| Primary | ONNX Runtime .NET + PaddleOCR ONNX models | Production local OCR |
| Secondary | C++ local worker | If .NET ONNX insufficient |
| Fallback | Python PaddleOCR | Legacy/experimental only |
| Last resort | Human review | Manual verification |
| Disabled | Cloud SaaS OCR | Default blocked |

## Rationale

- NODAL OS is a .NET/Windows product.
- ONNX Runtime .NET avoids Python runtime fragility.
- Better packaging, memory control, CI/testing, and commercial operations.
- Models can be versioned, checksummed, and loaded on demand.

## Constraints preserved

- Crop-only.
- Redacted-only.
- No raw persistence.
- No full-screen.
- No sensitive data.
- No authority.
- Production public OCR remains blocked.

## Status

`NodalOsOcrRuntimeStrategyService` reports `PrimaryReady` when ONNX Runtime package and verified ONNX models are available. Currently model files are missing, so execution remains blocked.
