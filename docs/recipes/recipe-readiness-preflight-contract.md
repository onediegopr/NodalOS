# Recipe Readiness Preflight Contract

Phase: 2/9 - Limits / Validation / Risk / Deterministic Policy.

`RecipePolicyPreflightEvaluator` is a static, fixture-safe contract evaluator. It accepts a `RecipeDefinition` and a requested `RecipeRunMode`, then returns readiness metadata, blocking issues, warnings, and authority flags.

## Outputs

The evaluator reports:

- readiness status,
- blocking issues,
- warnings,
- missing limits,
- missing validation,
- missing complete criteria,
- missing terminate criteria,
- risk gate failures,
- missing approval policy,
- missing evidence expectations,
- live runtime blocked,
- deterministic policy violations.

`LiveRuntimeEnabled` and `ActionAuthorityGranted` are always false in this phase.

## Readiness Statuses

Supported statuses include:

- `ReadyForCatalogPreview`,
- `ReadyForDryRun`,
- `ReadyForFixtureRun`,
- `BlockedMissingLimits`,
- `BlockedMissingValidation`,
- `BlockedMissingCompleteCriteria`,
- `BlockedMissingTerminateCriteria`,
- `BlockedMissingApprovalPolicy`,
- `BlockedMissingEvidencePolicy`,
- `BlockedMissingToolTrust`,
- `BlockedMissingSecretReference`,
- `BlockedRiskGate`,
- `BlockedActionResolutionPolicy`,
- `BlockedLiveRuntimeDisabled`,
- `BlockedByProtectedScope`.

## Evaluation Boundary

Preflight does not:

- start a scheduler,
- call a browser,
- call desktop automation,
- access files by path,
- call a connector,
- read secrets,
- call network APIs,
- create runtime side effects.

Evidence, timeline, approval, mission, tool trust, file scope, and secret references are IDs only.
