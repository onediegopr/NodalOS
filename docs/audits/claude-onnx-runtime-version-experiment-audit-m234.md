# Claude Audit Prompt - M232-M234 ONNX Runtime Version Experiment

Audit the NODAL OS M232-M234 runtime-version experiment.

Required checks:

- Verify the experiment was reversible and did not leave the branch in an unplanned package version.
- Verify tested package versions: baseline `Microsoft.ML.OnnxRuntime 1.18.1`, candidates `1.22.1`, `1.23.2`, `1.25.0`.
- Verify CPU provider only; no GPU, DirectML, SaaS OCR, embedded runtime, or Chromium fork.
- Audit package/runtime observed evidence, restore/build outcome, detector sanity, and recognizer probes per version.
- Confirm risky recognizer probes were out-of-process only.
- Confirm ONNX models were not committed and remained gitignored.
- Confirm no raw persistence, no sensitive/full-screen OCR, no SaaS, and no OCR-as-authority.
- Confirm dictionary/CTC mismatch remains a secondary gate unless recognizer runtime succeeds.
- Decide whether the evidence supports runtime upgrade, dictionary completion, recognizer model replacement, or continued runtime block.
