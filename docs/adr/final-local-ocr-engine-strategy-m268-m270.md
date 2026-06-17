# ADR - M268-M270 Final Local OCR Engine Strategy

## Status

Accepted.

## Context

NODAL OS requires private, local, no-authority OCR without SaaS, raw persistence, full-screen OCR, sensitive OCR, productive OCR, or shadow mode.

PP-OCRv4 and PP-OCRv5 both run locally, but decode remains blocked by unresolved class semantics:

- PP-OCRv4: `95 + blank = 96`, observed `97`.
- PP-OCRv5 English: `436 + blank = 437`, observed `438`.
- PP-OCRv5 extra class probability is non-trivial.

## Decision

Do not approve ignored-extra-class.

Do not run decode.

Do not implement a new OCR engine in this block.

Prepare Claude-first audit artifacts and choose the next review route:

```text
M268+M269+M270 CERRADO / READY_FOR_PPOCRV6_REVIEW
```

## Consequences

- OCR remains blocked for productive use.
- PP-OCRv5 Latin/Chinese remain possible probes, not selected defaults.
- Tesseract remains fallback-only pending separate review.
- PP-OCRv6 review must identify explicit ONNX+dictionary/config refs, hash/size pinning, class-count semantics, and local runtime feasibility before acquisition.
