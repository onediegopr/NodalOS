# NODAL OS - M454 Selector Safety Policy + Human Handoff Contract V1

## Summary

M452-M454 creates contract-only selector safety and human handoff schemas for future NODAL OS automation. The milestone formalizes safe selector hierarchy, selector risk decisions, and human handoff semantics without implementing runtime automation.

## Created

- `NodalOsSelectorSafetyHumanHandoffContracts.cs`
- `NodalOsSelectorSafetyHumanHandoffServices.cs`
- `NodalOsSelectorSafetyHumanHandoffContractsV1M452M454Tests.cs`
- M452 discovery report
- M454 report
- M454 artifact
- roadmap entry

## Selector Strategy Hierarchy

Preferred strategy order is semantic/DOM/CDP first:

- `Semantic`
- `DomStableAttribute`
- `DomCssPath`
- `DomXPath`
- `CdpAccessibilityTreeFuture`
- `VisualCheckpointFuture`
- `OcrTextFallbackFuture`

Visual/OCR cannot be first and remains fallback/evidence-only.

## Selector Risk And Decision

Selector evaluations include:

- `AllowedForObservationOnly`
- `RequiresHumanReview`
- `RejectedSensitive`
- `RejectedUnstable`
- `RejectedMutableIntent`
- `RejectedUnsupported`

Evaluations include risk level, reasons, warnings, evidence refs, runtime-deferred flags, and `CanAuthorizeAction=false`.

## Raw Sensitive Material

Selector candidates reject raw secrets, cookies, headers, private bodies, and redaction-sensitive selector paths/labels. Reasons and warnings are sanitized through common redaction.

## Mutable Intent And Stability

Mutable intent forces rejection. Low stability selectors are rejected or require human review. Visual/OCR fallback strategies require human review and cannot become authority for actions.

## Human Handoff Contract

Human handoff V1 formalizes blocked states for:

- login;
- captcha;
- two-factor authentication;
- credential requirement;
- ambiguity;
- policy block;
- selector instability;
- external service block;
- user decision required.

The contract requires a specific redacted blocker, explicit user options, evidence refs, runtime-deferred flags, and `CanAuthorizeAction=false`.

## User Options

Supported options include:

- `ContinueAfterUserAction`
- `PauseMission`
- `ChangeInstruction`
- `RetryAfterFix`
- `CopyTechnicalLog`
- `CancelMission`
- `AskForExplanation`

The validator requires at least two useful options and warns when recommended options are missing.

## Generic Blocking

Generic `blocked` text is rejected. The blocker must explain the exact reason and user path forward.

## Timeline / Mission Control Compatibility

Selector candidates, evaluations, and handoff contracts include Mission/Task/Recipe/Step correlation fields for future Mission Control and Timeline display.

## Deferred

No UI, recorder, replay, queue, trigger, scheduler, browser automation, browser action, desktop action, recipe/skill/step execution, worker runtime, persistence, cloud runtime, or external RPA dependency was implemented.

## Next Recommended Milestone

M455-M457 Recipe Risk Classifier and DSL Decision Record.
