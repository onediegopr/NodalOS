# Recipe Lab Read-Only UI Mount

Decision target: `GO_RECIPE_LAB_READ_ONLY_UI_MOUNT_READY`

## Scope

Recipe Lab is mounted as a visible Mission Control product surface at `#recipeLabSurface`.

The mount uses `RecipeLabReadOnlyUiMount.CreateFixture()` as the Core adapter over existing read-only Recipe Product Surface contracts:

- `RecipeProductSurfaceFactory.CreateCatalogSurface`
- `RecipeProductSurfaceFactory.CreateLabSurface`
- `RecipeProductSurfaceFactory.CreateTemplateDetailSurface`
- `RecipeProductSurfaceFactory.CreateOperatorPreviewHandoffExportSurface`
- `ReliableRecipeLabAuditSurfacePresenter.CreateDefault`

## Visible Surface

The UI shows:

- recipe catalog summary;
- selected recipe detail preview;
- readiness matrix;
- blocked reasons;
- required human actions;
- operator preview;
- handoff/export preview;
- read-only, fixture-safe, no-runtime and no-live notices.

## Safety Boundary

Recipe Lab UI mount is read-only, fixture-safe and no-runtime.
It does not execute recipes.
It does not enable browser/CDP automation.
It does not enable WCU/OCR live automation.
It does not call providers or cloud.
It does not write files.
It does not persist durable recipe state.

The only visible actions are metadata-only copy actions:

- `Copy preview`
- `Copy handoff preview`

These use local text assembled from deterministic fixture data and do not call the bridge, provider, browser runtime, filesystem or network.

## Remaining Debt

- Recipe persistence remains out of scope.
- Recipe runtime/live execution remains blocked.
- Browser/CDP, WCU, OCR and recorder live flows remain blocked.
- Provider/cloud connector execution remains blocked.
- Manual browser QA and navigation polish remain recommended follow-up work.
