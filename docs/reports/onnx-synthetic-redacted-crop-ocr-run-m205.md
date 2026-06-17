# ONNX Synthetic Redacted-Crop OCR Run — M205

## Summary
First end-to-end ONNX Runtime .NET OCR inference pipeline executed on a synthetic, redacted, non-sensitive crop. The pipeline covered pixel-redaction validation, image pre-processing, detector pre-processing, real ONNX detection inference, detector post-processing, and readiness to run recognition if boxes exist.

## Models Used
| Model | File | SHA-256 | Status |
|-------|------|---------|--------|
| Detection | `tools/ocr-worker/models/onnx/ch_PP-OCRv4_det.onnx` | `d2a7720d45a54257208b1e13e36a8479894cb74155a5efe29462512d42f49da9` | Verified, session loaded, inference attempted |
| Recognition | `tools/ocr-worker/models/onnx/ch_PP-OCRv4_rec.onnx` | `e8770c967605983d1570cdf5352041dfb68fa0c21664f49f47b155abd3e0e318` | Verified, session loaded, not needed (no boxes) |
| Classification | `tools/ocr-worker/models/onnx/ch_ppocr_mobile_v2.0_cls.onnx` | `e47acedf663230f8863ff1ab0e64dd2d82b838fceb5957146dab185a89d6215c` | Verified, optional for V1 |

## Synthetic Crop
- Dimensions: 128 x 64 pixels.
- Format: raw RGBA32.
- Pattern: horizontal black/white stripes (non-text synthetic pattern).
- Redaction: `CleanNoRedactionRequired` (no sensitive regions).
- `safeForOcr=true`, `originalRawPersisted=false`, `fullScreen=false`, `sensitive=false`.

## Inference Result
- Detection status: `NoTextDetected`.
- Detection boxes: 0.
- Recognition runs: 0 (no boxes to recognize).
- Total inference time: < 1 s.
- The detector correctly reported no text regions for a non-text stripe pattern.

## Normalization & Evidence
- `NodalOsOnnxOcrResultNormalizer` produced a normalized result with status `NoTextDetected`.
- `NodalOsOnnxOcrEvidenceBuilder` produced a redacted evidence summary (no raw image).
- `NodalOsOnnxOcrTimelineAdapter` produced a timeline evidence card.
- `NodalOsOnnxOcrHumanReviewPolicy` did not require human review for the no-text case.

## Readiness Decision
`ReadyForMoreSyntheticFixtures`

The pipeline is structurally ready and the models run, but the synthetic fixture did not produce detectable text. Before advancing to `ReadyForRedactedCropShadow`, we need additional synthetic fixtures with actual text-like patterns to validate detection + recognition quality.

## Compliance
- No real OCR on documents or screens.
- No SaaS OCR.
- No raw persistence.
- No full-screen OCR.
- No sensitive OCR.
- No OCR as authority.
- Production public OCR remains blocked.
- CDP pipeline not yet integrated.

## Next Step
M206–M208: richer synthetic text fixtures and/or controlled shadow mode on real redacted non-sensitive crops.
