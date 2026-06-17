# Claude Audit Prompt - M238-M240 PaddleOCR Dictionary / CTC Compatibility

Audit the NODAL OS M238-M240 PaddleOCR dictionary and CTC compatibility gate.

Required checks:

- Audit the dictionary manifest contract and confirm it records the required recognizer class count `97`.
- Audit that the expected dictionary charset count is `96` with blank token appended at index `96`.
- Audit that the current ASCII dictionary remains blocked because `86 + blank = 87`, not `97`.
- Confirm no decode is attempted with an incompatible dictionary.
- Confirm no text is invented.
- Confirm no dictionary source URL, SHA-256, or expected size is invented.
- Audit acquisition status and confirm download is blocked pending source selection.
- Audit planned download/verify/rollback script paths and confirm they are not active downloaders without approved source/hash/size.
- Confirm no ONNX models are committed or deleted.
- Confirm no productive OCR, no SaaS OCR, no raw persistence, no full-screen/sensitive OCR, no shadow mode, and no OCR-as-authority.
- Recommend the next route: approved dictionary source selection, then controlled acquisition/verification, then synthetic decode fixtures.
