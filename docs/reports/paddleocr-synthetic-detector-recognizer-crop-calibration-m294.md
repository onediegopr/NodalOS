# M292-M294 PaddleOCR Synthetic Detector Recognizer Crop Calibration

## Decision

`M292+M293+M294 CERRADO / READY_FOR_SYNTHETIC_PIPELINE_CALIBRATION_AUDIT`

The detector model is recovered and verified, and the detector-to-recognizer pipeline now supports a controlled crop calibration matrix. Calibration improved synthetic decode evidence, but not enough to advance to controlled real image fixtures. The result remains internal-only and non-authoritative.

## Scope

- Synthetic full images only.
- Local ONNX detector and recognizer only.
- No real screens.
- No real documents.
- No sensitive data.
- No SaaS OCR.
- No raw persistence of real data.
- No OCR authority.

## Baseline From M289-M291

- `MARMOLES PVC` -> `ARMOL FS PO`, mismatch.
- `PVC WALL` -> `LCNHL`, mismatch.
- `GENOVA` -> `EAD`, mismatch.
- `ROMA` -> empty, mismatch.
- `12 34` -> empty, mismatch.
- Exact matches: `0`.
- Normalized matches: `0`.
- Mismatches: `5`.

## Box Geometry Audit

The synthetic fixture generator does not expose glyph-level bounding boxes, so expected text geometry is estimated from fixture padding. Detector boxes did not clip image edges, but boxes are tight vertically and the recognizer result is sensitive to crop padding.

Detector metadata remains:

- input: `x=[-1,3,-1,-1]`
- output: `sigmoid_0.tmp_0=[-1,1,-1,-1]`
- runtime shape: `[1,3,640,640] -> [1,1,640,640]`

Recognizer metadata remains:

- output layout: `[B,T,C]`
- output shape: `[1,40,438]`
- official space policy: blank `0`, dictionary `1..436`, space `437`
- softmax reapplied: `false`

## Calibration Matrix

Attempts: `45`

Margins:

- `0%`
- `2px`
- `4px`
- `8px`
- `12px`
- `5%`
- `10%`
- `15%`
- `20%`

Unclip policies:

- `actual`
- `1.2`
- `1.5`
- `2.0`

Crop strategies:

- `raw-detected-box`
- `fixed-pixel-expanded-box`
- `percent-expanded-box`
- `line-height-padded-crop`
- `aspect-ratio-padded-crop`

Fixture rendering variants:

- `PixelFont`
- `AntiAliasedPixelFont`

## Best Evidence

- Best crop strategy: `percent-expanded-box`
- Best margin policy: `10%`
- Best unclip policy: `1.5`
- Best edit distance: `0`
- Improved over baseline: `true`

Best per fixture:

- `12 34` -> `1234`, normalized match, edit distance `0`.
- `GENOVA` -> `GENOYA`, mismatch, edit distance `1`.
- `MARMOLES PVC` -> `IArmOLES PVC`, mismatch, edit distance `1`.
- `PVC WALL` -> `PYC WALL`, mismatch, edit distance `1`.
- `ROMA` -> `EMA`, mismatch, edit distance `2`.

## Interpretation

The calibration matrix improved the bridge from detector-derived crops into the recognizer, but only one distinct fixture reached normalized match. This does not satisfy the stronger readiness condition for controlled real images. The correct next step is an audit/calibration pass focused on crop geometry and synthetic glyph rendering, not real-image fixtures.

## Safety

- No public product readiness.
- No uncontrolled OCR.
- No real screen OCR.
- No real document OCR.
- No model or dictionary binaries committed.
- No OCR-based action authority.
