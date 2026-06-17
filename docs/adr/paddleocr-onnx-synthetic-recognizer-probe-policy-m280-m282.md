# ADR: PaddleOCR ONNX Synthetic Recognizer Probe Policy - M280-M282

## Status

Accepted for out-of-process synthetic recognizer probing only.

## Context

M277-M279 validated deterministic synthetic CTC decode fixtures for the official PaddleOCR space token policy. M280-M282 validates the next layer: recognizer ONNX runtime output can be consumed by the approved policy plumbing without claiming useful OCR text.

## Decision

Run recognizer ONNX only through the out-of-process guard using synthetic tensors.

Approved boundaries:

- input is synthetic tensor only
- output summary is metadata/statistics only
- output layout must be `[B,T,C]`
- class axis must be final
- `SoftmaxReapplied=false`
- official space policy must be `blank(0) + dictionary(1..N) + space(N+1)`
- OCR remains no-authority

Not approved:

- productive OCR
- shadow mode
- real image/screen/document OCR
- CDP pipeline integration
- OCR-based actions

## Consequence

NODAL OS can proceed to synthetic image recognizer crop fixtures. This is still not productive OCR readiness.
