# PaddleOCR ONNX Model Source Decision — M200

## Summary
After investigating official PaddleOCR channels, community conversions, and toolchain requirements, NODAL OS selects the RapidOCR/ModelScope pre-converted ONNX models as the primary source for the local .NET OCR pipeline.

## Decision
- **Primary source:** RapidOCR community ONNX models hosted on ModelScope.
- **Secondary source:** Paddle2ONNX conversion from official PaddleOCR/PaddleX models if RapidOCR source becomes unavailable.
- **Tertiary:** C++ Paddle Inference worker.
- **Fallback:** Python PaddleOCR legacy/experimental only.
- **SaaS OCR:** disabled-by-default.

## Why This Source
1. **No heavy toolchain** — avoids installing PaddlePaddle/PaddleX/paddle2onnx.
2. **Verified integrity** — SHA-256 hashes published by RapidOCR and confirmed against downloaded files and `X-Linked-Etag` header.
3. **Small footprint** — mobile model set ~13 MB total.
4. **Apache-2.0 license** — compatible with NODAL OS.
5. **ONNX Runtime .NET compatible** — direct `.onnx` files.

## Selected Models (V1)
| Model ID | Kind | File | Source URL | SHA-256 | Bytes |
|----------|------|------|------------|---------|-------|
| paddleocr-det-onnx | TextDetection | ch_PP-OCRv4_det.onnx | [ModelScope](https://www.modelscope.cn/models/RapidAI/RapidOCR/resolve/v3.8.0/onnx/PP-OCRv4/det/ch_PP-OCRv4_det_mobile.onnx) | `d2a7720d45a54257208b1e13e36a8479894cb74155a5efe29462512d42f49da9` | 4,745,517 |
| paddleocr-rec-onnx | TextRecognition | ch_PP-OCRv4_rec.onnx | [ModelScope](https://www.modelscope.cn/models/RapidAI/RapidOCR/resolve/v3.8.0/onnx/PP-OCRv4/rec/en_PP-OCRv4_rec_mobile.onnx) | `e8770c967605983d1570cdf5352041dfb68fa0c21664f49f47b155abd3e0e318` | 7,653,044 |
| paddleocr-cls-onnx | TextDirectionClassification | ch_ppocr_mobile_v2.0_cls.onnx | [ModelScope](https://www.modelscope.cn/models/RapidAI/RapidOCR/resolve/v3.8.0/onnx/PP-OCRv4/cls/ch_ppocr_mobile_v2.0_cls_mobile.onnx) | `e47acedf663230f8863ff1ab0e64dd2d82b838fceb5957146dab185a89d6215c` | 585,532 |

## Supply-Chain Risk
- RapidOCR is a community project based on PaddleOCR.
- Risk is mitigated by:
  - SHA-256 verification after download.
  - Pinned version (`v3.8.0`).
  - Rollback script to remove models if trust changes.
  - Conversion plan as secondary option.

## Compliance
- No SaaS OCR.
- No API keys.
- No real customer data.
- No authority claimed.
- Production OCR remains blocked.

## Next Step
M201: implement controlled acquisition scripts and M202: run first ONNX Runtime .NET session smoke.
