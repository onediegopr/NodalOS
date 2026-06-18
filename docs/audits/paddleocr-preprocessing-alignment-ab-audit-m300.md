# Claude Audit Prompt: M298-M300 PaddleOCR Preprocessing Alignment A/B

Audit the NODAL OS PaddleOCR preprocessing alignment A/B probe.

## Evidence

- Base commit: `1b28f70`
- Detector model: local PP-OCRv4 detector, verified.
- Recognizer model: local PP-OCRv5 English recognizer.
- Dictionary: local PP-OCRv5 English dictionary.
- Output layout: `[B,T,C]`
- Output shape: `[1,40,438]`
- Official space policy: blank `0`, dictionary `1..436`, space `437`
- Softmax reapplied: `false`

## A/B Modes

- `stretch`: legacy fixed width stretch.
- `ratio`: PaddleOCR-aligned aspect-preserving resize with right pad.

## Results

- Stretch exact matches: `0`
- Stretch normalized matches: `1`
- Stretch mismatches: `4`
- Stretch total edit distance: `7`
- Ratio exact matches: `3`
- Ratio normalized matches: `0`
- Ratio mismatches: `2`
- Ratio total edit distance: `2`
- Ratio improved over stretch: `true`
- Success criteria met: `true`

## Questions

- Is the A/B methodology sufficient to approve the next controlled internal real-image fixture gate?
- Are the remaining `MARMOLES PVC` and `GENOVA` errors more likely crop geometry, rendering, or recognition model limitations?
- Should the next block use controlled real images, rotated crop policy, or perspective crop implementation first?
- Are no-authority, no-SaaS, no-raw-real-data and no-screen/document constraints preserved?

## Required Safety Position

- Do not approve public/productive OCR.
- Do not approve shadow mode.
- Do not approve uncontrolled screen or document OCR.
- Do not treat decoded text as authority.
- Do not recommend committing ONNX models or gitignored dictionaries.
