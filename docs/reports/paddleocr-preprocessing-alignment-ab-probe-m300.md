# M298-M300 PaddleOCR Preprocessing Alignment A/B Probe

## Decision

`M298+M299+M300 CERRADO / READY_FOR_INTERNAL_CONTROLLED_REAL_IMAGE_FIXTURES`

The ONNX detector-to-recognizer pipeline was executed out-of-process over synthetic fixtures in two recognizer preprocessing modes:

- `stretch`: legacy fixed-width per-axis stretch.
- `ratio`: PaddleOCR-aligned ratio-preserving resize with right pad.

Ratio-preserving preprocessing improved real ONNX decode evidence enough to satisfy the success criteria for the next internal gate.

## Scope

- Synthetic images only.
- Local ONNX detector and recognizer only.
- No real screens.
- No real documents.
- No sensitive data.
- No SaaS OCR.
- No raw persistence of real data.
- No OCR authority.

## A/B Configuration

- Crop strategy: `percent-expanded-box`
- Margin policy: `10%`
- Unclip policy: `1.5`
- Padding: `24`
- Recognizer tensor: `[1,3,48,320]`
- Recognizer output: `[1,40,438]`
- Official space policy: blank `0`, dictionary `1..436`, space `437`
- Softmax reapplied: `false`

## Results

Stretch:

- Exact matches: `0`
- Normalized matches: `1`
- Mismatches: `4`
- Total edit distance: `7`

Ratio-preserving:

- Exact matches: `3`
- Normalized matches: `0`
- Mismatches: `2`
- Total edit distance: `2`
- Edit distance improvement: `71.43%`

Per fixture:

- `MARMOLES PVC`: stretch `IArmOLES PVC`, ratio `IArmoles PVc`, delta `0`.
- `PVC WALL`: stretch `PYC WALL`, ratio `PVC WALL`, delta `1`.
- `GENOVA`: stretch `GENOLF`, ratio `GENOVF`, delta `1`.
- `ROMA`: stretch `2MF`, ratio `ROMA`, delta `3`.
- `12 34`: stretch `1234`, ratio `12 34`, delta `0`.

## Interpretation

The A/B probe confirms that the M295-M297 preprocessing correction materially improves ONNX recognizer decode over detector-derived synthetic crops. The ratio-preserving mode reaches `3/5` exact matches and reduces total edit distance from `7` to `2`, satisfying the configured success criterion.

Remaining misses are concentrated in `MARMOLES PVC` and `GENOVA`, so the next internal gate should remain controlled and evidence-driven. This does not enable public/productive OCR.

## Safety

- No SaaS OCR.
- No API keys.
- No real screens.
- No real documents.
- No sensitive data.
- No model or dictionary binaries committed.
- No OCR-based action authority.
- No shadow mode.
