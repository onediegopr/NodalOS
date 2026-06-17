# ADR - M256-M258 PaddleOCR Extra Class Decode Policy

## Status

Accepted.

## Context

M253-M255 acquired and verified the PP-OCRv5 English recognizer+dictionary candidate. Runtime smoke succeeded, but output class count was `438` while the dictionary has `436` tokens and PaddleOCR CTC blank index `0` explains `437`.

This repeats the PP-OCRv4 pattern where `95` dictionary tokens plus blank explain `96`, but the recognizer outputs `97`.

## Decision

Do not approve decode.

The official PaddleOCR CTC policy adds one blank token at index `0`. It does not provide evidence for an additional ignored, unknown, padding, reserved, space, or separator class in the CTC decoder path. The extra class can be made to fit arithmetically, but not safely.

Final decision:

`M256+M257+M258 CERRADO / BLOCKED_BY_EXTRA_CLASS_SEMANTICS`

## Consequences

- PP-OCRv5 candidate remains acquired and runtime-healthy, but not decode-approved.
- Synthetic decode fixtures remain blocked.
- Productive OCR and shadow mode remain blocked.
- Next route requires manual decode policy approval with explicit risk acceptance, or recognizer model replacement with an output class count fully explained by an approved dictionary/token policy.
