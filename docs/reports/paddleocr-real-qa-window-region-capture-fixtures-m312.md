# M310-M312 PaddleOCR Real QA Window Region Capture

## Decision

`M310+M311+M312 CERRADO / BLOCKED_BY_REAL_QA_WINDOW_CAPTURE_TECHNIQUE`

This block added a real QA window-region capture gate, but did not execute OCR over a real QA window. The runner currently has no stable helper window host that can create a controlled visible QA window and capture only its bounded region. The gate refuses to treat `simulated-window-region` as real.

## Policy

The real QA window gate requires:

- capture mode `real-qa-window-region`,
- exact QA window title match,
- exact process/source match,
- non-empty window handle/source id,
- valid window bounds,
- valid region bounds,
- region inside window,
- window exists,
- window visible,
- liveness confirmed,
- no full-screen,
- no sensitive, document, credential, customer, financial, or person data,
- expected text.

Rejected modes:

- `simulated-window-region`,
- `blocked-before-real-capture`,
- unknown window,
- process mismatch,
- full-screen,
- invalid bounds,
- region outside window,
- missing liveness/visibility.

## Probe Result

Capture mode: `blocked-before-real-capture`

Window title: `NODAL OS OCR QA Window`

Process/source: `onnx-ocr-probe-runner`

Window handle/source id: not available

Window bounds: unavailable

Region bounds intended: `x=80`, `y=64`, `width=640`, `height=160`

Pipeline executed: no

Reason: no stable real QA helper window host exists in the runner yet; full-screen and simulated-window fallback are blocked.

## Safety

- No SaaS OCR.
- No API keys.
- No full-screen capture.
- No real document used.
- No sensitive region.
- No raw persistence of sensitive data.
- No OCR authority.
- No ONNX models committed.
- No gitignored dictionaries committed.

## Validation

- Restore: passed.
- Build: passed.
- OCR/real-QA-window-region filter: `182 passed`, `1 skipped`, `0 failed`.
- Full suite: not clean due to one unrelated browser smoke flake.
- Browser flake triage: `BrowserRuntimeSmokeIdempotencyGateReportsDuplicateReplay` failed in the full suite, then passed isolated `1/1`.
