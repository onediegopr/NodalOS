# Recipe Lab Contract

Phase: 7/9 - Recipe Lab + Locator Repair Studio.

Recipe Lab is a read-only, preview-safe and fixture-safe inspection surface for Recipe Runtime. It makes recipes product-visible without enabling execution.

## Snapshot Scope

`RecipeLabSnapshot` summarizes:

- recipe id/version/display metadata,
- canonical readiness from `RecipePolicyPreflightEvaluator`,
- limits, complete criteria and terminate criteria,
- validation and risk,
- deterministic action resolution,
- evidence and timeline refs,
- human intervention and approval narrative refs,
- tool trust refs and secret aliases/refs,
- trigger observe-only state,
- workitem/queue refs,
- locator repair summary,
- safe next action and remaining blocks.

## Safety

- Recipe Lab cannot start a recipe run.
- Recipe Lab cannot process workitems.
- Recipe Lab cannot apply locator repair.
- Recipe Lab cannot unlock live runtime.
- Recipe Lab shows secret aliases/refs only, never raw values.
- Live runtime remains blocked and visible.

OpenRPA/OpenCore patterns are inspiration only. No dependency, code copy or XAML import is used.
