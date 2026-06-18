# Claude Audit Prompt - PaddleOCR Synthetic Pipeline Calibration (M295-M297)

## Purpose

Independently verify that the residual synthetic detector→recognizer decode failures are caused by the crop
bridge distorting aspect ratio (stretch to fixed `48×320`), not by synthetic font fidelity, and that the
implemented PaddleOCR-aligned resize is correct and safe.

## Claims to verify

1. **Isolation.** M283-M285 reached exact match (`MARMOLES PVC`) with crops rendered at correct aspect into
   `48×320`; M286-M294 detector crops degraded to edit distance 1-2. The variable is geometry, not glyphs.
2. **Pre-fix code.** `BuildRecognizerTensorFromImageCrop` used `scaleX=w/320, scaleY=h/48` nearest-neighbour
   → per-axis stretch, no aspect preservation, no bilinear, no right-pad.
3. **PaddleOCR reference.** `resize_norm_img`: height 48, width `min(320, ceil(48·aspect))`, bilinear,
   `(x-0.5)/0.5`, right-pad with `0.0`.
4. **Fix.** `NodalOsPaddleOcrRecognizerCropPreprocessor` implements that; pipeline default is
   `RatioPreservingRightPad`; `--recognizer-resize stretch` keeps the legacy mode for A/B.
5. **Tests.** Aspect preserved + right-pad (24×48 → resizedWidth 24, 296 padded, pad 0.0); wide crop capped at
   320 with no pad; white→+1, black→−1; stretch vs ratio diverge on a vertical-bar crop; default = ratio.

## What must NOT be claimed

- No measured ONNX decode improvement in this block (geometry fix is unit-tested only).
- No productive/public OCR readiness; internal synthetic-only.

## Checks

- [ ] PaddleOCR `resize_norm_img` semantics confirmed upstream (aspect-preserving + right-pad 0.0).
- [ ] Pre-fix stretch reproduced from git history of `BuildRecognizerTensorFromImageCrop`.
- [ ] New preprocessor: width formula, bilinear, normalization, pad value 0.0.
- [ ] 7 unit tests green; geometry assertions match expectations.
- [ ] Pipeline child default = ratio-preserving; `--recognizer-resize stretch` reaches legacy path.
- [ ] No ONNX models / gitignored dictionaries committed; out-of-process guard intact.
- [ ] Official space policy unchanged (blank 0, dict 1..436, space 437); `[B,T,C]`; softmax not re-applied.
- [ ] Recommended follow-up: A/B ONNX pipeline probe to capture decode evidence.
