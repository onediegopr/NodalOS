# NODAL OS M295-M297 - PaddleOCR Synthetic Pipeline Calibration Audit (Rendering Fidelity vs Crop/Preprocessing)

## Decision

`M295+M296+M297 CERRADO / READY_FOR_PADDLEOCR_PREPROCESSING_ALIGNMENT_FIX`

## Executive finding

The residual synthetic detector→recognizer decode failures are **not** primarily caused by synthetic font
fidelity. They are caused by the **recognizer crop bridge distorting aspect ratio**: it stretched every
detector crop to a fixed `[48,320]` tensor with per-axis nearest-neighbour sampling.

The decisive evidence was already in the repo: in M283-M285 the **same block glyphs** decoded to an
**exact match** (`MARMOLES PVC -> MARMOLES PVC`) when rendered directly at the correct aspect ratio into the
`48×320` recognizer canvas. The only thing that changed in M286-M294 was the geometry of how the crop
reaches the recognizer. Therefore the variable is **geometry (resize), not glyph realism**.

This block implements the PaddleOCR-aligned, aspect-preserving resize and unit-tests it. Re-running the ONNX
pipeline to capture the improved decode strings is the immediate follow-up.

## Root cause (code-level)

`tools/onnx-ocr-probe-runner/Program.cs` `BuildRecognizerTensorFromImageCrop` (pre-fix):

```
scaleX = crop.Width / 320;   scaleY = crop.Height / 48;
srcX = (int)(x * scaleX);    srcY = (int)(y * scaleY);   // independent per-axis nearest neighbour
```

This **stretches** the crop to fill `48×320` regardless of the crop's real aspect ratio:

- narrow text (`ROMA`, aspect ~3:1) is stretched horizontally toward 320/48 ≈ 6.7:1 → glyphs widen and merge
  (`ROMA -> EMA`);
- wide text (`MARMOLES PVC`) is squished → `MARMOLES PVC -> IArmOLES PVC`;
- diagonals distort under non-uniform scale → `V -> Y` (`GENOVA -> GENOYA`, `PVC WALL -> PYC WALL`);
- nearest-neighbour (no area/bilinear filtering) aliases strokes when downscaling, dropping characters.

PaddleOCR `ppocr/data/imaug/rec_img_aug.py::resize_norm_img` instead does:

```
imgH = 48; imgW = 320
ratio = w / h
resized_w = min(imgW, ceil(imgH * ratio))     # aspect ratio PRESERVED
resized = cv2.resize(img, (resized_w, imgH))   # bilinear
img = resized/255; img -= 0.5; img /= 0.5
padding_im = zeros(C, imgH, imgW); padding_im[:, :, 0:resized_w] = img   # RIGHT-PAD with 0.0
```

### Secondary divergences found

1. **Two inconsistent recognizer preprocessors.** `NodalOsOnnxOcrRecognizerPreProcessor` (M198) preserves
   aspect but uses the wrong height (32 vs 48) and **ImageNet mean/std** normalization (detector-style), while
   the pipeline child used height 48 + correct `(x-0.5)/0.5` but the stretch. Neither matched PaddleOCR fully.
2. **Nearest-neighbour resampling** instead of bilinear (`cv2.resize` default).
3. **Axis-aligned crop only.** The bridge (`ExtractCropForProbe` + `ExpandBox`) uses an axis-aligned
   rectangle; PaddleOCR/RapidOCR use `get_rotate_crop_image` (4-point perspective). Low impact for the current
   horizontal synthetic fixtures, but a real divergence to carry forward for rotated/real text.

## What was investigated

| Area | Finding |
| --- | --- |
| Rendering fidelity isolation | Direct crop (correct aspect) → exact match in M283-M285; detector crop (stretched) → edit distance 1-2. Isolates **geometry**, not font realism. |
| Detector preprocessing | Resize to 640, `sigmoid_0.tmp_0` output, fallback thresholds 0.30→0.05. Produces valid boxes; not the residual cause. |
| Detector postprocess | Axis-aligned boxes, no edge clipping, vertically tight, padding-sensitive (matches M292-M294). |
| Recognizer preprocessing | **Stretch to fixed 320** (root cause). PP-OCRv5 expects height 48 + `(x-0.5)/0.5` + aspect-preserving width + right-pad. |
| RapidOCR crop policy | Uses `get_rotate_crop_image` (perspective) + ratio-preserving recognizer resize + right pad; we used axis-aligned + stretch. |
| PP-OCRv4 det + PP-OCRv5 rec compatibility | Reasonable: detector is language-agnostic (DB boxes). Recognizer just needs aspect-correct, height-48, properly-normalized crops. The combination is fine once geometry is fixed. |

## What was implemented (M296)

- `NodalOsPaddleOcrRecognizerCropPreprocessor` (`OneBrain.BrowserExecutor.Cdp`): PaddleOCR `resize_norm_img`
  semantics — height 48, width `min(320, ceil(48·aspect))`, **bilinear** resampling, `(x-0.5)/0.5`
  normalization, **right-pad with 0.0**. Legacy `StretchToFixedWidth` retained for A/B.
- `NodalOsPaddleOcrRecognizerResizeMode` + `NodalOsPaddleOcrRecognizerCropTensor` contracts.
- `BuildRecognizerTensorFromImageCrop` now delegates to the aligned preprocessor (default
  `RatioPreservingRightPad`); the pipeline child accepts `--recognizer-resize stretch|ratio` for A/B.
- 7 deterministic unit tests proving: aspect preservation + right-pad, width cap on very wide crops,
  normalization (white→+1, black→−1), stretch-vs-ratio divergence on a vertical-bar crop, default = ratio.

These are ONNX-free and deterministic. They prove the **geometry** is now correct; they do not by themselves
claim improved ONNX decode strings (that requires the follow-up probe run).

## Baseline (M292-M294) and expected effect

| Fixture | M292-M294 calibrated (stretch) | Edit distance | Expected with aspect-preserving resize |
| --- | --- | --- | --- |
| `12 34` | `1234` (normalized) | 0 | maintained / improved |
| `GENOVA` | `GENOYA` | 1 | `V` no longer sheared by horizontal stretch |
| `MARMOLES PVC` | `IArmOLES PVC` | 1 | no horizontal squish of leading `M` |
| `PVC WALL` | `PYC WALL` | 1 | `V` no longer sheared |
| `ROMA` | `EMA` | 2 | narrow text no longer stretched ~2× |

(Expected effects are hypotheses to be confirmed by the follow-up ONNX A/B probe; not claimed as measured.)

## Recommended next step (Codex implementation block)

Run the detector→recognizer pipeline probe **A/B** on the same synthetic fixtures:

1. `--synthetic-detector-to-recognizer-pipeline-probe` with `--recognizer-resize ratio` (new default) vs
   `--recognizer-resize stretch` (legacy), out-of-process through the guard.
2. Capture per-fixture decode + edit distance for both modes into an artifact.
3. If aspect-preserving reaches exact/normalized on ≥3 fixtures →
   `READY_FOR_INTERNAL_CONTROLLED_REAL_IMAGE_FIXTURES`.
4. If still short, add bilinear-on-detector-crop + `get_rotate_crop_image` perspective crop and re-A/B before
   concluding `BLOCKED_BY_SYNTHETIC_FONT_OR_FIXTURE_RENDERING`.

## Safety

Internal development only. No SaaS, no API keys, no sensitive data, no real screens/documents in this block,
no raw persistence of real data, ONNX out-of-process guard preserved, parent survives. No ONNX models or
gitignored dictionaries committed. Official space policy preserved (blank 0, dict 1..436, space 437), output
`[B,T,C]`, softmax not re-applied.
