# Recipe Human Blocking Scenarios Catalog

Phase: 4/9 - Human Intervention + Approval Narrative 2.0.

`RecipeHumanBlockingScenarioCatalog` defines the default handling of critical human-review scenarios.

## Scenarios

- `LoginRequired`
- `CredentialRequired`
- `TwoFactorRequired`
- `CaptchaOrChallengeDetected`
- `PaymentConfirmationRequired`
- `FiscalOrLegalSubmissionReview`
- `EmailOrMessageSendReview`
- `PublicPostingReview`
- `MarketplaceListingChangeReview`
- `PriceOrStockChangeReview`
- `DataDeletionReview`
- `DataMutationReview`
- `FileWriteReview`
- `ExternalSystemMutationReview`
- `PersonalDataReview`
- `SecretHandlingReview`
- `LocatorAmbiguity`
- `PerceptionAmbiguity`
- `ValidationFailureReview`
- `PolicyBlockedReview`
- `UnknownUnsafeState`

Each scenario defines default status, decision options, allowed run modes, evidence requirements, timeline event kind, safe next action, blocked actions, and whether approval can resolve the scenario in this phase.

## Phase 4 Resolution Boundary

Approval can resolve only preview, dry-run, fixture, or manual-continuation decisions. It cannot enable live browser runtime, live desktop runtime, external mutation execution, payment/fiscal/message/delete/publication execution, or automated challenge handling.
