# NODAL OS Recipe Limits / Validation / Risk Policy Handoff

## State

Decision target: `GO_RECIPE_LIMITS_VALIDATION_RISK_POLICY_READY`.

Phase status:

- Total phases: 9.
- Current phase: 2/9.
- Current phase name: Limits / Validation / Risk / Deterministic Policy.
- Phase 2 start progress: 0%.
- Phase 2 end progress estimate: 95%.
- Overall Recipe Runtime line completion estimate: 22%.

## Added

- Optional Phase 2 policy contracts on `RecipeDefinition`.
- `RecipeRunLimits` for max steps, retries, loop bounds, workitem bounds, artifact bounds, action categories, and blocked live eligibility.
- `RecipeCompleteCriteria` and `RecipeTerminateCriteria`.
- `RecipeValidationPolicy` and validation requirements by block type.
- `RecipeRiskProfile`, sensitive action categories, and policy gate requirements.
- `ActionResolutionPolicy` with deterministic-first strategy ordering.
- `RecipePolicyPreflightEvaluator` for static readiness checks.
- Fixture-safe tests under `RecipeLimitsValidationRiskPolicy`.

## Boundaries

No OpenRPA dependency, no code copy, no XAML, no extension/native messaging, no real browser automation, no desktop/computer-use automation, no CDP/Playwright/Selenium/Puppeteer, no scheduler, no recorder/replay, no file watcher, no OS hook, no network/API call, no secret vault access, and no live runtime.

Evidence, timeline, approval, mission, secret, file scope, and tool trust concepts remain references only.

## Next Phase

Phase 3/9: Evidence Pack + Timeline Projection.
