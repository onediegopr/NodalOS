# NODAL OS Recipe Runtime Foundation + Workitems Handoff

## State

Decision target: `GO_RECIPE_RUNTIME_FOUNDATION_WORKITEMS_READY`.

Phase status:

- Total phases: 9.
- Current phase: 1/9.
- Current phase name: Recipe Runtime Foundation + Workitems.
- Phase 1 start progress: 0%.
- Phase 1 end progress estimate: 95%.
- Global line progress estimate after this block: 11%.

## Added

- Optional recipe metadata on existing `RecipeDefinition`.
- `RecipeBlock`, lifecycle, run mode, readiness, run, and run-step contracts.
- Workitem queue contracts with payload refs, attachment refs, retry policies, failure types, and retry decisions.
- Conservative readiness evaluator.
- Fixture-safe tests under `RecipeRuntimeFoundation`.

## Boundaries

No OpenRPA dependency, no code copy, no XAML, no extension/native messaging, no real browser automation, no desktop/computer-use automation, no scheduler, no recorder/replay, no OS hooks, no network provider calls, and no live runtime.

Mission, evidence, timeline, approval, redaction, tool trust, and secret concepts are references only.

## Next Phase

Phase 2/9: Limits / Validation / Risk / Deterministic Policy.
