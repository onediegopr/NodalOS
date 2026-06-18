# Claude Audit Prompt: M310-M312 Real QA Window Region Capture

Audit the M310-M312 real QA window-region gate.

Verify:

- simulated-window capture is not accepted as real QA window capture,
- the artifact honestly says `blocked-before-real-capture`,
- real QA window liveness requires window exists, visible, handle/source id, matching title, matching process/source, and bounds,
- full-screen capture remains blocked,
- sensitive, document, credential, customer, financial, and person data regions are rejected,
- no OCR pipeline was executed when real capture was unavailable,
- no SaaS, API keys, CDP pipeline, real document, full-screen, or sensitive data were used,
- PaddleOCR policy remains `OfficialSpaceToken`,
- blank index is `0`, dictionary indexes are `1..436`, and space is `437`,
- recognizer output layout remains `[B,T,C]`,
- softmax is not reapplied,
- no ONNX models or gitignored dictionaries were committed.

Recommend whether the next block should build a minimal QA helper window host or use an existing safe window/capture helper if one is available.
