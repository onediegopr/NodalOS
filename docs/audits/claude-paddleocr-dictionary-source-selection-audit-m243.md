# Claude Audit Prompt - M241-M243 PaddleOCR Dictionary Source Selection

Audit the NODAL OS M241-M243 dictionary source selection gate.

Required checks:

- Audit that sources were reviewed in the required order: local reports/artifacts, RapidOCR/ModelScope manifest, official PaddleOCR/RapidOCR sources, and ONNX embedded metadata.
- Verify `ch_PP-OCRv4_rec.onnx` remains verified and has recognizer output class count `97`.
- Verify the current contract requires `96` dictionary tokens plus CTC blank.
- Audit the RapidOCR/ModelScope source URL and confirm hash/size/count evidence.
- Audit the PaddleOCR GitHub source URL and confirm hash/size/count evidence.
- Audit the ONNX embedded `character` metadata and confirm token count evidence.
- Confirm official candidates are rejected because they expose `95` tokens, not `96`.
- Confirm no source/hash was invented.
- Confirm no active dictionary download/verify/rollback scripts were created.
- Confirm no dictionary decode was attempted and no text was invented.
- Confirm no ONNX models were committed or deleted.
- Confirm no raw persistence, no sensitive/full-screen OCR, no SaaS OCR, no productive OCR, no shadow mode, and no OCR-as-authority.
- Recommend whether the next block should revise the CTC class semantics or replace/select another recognizer/dictionary pair.
