# Claude Audit Prompt: Synthetic Detector-To-Recognizer Pipeline M288

Audit the M286-M288 implementation and blocked decision.

## What Changed

- Added guarded runner mode `--synthetic-detector-to-recognizer-pipeline-probe`.
- Added child mode `--synthetic-detector-to-recognizer-pipeline-child`.
- Added synthetic full-image fixture plan for:
  - `MARMOLES PVC`
  - `PVC WALL`
  - `GENOVA`
  - `ROMA`
  - `12 34`
- Added detector-to-recognizer crop extraction and PP-OCRv5 recognizer preprocessing logic.

## Current Evidence

- Detector model missing: `tools/ocr-worker/models/onnx/ch_PP-OCRv4_det.onnx`.
- Recognizer model present.
- PP-OCRv5 dictionary present.
- Detector probe was not launched.
- Recognizer probe was not launched because no detector boxes were available.
- Decision: `BLOCKED_BY_MODEL_OR_DICTIONARY_AVAILABILITY`.

## Safety Constraints

- Internal development only.
- No public product readiness.
- No SaaS OCR.
- No API keys.
- No real screen input.
- No real document input.
- No sensitive data.
- No raw persistence of real data.
- No OCR authority.
- No model/dictionary commits.

## Audit Questions

1. Is failing closed before detector runtime the correct decision when the detector model is unavailable?
2. Is the planned synthetic pipeline sequence safe and auditable?
3. Are the stage boundaries detector, crop extraction, recognizer preprocessing, recognizer runtime, and decode policy clear enough?
4. Is the official PaddleOCR space-token policy preserved?
5. Should the next block restore/verify detector models before re-running the synthetic pipeline?
