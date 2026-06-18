# M304-M306 PaddleOCR Internal Controlled Screen Region Fixtures

## Decision

`M304+M305+M306 CERRADO / READY_FOR_SCREEN_REGION_FIXTURE_SET_EXPANSION`

This block created the first bounded screen-region fixture gate. The probe used `simulated-region` capture only: no real screen, no full-screen capture, no document, no sensitive app, and no CDP pipeline.

Because the capture was simulated rather than `qa-window-region` or `real-window-region`, the next gate is fixture-set expansion and replacement of the simulated capture with an actual bounded QA window/region harness.

## Screen Region Policy

Provenance categories:

- `InternalControlledScreenRegion`
- `InternalQaWindowRegion`
- `RejectedFullScreen`
- `RejectedUnknownWindow`
- `RejectedSensitiveWindow`
- `RejectedDocumentRegion`
- `RejectedUnboundedRegion`

Fail-closed rejections:

- full-screen,
- empty or invalid bounds,
- unknown provenance/window,
- sensitive windows,
- document regions,
- credential/password regions,
- customer/financial/person data,
- missing expected text.

## Fixture Storage

Tracked metadata only:

`tests/fixtures/ocr/internal-controlled-screen-regions/internal-controlled-screen-region-fixtures.json`

No region image was persisted. The runner generated bounded QA region pixels in memory.

## Fixtures

- `qa-screen-pvc-wall`: `PVC WALL`
- `qa-screen-roma`: `ROMA`
- `qa-screen-12-34`: `12 34`

Bounds for all fixtures: `x=80`, `y=64`, `width=640`, `height=160`.

Capture mode: `simulated-region`.

Accepted: `3`

Rejected: `0`

## Pipeline Evidence

- Detector model available: yes
- Detector verified: yes
- Recognizer model available: yes
- Dictionary available: yes
- Out-of-process guard: yes
- Parent survived: yes
- Recognizer resize mode: `RatioPreservingRightPad`
- Recognizer tensor shape: `[1,3,48,320]`
- Recognizer output shape: `[1,40,438]`
- Class count: `438`
- Official space policy: blank `0`, dictionary `1..436`, space `437`
- Softmax reapplied: `false`

Results:

- `PVC WALL` -> `PVC WALL`, exact, edit distance `0`
- `ROMA` -> `ROMA`, exact, edit distance `0`
- `12 34` -> `12 34`, exact, edit distance `0`

Exact matches: `3`

Normalized matches: `0`

Mismatches: `0`

Total edit distance: `0`

## Safety

- No SaaS OCR.
- No API keys.
- No full-screen capture.
- No real document used.
- No sensitive region.
- No raw persistence of sensitive data.
- No OCR authority.
- No public product readiness claim.
- No ONNX models committed.
- No gitignored dictionaries committed.

## Validation

- Restore: passed.
- Build: passed.
- OCR/screen-region filter: `170 passed`, `1 skipped`, `0 failed`.
- Full suite: not clean due to one unrelated browser smoke flake.
- Browser flake triage: `BrowserRuntimeSmokeRunnerExecutesAllGatesOnFixture` failed in the full suite on cleanup, failed once isolated on WebSocket `Aborted`, then passed isolated `1/1`.
