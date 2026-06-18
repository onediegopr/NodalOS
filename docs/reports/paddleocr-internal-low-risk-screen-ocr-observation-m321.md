# M319-M321 — Internal Low-Risk Screen OCR Observation

## Scope

This block introduced an evidence-only OCR observation mode for bounded internal QA window regions.

It does not:

- allow actions
- allow OCR authority
- allow full-screen
- allow sensitive/document/credential regions
- persist raw sensitive data

## Implementation

- Added low-risk OCR observation contracts and evaluator.
- Added runner mode `--low-risk-screen-ocr-observation-probe`.
- Reused the real QA window host and real bounded region capture path from M313-M318.
- Reused PP-OCRv4 detector + PP-OCRv5 recognizer + `OfficialSpaceToken` decode policy.
- Emitted typed observation envelopes with provenance, bounded region metadata, policy decision, and non-authoritative OCR output.

## Observation outcome

Three observation envelopes were created over `real-qa-window-region`.

Observed live results were contaminated by other visible desktop text:

- `PVC WALL -> Usó Chrome integración`
- `ROMA -> Usó Chrome integración`
- `12 34 -> E io`

Summary:

- exact: `0`
- normalized: `0`
- mismatch: `3`
- total edit distance: `38`

## Interpretation

The evidence envelope model works and the pipeline remains bounded, out-of-process, and non-authoritative.

The weak point is live low-risk observation fidelity on an active desktop session, not the OCR contracts, not the dictionary policy, and not the softmax/decode path.

This block therefore closes as:

`M319+M320+M321 CERRADO / READY_FOR_LOW_RISK_SCREEN_OCR_OBSERVATION_EXPANSION`
