# ADR - M247-M249 Recognizer Dictionary Pair Reconciliation

## Status

Accepted.

## Context

The recognizer output exposes `97` classes. PaddleOCR documentation says `ppocr/utils/en_dict.txt` is an English dictionary with `96` characters. Prior audits found official/verifiable dictionary candidates with `95` tokens and CTC blank at index `0`.

## Decision

Do not pin a dictionary and do not decode yet.

The audited raw sources are identical:

- `190` bytes.
- SHA-256 `5662df9d2d03f0e8ca0d3b0649d6acbab904b6a14b3d3521463c71c37c668ce3`.
- `96` LF split segments only because the file ends with a terminal newline.
- `95` effective PaddleOCR parser tokens, including one single-space token.

PaddleOCR CTC prepends blank at index `0`, so the official dictionary explains `96` classes, not the recognizer's `97`.

Final decision:

`M247+M248+M249 CERRADO / READY_FOR_RECOGNIZER_MODEL_DICTIONARY_PAIR_REPLACEMENT`

## Consequences

- Decode remains blocked.
- Shadow mode remains blocked.
- Productive OCR remains blocked.
- Next work should review or replace the recognizer/model/dictionary pair instead of inventing an extra token.
