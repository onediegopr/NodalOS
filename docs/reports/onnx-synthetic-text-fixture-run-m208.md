# ONNX Synthetic Text Fixture Run — M208

## Summary
M206 delivered a dependency-free synthetic OCR text fixture generator with an in-memory 5×7 pixel font, anti-aliased rendering, color schemes, sensitive-text filtering, and Pixel Redaction V2 integration. M207 wired the M203 inference pipeline to run over M206 fixtures and extended honest status reporting with `RecognitionEmpty`, `DictionaryMismatch`, and `BlockedByModelRuntime`. M208 rechecked readiness after discovering that the native ONNX Runtime host crashes on synthetic text-like fixtures (pixel font and thick horizontal bars), while simple non-text shapes (stripes, solid rectangles, circles) load and execute without crashing.

## Models Used
| Model | File | SHA-256 | Status |
|-------|------|---------|--------|
| Detection | `tools/ocr-worker/models/onnx/ch_PP-OCRv4_det.onnx` | `d2a7720d45a54257208b1e13e36a8479894cb74155a5efe29462512d42f49da9` | Verified, session loaded, crashes on text-like fixtures |
| Recognition | `tools/ocr-worker/models/onnx/ch_PP-OCRv4_rec.onnx` | `e8770c967605983d1570cdf5352041dfb68fa0c21664f49f47b155abd3e0e318` | Verified, session loaded, not reached due to detector crash |
| Classification | `tools/ocr-worker/models/onnx/ch_ppocr_mobile_v2.0_cls.onnx` | `e47acedf663230f8863ff1ab0e64dd2d82b838fceb5957146dab185a89d6215c` | Verified, optional for V1 |

## M206 Fixtures
- `NodalOsSyntheticOcrTextFixtureGenerator` renders `TEST`, `NODAL`, `HELLO`, `SAFE`, `ABC123`, `12345`.
- Render modes: `PixelFont` and `AntiAliasedPixelFont` (default).
- Color schemes: `BlackOnWhite`, `WhiteOnBlack`, `GrayOnWhite`.
- Dimensions: default 640×640.
- Sensitive keywords blocked; no raw persistence; no full-screen; no sensitive; no authority.
- Unit tests pass for all fixture variants.

## M207 Run Observations
- Safe-shape diagnostics pass:
  - Stripe fixture (64×32): `NoTextDetected`, 0 boxes, 0 recognition attempts.
  - Small solid rectangle (128×64): `NoTextDetected`, 0 boxes, 0 recognition attempts.
  - Large solid rectangle (640×640): `NoTextDetected`, 0 boxes, 0 recognition attempts.
  - Large circle (640×640): `NoTextDetected`, 0 boxes, 0 recognition attempts.
- Text-like fixtures crash the native ONNX Runtime host before returning results:
  - Pixel-font text (`TEST`, `OOOO`, anti-aliased): test host process terminated.
  - Thick horizontal bars (text-like dense horizontal structures): test host process terminated.
- Thin 2-pixel stripe pattern runs but returns `NoTextDetected`.
- The crash is isolated to the detector model when the input contains dense/thick horizontal text-like structures.

## Quarantine
Seven M207 tests that depend on text-like fixtures or would crash the native runtime are quarantined with `[Ignore("M207 quarantine: native ONNX Runtime crash on synthetic text fixture render mode. See M208 report.")]`. Safe-shape diagnostics and unit-level status tests remain enabled.

## Honest Reporting
- `NoTextDetected` is reported for safe shapes.
- `BlockedByModelRuntime` is now a first-class inference status and run decision.
- `RecognitionEmpty` and `DictionaryMismatch` are reported when recognition produces empty or out-of-dictionary text.
- Low-confidence results map to `RequiresHumanReview`.
- No raw persistence, no SaaS, no full-screen, no sensitive, no authority are preserved.

## Readiness Decision
`BlockedByModelRuntime`

The pipeline is structurally complete and the models load, but synthetic text fixtures cannot be exercised without crashing the native ONNX Runtime host. We cannot advance to `ReadyForRedactedCropShadow` because no successful detection + recognition run on a text fixture has been observed.

## Compliance
- No real OCR on documents or screens.
- No SaaS OCR.
- No raw persistence.
- No full-screen OCR.
- No sensitive OCR.
- No OCR as authority.
- Production public OCR remains blocked.
- CDP pipeline not yet integrated.

## Next Steps
1. Investigate the detector crash root cause (ONNX Runtime version, model input preprocessing, or out-of-process execution).
2. Consider running ONNX inference in an isolated worker process so a model-runtime crash becomes a recoverable `BlockedByModelRuntime` result instead of a test-host termination.
3. Alternatively, evaluate a real font-rendering dependency (e.g., SkiaSharp) to produce more detector-friendly synthetic text fixtures.
4. Re-run quarantined M207 tests only after a fix is validated.
5. Do not advance to redacted-crop shadow mode until a synthetic text fixture produces detection boxes and a recognition attempt with honest status reporting.
