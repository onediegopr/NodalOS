# Claude Audit Prompt - M268 Alternative Local OCR Family Review

This block is Claude-first. Do not treat this as implementation approval.

## Current NODAL OS OCR State

- Runtime: `Microsoft.ML.OnnxRuntime 1.22.1`
- Provider: `CPUExecutionProvider`
- Detector: current PaddleOCR ONNX detector runs successfully.
- OCR productive mode: blocked.
- Shadow mode: blocked.
- CDP real pipeline: blocked.
- OCR authority: prohibited.

## Evidence Summary M200-M267

NODAL OS selected RapidOCR/ModelScope PaddleOCR ONNX assets, acquired and verified detector/recognizer/classifier models, fixed a recognizer crash by upgrading ONNX Runtime from `1.18.1` to `1.22.1`, then blocked decode on model/dictionary semantics.

### PP-OCRv4 Evidence

- Recognizer runtime: OK.
- Dictionary tokens `95`.
- PaddleOCR CTC blank index `0`.
- Expected classes with blank: `96`.
- Observed output classes `97`.
- Decode blocked.
- No token was invented.

### PP-OCRv5 English Evidence

- Recognizer runtime: OK.
- Dictionary tokens `436`.
- PaddleOCR CTC blank index `0`.
- Expected classes with blank: `437`.
- Observed output classes `438`.
- Extra class index: `437`.
- Argmax count for extra class: `0`.
- Extra class max probability `0.2835`.
- Extra class average/max probability `0.1183`.
- Decode blocked because the extra class probability is non-trivial.
- `ignored-extra-class` was not approved.

## M265-M267 Candidate Matrix

- PP-OCRv5 English: rejected for automatic decode because extra class is unresolved and non-trivial.
- PP-OCRv5 Latin: official explicit pair exists, but needs runtime/class-count probe.
- PP-OCRv5 Chinese/general: official explicit pair exists, but needs runtime/class-count probe.
- PP-OCRv6: official PaddleOCR family exists, but NODAL OS has not pinned an ONNX+dictionary pair.
- Tesseract local: possible fallback, not primary yet.
- Other ONNX OCR family: requires official, local, pinnable source review.

## Security Constraints

- No OCR productivo.
- No SaaS OCR.
- No API keys.
- No datos reales.
- No documentos reales.
- No pantallas reales.
- No full-screen OCR.
- No sensitive OCR.
- No raw persistence.
- No OCR como autoridad.
- No shadow mode.
- No CDP pipeline real.
- No risky OCR in-process.
- No model/dictionary download in this strategy block.
- No decode without approved class/token policy.

## Questions For Claude

1. Should NODAL OS continue with PaddleOCR/RapidOCR ONNX despite the extra class?
2. Is `dictionary + blank + 1` a known PaddleOCR/RapidOCR convention that is safe to ignore?
3. Could PP-OCRv5 Latin or Chinese/general offer a cleaner pair?
4. Does PP-OCRv6 provide a verifiable ONNX + dictionary pairing suitable for NODAL OS?
5. Is there a cleaner local ONNX OCR family for this use case?
6. Should Tesseract local be used as fallback or temporary recognizer?
7. Should the architecture split PaddleOCR detector, alternative recognizer, and Tesseract fallback behind router/gates?
8. What is the safest path to private, local, no-authority OCR with auditability?

## Requested Output

Recommend one next route:

- `READY_FOR_PPOCRV5_LATIN_PROBE`
- `READY_FOR_PPOCRV5_CHINESE_GENERAL_PROBE`
- `READY_FOR_PPOCRV6_REVIEW`
- `READY_FOR_TESSERACT_LOCAL_FALLBACK_REVIEW`
- `READY_FOR_ALTERNATIVE_ONNX_OCR_FAMILY_REVIEW`
- `READY_FOR_MANUAL_IGNORED_EXTRA_CLASS_POLICY_APPROVAL`
- `OCR_LOCAL_RECOGNIZER_STRATEGY_BLOCKED`
