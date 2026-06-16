# Claude Audit Prompt: Pre-PaddleOCR Readiness (M190)

Audit NODAL OS M188-M190 before any PaddleOCR/Tesseract install plan.

## Hard Rules

- Do not approve real OCR activation.
- Do not approve SaaS OCR activation.
- Do not approve Python OCR worker execution.
- Do not approve PaddleOCR/Tesseract installation unless the output is only an install plan.
- OCR/Vision remains no-authority.
- Core remains the authority.
- Raw images must not be persisted.

## Review Targets

1. Pixel redaction V2
   - Verify whether `NodalOsPixelImageRedactor` really modifies pixel bytes.
   - Verify whether redacted hash differs from original hash when masks are applied.
   - Verify whether malformed image bytes, out-of-bounds regions, full-screen/oversized inputs and high sensitivity without reliable regions fail closed.
   - Verify whether original raw bytes are persisted anywhere.
   - Determine if this is real pixel redaction or still marker/string based.

2. Worker process boundary
   - Verify whether any OCR process is launched.
   - Verify whether the innocent echo process is bounded by timeout and allow-list.
   - Verify whether the code claims strong OS isolation without evidence.
   - Verify whether network/filesystem claims are modeled, observed, enforced or not enforced.

3. IPC hardening
   - Verify auth token checks.
   - Verify contract version checks.
   - Verify max message size checks.
   - Verify timeout/lifetime checks.
   - Verify safe JSON deserialization and no arbitrary type deserialization.

4. Readiness review
   - Verify `NodalOsPrePaddleOcrReadinessReviewer`.
   - Verify it blocks missing pixel redaction, raw persistence risk, missing auth, oversize, network observation and authority risk.
   - Verify it reports modeled/observed/enforced isolation honestly.
   - Verify it never enables real OCR or SaaS OCR.

## Questions To Answer

- Is M188 true pixel redaction over image bytes, not marker-based redaction?
- Is M189 honest about OS isolation level?
- Is M189 IPC hardened enough for a pre-PaddleOCR design phase?
- Does M190 block real OCR while allowing only design-only or synthetic-install-plan readiness?
- Is it safe to move to a PaddleOCR install plan, still without activation?
- What mandatory fixes remain before installing any OCR runtime?

## Required Verdict

Return one of:

- GO for PaddleOCR install plan design only
- GO with corrections before install plan
- NO-GO, fix redaction first
- NO-GO, fix isolation/IPC first

Include confidence from 1 to 10 and list blocking issues first.

