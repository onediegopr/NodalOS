# Windows Computer Use Locator Fusion v1

Status: fixture-safe / read-only / no-live / no-action-authority.

## Purpose

Locator Fusion combines read-only evidence from multiple WCU subsystems to produce ranked locator candidates and a safety assessment. It is a diagnostic and planning aid, not an action authority.

## Inputs

- UIA snapshot candidates (`ComputerUseSnapshot`)
- OCR/visual candidates (`RobustPerceptionBridgeResult`)
- Win32 read-only context (`Win32ContextCollectionResult`)
- UIA event stream metadata (`WindowsUiAutomationEventStreamState`)
- Existing perception fusion output (`ComputerUsePerceptionFusionResult`)

## Output

`ComputerUseLocatorFusionResult` contains:

- `LocatorCandidates`: ranked selector candidates.
- `BestCandidate`: top candidate, if any.
- `Ambiguity`: ambiguity detection between competing candidates.
- `StaleElementRisk`: staleness risk from Win32/UIA events.
- `VisualHintMatches`: OCR/visual hint matches.
- `EventContinuitySignals`: UIA event continuity signals.
- `Win32Anchor`: Win32 active window anchor signal.
- `VisualFallbackRequired`: true when visual-only candidates are needed.
- `RequiresHumanHandoff`: true when a human must review.
- `HandoffReasons`: reasons for handoff.
- `Blockages`: blockages detected.
- `SensitiveSurfaces`: sensitive surfaces detected.
- `EvidenceRefs`: redacted evidence references.
- `ActionAuthorityGranted`: always false.
- `AllowedToExecuteLive`: always false.
- `LiveProviderCalled`: always false.
- `RealPcRead`: always false.
- `RawScreenshotStored`: always false.
- `RawTextPresent`: true only when sensitive raw text was observed (and redacted).

## Candidate Properties

Each `ComputerUseSelectorCandidate` exposes:

- `CandidateId`
- `SelectorKind` (e.g., `AutomationId+Process+WindowContext`, `VisualHintOnly`, `BoundingBoxFallbackMetadata`)
- `Identity` (UIA identity when available)
- `Bounds` (UIA bounds when available)
- `LabelRedacted`
- `ConfidenceBreakdown` with per-signal scores and penalties
- `Evidence` list with redacted refs
- `RequiresVisualFallback`, `RequiresHumanHandoff`
- `SensitiveSurface`, `Stale`, `Ambiguous`
- `ActionAuthority`: always false

## Fusion Rules

### Strong candidate

- UIA identity is present (automation id, runtime id, control type, name).
- Win32 active window matches expected fixture.
- No sensitive/risky action language.
- Confidence >= 0.7.
- Still no action authority.

### Medium candidate

- UIA exists but weak.
- OCR/visual hint supports the target.
- UIA events provide context but no mutation.
- Human review recommended.

### Low candidate / blockage

- Conflicting UIA/OCR text.
- Missing role or window/process context.
- Sensitive field detected.
- submit/pay/delete/overwrite/login/password/fiscal keywords detected.
- Event claims execution or mutation.
- Any source reports action authority, live provider call, raw screenshot, real PC read, live subscription, or event-triggered execution.

### Mandatory human review

- Low confidence (< 0.6).
- Ambiguity.
- Conflict.
- Sensitive/regulated text.
- Password/credential/secret/token/card/email/JWT.
- Payment/fiscal/login/delete/submit action language.
- Unexpected event sequence.
- Missing redaction.
- Mismatch between Win32 window context and UIA/OCR source.

## Evidence Redaction Hardening

`ComputerUseEvidenceRedactor` redacts:

- passwords and credential-like key/value pairs
- emails
- JWTs
- credit/debit cards
- SSN-like values
- API keys and tokens
- Windows user profile paths
- phone numbers
- fiscal/bank-like identifiers (`account`, `routing`, `iban`, `swift`, `tax id`, `vat`, etc.)

Evidence packs never store raw screenshots, raw documents, raw sensitive OCR text, raw credentials, raw clipboard, or raw local profile paths.

## Unified Evidence Pack

`ComputerUseUnifiedEvidencePackBuilder` aggregates:

- UIA snapshot ref
- locator fusion result
- ambiguity/stale risk summaries
- Win32 anchor ref
- visual hint refs
- UIA event continuity refs
- dry-run plan ref (if any)
- perception fusion ref (if any)
- SHA-256 tamper guard hash
- `AuditLogBypassGuard: true`
- `ActionAuthorityGranted: false`
- `RawScreenshotPresent: false`
- `ClipboardPresent: false`

## No-Live Boundary

Locator Fusion does not:

- call live UIA APIs
- call live Win32 APIs
- capture screenshots
- read clipboard
- subscribe to live UIA events
- register action callbacks
- claim action authority
- authorize clicks, typing, submits, logins, payments, deletes, overwrites, or fiscal submissions

## Future Gates

Before any live locator/action system:

- prove read-only adapter isolation
- prove no action authority leakage
- prove redaction invariants with property-based tests
- complete controlled action planning hardening
