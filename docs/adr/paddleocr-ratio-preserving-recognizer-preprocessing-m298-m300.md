# ADR: M298-M300 PaddleOCR Ratio-Preserving Recognizer Preprocessing

## Status

Accepted for next internal controlled real-image fixture gate.

## Context

M295-M297 replaced legacy recognizer crop stretching with PaddleOCR-aligned ratio-preserving resize and right padding. That block implemented and unit-tested the preprocessing fix, but did not measure ONNX decode improvement.

M298-M300 measures the same synthetic detector-to-recognizer pipeline with:

- legacy `stretch`;
- PaddleOCR-aligned `ratio`.

## Decision

Use ratio-preserving right-pad preprocessing as the forward recognizer preprocessing policy for controlled internal fixtures.

The measured A/B result is:

- stretch exact/normalized: `0/1`;
- ratio exact/normalized: `3/0`;
- stretch total edit distance: `7`;
- ratio total edit distance: `2`.

The hito closes as:

`M298+M299+M300 CERRADO / READY_FOR_INTERNAL_CONTROLLED_REAL_IMAGE_FIXTURES`

## Consequences

- Public/productive OCR remains blocked.
- Shadow mode remains blocked.
- No real screens/documents were used in this block.
- ONNX models and gitignored dictionaries remain uncommitted.
- Future controlled real-image fixtures must preserve no-authority and evidence logging.
