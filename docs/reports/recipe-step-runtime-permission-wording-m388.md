# Recipe / Step Runtime Permission Wording M388

## Problem

Recipe Manifest V1 and Step Library V1 intentionally model governance metadata, not execution. However, names such as `CanExecute`, `Approved`, and `IsAllowedInV1` could be interpreted by future consumers as runtime permission.

M386-M388 makes the contract semantics explicit without breaking compatibility.

## Recipe Manifest Fields

`NodalOsRecipeManifestValidationResult` now includes:

- `CanPassManifestPolicy`: local manifest-policy validation passed.
- `RuntimeExecutionAllowed`: always `false` in this phase.
- `RuntimeExecutionDeferred`: always `true` in this phase.
- `RequiresGlobalPolicyEvaluation`: always `true`.

`CanExecute` remains as a compatibility alias for `CanPassManifestPolicy`. It does not grant runtime permission.

## Approved Status

`NodalOsRecipeStatus.Approved` means the manifest can pass governance review when local policy validation succeeds. It does not bypass global policy, human approval, or future runtime gates.

## Step Library Fields

`NodalOsStepDefinition` now includes:

- `IsCatalogAvailableInV1`: descriptive/governance catalog availability.
- `RuntimeExecutionDeferred`: always `true`.
- `RequiresGlobalPolicyEvaluation`: always `true`.

`IsAllowedInV1` remains as a compatibility alias for catalog availability. It does not grant runtime permission.

`NodalOsStepValidationResult` now includes:

- `CanPassStepPolicy`: descriptive step-policy metadata passed.
- `RuntimeExecutionAllowed`: always `false`.
- `RuntimeExecutionDeferred`: always `true`.
- `RequiresGlobalPolicyEvaluation`: always `true`.

`IsAllowed` remains compatible with `CanPassStepPolicy`.

## Runtime Execution Remains Deferred

Click, Type, DownloadRequest, and UploadRequest can be cataloged and validated as governance metadata, but none are runtime-executable through these contracts.

Submit-like, login-related, captcha, and two-factor automation remain blocked.

## Compatibility

Existing tests that assert `CanExecute`, `IsAllowedInV1`, or `IsAllowed` continue to pass. New tests assert that these compatibility aliases do not imply runtime permission.

## What This Does Not Implement

- No recipe execution.
- No step execution.
- No orchestration API.
- No UI.
- No scheduled runs.
- No browser actions.
- No desktop actions.
- No persistence DB.

## Next Step

Recommended next milestone:

`M389-M391 Agent Operations Namespace/Naming ADR or Package/Skill Manifest V1`
