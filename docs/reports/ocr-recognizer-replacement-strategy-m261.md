# NODAL OS M261 - OCR Recognizer Replacement Strategy

## Decision

`M259+M260+M261 CERRADO / READY_FOR_CLAUDE_EXTRA_CLASS_AUDIT`

## Strategy Matrix

| Rank | Strategy | Status | Decode |
| --- | --- | --- | --- |
| 1 | External Claude extra-class deep audit | Recommended | Blocked |
| 2 | Manual PP-OCRv5 policy approval | Needs approval | Blocked until approved |
| 3 | Search another explicit recognizer+dictionary pair | Needs research | Blocked |
| 4 | Adopt RapidOCR postprocessor conventions directly | Needs approval | Blocked |
| 5 | Review another local OCR ONNX family | Needs research | Blocked |
| 6 | Tesseract local fallback | Needs research | Blocked |
| 7 | Keep OCR blocked | Blocked | Blocked |

## Rationale

The evidence is strong enough to show a recurring `dictionary + blank + 1` pattern, but not strong enough to approve a decode policy. The safest next step is a focused external semantics audit. If that audit cannot resolve the extra class, the next engineering route should be recognizer model replacement search or alternative local OCR family review.

## Safety

- No productive OCR.
- No SaaS OCR.
- No raw persistence.
- No full-screen OCR.
- No sensitive OCR.
- No-authority preserved.
- No shadow mode.
- No CDP pipeline integration.
- No risky OCR in-process.
- No decode attempted.
