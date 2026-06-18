# ADR: PaddleOCR Real QA Window Region Capture Policy M310-M312

## Status

Accepted as blocked gate.

## Context

M307-M309 validated a traceable `simulated-window-region` abstraction. M310-M312 requires replacing that abstraction with real QA window-region capture.

## Decision

Do not use simulated window capture as real QA evidence. Introduce a real QA window provenance gate and close the block as `BLOCKED_BY_REAL_QA_WINDOW_CAPTURE_TECHNIQUE` until a stable helper window host and bounded region capture path exists.

## Rationale

Using full-screen capture would violate the OCR safety rules. Reusing `simulated-window-region` would overstate readiness. A real QA window requires liveness, visibility, handle/source id, title/process match, and verified bounds.

## Next Step

Implement a minimal QA helper window host with:

- fixed title `NODAL OS OCR QA Window`,
- controlled dummy text,
- known window bounds,
- known region bounds,
- visible/liveness signal,
- bounded capture only.
