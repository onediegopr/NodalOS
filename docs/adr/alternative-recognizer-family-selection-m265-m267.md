# ADR - M265-M267 Alternative Recognizer Family Selection

## Status

Accepted.

## Context

M262-M264 showed PP-OCRv5 English recognizer output class count `438` while the verified dictionary has `436` tokens and PaddleOCR CTC blank index `0`, explaining only `437` classes.

The extra class index `437` never appeared as argmax, but its probability was non-trivial:

- max probability: `0.28353992104530334`
- max average probability: `0.11832536425208673`

Ignoring the extra class is not safe without stronger official evidence.

## Decision

Do not select PP-OCRv5 English as a clean decode pair.

Do not approve decode.

Do not download additional model/dictionary files in this block.

Proceed to alternative local OCR family review or controlled runtime/class-count probes for official explicit alternatives.

```text
M265+M266+M267 CERRADO / READY_FOR_ALTERNATIVE_LOCAL_OCR_FAMILY_REVIEW
```

## Consequences

- PP-OCRv4 and PP-OCRv5 remain usable only for runtime diagnostics, not approved text decode.
- Synthetic decode fixtures remain blocked.
- Shadow mode remains blocked.
- Productive OCR remains blocked.
- OCR remains non-authoritative.

## Rejected Options

- Auto-approve `ignored-extra-class` for PP-OCRv5 English.
- Force decode with an incompatible dictionary.
- Select a candidate without explicit dictionary/config pairing.
- Select a candidate without ONNX availability for clean pair acquisition.
- Switch to Tesseract without a separate fallback review.
