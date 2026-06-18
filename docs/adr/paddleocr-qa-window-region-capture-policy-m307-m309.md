# ADR: PaddleOCR QA Window Region Capture Policy M307-M309

## Status

Accepted for internal development gate.

## Context

M304-M306 validated bounded simulated screen-region fixtures. M307-M309 moves toward QA window-region capture by requiring traceable window title/source, process/source, window bounds, and region-inside-window checks.

## Decision

Use a `simulated-window-region` harness for M307-M309 and keep the decision honest. The implementation does not claim real QA window capture.

The policy rejects:

- full-screen,
- unknown window/process,
- invalid bounds,
- regions outside the window,
- sensitive, document, credential, customer, financial, or person data,
- missing expected text.

## Evidence

The simulated-window-region probe produced:

- `3` accepted fixtures,
- `3` exact matches,
- `0` mismatches,
- total edit distance `0`,
- parent process survived under out-of-process guard.

## Consequences

The next step is `READY_FOR_QA_WINDOW_REGION_FIXTURE_SET_EXPANSION`: replace the simulated-window abstraction with a real QA window or stable local helper window before low-risk screen OCR observation.
