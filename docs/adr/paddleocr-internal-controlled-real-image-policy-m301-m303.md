# ADR: M301-M303 Internal Controlled Real Image OCR Fixture Policy

## Status

Accepted for internal controlled fixture progression.

## Context

M298-M300 validated PaddleOCR-aligned ratio-preserving preprocessing with synthetic detector-derived crops. The next gate requires non-sensitive internal controlled real image fixtures with explicit provenance.

## Decision

Use tracked metadata for internal controlled real image fixtures and generate raw RGBA images in memory for the ONNX probe. Do not persist raw real/sensitive data. Do not use screens or documents in this block.

Accepted fixture categories:

- `InternalControlledRealImage`
- `InternalNonSensitiveFixture`

Rejected fixture categories/conditions:

- unknown provenance,
- sensitive content,
- screen capture,
- document data,
- real person data,
- customer data,
- financial data.

## Result

The M301-M303 probe produced `3/3` exact matches with total edit distance `0`, using local ONNX detector and recognizer out-of-process.

The hito closes as:

`M301+M302+M303 CERRADO / READY_FOR_INTERNAL_CONTROLLED_SCREEN_REGION_FIXTURES`

## Consequences

- Internal controlled screen-region fixtures may be planned next.
- Public/productive OCR remains blocked.
- No OCR authority is granted.
- No model or dictionary binaries are committed.
