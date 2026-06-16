# ADR: Pre-PaddleOCR Redaction and Isolation Readiness (M188-M190)

## Status

Accepted for pre-real OCR design readiness. Not accepted for real OCR activation.

## Context

Claude identified two high-risk gaps before any PaddleOCR/Tesseract runtime can be installed:

- A-1: worker isolation was modeled/synthetic, not enforced by the operating system.
- A-2: image redaction was marker/string based, not real pixel redaction.

Claude also identified medium risks:

- M-1: IPC needed hardening beyond a synthetic contract.
- M-2: redaction needed to stop depending on substring/marker fixtures.

## Decision

M188-M190 add the final pre-PaddleOCR readiness layer:

- Pixel redaction V2 modifies real synthetic image bytes in memory.
- Original raw image bytes are never persisted by this layer.
- The worker boundary can exercise an innocent echo process for lifecycle, timeout and output validation.
- IPC validation requires auth token, contract version, size limit and timeout.
- The readiness review reports isolation honestly as modeled, observed, enforced or not enforced.
- Real OCR remains disabled.
- SaaS OCR remains disabled.
- OCR remains no-authority.

## Pixel Redaction V2

`NodalOsPixelImageRedactor` accepts raw RGBA32 synthetic image bytes plus candidate sensitive regions. It masks candidate regions on a copy, verifies the mask changed pixels, computes original/redacted hashes, and returns a redacted in-memory handoff only when verification passes.

Allowed decisions:

- `RedactedPixels`
- `CleanNoRedactionRequired`
- `BlockedSensitive`
- `RedactionFailed`

Fail-closed behavior:

- malformed bytes fail
- out-of-bounds regions fail
- full-screen/oversized images fail
- high sensitivity without reliable regions blocks
- verification failure blocks
- raw persistence request fails

## Process Boundary Reality Level

The process boundary uses an innocent echo process only when explicitly allowed by policy. It is not OCR and does not invoke Python, PaddleOCR, Tesseract, SaaS, or arbitrary commands.

Current isolation claims are intentionally conservative:

- network isolation: modeled unless OS evidence proves otherwise
- filesystem isolation: modeled unless OS evidence proves otherwise
- process isolation: observed for the innocent echo lifecycle, not a strong sandbox

The system must not claim strong OS sandboxing unless evidence explicitly reports `Enforced`.

## IPC Hardening

The IPC channel validates:

- auth token present and exact
- contract version
- maximum message size
- message lifetime/timeout
- safe JSON serialization/deserialization
- no-authority invariant

Oversize, missing auth, invalid auth, invalid version and expired messages are rejected.

## Claims Allowed

- Real pixel bytes are redacted in synthetic test images.
- Redacted image hash differs from original hash when masks are applied.
- Raw original bytes are not persisted by this layer.
- IPC validates auth, version, size and timeout.
- Innocent echo lifecycle can be observed.
- Readiness can reach design-only or synthetic-install-plan readiness.

## Claims Prohibited

- OCR works.
- PaddleOCR/Tesseract are installed.
- OCR quality or accuracy has been measured.
- Python worker isolation is enforced.
- Network/filesystem isolation is OS-enforced when evidence is modeled or observed only.
- Real OCR can be enabled.
- SaaS OCR can be enabled.
- OCR can authorize actions.

## Next Phase Options

- Ask Claude to audit M188-M190 before any install plan.
- If accepted, prepare a PaddleOCR install plan only, still no OCR activation.
- If rejected, harden pixel redaction, IPC, or isolation before continuing.

