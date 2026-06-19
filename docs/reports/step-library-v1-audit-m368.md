# Step Library V1 Audit M368

## Scope

M368 reviewed existing recipe, run report, browser recorder, SafeAction, and sensitive-site action concepts before adding Step Library V1.

## Models Found

- `NodalOsRecipeActionKind` in `NodalOsRecipeManifestContracts.cs`.
- `NexaRunStepReport.ActionKind` as a string action label in `NodalOsRunReportContracts.cs`.
- Browser recorder/replay action kinds for historical read-only replay surfaces.
- `NodalOsSafeAction` and sensitive-site action policy concepts for governed execution decisions.

## Reused

- Recipe action kinds are mapped into step kinds.
- Run report step action strings are mapped from step kinds.
- Failure taxonomy is used in step definitions and validation failures.

## Added

- `NodalOsStepKind`.
- `NodalOsStepRiskLevel`.
- `NodalOsStepCapabilityKind`.
- `NodalOsStepDefinition`.
- `NodalOsStepValidationContext`.
- `NodalOsStepValidationResult`.
- `NodalOsStepLibrary`, validator, and sanitizer.

## Kept Separate

- Step Library V1 is descriptive and governance-only.
- It does not replace SafeAction.
- It does not execute browser actions.
- It does not change BrowserExecutor or CDP behavior.
- It does not bypass Recipe Manifest or global policy.

## Recipe Manifest Relationship

Every current `NodalOsRecipeActionKind` maps to a `NodalOsStepKind`. Recipe policy remains authoritative for recipe validation.

## Run Report Relationship

Step kinds map to stable `NexaRunStepReport.ActionKind` strings for reporting.

## Failure Taxonomy Relationship

Each step definition declares possible `NexaFailureKind` values. Blocked submit/login/captcha/2FA contexts map to failure taxonomy.

## Future Orchestration API Relationship

Future orchestration can reference Step Library metadata, but this milestone intentionally does not implement an orchestration API or execution engine.

## Not Touched

- Runtime action behavior.
- UI or sidepanel.
- Recipe execution.
- Scheduled runs.
- Persistence.
- Worker runtime.

## Duplication Risk

Several execution-facing action models already exist. Step Library V1 avoids semantic conflict by being a metadata catalog only and by mapping to existing recipe and run report contracts rather than replacing them.

## Decision

No blocking conflict was found. Proceed with a dedicated Step Library V1 catalog in `OneBrain.BrowserExecutor.Contracts` and governance helpers in `OneBrain.BrowserExecutor.Cdp`.
