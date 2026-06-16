# PaddleOCR Synthetic Redacted Crop Run Report (M193)

**Date:** 2026-06  
**Milestone:** M193  
**Decision:** `BlockedByEnvironment`

## Summary

Attempted a controlled synthetic redacted crop OCR run with the production-grade PaddleOCR local worker. The run was cleanly blocked because PaddleOCR/PaddlePaddle cannot be installed on the current Python 3.13 environment.

## Synthetic Crop

- Type: raw RGBA32, 128x64
- Content: simple non-sensitive bar pattern
- Pixel redaction V2: `CleanNoRedactionRequired`
- Full-screen: false
- Sensitive: false
- Raw persistence: false

## Runtime Status

| Component | Status |
|-----------|--------|
| Python | 3.13.14 |
| pip | Available |
| venv | Available |
| PaddleOCR | Not installed |
| PaddlePaddle | Not installed |

## Blocker

**Python 3.13 is not supported by PaddlePaddle/PaddleOCR wheels.**

The isolated install script `setup-paddleocr.ps1` refuses to proceed on Python 3.13 to prevent a broken install.

## What was exercised

- Synthetic crop generation.
- Pixel redaction V2 path.
- Runtime inspection.
- Adapter invocation policy.
- Clean blocked result.
- No real OCR executed.

## Conclusion

M193 is closed as **BlockedByEnvironment**. The implementation is production-grade and ready to execute real OCR as soon as Python 3.10/3.11 is available. No unsafe fallback was used.
