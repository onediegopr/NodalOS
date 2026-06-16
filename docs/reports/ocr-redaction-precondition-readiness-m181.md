# OCR Redaction Precondition Readiness M181

Date: 2026-06-16

Product: NODAL OS

Decision: M181 closes the redaction-as-assumed-flag gap for model-only OCR routing. OCR real remains disabled.

## Current Status

- Redaction request/result contracts exist.
- Synthetic byte redactor exists.
- Sensitive finding taxonomy exists.
- Router requires a valid redaction result before OCR.
- Local OCR stubs require a valid redaction result.
- Local worker boundary requires a valid redaction result.
- Cloud detection uses explicit external-transfer policy.
- Activation gate still returns `RealOcrEnabled=false`.
- SaaS OCR remains disabled-by-default.

## What Is Protected

- A crop cannot reach OCR routing just because `CropRedacted=true` was passed.
- Missing redaction result blocks.
- Redaction failed blocks.
- Sensitive result blocks or requires human review.
- Low confidence redaction asks human review.
- Full-screen OCR remains blocked.
- Raw image persistence remains blocked.
- OCR/Vision remains no-authority.

## What Remains Not Proven

- Real pixel redaction on browser screenshots.
- Real image masking or crop byte transformation.
- Real OCR quality.
- Real local worker IPC.
- PaddleOCR/Tesseract runtime behavior.
- SaaS OCR behavior.

## Why OCR Real Remains Disabled

This phase only establishes the precondition: redaction evidence must be generated before OCR routing. There is still no worker, no OCR runtime, no Python, no subprocess invocation, no SaaS endpoint and no API key.

## What Is Needed For M182 worker skeleton

- Preserve mandatory redaction result input.
- Keep raw image persistence disabled.
- Keep full-screen blocked.
- Keep local loopback/process/container/CLI boundary model-only first.
- Add versioned JSON IPC schema.
- Add bounded timeout/memory/page/image-size checks.
- Keep `RealOcrEnabled=false`.

## Final M181 Result

- Redaction precondition: implemented.
- Cloud detection hardening: implemented.
- OCR real executed: no.
- SaaS OCR executed: no.
- Real worker created: no.
- PaddleOCR/Tesseract installed: no.
