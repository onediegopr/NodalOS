# Claude Audit Prompt - PaddleOCR ONNX Synthetic Recognizer Probe - M282

Audit M280-M282 for NODAL OS.

Verify:

- The recognizer ONNX probe ran out-of-process.
- The parent process survived.
- Timeout/crash containment remains available through the guard.
- Only synthetic tensor input was used.
- No real image, screen, crop, document, CDP, or SaaS OCR was used.
- Raw tensors were not persisted.
- PP-OCRv5 output shape was `[1,40,438]`.
- Output layout was treated as `[B,T,C]`.
- Class count `438` matches `436 dictionary + blank + space`.
- Blank index is `0`.
- Space index is `437`.
- Output appears already softmax.
- Softmax was not reapplied.
- Decode plumbing consumed output without claiming useful OCR text.
- Productive OCR and shadow mode remain blocked.
- No-authority remains enforced.
- No ONNX model or downloaded dictionary was committed.

Recommend whether the next milestone should proceed to synthetic image recognizer crop fixtures or require more ONNX synthetic recognizer probes.
