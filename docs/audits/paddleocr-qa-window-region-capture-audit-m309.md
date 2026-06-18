# Claude Audit Prompt: M307-M309 QA Window Region Capture Fixtures

Audit the M307-M309 QA window-region gate for NODAL OS.

Verify:

- capture mode is honestly documented as `simulated-window-region`,
- `qaWindowRegionUsed` is false because no real QA window was captured,
- simulated window provenance is traceable and bounded,
- full-screen is rejected,
- unknown window/process is rejected,
- region outside window is rejected,
- empty bounds are rejected,
- sensitive, document, credential, customer, financial, and person data regions are rejected,
- out-of-process guard was used for ONNX runtime,
- parent process survived,
- no real document, full-screen, sensitive app, CDP pipeline, or authoritative action was used,
- no ONNX models or gitignored dictionaries were committed,
- `OfficialSpaceToken` remains active,
- blank index is `0`, dictionary indexes are `1..436`, and space is `437`,
- recognizer output layout is `[B,T,C]`,
- softmax was not reapplied.

Review whether the decision should remain:

`M307+M308+M309 CERRADO / READY_FOR_QA_WINDOW_REGION_FIXTURE_SET_EXPANSION`

or whether the next block should focus directly on a real QA helper window capture technique.
