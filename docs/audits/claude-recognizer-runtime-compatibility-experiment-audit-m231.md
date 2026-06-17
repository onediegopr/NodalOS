# Claude Audit Prompt - M229-M231 Recognizer Runtime Compatibility Experiment

Audit the NODAL OS M229-M231 implementation.

Verify:

- recognizer runtime experiment runs risky recognizer probes only out-of-process
- zero/ones/gradient/checker/crop tensor variants are represented
- session options are recorded: default, graph disabled, graph basic, single-threaded, memory pattern disabled, CPU arena disabled, sequential, deterministic minimal
- invalid shape blocks before runtime
- NHWC is skipped unless local metadata justifies it
- recognizer model metadata is captured
- recognizer crash stage is correctly mapped to `session.Run`
- parent survives native child crashes
- temp cleanup succeeds
- no raw image persistence occurs
- no full-screen or sensitive fixture is allowed
- no SaaS OCR is called
- OCR is not treated as authority
- dictionary/CTC mismatch is treated as a secondary deferred gate
- no text is invented
- productive OCR and shadow mode remain blocked
- final decision is honest

Recommend whether the next block should be ONNX Runtime version experiment, recognizer model replacement, recognizer session options fix, recognizer preprocessing fix, or dictionary completion.
