# PaddleOCR ONNX Models

This directory holds PaddleOCR ONNX models for NODAL OS local OCR.

## Model files

`.onnx` files are **gitignored** and are not committed. They must be downloaded using the provided script and verified before use.

## Download

```powershell
.\download-models.ps1 -Confirm
```

The script downloads the models from the configured source URLs and computes SHA-256 hashes.

## Verify

```powershell
.\verify-models.ps1
```

Checks file existence, size, and SHA-256 against `paddleocr-onnx-model-manifest.json`.

## Rollback

```powershell
.\rollback-models.ps1 -Confirm
```

Removes only the `.onnx` files under this directory. Does not delete code, data, or images.

## Conversion plan

If the RapidOCR/ModelScope source becomes unavailable, use `convert-models-plan.md` as the secondary path.

## License

Models are derived from PaddleOCR / RapidOCR and are used under the Apache-2.0 license.
