# ADR: PaddleOCR Detector Model Recovery Policy M289-M291

## Status

Accepted. Pipeline remains blocked by synthetic decode evidence.

## Context

M286-M288 could not execute the synthetic detector-to-recognizer pipeline because `ch_PP-OCRv4_det.onnx` was absent. M289-M291 recovered the detector through the existing manifest-backed acquisition script and re-ran the guarded synthetic pipeline.

## Decision

Use the existing manifest and scripts for detector recovery:

- `download-models.ps1 -Confirm`
- `verify-models.ps1`

Do not commit downloaded `.onnx` files. The detector is valid only after size and SHA-256 verification.

Run the synthetic full-image pipeline out-of-process after verification. Advance only if detector boxes and decode evidence are sufficient.

## Result

- Detector acquisition: passed.
- Detector verification: passed.
- Detector boxes: passed, 5 boxes across 5 synthetic fixtures.
- Recognizer runtime: passed, 5 crops.
- Decode evidence: failed, 0 exact/normalized matches.

## Consequence

The correct final state is:

`BLOCKED_BY_SYNTHETIC_PIPELINE_DECODE_EVIDENCE`

The next block should focus on detector-box/crop/preprocessing calibration using synthetic fixtures only.

## Safety

This decision does not enable product/public OCR, real screen OCR, document OCR, CDP integration, OCR authority, SaaS OCR, or action execution based on OCR.
