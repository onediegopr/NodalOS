# Claude Audit Prompt - M235-M237 ONNX Runtime Upgrade Regression

Audit the NODAL OS M235-M237 permanent ONNX Runtime upgrade.

Required checks:

- Verify `Microsoft.ML.OnnxRuntime` was permanently upgraded from `1.18.1` to `1.22.1`.
- Verify `1.22.1` was chosen because M232-M234 proved it was the minimum candidate that avoided recognizer `session.Run` crash.
- Audit restore/build/test results.
- Audit `verify-models.ps1` evidence for det/rec/cls.
- Audit detector sanity and recognizer probes.
- Confirm the previous recognizer crash `0xC0000094` did not recur.
- Confirm risky OCR did not run in-process.
- Confirm no `.onnx` models were committed and models remained gitignored.
- Confirm no SaaS, no raw persistence, no full-screen/sensitive OCR, no productive OCR, no shadow mode, and no OCR-as-authority.
- Audit dictionary/CTC status: recognizer class count `97` vs ASCII+blank `87`; no decode and no invented text.
- Recommend the next block: controlled dictionary/CTC completion.
