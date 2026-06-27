# Recipe Evidence Pack Contract

Phase: 3/9 - Evidence Pack + Timeline Projection.

Phase 1 commit: `2079a04efe66e6187f7fe018c772ec3f6b51f9d8`.

Phase 2 commit: `29573f36c8ce1e9fef83d2627aaeb34d592b8b2c`.

## Scope

`RecipeEvidencePack` is a fixture-safe, reference-only evidence container for Recipe Runtime runs.

It records:

- evidence pack id,
- recipe id and version,
- run id,
- mission id reference,
- workitem refs,
- step evidence refs,
- validation evidence refs,
- approval refs,
- timeline event refs,
- artifact refs,
- redaction report ref,
- sensitivity summary,
- completeness status,
- capture mode,
- source runtime mode,
- failure summary.

## Capture Modes

Supported capture modes are:

- `FixtureOnly`,
- `ReferenceOnly`,
- `ManualAttachment`,
- `FutureBrowserRuntime`,
- `FutureDesktopRuntime`,
- `FutureConnectorRuntime`.

The future runtime modes are descriptive only in this phase. They do not enable live browser, desktop, connector, CDP, screenshot, DOM, accessibility tree, HAR, network, or filesystem capture.

## Evidence Sources

Evidence source kinds are ref-only:

- screenshot before/after refs,
- visible tree refs,
- accessibility tree refs,
- DOM snapshot refs,
- redacted network summary refs,
- downloaded/uploaded/generated file refs,
- extracted data refs,
- validation result refs,
- approval decision refs,
- policy decision refs,
- workitem state refs,
- timeline event refs,
- error trace refs,
- human note refs,
- handoff summary refs.

No raw payloads are embedded.

## Safety Boundary

OpenRPA/OpenCore remains inspiration only. No dependency, code copy, XAML import, browser extension/native messaging, real browser automation, real desktop automation, scheduler, recorder/replay, file watcher, OS hook, network/API call, real screenshot/HAR/DOM/accessibility capture, or secret exposure was added.
