# NODAL OS M270 - Final Local OCR Engine Strategy

## Decision Context

M268-M270 is a Claude-first strategy block. No OCR implementation, model download, decode, shadow mode, or productive OCR was enabled.

## Strategy Matrix

| Option | Strategy | Status | Rationale |
| --- | --- | --- | --- |
| A | Continue PaddleOCR/RapidOCR and seek manual policy approval | RejectedHypothesisPolicy | `ignored-extra-class` remains unsafe without strong evidence. |
| B | Probe PP-OCRv5 Latin | ViableNeedsProbe | Explicit official pair exists; class count must be measured. |
| C | Probe PP-OCRv5 Chinese/general | ViableNeedsProbe | Explicit official pair exists; broader charset requires controlled probe. |
| D | Review PP-OCRv6 ONNX+dictionary | Recommended | Best strategic next review to avoid the PP-OCRv4/v5 extra-class dead end while staying local. |
| E | PaddleOCR detector + Tesseract recognizer fallback | FallbackOnly | Possible local fallback, not a clean ONNX recognizer strategy. |
| F | Tesseract full fallback | FallbackOnly | Local/no-SaaS, but requires separate dependency/sandbox/confidence review. |
| G | Review another ONNX OCR family | ViableNeedsManualReview | Useful if PP-OCRv6 does not yield a clean pinned pair. |
| H | Keep OCR blocked | Blocked | Safest fallback if no clean local strategy is approved. |

## Recommended Strategy

```text
READY_FOR_PPOCRV6_REVIEW
```

Rationale:

- PP-OCRv4 and PP-OCRv5 English are both blocked by unresolved `dictionary + blank + 1` semantics.
- PP-OCRv5 Latin/Chinese may repeat the same family issue and should be probes, not the primary strategy.
- Tesseract is viable as local fallback but not a clean ONNX recognizer pair.
- PP-OCRv6 deserves the next Claude-audited review for explicit ONNX+dictionary availability.

## Safety

- No OCR productivo.
- No SaaS.
- No raw persistence.
- No full-screen.
- No sensitive.
- No-authority preserved.
- No decode attempted.
- No model/dictionary download.
- No risky OCR in-process or out-of-process.

## Decision

```text
M268+M269+M270 CERRADO / READY_FOR_PPOCRV6_REVIEW
```
