# ONNX Model Session Smoke — M202

## Summary
First ONNX Runtime .NET session smoke tests executed against verified RapidOCR/ModelScope PaddleOCR ONNX models.

## Models Verified
| Model ID | File | SHA-256 | Size | Session Loaded |
|----------|------|---------|------|----------------|
| paddleocr-det-onnx | ch_PP-OCRv4_det.onnx | `d2a7720d45a54257208b1e13e36a8479894cb74155a5efe29462512d42f49da9` | 4,745,517 | yes |
| paddleocr-rec-onnx | ch_PP-OCRv4_rec.onnx | `e8770c967605983d1570cdf5352041dfb68fa0c21664f49f47b155abd3e0e318` | 7,653,044 | yes |
| paddleocr-cls-onnx | ch_ppocr_mobile_v2.0_cls.onnx | `e47acedf663230f8863ff1ab0e64dd2d82b838fceb5957146dab185a89d6215c` | 585,532 | yes |

## Source
- RapidOCR community ONNX conversion hosted on ModelScope.
- Pinned version: v3.8.0.
- License: Apache-2.0.

## Session Smoke Results
- ONNX Runtime .NET package: available.
- Detection model session: created, input/output metadata inspected.
- Recognition model session: created, input/output metadata inspected.
- Classification model session: created, input/output metadata inspected.
- Dummy inference: executed for detection model with zero tensor (no real image).
- Real image inference: NOT executed.

## Test Evidence
- `NodalOsOnnxModelSourceM200Tests`: 4 passed.
- `NodalOsOnnxModelAcquisitionM201Tests`: 5 passed.
- `NodalOsOnnxModelSessionSmokeM202Tests`: 4 passed.
- Total M200-M202 tests: 13 passed, 0 failed.

## Offline Readiness Decision
`ReadyForOnnxSyntheticRun`

## Next Step
M203-M205: first synthetic redacted-crop ONNX OCR inference.

## Compliance
- No real OCR on documents or screens.
- No SaaS OCR.
- No raw persistence.
- No full-screen or sensitive OCR.
- No authority claimed.
- Production public OCR remains blocked.
