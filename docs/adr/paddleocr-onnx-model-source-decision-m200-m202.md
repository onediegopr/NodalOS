# ADR — PaddleOCR ONNX Model Source Decision (M200-M202)

## Status

Accepted. Primary source: verified RapidOCR/ModelScope ONNX model files.

## Context

M197-M199 left the local OCR pipeline in `MODEL_MISSING_WITH_PRODUCTION_PLAN` because no verified PaddleOCR ONNX files were available in the repository or from an obvious official source. PaddleOCR itself publishes only Paddle-format inference models; converting them requires the PaddleX/paddle2onnx Python toolchain, which is heavy and was not authorized for installation in this environment.

## Options Evaluated

### A. Download pre-converted ONNX models from RapidOCR/ModelScope
- **Source:** RapidOCR community project, which converts PaddleOCR models to ONNX and publishes them.
- **URLs:** ModelScope CDN (`https://www.modelscope.cn/models/RapidAI/RapidOCR/resolve/v3.8.0/onnx/...`).
- **License:** Apache-2.0.
- **Size:** Mobile set ~13 MB total (det 4.5 MB, rec 7.3 MB, cls 0.6 MB).
- **SHA-256:** Published in RapidOCR `default_models.yaml`.
- **Windows CPU:** yes.
- **ONNX Runtime .NET:** yes.
- **Reproducibility:** high (URLs pinned to v3.8.0, hashes known).
- **Supply-chain risk:** medium (community mirror, not PaddlePaddle official, but hashes match downloaded files and ModelScope returns the same SHA as `X-Linked-Etag`).

### B. Convert PaddleOCR/PaddleX models with Paddle2ONNX
- **Toolchain:** Python 3.10/3.11 + PaddlePaddle + PaddleX + paddle2onnx plugin.
- **Reproducibility:** high if versions pinned.
- **Size:** similar.
- **License:** Apache-2.0.
- **Windows CPU:** yes.
- **Risks:** heavy toolchain, Python dependency during build, version fragility, CI complexity.

### C. C++ local worker with Paddle Inference
- **Pros:** avoids ONNX conversion.
- **Cons:** adds C++ build/packaging complexity, secondary path only.

### D. Python PaddleOCR legacy
- **Pros:** direct official support.
- **Cons:** Python 3.13 incompatible in this environment; operational fragility; not acceptable as primary.

### E. Human review fallback
- **Cons:** no automation, last resort only.

## Decision

1. **Primary (M200-M202):** Option A — download verified RapidOCR/ModelScope ONNX models.
2. **Secondary:** Option B — Paddle2ONNX conversion if Option A becomes unavailable or untrusted.
3. **Tertiary:** Option C — C++ Paddle Inference worker.
4. **Fallback:** Option D — Python PaddleOCR legacy/experimental only.
5. **Last resort:** Option E — human review.

## Models Selected (V1)

| Model ID | File | Source URL | SHA-256 | Size |
|----------|------|------------|---------|------|
| paddleocr-det-onnx | ch_PP-OCRv4_det.onnx | ModelScope | d2a7720d45a54257208b1e13e36a8479894cb74155a5efe29462512d42f49da9 | 4,745,517 |
| paddleocr-rec-onnx | ch_PP-OCRv4_rec.onnx | ModelScope | e8770c967605983d1570cdf5352041dfb68fa0c21664f49f47b155abd3e0e318 | 7,653,044 |
| paddleocr-cls-onnx | ch_ppocr_mobile_v2.0_cls.onnx | ModelScope | e47acedf663230f8863ff1ab0e64dd2d82b838fceb5957146dab185a89d6215c | 585,532 |

## Why RapidOCR/ModelScope

- Smallest operational footprint for NODAL OS (.NET-only production path).
- Verified SHA-256 hashes match downloaded files.
- Apache-2.0 license compatible with NODAL OS.
- Models are small enough to remain gitignored and downloaded on demand.
- Avoids installing PaddlePaddle/PaddleX/paddle2onnx in this environment.

## Risks Accepted

- Supply-chain trust in RapidOCR community conversion (mitigated by hash verification).
- ModelScope availability (mirror could be replaced by Hugging Face or direct download if needed).
- Pre/post-processing shapes must be validated against real model metadata.

## Risks Blocked

- Heavy Python toolchain as production dependency.
- PaddlePaddle runtime fragility.
- SaaS OCR data leak.

## Constraints Preserved

- Crop-only.
- Redacted-only.
- No raw persistence.
- No full-screen.
- No sensitive data.
- No authority.
- No API keys.
- Production public OCR remains blocked.

## Next Steps

1. M201: Implement controlled download/verify/rollback scripts.
2. M202: Run first ONNX Runtime .NET session smoke with verified models.
3. M203-M205: First synthetic redacted-crop inference.
