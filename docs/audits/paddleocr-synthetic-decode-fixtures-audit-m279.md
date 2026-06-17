# Claude Audit Prompt - PaddleOCR Synthetic Decode Fixtures - M279

Audit the NODAL OS M277-M279 PaddleOCR synthetic official-space decode fixture block.

Verify:

- Fixtures are probability-only and do not use real documents, screens, crops, or raw image persistence.
- The official PaddleOCR mapping is `blank(0) + dictionary(1..N) + space(N+1)`.
- The output layout is treated as `[B,T,C]`.
- The recognizer output is treated as already-softmax and softmax is not reapplied.
- `12 34`, `PVC WALL`, `A B C`, and `MARMOLES PVC` decode with spaces preserved.
- CTC repeats `LL`, `OO`, and `11` use intermediate blank timesteps.
- `P blank V blank C` collapses to `PVC`.
- Multiple spaces and leading/trailing spaces are handled explicitly by policy.
- `IgnoreExtraClass` remains unsafe and unapproved because it drops real spaces.
- Top-k evidence is retained for audit.
- Productive OCR, shadow mode, SaaS OCR, full-screen OCR, sensitive OCR, and OCR authority remain blocked.
- The ONNX probe was intentionally deferred to the next out-of-process recognizer decode gate.

Recommend whether the next block should run the ONNX synthetic recognizer decode probe or require more synthetic decoder fixtures.
