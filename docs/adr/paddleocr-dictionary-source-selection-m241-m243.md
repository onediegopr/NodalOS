# ADR - M241-M243 PaddleOCR Dictionary Source Selection

## Status

Accepted.

## Context

M238-M240 required a dictionary compatible with recognizer output class count `97`.

The current compatibility gate expects:

```text
96 dictionary tokens + 1 CTC blank = 97
```

## Decision

Do not pin or download the current official English dictionary sources because they expose `95` tokens, not `96`.

Official sources audited:

- RapidOCR/ModelScope `en_PP-OCRv4_rec_mobile/en_dict.txt`.
- PaddleOCR `release/2.8/ppocr/utils/en_dict.txt`.
- Embedded `character` metadata inside verified `ch_PP-OCRv4_rec.onnx`.

All three are official/verifiable, but all expose `95` tokens.

## Consequences

- Dictionary acquisition remains disabled.
- No active dictionary download/verify/rollback scripts are created.
- No CTC decode is attempted.
- No positive OCR recognition is claimed.
- Productive OCR and shadow mode remain blocked.

## Required Follow-Up

Determine whether recognizer class `97` means:

- `95 dictionary tokens + blank + extra special class`; or
- a truly missing `96`th dictionary token.

Until this is resolved, the correct state is blocked by count mismatch.

## Decision Text

```text
M241+M242+M243 CERRADO / BLOCKED_BY_DICTIONARY_COUNT_MISMATCH
```
