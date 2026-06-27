# Recipe Template Composite Readiness

Phase: 8/9 - Global + LATAM Recipe Templates Pack v1.

`RecipeTemplateReadinessEvaluator` is the Phase 8 composite readiness path. It is contract-only and fixture-safe.

## Composed Checks

The evaluator composes:

- canonical Phase 2+ `RecipePolicyPreflightEvaluator` for limits, validation, complete criteria, terminate criteria, risk gates and deterministic action resolution.
- tool trust registry readiness.
- secret reference readiness and raw secret blocking.
- credentialed action gate and connector eligibility.
- trigger observe-only readiness.
- evidence completeness and failed blocking validation evidence.
- human intervention and approval narrative readiness.
- Recipe Lab / locator safety flags.
- live runtime blocked status.

## Blocking Rules

Readiness blocks when:

- limits, validation, evidence, complete criteria or terminate criteria are missing.
- required tool trust refs or secret refs are missing.
- a tool is untrusted, disabled, deprecated, live-blocked or future-gated.
- a raw secret is detected.
- a connector would execute or mutate live systems.
- a trigger would autorun, process workitems, create watchers, listeners, hooks or schedulers.
- blocking validation evidence failed.
- sensitive templates lack a human/approval path.
- browser or desktop runtime would be required.

## Output

The output includes template status, blocking issues, warnings, safe next action, blocked run modes and operator summary. It always reports:

- `LiveRuntimeEnabled=false`
- `ActionAuthorityGranted=false`
- `ConnectorExecutionEnabled=false`
- `StartsRecipeRun=false`
- `ProcessesWorkitems=false`

## Safety Boundary

Composite readiness does not execute recipes. It does not call connectors, APIs, browsers, desktops, vaults, filesystems, schedulers or triggers.
