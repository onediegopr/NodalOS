# ADR - Identity/Fingerprint v2 Local Fixture-First - M133-M135

## Status

Accepted.

## Context

M130-M132 rewrote the legacy HITO-162 intent as a NODAL OS sequence. The recovered intent points to Identity/Fingerprint v2 and robust perception, not production launch or broad external automation.

NODAL OS is currently stable for internal local private preview. M51 is closed for HTTP read-only target-owned proof, and M65 is closed for limited target-owned Chrome/CDP/DOM read-only proof. External CDP general-ready remains false.

## Decision

Identity/Fingerprint v2 is implemented fixture-first and local-first.

It combines redacted, non-sensitive signals:

- runtime provider
- normalized window title
- local process/app identity when fixture-controlled
- allowlisted URL/host and route/path when applicable
- redacted DOM/page metadata
- redacted UIA metadata
- safety profile
- target ownership/scope
- evidence refs
- timestamp/source

## Confidence

Supported confidence states:

- Unknown
- Low
- Medium
- High
- VerifiedFixture
- VerifiedTargetOwned

## Mismatch Reasons

Supported mismatch reasons:

- MissingRequiredSignal
- HostMismatch
- WindowTitleMismatch
- RuntimeProviderMismatch
- UnsafeSurface
- SensitiveSurface
- StaleFingerprint
- InsufficientEvidence
- AmbiguousIdentity

## Scope

Identity/Fingerprint v2 covers local controlled surfaces and target-owned metadata fixtures. It provides an evidence signal to Core, Product/Admin readiness, operator UX, and local preview summaries.

## Non-Scope

Identity/Fingerprint v2 does not authorize actions.

It does not open:

- SaaS public
- public API real
- billing/email real
- real credentials
- sensitive sites
- submit/pay/sign/delete
- productive recorder/replay
- external CDP general-ready
- embedded runtime
- Chromium fork

## Private Preview Use

Product/Admin and operator summaries may display identity confidence and warnings. Low, unknown, stale, ambiguous, sensitive, or insufficient identity must remain blocking or warning input for Core.

UI/Admin/Companion cannot convert identity confidence into action authority.

## Next Steps

M136-M138 should continue into robust perception stabilization: WindowLivenessMonitor, SystemOverlayDetector, UIA empty/block detection, SemanticAccessFallback, and auxiliary OCR/vision verification.

