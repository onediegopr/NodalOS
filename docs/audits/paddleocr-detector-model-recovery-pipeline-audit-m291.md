# Claude Audit Prompt: PaddleOCR Detector Model Recovery And Pipeline Retry M291

Audit M289-M291.

## Evidence

- Detector was absent before the block.
- Detector was acquired through the manifest-backed script.
- Detector URL: `https://www.modelscope.cn/models/RapidAI/RapidOCR/resolve/v3.8.0/onnx/PP-OCRv4/det/ch_PP-OCRv4_det_mobile.onnx`.
- Detector SHA-256 expected/observed: `d2a7720d45a54257208b1e13e36a8479894cb74155a5efe29462512d42f49da9`.
- Detector size expected/observed: `4745517`.
- `verify-models.ps1` passed.
- `.onnx` files remain gitignored and untracked.
- Pipeline ran out-of-process.
- Parent survived.
- Synthetic fixtures only.
- Detector produced 5 boxes for 5 fixtures.
- Recognizer consumed 5 detector-derived crops.
- Recognizer output shape: `[1,40,438]`.
- Official space policy applied: blank `0`, dictionary `1..436`, space `437`.
- Softmax was not reapplied.
- Decode result: 0 exact/normalized matches, 5 mismatches.
- Decision: `BLOCKED_BY_SYNTHETIC_PIPELINE_DECODE_EVIDENCE`.

## Questions

1. Was detector acquisition and verification sufficiently pinned and auditable?
2. Is it correct to keep `.onnx` files out of git?
3. Is the detector box evidence sufficient?
4. Does the evidence point to crop/preprocessing calibration rather than model availability?
5. Is the final decision honest and conservative?
6. What should the next calibration block prioritize: box expansion, crop aspect ratio, recognizer normalization, synthetic glyph style, or detector thresholding?

Confirm that no SaaS OCR, API keys, real screens, real documents, sensitive data, raw real-data persistence, or OCR-authority actions were introduced.
