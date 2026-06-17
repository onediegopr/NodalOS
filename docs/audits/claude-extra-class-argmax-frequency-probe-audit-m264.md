# Claude Audit Prompt - M264 Extra Class Argmax Frequency Probe

Audit the NODAL OS PP-OCRv5 extra-class argmax/probability evidence.

Context:

- Runtime: `Microsoft.ML.OnnxRuntime 1.22.1`
- Provider: `CPUExecutionProvider`
- Recognizer: `en_PP-OCRv5_rec_mobile.onnx`
- Dictionary: `ppocrv5_en_dict.txt`
- Dictionary tokens: `436`
- PaddleOCR CTC blank index: `0`
- Expected classes from dictionary + blank: `437`
- Observed output shape: `[1,40,438]`
- Extra class index: `437`
- Decode was not attempted.

Probe evidence:

- Normal fixtures: `TEST`, `NODAL`, `12345`, `ABC123`, `HighContrastCrop`, `DetectorDerivedCrop`
- Extreme fixtures: `Black`, `White`, `DeterministicNoise`, `Gradient`, `ThinLines`, `Checkerboard`, `OutOfDictionary`, `InvalidEmptyCrop`
- `InvalidEmptyCrop` was blocked before runtime.
- Extra class `437` never appeared as argmax in runnable fixtures.
- Extra class max probability was non-trivial: `0.28353992104530334` on `12345`.
- Extra class average probability maximum was `0.11832536425208673` on `12345`.
- Blank index `0` dominated many fixtures.

Questions:

1. Does the fact that class `437` is never argmax justify ignoring it?
2. Is the observed non-trivial probability enough to block an ignored-extra-class policy?
3. Could class `437` be a known PaddleOCR/RapidOCR reserved/unknown/padding/export class?
4. Is there official evidence that class `437` can be safely ignored?
5. Should NODAL OS require manual approval, continue external audit, or replace the model pair?

Hard audit constraints:

- Do not invent tokens.
- Do not infer approval merely because a policy makes the count fit.
- Do not approve decode without evidence.
- Verify no risky OCR ran in-process.
- Verify no raw image persistence.
- Verify no sensitive/full-screen OCR.
- Verify no SaaS OCR.
- Verify no-authority.

Recommended output:

- Approve/reject `ignored-extra-class` policy.
- State whether non-trivial probability blocks approval.
- Recommend next route: manual policy approval, further Claude audit, synthetic decode fixtures, or model replacement.
