# Claude Audit Prompt: M304-M306 Internal Controlled Screen Region Fixtures

Audit the M304-M306 screen-region gate for NODAL OS.

Verify:

- screen-region provenance categories are fail-closed,
- full-screen capture is rejected,
- unknown windows/provenance are rejected,
- sensitive, document, credential, customer, financial, and person data regions are rejected,
- empty or invalid bounds are rejected,
- the capture mode is documented as `simulated-region`,
- no real screen, real document, sensitive data, or CDP pipeline was used,
- out-of-process guard was used for ONNX runtime,
- parent process survived,
- detector PP-OCRv4 and recognizer PP-OCRv5 are local only,
- no ONNX models or gitignored dictionaries were committed,
- `OfficialSpaceToken` remains active,
- blank index is `0`, dictionary indexes are `1..436`, and space is `437`,
- recognizer output layout is `[B,T,C]`,
- softmax was not reapplied,
- OCR output is preview/evidence only and not authoritative.

Review whether the decision should remain:

`M304+M305+M306 CERRADO / READY_FOR_SCREEN_REGION_FIXTURE_SET_EXPANSION`

or whether stronger evidence is needed before moving from `simulated-region` to `qa-window-region`.
