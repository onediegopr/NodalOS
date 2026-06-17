# ADR: M292-M294 PaddleOCR Detector Recognizer Crop Calibration Policy

## Status

Accepted for internal synthetic calibration only.

## Context

M289-M291 recovered and verified the PP-OCRv4 detector model and exercised the synthetic detector-to-recognizer pipeline. The pipeline ran without crash, but all detector-derived crops decoded as mismatches.

M292-M294 calibrates the bridge between detector boxes and PP-OCRv5 recognizer crops without changing OCR semantics.

## Decision

Use a controlled calibration matrix for synthetic fixtures:

- detector boxes remain source evidence;
- crop expansion is parameterized;
- recognizer preprocessing remains `[1,3,48,320]`;
- CTC semantics remain `blank(0) + dictionary(1..N) + space(N+1)`;
- ONNX output remains `[B,T,C]`;
- softmax is not reapplied;
- OCR remains non-authoritative.

The block closes as:

`M292+M293+M294 CERRADO / READY_FOR_SYNTHETIC_PIPELINE_CALIBRATION_AUDIT`

## Rationale

Calibration improved edit distance and produced normalized decode for `12 34`, but only one distinct fixture matched. Advancing to real-image fixtures would overstate readiness. The next block should audit crop geometry, glyph rendering, and recognizer preprocessing.

## Consequences

- No productive OCR is enabled.
- No shadow mode is enabled.
- No real screens or documents are used.
- No model or dictionary binaries are committed.
- Calibration parameters remain experimental and internal.
