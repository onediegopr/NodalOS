# PaddleOCR ONNX Conversion Plan (Secondary Path)

## Status
Secondary / fallback. Not executed in this session.

## When to Use
If the RapidOCR/ModelScope source becomes unavailable, untrusted, or incompatible with NODAL OS.

## Toolchain Requirements
- Python 3.10 or 3.11 (PaddlePaddle does not support 3.13).
- PaddlePaddle CPU wheel for Windows.
- PaddleX CLI with paddle2onnx plugin.
- Official PaddleOCR inference models (`*.pdmodel` + `*.pdiparams`).

## Steps
1. Create an isolated Python virtual environment outside the repo.
2. Install dependencies:
   ```bash
   python -m pip install paddlepaddle==x.y.z
   paddlex --install paddle2onnx
   ```
3. Download official PaddleOCR inference models from PaddleOCR release assets.
4. Convert each model:
   ```bash
   paddlex --paddle2onnx --paddle_model_dir /path/to/det --onnx_model_dir tools/ocr-worker/models/onnx --opset_version 17
   paddlex --paddle2onnx --paddle_model_dir /path/to/rec --onnx_model_dir tools/ocr-worker/models/onnx --opset_version 17
   paddlex --paddle2onnx --paddle_model_dir /path/to/cls --onnx_model_dir tools/ocr-worker/models/onnx --opset_version 17
   ```
5. Rename outputs to match `paddleocr-onnx-model-manifest.json`.
6. Compute SHA-256 and update manifest.
7. Run `verify-models.ps1`.
8. Run M202 session smoke tests.

## Risks
- Heavy toolchain (~500 MB+ of Python packages).
- Version compatibility between PaddlePaddle, PaddleX, paddle2onnx, and opset 17.
- Windows-specific build issues.
- Operational fragility compared to downloading verified ONNX files.

## Rollback
Run `rollback-models.ps1 -Confirm` to delete converted `.onnx` files.
Run `deactivate` and delete the virtual environment to remove the toolchain.

## License
Converted models remain under PaddleOCR's Apache-2.0 license.
