# ADR: PaddleOCR Synthetic Decode No-Authority Policy - M277-M279

## Status

Accepted for synthetic probability fixtures only.

## Context

M274-M276 approved PaddleOCR's official space-token mapping:

```text
blank(0) + dictionary(1..N) + space(N+1)
```

The next step is to validate deterministic CTC decode behavior before any ONNX recognizer decode probe or image crop fixture work.

## Decision

Use synthetic probability matrices to validate:

- blank collapse
- repeat handling with intermediate blank
- official space index decoding
- blanks inside sequences
- multiple spaces
- leading/trailing space behavior
- top-k evidence capture
- no double softmax

Do not run productive OCR, shadow mode, document OCR, screen OCR, or CDP OCR pipeline.

## ONNX Boundary

The ONNX recognizer probe is deferred. The readiness decision is therefore `READY_FOR_ONNX_SYNTHETIC_RECOGNIZER_DECODE_PROBE`, not an image-crop or pipeline readiness.

## Consequences

NODAL OS can proceed to an out-of-process ONNX recognizer synthetic decode probe. OCR remains no-authority and non-productive.
