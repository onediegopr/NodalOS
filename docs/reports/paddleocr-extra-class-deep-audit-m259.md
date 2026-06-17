# NODAL OS M259 - PaddleOCR Extra Class Deep Audit Pack

## Decision

M259 prepares the external audit package. It does not approve decode.

## Evidence Summary

PP-OCRv4:

- Dictionary tokens: `95`.
- CTC blank index: `0`.
- Expected classes: `96`.
- Observed classes: `97`.

PP-OCRv5:

- Dictionary tokens: `436`.
- CTC blank index: `0`.
- Expected classes: `437`.
- Observed classes: `438`.

## Audit Question

The package asks Claude to determine whether the recurring extra class is a known PaddleOCR/RapidOCR convention, an ignored/unknown/padding/reserved token, an export artifact, or a model/dictionary mismatch.

## Result

No decode was attempted. No token was invented. The next decision gate remains external semantics audit before any manual policy approval or replacement.
