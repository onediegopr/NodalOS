# NODAL OS - M457 Recipe Risk Classifier + DSL Decision

## Summary

M455-M457 creates Recipe Risk Classifier V1 and a Recipe DSL Decision Record. The classifier assigns risk categories, risk levels, approval requirements, handoff requirements, and no-authority runtime flags to recipe step inputs and recipe profiles.

## Created

- `NodalOsRecipeRiskClassifierContracts.cs`
- `NodalOsRecipeRiskClassifierServices.cs`
- `recipe-dsl-decision-record.md`
- `NodalOsRecipeRiskClassifierDslDecisionM455M457Tests.cs`
- M455 audit report
- M457 report
- M457 artifact
- roadmap entry

## Risk Categories

- `ReadOnlyObservation`
- `Extraction`
- `FormFill`
- `Submit`
- `PurchaseOrPayment`
- `DeleteOrDestructive`
- `ExternalPublishOrSend`
- `CredentialOrLogin`
- `CaptchaOrTwoFactor`
- `FileSystemMutation`
- `DataExport`
- `NetworkOrExternalService`
- `BrowserAutomationFuture`
- `HumanDecisionRequired`
- `Unsupported`

## Risk Levels

- `Low`
- `Medium`
- `High`
- `Critical`

## Approval Requirements

Read-only observation can avoid approval at the classifier level, but still cannot execute. Form-fill, submit, destructive actions, publish/send, payment, credential use, file mutation, unsupported, and high-risk categories require approval or handoff.

## Handoff Requirements

Credential/login, captcha/two-factor, and human-decision categories require human handoff. This aligns with Selector Safety + Human Handoff V1.

## DSL Decision

The Recipe DSL ADR states:

- DSL is representation, not runtime.
- Parser is deferred.
- Runtime is deferred.
- Direct execution is forbidden.
- Import requires validation.
- JSON canonical model is required.
- TagUI is inspiration only; no dependency is introduced.

## Relationship With Existing Contracts

Recipe Risk Classifier complements Recipe Manifest and Step Library. It does not replace them, execute them, or authorize them. It also aligns with Scheduled Read-Only, Automation Event/Evidence, Selector Safety/Handoff, Orchestration, EvidenceRefBridge, and common redaction.

## Deferred

No DSL parser, DSL executor, recorder, replay, browser automation, workflow designer, queue, trigger, scheduler, API, UI, worker runtime, recipe/skill/step execution, persistence, cloud runtime, package install/uninstall, external RPA dependency, or direct import implementation was added.

## Next Recommended Milestone

M458-M459 Claude Automation Layer Pre-Implementation Audit.
