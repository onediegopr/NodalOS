# ADR - M295-M297 PaddleOCR Synthetic Pipeline Calibration Decision

## Status

Accepted.

## Context

After M271-M282 resolved the extra-class/space policy, runtime and softmax, the synthetic
detector→recognizer pipeline still decoded imperfectly (M286-M294), even though:

- the recognizer alone, over crops rendered directly at the correct aspect ratio into `48×320`, achieved an
  **exact match** in M283-M285 (`MARMOLES PVC -> MARMOLES PVC`);
- class count (438/438), softmax, `[B,T,C]` layout and the `OfficialSpaceToken` policy were all correct.

M292-M294 brute-forced 45 margin/unclip/crop combinations and only reached edit distance 1-2 with errors
(`V->Y`, `M->IA`, `RO->E`) that look like glyph distortion. The user asked to stop blind calibration and first
isolate whether the cause is synthetic font fidelity or crop/preprocessing.

## Decision

The dominant residual cause is **geometry, not font fidelity**: the detector→recognizer bridge stretched
crops to a fixed `[48,320]` (per-axis nearest neighbour), distorting aspect ratio. The same glyphs decoded
exactly when fed at correct aspect (M283-M285), which isolates the resize as the variable.

Adopt PaddleOCR `resize_norm_img` semantics for the recognizer crop: height 48, width
`min(320, ceil(48·aspect))` (aspect preserved), bilinear resampling, `(x-0.5)/0.5` normalization, right-pad
with `0.0`. Implement it as a pure, unit-tested preprocessor and make it the pipeline default; keep the legacy
stretch for A/B.

Final decision:

`M295+M296+M297 CERRADO / READY_FOR_PADDLEOCR_PREPROCESSING_ALIGNMENT_FIX`

## Scope

- This block lands and unit-tests the **geometry fix** (deterministic, ONNX-free).
- It does **not** claim new ONNX decode strings; capturing them is the next Codex block (A/B probe).
- No productive/public OCR readiness is claimed.

## Consequences

- Blind margin/unclip calibration is retired in favour of a principled, PaddleOCR-aligned resize.
- A follow-up A/B ONNX probe (`--recognizer-resize ratio|stretch`) will confirm decode improvement and decide
  between `READY_FOR_INTERNAL_CONTROLLED_REAL_IMAGE_FIXTURES` and adding bilinear-on-crop /
  `get_rotate_crop_image` perspective crop.
- Two inconsistent recognizer preprocessors (M198 height-32/ImageNet vs M283 height-48/stretch) are now
  documented; the aligned preprocessor is the reference going forward.
- Safety posture unchanged: internal-only, synthetic, no-authority, out-of-process guard, no models/dicts
  committed.
