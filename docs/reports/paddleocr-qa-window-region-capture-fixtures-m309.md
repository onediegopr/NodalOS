# M307-M309 PaddleOCR QA Window Region Capture Fixtures

## Decision

`M307+M308+M309 CERRADO / READY_FOR_QA_WINDOW_REGION_FIXTURE_SET_EXPANSION`

This block added QA window-region provenance and a traceable `simulated-window-region` probe. It did not claim real QA window capture. No full-screen capture, real document, sensitive app, CDP pipeline, or authoritative action was used.

## QA Window Region Policy

The policy extends internal screen-region provenance with:

- window title/source,
- process/source,
- window bounds,
- region bounds,
- window-relative region coordinates,
- fail-closed region-inside-window validation.

Rejected cases include:

- full-screen,
- unknown window or process,
- region outside window,
- zero/negative bounds,
- sensitive/document/credential/customer/financial/person data,
- missing expected text.

## Fixtures

- `qa-window-pvc-wall`: `PVC WALL`
- `qa-window-roma`: `ROMA`
- `qa-window-12-34`: `12 34`

Window: `NODAL OS OCR QA Window`

Window bounds: `x=0`, `y=0`, `width=800`, `height=320`

Region bounds: `x=80`, `y=64`, `width=640`, `height=160`

Capture mode: `simulated-window-region`.

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
- OCR/QA-window-region filter: `176 passed`, `1 skipped`, `0 failed`.
- Full suite: not clean due to one unrelated browser smoke flake.
- Browser flake triage: `BrowserRuntimeSmokeIdempotencyGateReportsDuplicateReplay` failed in the full suite and first isolated rerun, then passed isolated `1/1`.
