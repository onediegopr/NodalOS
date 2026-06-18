# Claude Audit Prompt: M301-M303 Internal Controlled Real Image Fixtures

Audit the NODAL OS internal controlled real image OCR fixture gate.

## Evidence

- Base commit: `80b2129`
- Fixture storage: `tests/fixtures/ocr/internal-controlled-real-images/internal-controlled-real-image-fixtures.json`
- Fixture images: generated in memory from tracked non-sensitive QA metadata
- Raw sensitive persistence: false
- Real screen used: false
- Real document used: false
- Detector: local PP-OCRv4 ONNX, verified
- Recognizer: local PP-OCRv5 ONNX
- Dictionary: local PP-OCRv5 dictionary
- Preprocessing: `RatioPreservingRightPad`
- Output: `[1,40,438]`
- Official space policy: blank `0`, dictionary `1..436`, space `437`
- Softmax reapplied: false
- Out-of-process guard: true
- Parent survived: true

## Results

- `PVC WALL` -> `PVC WALL`, exact
- `ROMA` -> `ROMA`, exact
- `12 34` -> `12 34`, exact
- Exact matches: `3`
- Total edit distance: `0`

## Questions

- Is the fixture provenance policy strict enough before controlled screen-region fixtures?
- Is metadata-only storage acceptable for these internal controlled real image fixtures?
- Should future screen-region fixtures require separate source capture policy?
- Are no-SaaS, no-sensitive, no-document and no-authority constraints preserved?

## Required Safety Position

- Do not approve public/productive OCR.
- Do not approve uncontrolled screen OCR.
- Do not approve document OCR.
- Do not treat OCR as authority.
- Do not recommend committing ONNX models or gitignored dictionaries.
