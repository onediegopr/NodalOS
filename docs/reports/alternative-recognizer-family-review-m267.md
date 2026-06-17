# NODAL OS M265-M267 - Alternative Recognizer Family Review

## Scope

This block reviewed local/offline OCR recognizer alternatives after PP-OCRv5 showed a non-trivial unresolved extra class probability.

No productive OCR, SaaS OCR, real documents, real screens, full-screen OCR, sensitive OCR, raw persistence, CDP pipeline integration, shadow mode, or in-process risky OCR was enabled.

## Baseline

- Worktree: `C:\DESARROLLO\NodalOS\Codigo-m12-audit`
- Expected base commit: `541df09`
- Remote: `https://github.com/onediegopr/NodalOS.git`
- Runtime: `Microsoft.ML.OnnxRuntime 1.22.1`
- Current PP-OCRv4 recognizer: runtime OK, decode blocked by class/dictionary mismatch.
- PP-OCRv5 English candidate: runtime OK, decode blocked by unresolved extra class.

## Current Risk

PP-OCRv5 English candidate:

- Output shape: `[1,40,438]`
- Dictionary tokens: `436`
- Blank index: `0`
- Explained classes: `437`
- Extra class index: `437`
- Extra class argmax count: `0`
- Extra class max probability: `0.28353992104530334`
- Extra class max average probability: `0.11832536425208673`

The extra class did not win argmax, but its probability is non-trivial. `ignored-extra-class` remains unsafe without stronger official evidence.

## Candidate Sources Audited

| Candidate | Source | Status | Reason |
| --- | --- | --- | --- |
| PP-OCRv5 English mobile ONNX | RapidOCR/ModelScope v3.8.0 | RejectedExtraClassUnresolved | Observed `[1,T,438]` with `436+blank=437`; extra class probability non-trivial. |
| PP-OCRv5 Latin mobile ONNX | RapidOCR/ModelScope v3.8.0 | CandidateNeedsRuntimeProbe | Official explicit pair exists, but output class count is not locally measured. |
| PP-OCRv5 Chinese/general mobile ONNX | RapidOCR/ModelScope v3.8.0 | CandidateNeedsRuntimeProbe | Official explicit pair exists, but class count and dictionary size require separate controlled probe. |
| PP-OCRv4 English/current | RapidOCR/ModelScope v3.8.0 | RejectedExtraClassUnresolved | `95+blank=96`, observed `97`; already rejected for decode. |
| PP-OCRv6 official family | PaddleOCR docs | RejectedNoOnnx | Official docs reference local recognizers and ONNXRuntime benchmarking, but no pinned ONNX+dictionary pair was selected here. |
| Tesseract local fallback | Tesseract official | CandidateNeedsManualReview | Local fallback, not an ONNX recognizer+dictionary pair. |

## Compatibility Decision

No candidate is clean acquisition-ready in this block.

The safest next route is a separate alternative local OCR family review and/or controlled runtime/class-count probes for official explicit alternatives. PP-OCRv5 English must not be auto-selected by ignoring the extra class.

## Safety

- Model/dictionary download executed: no
- Decode attempted: no
- Productive OCR: blocked
- Shadow mode: blocked
- Raw persistence: no
- Full-screen OCR: no
- Sensitive OCR: no
- SaaS OCR: no
- OCR authority: no

## Decision

```text
M265+M266+M267 CERRADO / READY_FOR_ALTERNATIVE_LOCAL_OCR_FAMILY_REVIEW
```
