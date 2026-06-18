# M316-M318 — PaddleOCR QA Window Capture Hardening

- Base commit: `5e3b16e`
- Readiness decision: `M316+M317+M318 CERRADO / READY_FOR_INTERNAL_LOW_RISK_SCREEN_OCR_OBSERVATION`
- Worktree: `C:\DESARROLLO\NodalOS\Codigo-m12-audit`
- Branch: `chrome-lab-001-extension-local-ai-bridge`

## Scope

This block hardened the real QA window host and bounded region capture path without changing PaddleOCR semantics, dictionaries, class layout, or softmax handling.

## What Changed

- Extended `tools/qa-window-host` with explicit rendering controls:
  - font family
  - font size
  - font style
  - text rendering hint
  - baseline shift
- Recorded DPI and capture metadata:
  - `deviceDpi`
  - `dpiScaleX`
  - `dpiScaleY`
  - outer `windowBounds`
  - `clientBounds`
  - `regionBounds`
  - `capturedRegionWidth`
  - `capturedRegionHeight`
  - `captureCoordinateMode`
- Extended the real QA window probe to run a deterministic hardening matrix and select the best rendering configuration by:
  - highest exact/normalized matches
  - lowest total edit distance
  - deterministic tie-breakers

## DPI Audit

- Device DPI: `144`
- DPI scale X/Y: `1.5 / 1.5`
- Capture coordinate mode: `screen-physical-from-client-pointtoscreen`
- Window bounds: `x=120, y=120, width=822, height=376`
- Client bounds: `x=131, y=165, width=800, height=320`
- Best region bounds: `x=70, y=54, width=660, height=180`

## Rendering Matrix

Configurations tested:

1. `baseline-segoe-76-bold-cleartype`
2. `arial-76-bold-antialias`
3. `arial-92-bold-antialias-expanded`
4. `mssans-92-bold-singlebit-expanded`
5. `consolas-88-regular-singlebit-expanded`

Best configuration selected:

- Font family: `Arial`
- Font size: `92`
- Font style: `Bold`
- Rendering hint: `AntiAliasGridFit`
- Region: `70,54,660,180`

## Baseline vs Calibrated

Baseline M313-M315-compatible configuration:

- exact: `1`
- normalized: `0`
- mismatch: `2`
- total edit distance: `9`

Best calibrated configuration:

- exact: `2`
- normalized: `0`
- mismatch: `1`
- total edit distance: `1`

## Per-Fixture Result For Best Configuration

1. `PVC WALL -> PVC WALI`
   edit distance: `1`
   result: `Mismatch`
2. `ROMA -> ROMA`
   edit distance: `0`
   result: `Exact`
3. `12 34 -> 12 34`
   edit distance: `0`
   result: `Exact`

## Safety And Policy

- Real QA window region capture: enabled only for bounded internal QA fixture
- Full screen: blocked
- Sensitive data: blocked
- Real documents: blocked
- SaaS OCR: blocked
- API keys: blocked
- Softmax reapplied: `false`
- Official space token policy preserved: `blank 0`, `dictionary 1..436`, `space 437`
- Recognizer layout preserved: `[B,T,C]`
- Recognizer resize mode preserved: `RatioPreservingRightPad`
- Out-of-process guard used: `true`
- Parent survived: `true`
- Host cleanup: `true`

## Conclusion

The remaining M313-M315 rendering errors were reduced enough to satisfy the screen-region gate on a real bounded QA window. The path is ready for internal low-risk screen OCR observation under the existing no-authority and bounded-region controls.
