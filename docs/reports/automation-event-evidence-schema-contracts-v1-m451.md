# NODAL OS - M451 Automation Event/Evidence Schema Contracts V1

## Summary

M449-M451 creates contract-only automation event and evidence schemas for future NODAL OS automation. The schemas are designed for Mission Control and Timeline compatibility, evidence-first auditability, and common redaction before any automation runtime exists.

## Created

- `NodalOsAutomationEventEvidenceContracts.cs`
- `NodalOsAutomationEventEvidenceServices.cs`
- `NodalOsAutomationEventEvidenceContractsV1M449M451Tests.cs`
- M449 audit report
- M451 implementation report
- M451 artifact
- roadmap entry

## Automation Event Kinds

- `AutomationStepStarted`
- `AutomationStepCompleted`
- `AutomationStepFailed`
- `AutomationHandoffRequired`
- `SelectorChanged`
- `FallbackUsed`
- `EvidenceCaptured`
- `AutomationWarningRaised`

## Automation Evidence Kinds

- `SelectorEvidence`
- `DomSnapshotRedacted`
- `ScreenshotReferenceFuture`
- `StepLog`
- `NetworkMetadataRedacted`
- `HumanNote`
- `FallbackEvidence`
- `HandoffEvidence`

## Handoff Reasons

- `LoginRequired`
- `CaptchaRequired`
- `TwoFactorRequired`
- `CredentialRequired`
- `AmbiguousState`
- `PolicyBlocked`
- `SelectorUnstable`
- `ExternalServiceBlocked`
- `UserDecisionRequired`

## Runtime Semantics

Automation event/evidence contracts cannot grant runtime authority:

- `RuntimeExecutionAllowed=false`
- `RuntimeExecutionDeferred=true`
- global policy evaluation remains required
- evidence redaction remains required
- evidence refs remain no-authority

## Redaction

The validator uses common NODAL OS redaction. Human summaries, technical summaries, selector paths, DOM snippets, step logs, network metadata, and human notes are rejected when they contain raw sensitive content.

## Evidence Bridge

Automation events, evidence, and handoff states validate `NodalOsEvidenceBridgeRef` instances through the existing evidence bridge. Invalid authority, redaction-required evidence, rejected-sensitive evidence, or sensitive refs fail validation.

## Timeline And Mission Control Compatibility

Contracts include Mission/Task/Recipe/Step identifiers so future Mission Control and Timeline views can correlate events. Generic context remains possible but produces warnings and does not create authority.

## DOM / Screenshot / Network Restrictions

- DOM snapshots must be redacted.
- Screenshot evidence is reference-only and future/policy-bound.
- Inline screenshot binary content is rejected.
- Network metadata cannot include Authorization, Cookie, Set-Cookie, or body content.
- Selector evidence cannot contain secret-like values.

## Still Deferred

No recorder, replay, queue, scheduler, trigger policy runtime, browser automation, browser action, desktop action, workflow designer, DSL parser, API, UI, worker runtime, persistence, or external RPA dependency was implemented.

## Next Recommended Milestone

M452-M454 Selector Safety Policy and Human Handoff Contract V1.
