# Recipe Runtime Foundation

## Phase Status

- Total phases: 9.
- Current phase: 1/9.
- Current phase name: Recipe Runtime Foundation + Workitems.
- Phase 1 start progress: 0%.
- Phase 1 end progress estimate: 95%.
- Global line progress estimate after this block: 11%.

## Scope

This foundation adds contracts for recipe definitions, recipe blocks, recipe runs, run steps, workitem-style queue records, retry decisions, readiness gates, and dry-run/fixture-safe states.

It extends the existing `OneBrain.Core.Recipes.RecipeDefinition` with optional metadata instead of replacing the existing recipe manifest model.

## Contract Boundary

- OpenRPA/OpenCore inspiration: YES.
- OpenRPA dependency: NO.
- OpenRPA code copy: NO.
- XAML import: NO.
- Browser extension/native messaging: NO.
- Real browser automation: NO.
- Real desktop/computer-use automation: NO.
- Scheduler/background worker: NO.
- Recorder/replay/live runner: NO.
- CAPTCHA/2FA bypass: NO.
- Credential scraping: NO.
- Uncontrolled scripting: NO.

## Integration References

The runtime foundation references existing NODAL OS concepts by id only:

- mission ids,
- evidence pack refs,
- timeline event refs,
- approval decision refs,
- redaction/evidence expectation refs,
- tool trust refs,
- secret refs by id only.

No raw evidence payload, raw secret value, screenshot bytes, clipboard content, browser session, desktop handle, scheduler registration, or provider call is stored by these contracts.

## Runtime Modes

- `CatalogPreview`
- `DryRun`
- `FixtureRun`
- `AssistedRun`
- `LiveRunBlocked`
- `LiveRunAllowedFuture`

For this block, live runtime remains unavailable. `LiveRunAllowedFuture` is a reserved future state and `RecipeReadinessEvaluator` still returns `BlockedLiveRuntimeDisabled`.

## Next Phase

Phase 2/9: Limits / Validation / Risk / Deterministic Policy.
