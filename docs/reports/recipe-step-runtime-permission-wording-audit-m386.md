# Recipe / Step Runtime Permission Wording Audit M386

## Scope

M386 audits Recipe Manifest V1 and Step Library V1 contract wording that could be misread as runtime execution permission by a future orchestration API or UI.

This audit is contract-only. It does not introduce recipe execution, step execution, orchestration, UI, scheduling, persistence, or policy runtime behavior.

## Search

Discovery used PowerShell `Select-String` because `rg` was unavailable in the shell. The first broad search included compiled binaries, so the actionable audit was narrowed to source, tests, docs, and artifacts excluding `bin` and `obj`.

Search terms included:

- `CanExecute`
- `Approved`
- `IsAllowedInV1`
- `RuntimeExecution`
- `ExecutionAllowed`
- `CanPassManifestPolicy`
- `RequiresApproval`
- `SensitiveActionsBlocked`
- `NodalOsRecipeManifestValidationResult`
- `NodalOsRecipeStatus`
- `NodalOsStepDefinition`
- `NodalOsStepValidationResult`
- `RecipeManifest`
- `StepLibrary`

## Ambiguous Fields And Statuses

| Surface | Current field/status | Risk |
| --- | --- | --- |
| Recipe validation | `CanExecute` | Could be read as runtime execution permission instead of manifest-policy validation. |
| Recipe status | `Approved` | Could be read as bypassing global policy or human approval. |
| Step definition | `IsAllowedInV1` | Could be read as executable instead of cataloged/governance-available. |
| Step validation | `IsAllowed` | Could be read as runtime-allowed instead of step-policy metadata passing. |
| Click/Type/Download/Upload | Cataloged V1 step kinds | Could look like runtime actions are implemented. |

## Usage Found

- `NodalOsRecipeManifestContracts.cs`
- `NodalOsRecipeManifestServices.cs`
- `NodalOsRecipeManifestM353M355Tests.cs`
- `NodalOsStepLibraryContracts.cs`
- `NodalOsStepLibraryServices.cs`
- `NodalOsStepLibraryV1M368M370Tests.cs`
- `docs/reports/axiom-recipe-manifest-v1-m355.md`
- `docs/reports/step-library-v1-m370.md`
- `artifacts/agent-operations/m355/recipe-manifest-v1-summary.json`
- `artifacts/agent-operations/m370/step-library-v1-summary.json`

## Decision

Keep compatibility aliases but add explicit runtime-safe fields:

- `CanPassManifestPolicy`
- `RuntimeExecutionAllowed`
- `RuntimeExecutionDeferred`
- `RequiresGlobalPolicyEvaluation`
- `IsCatalogAvailableInV1`
- `CanPassStepPolicy`

`CanExecute` remains a compatibility alias for manifest-policy pass, not runtime execution. `IsAllowedInV1` remains a compatibility alias for catalog availability, not runtime execution.

## Changes

- Recipe validation now explicitly returns `RuntimeExecutionAllowed=false`.
- Recipe validation now explicitly returns `RuntimeExecutionDeferred=true`.
- Recipe validation now explicitly returns `RequiresGlobalPolicyEvaluation=true`.
- Approved recipes warn that approval is a governance state and does not grant runtime execution.
- Step definitions expose catalog availability separately from runtime execution.
- Step validation returns `RuntimeExecutionAllowed=false` and `RuntimeExecutionDeferred=true`.

## Compatibility Aliases

- `CanExecute`: compatibility alias for `CanPassManifestPolicy`.
- `IsAllowedInV1`: compatibility alias for `IsCatalogAvailableInV1`.
- `IsAllowed`: compatibility alias for `CanPassStepPolicy`.

## Not Touched

- No recipe execution.
- No step execution.
- No orchestration API.
- No UI.
- No scheduling.
- No browser or desktop actions.
- No namespace move.
- No broad rename.

## Test Impact

Existing tests remain meaningful because aliases are preserved. New tests assert that the aliases cannot be interpreted as runtime permission.
