# Claude Audit Prompt - Guarded Synthetic Text OCR Retry M222

Audit the M220-M222 guarded synthetic OCR retry.

Required checks:

- Verify det/rec/cls ONNX models are present and hash/size verified.
- Verify `.onnx` files are gitignored and not committed.
- Verify risky synthetic OCR did not run in-process.
- Verify all risky probes ran through the out-of-process guard.
- Verify child crash containment and parent survival.
- Verify detection result: all M220 probes ended as contained native crash during detection run.
- Verify recognition result: recognition is unreachable because detection produced no boxes.
- Verify dictionary/CTC compatibility: recognizer output class count is 97, current ASCII dictionary plus blank is 87, decode is blocked.
- Verify no raw image persistence, no sensitive fixture, no full-screen fixture, no SaaS OCR.
- Verify no-authority is preserved and OCR is not used to approve actions.
- Verify the final decision `BLOCKED_BY_MODEL_RUNTIME` is honest and does not claim positive OCR.
- Recommend the next route: runtime version experiment, model replacement/compatibility fix, dictionary completion, renderer fix, or more fixtures.

Primary evidence:

- Report: `docs/reports/guarded-synthetic-text-ocr-retry-m222.md`
- Summary: `artifacts/ocr-vision-onnx/m222/guarded-synthetic-text-ocr-retry-summary.json`
- ADR: `docs/adr/guarded-synthetic-text-ocr-retry-decision-m220-m222.md`
