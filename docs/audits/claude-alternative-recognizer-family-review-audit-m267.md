# Claude Audit Prompt - M265-M267 Alternative Recognizer Family Review

Audit the NODAL OS M265-M267 alternative recognizer review.

## Evidence To Review

- PP-OCRv4 current recognizer:
  - dictionary tokens: `95`
  - CTC blank index: `0`
  - explained classes: `96`
  - observed output classes: `97`
  - decode blocked.
- PP-OCRv5 English candidate:
  - dictionary tokens: `436`
  - CTC blank index: `0`
  - explained classes: `437`
  - observed output classes: `438`
  - extra class index: `437`
  - argmax count: `0`
  - max probability: `0.28353992104530334`
  - max average probability: `0.11832536425208673`
  - decode blocked.

## Candidate Matrix

Audit these candidates:

- RapidOCR/ModelScope PP-OCRv5 English mobile ONNX.
- RapidOCR/ModelScope PP-OCRv5 Latin mobile ONNX.
- RapidOCR/ModelScope PP-OCRv5 Chinese/general mobile ONNX.
- RapidOCR/ModelScope PP-OCRv4 English/current.
- PaddleOCR PP-OCRv6 official family.
- Tesseract local fallback.

## Audit Questions

- Was PP-OCRv4 rejection justified?
- Was PP-OCRv5 rejection justified given non-trivial extra class probability?
- Are any candidates clean enough for model+dictionary acquisition now?
- Do model, dictionary, and config refs form an explicit pair?
- Is class count formula exactly explained by dictionary tokens plus approved blank/special-token policy?
- Are model and dictionary hash/size pinnable?
- Should NODAL OS run controlled runtime/class-count probes for PP-OCRv5 Latin or Chinese/general?
- Should NODAL OS review PP-OCRv6 or another local OCR family?
- Should Tesseract local fallback be reviewed as a lower-priority fallback?

## Safety Assertions

Confirm:

- No model/dictionary download was executed in M265-M267.
- No decode was attempted.
- No OCR productive mode was enabled.
- No SaaS OCR was used.
- No raw image persistence occurred.
- No sensitive or full-screen OCR occurred.
- OCR remains non-authoritative.
- Shadow mode and CDP pipeline remain blocked.

## Requested Recommendation

Recommend the next route:

- controlled probes for explicit PP-OCR alternatives,
- alternative local OCR family review,
- Tesseract local fallback review,
- or block until a clean official ONNX recognizer+dictionary pair is identified.
