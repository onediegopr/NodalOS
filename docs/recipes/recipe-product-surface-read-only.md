# Recipe Product Surface Read-only Boundary

Product-surface phase: 1/4.

Phase name: Recipe Catalog + Lab Read-only Product Surface.

Built on completed Recipe Runtime line: `NODAL_OS_RECIPE_RUNTIME_OPENRPA_INSPIRED_CRITICAL_HIGH_ONLY`.

Final Recipe Runtime hardening commit: `409cd6da0ff902287d85b3dbc6a6b6262cd54162`.

## Boundary

The Recipe product surface exposes completed Recipe Runtime contracts as product-visible, preview-safe information.

It is read-only, fixture-safe, and reference-only. It does not add a Recipe Runtime executor, connector, vault, browser adapter, desktop adapter, scheduler, listener, recorder, capture service, or automatic workitem processor.

## Safe Surface Claims

- Recipe Catalog is available for preview.
- Global and LATAM template packs are visible as fixture-safe templates.
- Recipe Lab snapshots can be inspected.
- Template readiness, blocked modes, safety notes, and safe next actions are visible.
- Tool trust and secret status are summarized by reference only.
- Trigger status is observe-only.
- Locator Repair and Capture Draft summaries are preview-only.

## Non-Goals

- No live recipe execution.
- No browser or desktop automation.
- No CDP, Playwright, Selenium, or Puppeteer.
- No connector, API, network, or vault access.
- No scheduler, watcher, hook, or listener.
- No recorder, replay, or real capture.
- No automatic recipe or workitem progression.
- No raw secrets or raw payload display.
- No fiscal, payment, marketplace, message, delete, or write-like live action.

## Product Copy Rule

Product-facing Recipe surfaces must use preview-safe language: `Preview`, `Fixture-safe`, `Read-only`, `Template`, `Draft`, `Requires human review`, `Live runtime blocked`, `Connector execution not enabled`, `Secrets by reference only`, `Evidence by reference only`, and `Observe-only trigger`.

Action-oriented wording that implies live automation, credential use, connector connection, recording, playback, or direct browser/desktop control is not allowed on these surfaces.

## Next Phase

Phase 2/4: Template Detail + Readiness Explanation UX.
