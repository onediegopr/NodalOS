# ADR: PaddleOCR Internal Controlled Screen Region Policy M304-M306

## Status

Accepted for internal development gate.

## Context

M301-M303 validated internal controlled real image fixtures. M304-M306 moves one gate forward to bounded screen-region fixtures without enabling full-screen OCR, sensitive OCR, CDP pipeline, or authoritative actions.

## Decision

Use a fail-closed screen-region provenance gate and a `simulated-region` capture harness for this block.

The gate requires:

- internal QA provenance,
- explicit non-empty bounds,
- no full-screen,
- no sensitive, customer, financial, person, credential, or document data,
- expected text,
- no raw sensitive persistence.

The OCR pipeline remains local PaddleOCR ONNX:

- detector PP-OCRv4,
- recognizer PP-OCRv5,
- `RatioPreservingRightPad`,
- `OfficialSpaceToken`,
- blank `0`, dictionary `1..436`, space `437`,
- `[B,T,C]`,
- no softmax reapply.

## Evidence

`simulated-region` fixtures produced:

- `3` accepted fixtures,
- `3` exact matches,
- `0` mismatches,
- total edit distance `0`,
- parent process survived under out-of-process guard.

## Consequences

The next step is `READY_FOR_SCREEN_REGION_FIXTURE_SET_EXPANSION`, not real screen OCR observation, because this block did not capture an actual QA window or real screen region.
