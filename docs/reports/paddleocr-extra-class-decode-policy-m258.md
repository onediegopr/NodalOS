# NODAL OS M258 - PaddleOCR Extra Class Decode Policy

## Decision

`M256+M257+M258 CERRADO / BLOCKED_BY_EXTRA_CLASS_SEMANTICS`

## Summary

M256-M258 audited the repeated PaddleOCR/RapidOCR recognizer class mismatch:

- PP-OCRv4: `95` dictionary tokens + CTC blank index `0` explains `96`, but model output is `97`.
- PP-OCRv5 English: `436` dictionary tokens + CTC blank index `0` explains `437`, but model output is `438`.

The same `dictionary + blank + 1` pattern appears in both recognizers, but the extra class semantics remain unresolved. No official source audited in this block approved treating the extra class as ignored, unknown, padding, reserved, space, or any other token.

## Evidence Sources

- PaddleOCR `CTCLabelDecode` prepends `blank` and `get_ignored_tokens()` returns `[0]`.
- PaddleOCR dictionary loading strips CR/LF only, preserving real space lines but not making terminal newline a token.
- RapidOCR model list documents PP-OCRv5 recognizer availability for `en` with ONNX Runtime support.
- RapidOCR maintainer discussion confirms RapidOCR writes recognition dictionary metadata into ONNX models, but does not define an extra CTC class beyond dictionary metadata plus blank.
- Local runtime evidence from M253-M255 shows PP-OCRv5 output `fetch_name_0=[1,40,438]`.

## Hypotheses

| Hypothesis | Explains V4 | Explains V5 | Evidence | Decode |
| --- | --- | --- | --- | --- |
| Dictionary + blank only | No | No | Official PaddleOCR CTC policy | Blocked by count mismatch |
| Ignored extra class | Yes | Yes | Hypothesis only | Blocked |
| Unknown token | Yes | Yes | Hypothesis only | Blocked |
| Padding token | Yes | Yes | Hypothesis only | Blocked |
| Terminal empty line token | No | No | Rejected by PaddleOCR parser behavior | Blocked |
| Export artifact / pair mismatch | Yes | Yes | Local metadata/runtime evidence | Blocks decode |

## Decode Experiment

No positive decode was attempted or claimed.

Policy evaluation was limited to compatibility gates:

- Official blank-only policy: rejected for class count mismatch.
- Ignored-extra policy: hypothesis-only; decode blocked.
- Unknown policy: hypothesis-only; decode blocked.
- Padding policy: hypothesis-only; decode blocked.

## Safety

- No OCR productivo.
- No SaaS OCR.
- No raw persistence.
- No full-screen OCR.
- No sensitive OCR.
- No OCR as authority.
- No shadow mode.
- No CDP pipeline integration.
- No risky OCR in-process.
- No model/dictionary downloads in this block.
- No text invented.

## Next Gate

Do not proceed to synthetic decode fixtures until the extra class semantics are resolved by official evidence or an explicit manual approval accepts the risk. Recommended next path: manual decode policy approval or recognizer model replacement with a pair whose output class count equals the approved dictionary token policy.
