# Claude Audit Prompt - M258 PaddleOCR Extra Class Decode Policy

Audit NODAL OS M256-M258.

Verify:

- PaddleOCR/RapidOCR extra class semantics audit.
- Evidence source for PaddleOCR CTC blank index `0`.
- Evidence source for dictionary parser behavior.
- PP-OCRv4 formula: `95 + blank = 96`, observed `97`.
- PP-OCRv5 formula: `436 + blank = 437`, observed `438`.
- Whether any official source approves ignored/unknown/padding/reserved extra class semantics.
- That hypothesis-only policies cannot approve decode.
- That decode was not attempted with incompatible or hypothesis-only policy.
- That no text was invented.
- That no raw/no sensitive/no full-screen/no SaaS/no-authority were preserved.
- That productive OCR, shadow mode, and CDP integration remain blocked.

Recommend whether the next block should be manual decode policy approval, recognizer model replacement, or another official source review.
