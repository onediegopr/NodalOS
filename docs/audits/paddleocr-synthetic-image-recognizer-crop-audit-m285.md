# Claude Audit Prompt: PaddleOCR Synthetic Image Recognizer Crop Fixtures M285

Audit the M283-M285 implementation and evidence.

## Evidence To Review

- Synthetic crops are generated in memory from deterministic block glyphs.
- No real image, screen, document, or sensitive input is used.
- Input tensor is `[1,3,48,320]`.
- PP-OCRv5 recognizer output is `[1,40,438]`.
- Official class policy is `blank(0) + dictionary(1..436) + space(437)`.
- Output layout is `[B,T,C]`.
- Output already appears softmax; softmax is not reapplied.
- Recognizer execution is out-of-process.
- Parent process survives.
- Decode output is no-authority preview only.
- Productive OCR and shadow mode remain blocked.

## Probe Summary

- Fixtures: `12 34`, `PVC WALL`, `A B C`, `MARMOLES PVC`, `12345`, `GENOVA`, `ROMA`.
- Exact matches: `1`.
- Mismatches: `6`.
- Exact matching fixture: `MARMOLES PVC`.

## Questions

1. Is the synthetic crop generation appropriate for a no-authority OCR readiness gate?
2. Is the preprocessing policy sufficiently documented for PP-OCRv5 recognizer probes?
3. Is it acceptable to advance to synthetic detector-to-recognizer fixtures with one exact match and six mismatches, given that this gate is not an OCR accuracy gate?
4. Does the implementation avoid raw persistence, real data, SaaS OCR, full-screen OCR, sensitive OCR, shadow mode, and productive OCR?
5. Does the artifact provide enough evidence for the next synthetic-only gate?

Recommend whether the next block should be synthetic detector-to-recognizer pipeline fixtures, preprocessing refinement, or additional synthetic recognizer crop calibration.
