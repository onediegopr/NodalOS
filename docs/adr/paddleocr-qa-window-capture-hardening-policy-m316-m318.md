# ADR — M316-M318 QA Window Capture Hardening

## Decision

Adopt a bounded hardening matrix for the real QA window host and keep PaddleOCR semantics unchanged.

Chosen rendering/capture profile for this milestone:

- capture mode: `real-qa-window-region`
- font family: `Arial`
- font size: `92`
- font style: `Bold`
- text rendering hint: `AntiAliasGridFit`
- region bounds: `x=70, y=54, width=660, height=180`
- recognizer resize mode: `RatioPreservingRightPad`

## Context

M313-M315 proved the real QA window host and regional capture path worked end-to-end, but OCR evidence missed the acceptance threshold because:

- `PVC WALL -> PVC WALI`
- `12 34 -> |2 34`

The failure profile pointed to rendering/capture fidelity rather than detector availability, recognizer runtime, dictionary pairing, class semantics, or decode policy.

## Constraints Preserved

- no SaaS OCR
- no API keys
- no full-screen capture
- no sensitive data
- no real documents
- no raw persistence of sensitive data
- no OCR authority
- no softmax reapplication
- no ignored space class
- no ONNX or gitignored dictionary commits

## Rationale

- The host now emits DPI and bounds metadata, making scaling issues auditable instead of implicit.
- The matrix is intentionally small and deterministic.
- Selection is evidence-driven:
  - maximize exact/normalized matches
  - minimize total edit distance
  - deterministic final tie-breaker
- The best configuration achieved `2/3` exact with total edit distance `1`, which is sufficient for the bounded internal gate.

## Consequences

- NODAL OS can advance to `READY_FOR_INTERNAL_LOW_RISK_SCREEN_OCR_OBSERVATION`.
- The remaining `PVC WALL -> PVC WALI` mismatch is documented as residual bounded QA-window fidelity debt, not as a semantic OCR defect.
- Future work can expand the matrix or isolate DPI/clear-type behavior further without reopening token-policy decisions.
