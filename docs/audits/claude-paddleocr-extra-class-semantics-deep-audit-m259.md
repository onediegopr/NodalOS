# Claude Deep Audit Prompt - M259 PaddleOCR Extra Class Semantics

Audit NODAL OS M238-M258 evidence for PaddleOCR/RapidOCR recognizer class semantics.

## Safety Boundary

- Do not invent tokens.
- Do not infer a decode policy only because the count fits.
- Do not recommend productive OCR.
- Do not recommend shadow mode.
- Do not approve OCR as authority.
- Use only official/verifiable source evidence or explicitly label a finding as hypothesis-only.

## PP-OCRv4 Evidence

- Recognizer: `ch_PP-OCRv4_rec.onnx`.
- Runtime: `Microsoft.ML.OnnxRuntime 1.22.1`, `CPUExecutionProvider`.
- Runtime status: session creation OK, `session.Run` OK after runtime upgrade.
- Dictionary source candidates: PaddleOCR/RapidOCR English dictionary and ONNX `character` metadata.
- dictionary tokens = 95 under PaddleOCR parser.
- CTC blank index = 0 under PaddleOCR `CTCLabelDecode`.
- Expected classes: `95 + 1 = 96`.
- Observed output classes: `97`.
- Status: dictionary+blank does not explain model output.

## PP-OCRv5 Evidence

- Candidate recognizer: `en_PP-OCRv5_rec_mobile.onnx`.
- Recognizer SHA-256: `c3461add59bb4323ecba96a492ab75e06dda42467c9e3d0c18db5d1d21924be8`.
- Recognizer size: `7872351`.
- Dictionary: `ppocrv5_en_dict.txt`.
- Dictionary SHA-256: `e025a66d31f327ba0c232e03f407ae8d105e1e709e7ccb3f408aa778c24e70d6`.
- Dictionary size: `1416`.
- dictionary tokens = 436.
- CTC blank index = 0.
- Expected classes: `436 + 1 = 437`.
- Observed runtime output: `fetch_name_0=[1,40,438]`.
- Runtime smoke: zero, ones, gradient, synthetic crop, and high-contrast crop all ran OK out-of-process.
- Status: dictionary+blank does not explain model output.

## Evidence Sources

- PaddleOCR `rec_postprocess.py`: `CTCLabelDecode.add_special_char` prepends `blank`; `get_ignored_tokens()` returns `[0]`.
- PaddleOCR dictionary parser strips CR/LF only; a real space line is preserved, terminal newline is not a token.
- RapidOCR model list documents PP-OCRv5 recognizer availability and ModelScope hosting.
- RapidOCR maintainer discussion says RapidOCR writes recognition dictionary metadata into ONNX models.
- Local ONNX metadata/runtime smoke from M238-M258.

## Rejected or Unapproved Hypotheses

- `ignored-extra-class`: explains `dictionary + blank + 1` arithmetically, but no official CTC source has approved ignoring it.
- `unknown`: not evidenced for PaddleOCR CTC.
- `padding`: not evidenced for PaddleOCR CTC.
- `reserved`: not evidenced for PaddleOCR CTC.
- `space token not counted`: rejected for PP-OCRv4 by raw parser analysis; PP-OCRv5 dictionary does not have a missing space-only line.
- `terminal empty line`: rejected by PaddleOCR parser behavior.
- `export artifact`: possible, but not enough to approve decode.
- `model/dictionary mismatch`: possible, blocks decode unless resolved.

## Exact Questions

1. What is the extra class in PP-OCRv4 and PP-OCRv5?
2. Is it safe to ignore?
3. Is it an unknown, padding, reserved, end, or separator class?
4. Is it an export artifact?
5. Is this a known PaddleOCR/RapidOCR convention?
6. Should NODAL OS approve a policy or replace the model pair?
7. If policy approval is recommended, what exact class index and CTC handling should be used, and what official/verifiable evidence supports it?
8. If replacement is recommended, should the next block search for another recognizer+dictionary pair or another local OCR family?

## Required Audit Output

Return:

- Evidence-backed conclusion.
- Risk level.
- Whether decode remains blocked.
- Whether manual approval is acceptable.
- Whether model replacement is recommended.
- Whether alternative local OCR family review is recommended.

No positive OCR claim is allowed from this audit alone.
