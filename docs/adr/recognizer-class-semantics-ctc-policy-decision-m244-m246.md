# ADR - M244-M246 Recognizer Class Semantics / CTC Token Policy

## Status

Accepted.

## Context

The recognizer `ch_PP-OCRv4_rec.onnx` emits `97` classes.

Audited sources expose `95` dictionary tokens:

- RapidOCR/ModelScope English dictionary.
- PaddleOCR English dictionary.
- ONNX embedded `character` metadata.

PaddleOCR CTC decoding prepends blank at index `0`, so official CTC semantics explain:

```text
95 tokens + blank = 96 classes
```

They do not explain `97` classes.

## Decision

Do not decode and do not approve hypothesis-only token policies.

Policies such as `95 tokens + blank + unknown` or `95 tokens + blank + padding` can explain `97` arithmetically, but there is no approved evidence that this recognizer uses those tokens.

## Consequences

- Positive OCR recognition remains blocked.
- Dictionary acquisition remains blocked.
- Shadow mode remains blocked.
- Productive OCR remains blocked.
- The next block must review recognizer/model/dictionary source semantics or replace/re-export the model/dictionary pair.

## Decision Text

```text
M244+M245+M246 CERRADO / READY_FOR_RECOGNIZER_MODEL_DICTIONARY_SOURCE_REVIEW
```
