# PaddleOCR ONNX Model Acquisition Plan (M195)

**Date:** 2026-06  
**Milestone:** M195  
**Status:** `MODEL_MISSING` — awaiting manual acquisition with license review.

## Objective

Acquire or convert PaddleOCR ONNX models for use with ONNX Runtime .NET in NODAL OS.

## Options

### A. Download pre-converted ONNX models
- Check official PaddleOCR releases or Hugging Face for ONNX exports.
- Verify license (Apache-2.0) and attribution requirements.
- Verify SHA-256 checksum.
- Pros: fastest path.
- Cons: availability not guaranteed for latest models.

### B. Convert PaddleOCR models with Paddle2ONNX
- Use a temporary Python 3.10/3.11 environment (not production dependency).
- Install `paddle2onnx` and convert `.pdmodel` + `.pdiparams` to `.onnx`.
- Pros: full control over opset and model version.
- Cons: requires temporary Python tooling.

### C. C++ Paddle Inference fallback
- Use Paddle Inference C++ library directly.
- Pros: no ONNX conversion needed.
- Cons: heavier native dependency, harder packaging.

## Recommended option

**A or B**, depending on availability. Always verify checksums and license.

## Storage

- Local path: `tools/ocr-worker/models/onnx/`
- Do NOT commit large model files to Git unless explicitly decided.
- Use `.gitignore` or Git LFS if needed.

## Size limit

- Total max: 250 MB for required detection + recognition + classification models.
- Larger models require explicit approval.

## License

- PaddleOCR models are typically Apache-2.0.
- Attribution required.
- Commercial use allowed under Apache-2.0 but requires legal review.

## Rollback

- Remove `tools/ocr-worker/models/onnx/*.onnx` files.
- Update manifest status to `Missing`.
- ONNX worker returns `ModelMissing`.

## Checklist before first OCR run

- [ ] Models acquired or converted.
- [ ] SHA-256 checksums verified.
- [ ] License reviewed.
- [ ] Total size under limit.
- [ ] Manifest updated to `Verified`.
- [ ] Redaction V2 verified.
- [ ] Human escalation configured.
