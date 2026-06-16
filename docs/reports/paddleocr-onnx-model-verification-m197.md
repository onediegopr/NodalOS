# PaddleOCR ONNX Model Verification — M197

## Scope
M197 verifies that the PaddleOCR ONNX model acquisition and integrity infrastructure is production-grade, even though the actual model files are not present in this repository.

## Models Required
| Model ID | Kind | Version | Language | License | Status |
|----------|------|---------|----------|---------|--------|
| paddleocr-det-onnx | TextDetection | v4 | multi | Apache-2.0 | Missing |
| paddleocr-rec-onnx | TextRecognition | v4 | en | Apache-2.0 | Missing |
| paddleocr-cls-onnx | TextDirectionClassification | v2 | multi | Apache-2.0 | Missing |

## Source
- Origin: PaddleOCR official models converted to ONNX via Paddle2ONNX.
- URL: https://github.com/PaddlePaddle/PaddleOCR/blob/main/docs/version3.x/inference_deployment/others/obtaining_onnx_models.html
- License: Apache-2.0
- Attribution required: yes

## Acquisition Command (documented, not executed)
```bash
# Requires PaddleX + paddle2onnx plugin; NOT executed in this session.
paddlex --paddle2onnx --paddle_model_dir /path/to/paddle/det --onnx_model_dir tools/ocr-worker/models/onnx --opset_version 17
paddlex --paddle2onnx --paddle_model_dir /path/to/paddle/rec --onnx_model_dir tools/ocr-worker/models/onnx --opset_version 17
```

## Verification Results
- No model files downloaded or committed in this session.
- Reason: PaddleOCR does not publish standalone ONNX release assets; conversion requires the PaddleX/paddle2onnx Python toolchain, which is considered a heavy toolchain and was not installed without explicit authorization.
- Decision: `MODEL_MISSING_WITH_PRODUCTION_PLAN`.

## Integrity Infrastructure Implemented
- `NodalOsPaddleOcrOnnxModelCatalogService`
- `NodalOsPaddleOcrOnnxModelVerifierService`
- `NodalOsPaddleOcrOnnxModelIntegrityCheckerService`
- `NodalOsPaddleOcrOnnxModelReadinessService`
- Manifest loader with shape/input/output metadata.
- SHA-256 checksum verification.
- Size-limit enforcement.
- License acceptance gate.

## Checksums / Sizes
All checksums and sizes are currently empty (`""` / `0`) because the model files are missing.

## What is Missing
- Actual `.onnx` files.
- Verified SHA-256 hashes.
- License review sign-off.

## Next Step
Run the documented Paddle2ONNX conversion in a controlled Python 3.10/3.11 environment, compute hashes, update `tools/ocr-worker/models/onnx/paddleocr-onnx-model-manifest.json`, and re-run M197 tests.

## Compliance
- No real OCR executed.
- No SaaS used.
- No raw persistence.
- No full-screen or sensitive OCR.
- No authority claimed.
