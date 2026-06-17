# Claude Audit Prompt - M249 Recognizer Dictionary Pair Reconciliation

Audit NODAL OS M247-M249.

Verify:

- Raw dictionary source reconciliation for PaddleOCR `en_dict.txt`.
- Byte/hash/line/token counts for PaddleOCR release/main and RapidOCR/ModelScope sources.
- Handling of a single-space token versus terminal empty line.
- PaddleOCR parser policy: line read with CR/LF stripping, CTC blank prepended at index `0`.
- Whether documentation claim of `96` characters is sufficient to override raw/parser evidence.
- Whether `ch_PP-OCRv4_rec.onnx`/English recognizer output class count `97` matches the audited dictionary.
- That no token, dictionary, hash, or decoded text was invented.
- That decode remained blocked without an approved 96-token source/policy.
- That no OCR productivo, no SaaS, no raw persistence, no sensitive/full-screen OCR, and no-authority were preserved.

Recommend whether the next block should proceed to dictionary pinning, manual source review, or recognizer model/dictionary pair replacement.
