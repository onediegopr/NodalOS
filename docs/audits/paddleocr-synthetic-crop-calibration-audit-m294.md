# Claude Audit Prompt: M292-M294 Synthetic Crop Calibration

Audit the NODAL OS PaddleOCR detector-to-recognizer crop calibration block.

## Evidence To Review

- Detector model recovered and verified:
  - `tools/ocr-worker/models/onnx/ch_PP-OCRv4_det.onnx`
  - SHA-256 `d2a7720d45a54257208b1e13e36a8479894cb74155a5efe29462512d42f49da9`
  - size `4745517`
- Recognizer output:
  - `[1,40,438]`
  - official PaddleOCR space policy
  - blank index `0`
  - space index `437`
  - softmax not reapplied
- Baseline M289-M291:
  - `0` exact matches
  - `0` normalized matches
  - `5` mismatches
- M292-M294 calibration:
  - `45` calibration attempts
  - best strategy `percent-expanded-box`
  - best margin `10%`
  - best unclip `1.5`
  - `3` normalized match attempts, but only `1` distinct fixture matched
  - best fixture evidence: `12 34 -> 1234`

## Questions

- Is the crop geometry audit sufficient for synthetic calibration?
- Does the calibration evidence justify real-image fixtures now, or should it remain in synthetic audit?
- Are detector boxes too tight vertically?
- Is synthetic glyph rendering the likely blocker?
- Is recognizer resize/pad policy still suspect?
- Should next work focus on fixture rendering, crop geometry, or recognizer preprocessing?

## Safety Requirements

- No SaaS OCR.
- No API keys.
- No real screens.
- No real documents.
- No sensitive data.
- No raw persistence of real data.
- No model or dictionary binaries committed.
- No OCR authority.
- No shadow mode.
- No productive OCR.

## Requested Recommendation

Recommend one of:

- continue synthetic crop calibration,
- revise synthetic glyph rendering,
- revise detector crop expansion policy,
- revise recognizer resize/pad preprocessing,
- proceed to controlled real image fixtures only if evidence is sufficient.
