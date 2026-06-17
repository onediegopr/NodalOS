# Claude Audit Prompt - PaddleOCR Space Token No-Authority Readiness - M276

Audit the NODAL OS M274-M276 PaddleOCR official space token integration.

Verify:

- The official PaddleOCR `use_space_char=true` policy is represented as `blank(0) + dictionary(1..N) + space(N+1)`.
- PP-OCRv4 English is represented as `95 + blank + space = 97`.
- PP-OCRv5 English is represented as `436 + blank + space = 438`.
- The ONNX recognizer output is treated as `[B,T,C]`.
- The recognizer output is already softmax and softmax is not reapplied.
- `IgnoreExtraClass` is rejected as unsafe because the extra class is a real space token.
- Synthetic probability fixtures include `12 34`, `PVC WALL`, `A B C`, blank-dominant with space top-2, and a space-argmax timestep.
- Decode success is not claimed for production.
- Productive OCR remains blocked.
- Shadow mode remains blocked.
- No-authority is preserved.
- No raw image, sensitive, full-screen, SaaS, real document, or real screen OCR is used.
- No ONNX model or downloaded dictionary is committed.

Recommend whether the next block should proceed to synthetic official-space decode fixtures, require more policy review, or review PP-OCRv6 separately.
