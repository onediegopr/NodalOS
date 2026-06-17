# Claude Audit Prompt - M244-M246 Recognizer Class Semantics / CTC Token Policy

Audit the NODAL OS M244-M246 recognizer class semantics and CTC token policy decision.

Required checks:

- Audit recognizer output class count `97`.
- Audit official dictionary/token source count `95`.
- Audit ONNX embedded `character` metadata count `95`.
- Audit PaddleOCR CTC convention: blank is prepended and ignored at index `0`.
- Confirm `95 + blank = 96`, not `97`.
- Audit hypothesis-only policies: blank+unknown, blank+padding, blank+space, two special tokens.
- Confirm no hypothesis-only policy is used to claim decode success.
- Confirm no dictionary/token/text is invented.
- Confirm no decode is attempted without an approved token policy.
- Confirm no raw persistence, no sensitive/full-screen OCR, no SaaS, no productive OCR, no shadow mode, and no OCR-as-authority.
- Recommend whether the next block should review source semantics, re-export the model/dictionary pair, replace the recognizer, or manually approve a documented token policy.
