# Claude Audit Prompt - M226-M228 Full OCR Handoff + Recognition Runtime Isolation

Audit the NODAL OS M226-M228 implementation.

Verify:

- detector-to-recognizer handoff is isolated by stage
- crop extraction blocks invalid, out-of-bounds, empty, and too-small crops before runtime
- risky handoff and recognizer probes run only out-of-process
- native child crashes are contained and parent survives
- recognizer runtime probe captures input metadata, output metadata, tensor stats, output shape, class count, and crash stage
- recognizer output class count `97` is compared against the current ASCII dictionary count `86` / ASCII+blank `87`
- CTC decode is blocked when dictionary class count mismatches
- no OCR text is invented
- no raw image persistence occurs
- no full-screen fixture is allowed
- no sensitive fixture is allowed
- no SaaS OCR is called
- OCR is not used as authority
- productive OCR and shadow mode remain blocked
- the final decision is honest

Recommend the next route:

- dictionary completion
- recognizer runtime experiment
- recognizer model replacement
- handoff fix
- postprocessing fix

Pay special attention to whether recognizer `session.Run` succeeds on zero/ones/gradient tensors and whether the only remaining blocker is dictionary compatibility.
