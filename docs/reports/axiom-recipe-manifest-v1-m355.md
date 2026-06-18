# Axiom Recipe Manifest / Automation JSON V1 - M355

## Scope

M353-M355 creates the first NODAL OS Recipe Manifest / Automation JSON V1 layer. This is a portable definition and validation surface only. It does not execute recipes, introduce a scheduler, create a Step Library, add orchestration APIs, or modify browser/CDP runtime behavior.

## Axiom Benchmark Takeaways

NODAL OS adopts the useful definition/reporting ideas from Axiom-style browser automation: portable automation JSON, explicit step definitions, run-readiness validation, success criteria, failure signals, and evidence requirements.

NODAL OS rejects any model where a recipe can bypass global policy, approval, evidence, or runtime safety. Policy remains global and dominant over recipe-local policy.

## Manifest Model

The manifest model is implemented as:

- `NodalOsRecipeManifest`
- `NodalOsRecipeStepManifest`
- `NodalOsRecipePolicyManifest`
- `NodalOsRecipeManifestValidationResult`

Recipe statuses are:

- `Draft`: valid design artifact, cannot execute.
- `Shadow`: observe/simulate only, cannot execute real actions.
- `Supervised`: execution-capable only with explicit approval requirement.
- `Approved`: execution-capable only when all policy checks pass.
- `Deprecated`: cannot execute in V1.
- `Blocked`: cannot execute.

Action kinds are:

- `Navigate`
- `Read`
- `Click`
- `Type`
- `Extract`
- `Wait`
- `AskHuman`
- `Stop`
- `DownloadRequest`
- `UploadRequest`

## Policy Validation

The validator enforces:

- required manifest and step fields
- ordered and unique step indexes
- `DisallowedActionKinds` overriding `AllowedActionKinds`
- `AllowedDomains` validation for URL templates
- `MaxRuntimeSteps`
- approval requirements for upload/download
- sensitive typing blocked or approval-required according to policy
- secret-like marker rejection for manifest strings
- Draft/Shadow/Blocked/Deprecated non-execution states

## Relationships

Mission/Task can reference a recipe by ID. Run Report V1 can preserve `RecipeId`, policy decisions, failures, and evidence refs produced by a future runner. Failure Taxonomy remains the typed failure language for blocked or unsafe recipe validation and future execution reports.

## Non-Goals

This milestone intentionally does not implement:

- recipe execution
- browser actions
- orchestration API
- scheduled runs
- Step Library
- sidepanel UI
- approval UI
- persistence DB
- package registry
- multi-worker runtime
- cloud runtime
- marketplace
- captcha solving
- bot bypassing

## Readiness

Decision: `M353+M354+M355 CERRADO / RECIPE_MANIFEST_V1_READY_WITH_EXECUTION_DEFERRED`.

Next recommended milestone: `M356-M358 Blocker + Progress Reporting Contract`.
