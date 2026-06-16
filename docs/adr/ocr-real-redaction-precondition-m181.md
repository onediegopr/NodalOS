# OCR Real Redaction Precondition M181

Date: 2026-06-16

Product: NODAL OS

Decision: before any real OCR worker, `CropRedacted` cannot be an assumed flag. CropRedacted cannot be an assumed flag in code, tests, docs or operator claims. OCR routing must require a verified redaction result produced by a redaction pipeline. OCR real remains disabled.

## Claude Finding A-1

The previous OCR/Vision design correctly blocked on `RedactionStatus`, `CropRedacted`, `BlockedSensitive` and `RedactionFailed`, but those were downstream input flags. A buggy upstream component could claim `CropRedacted=true` for a crop that still contains visible credentials. That is not acceptable before a real worker.

## M181 Decision

NODAL OS now models a real/synthetic crop redaction pipeline:

- `NodalOsImageCropRedactionRequest`
- `NodalOsImageCropRedactionResult`
- `NodalOsImageCropRedactor`
- `NodalOsImageRedactionFinding`
- `NodalOsImageRedactionDecision`
- `NodalOsImageRedactionPolicy`
- `NodalOsImageRedactionEvidence`

The current implementation uses synthetic fixture bytes and local heuristics. It does not execute OCR, does not process real screenshots, and does not persist raw images.

## Why CropRedacted Cannot Be Assumed

OCR can only consume a crop when a redaction result exists with:

- decision `Redacted` or `CleanNoRedactionRequired`
- `CropRedacted=true`
- `SafeForOcr=true`
- `OriginalRawPersisted=false`
- redacted evidence refs
- no-authority confirmed

If the redaction result is missing, failed, sensitive, low confidence, or uncertain, OCR routing blocks or asks human review.

## Fail-Closed Policy

The redaction pipeline is fail-closed: when confidence is low, evidence is missing, the crop is too broad, or a sensitive marker is ambiguous, OCR must not proceed.

Findings include:

- PasswordField
- CredentialLikeText
- TokenLikeText
- JwtLikeText
- CookieLikeText
- ApiKeyLikeText
- EmailLikeText
- PhoneLikeText
- CreditCardLikeText
- DocumentIdLikeText
- SensitiveKeyword
- UnknownSensitivePattern
- LowConfidence
- RedactionEngineUncertain

Unknown, low-confidence, raw persistence, full-screen, oversized and sensitive conditions fail closed.

## Router Integration

`NodalOsOcrVisionRouter` now requires `NodalOsImageCropRedactionResult` for OCR over crops. Plain `CropRedacted=true` is not sufficient.

Blocked behavior:

- missing redaction result -> blocked by redaction
- `RedactionFailed` -> blocked by redaction
- `BlockedSensitive` -> human review / sensitive blocked
- low confidence -> human review
- full-screen -> blocked

Valid behavior:

- `Redacted` -> local stub routing may proceed
- `CleanNoRedactionRequired` -> local stub routing may proceed

## Local Stub / Worker Boundary Integration

Local OCR stubs and the local OCR worker boundary also require a valid redaction result. They remain model-only and do not call OCR runtimes, external processes, Python, PaddleOCR, Tesseract or SaaS.

## Cloud Detection Hardening

Claude finding M-1 was correct: provider cloud classification by `Kind.ToString().StartsWith("Cloud")` is fragile.

M181 replaces that classification with explicit policy:

- `Policy.ExternalDataTransfer`
- `PrivacyProfile.ExternalDataTransfer`
- `Provider.RequiresExternalDataTransfer`
- `Provider.IsCloud`
- activation readiness `RequiresExternalDataTransfer`

This means an external provider with a non-cloud enum/name is still blocked, and a local provider whose id contains `cloud` is not blocked solely by its name.

## No-Authority

Redaction, OCR routing, local stubs, worker boundary and activation gate remain no-authority. They cannot approve actions, clicks, credential entry, submit, payment, sign, delete, recorder/replay or production flows.

## Still Blocked

- OCR real
- SaaS OCR real
- PaddleOCR runtime
- Tesseract runtime
- Python worker
- external OCR APIs
- API keys/secrets
- full-screen OCR
- sensitive screenshots/crops
- raw image persistence

## Next Step

M182 can design a worker skeleton only after preserving this redaction-result precondition. It must not activate OCR real until a separate opt-in/audit gate allows it.
