# ADR: PaddleOCR Official Space Token Policy - M274-M276

## Status

Accepted for synthetic, no-authority fixtures only.

## Context

M271-M273 established the official PaddleOCR root cause for the previous extra-class mismatch: `use_space_char=true` appends `" "` to the dictionary before CTC blank handling prepends blank at index `0`.

This resolves:

```text
PP-OCRv4: 95 + blank + space = 97
PP-OCRv5: 436 + blank + space = 438
```

## Decision

Use `OfficialSpaceToken` as the approved class mapping policy:

```text
blank index = 0
dictionary tokens = 1..N
space index = N+1
output layout = [B,T,C]
output softmax = already applied
```

Do not approve `IgnoreExtraClass` for PaddleOCR English. It drops real spaces and can silently corrupt text.

## Boundaries

This decision does not enable:

- productive OCR
- shadow mode
- CDP pipeline OCR integration
- OCR as authority
- clicks, submit, pay, sign, or delete based on OCR

## Consequences

NODAL OS can proceed to controlled synthetic official-space decode fixtures. Any future PP-OCRv6 model must still prove its own dictionary, class count, blank policy, and output layout.
