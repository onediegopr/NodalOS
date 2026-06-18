# ADR: PaddleOCR QA Window Host Real Region Capture Policy M313-M315

## Status

Accepted for QA capture hardening.

Final decision: `M313+M314+M315 CERRADO / READY_FOR_QA_WINDOW_CAPTURE_HARDENING`.

## Context

M310-M312 intentionally blocked before real capture because no stable QA window host existed. M313-M315 adds that missing controlled host and validates real bounded region capture without moving to uncontrolled screen OCR.

## Decision

Use a dedicated local Windows QA host for this gate.

The host:

- Must be controlled by NODAL OS test tooling.
- Must use fixed title `NODAL OS OCR QA Window`.
- Must expose known window/client bounds.
- Must capture only an explicit region.
- Must render dummy non-sensitive text.
- Must clean up after the probe.

The OCR runner:

- Must validate real QA window provenance fail-closed.
- Must reject simulated windows as real QA windows.
- Must run ONNX under out-of-process guard.
- Must preserve OfficialSpaceToken mapping.
- Must not reapply softmax.
- Must not claim productive OCR or action authority.

## Consequences

Positive:

- Real QA window capture infrastructure now exists.
- The parent survives ONNX child execution.
- Provenance, bounds, liveness and no-full-screen constraints are enforced.

Limitations:

- OCR evidence reached only 1/3 exact matches.
- The gate does not open internal low-risk screen OCR observation.
- Additional capture/render/crop hardening is required.

## Non-Goals

- No full-screen capture.
- No real documents.
- No sensitive data.
- No CDP pipeline.
- No OCR-driven actions.
- No production/product readiness claim.
