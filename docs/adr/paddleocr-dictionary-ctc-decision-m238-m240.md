# ADR - M238-M240 PaddleOCR Dictionary / CTC Decision

## Status

Accepted.

## Context

After upgrading `Microsoft.ML.OnnxRuntime` to `1.22.1`, recognizer `session.Run` succeeds and emits:

```text
softmax_2.tmp_0=[1,40,97]
```

The current embedded ASCII fixture dictionary contains:

```text
86 characters / 87 including blank
```

This does not match the recognizer class count `97`.

## Decision

Do not decode with the ASCII dictionary. Do not download a dictionary until source URL/ref, SHA-256, expected size, and charset count are approved.

Add a manifest contract for the required PaddleOCR dictionary:

- expected charset count: `96`.
- recognizer class count: `97`.
- blank policy: blank appended at index `96`.
- expected path: `tools/ocr-worker/models/onnx/dictionaries/paddleocr-ppocrv4-en-dict.txt`.
- source/hash/size: not selected.

## Consequences

- Recognition decode remains blocked.
- No positive OCR recognition is claimed.
- Dictionary acquisition scripts remain planned, not active.
- The next block must select and verify a dictionary source before decode.

## Decision Text

```text
M238+M239+M240 CERRADO / READY_FOR_DICTIONARY_SOURCE_SELECTION
```
