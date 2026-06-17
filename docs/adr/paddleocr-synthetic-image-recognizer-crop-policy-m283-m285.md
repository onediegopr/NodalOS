# ADR: PaddleOCR Synthetic Image Recognizer Crop Policy M283-M285

## Status

Accepted for synthetic/no-authority fixtures only.

## Context

Previous milestones approved the official PaddleOCR class layout:

`blank(0) + dictionary(1..N) + space(N+1)`

M280-M282 validated ONNX recognizer runtime and decode-policy plumbing with a synthetic tensor. M283-M285 moves one step closer to image-like input by generating synthetic crops in memory and preprocessing them to the recognizer tensor shape.

## Decision

Use in-memory synthetic crop generation for recognizer-only probes:

- Render deterministic block glyphs for approved fixture texts.
- Preprocess directly to `[1,3,48,320]`.
- Normalize as `(pixel/255 - 0.5) / 0.5`.
- Run PP-OCRv5 recognizer out-of-process.
- Decode only as a no-authority preview under the official space-token policy.
- Do not persist synthetic crop images unless a later QA artifact explicitly requires it.

## Consequences

- The gate validates runtime, preprocessing shape, softmax evidence, output layout, official space-token mapping, and parent-process isolation.
- The gate does not certify OCR accuracy.
- A single exact synthetic match is enough evidence to proceed to synthetic detector-to-recognizer fixtures, because productive OCR remains blocked.

## Prohibitions

- No real images.
- No real screens.
- No documents.
- No sensitive data.
- No raw persistence of real data.
- No SaaS OCR.
- No shadow mode.
- No productive OCR.
- No OCR authority.
- No model or dictionary commits.
