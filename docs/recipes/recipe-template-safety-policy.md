# Recipe Template Safety Policy

Phase: 8/9 - Global + LATAM Recipe Templates Pack v1.

Template packs are safe catalog artifacts, not runtime adapters.

## Required Claims

- Templates are preview/fixture/reference-only.
- Composite readiness is required before any fixture-ready claim.
- Fiscal, payment, marketplace, message, delete, write, browser and computer-use templates are human/approval gated and live-blocked where applicable.
- Secrets are refs only.
- Evidence is refs only.
- Triggers are observe-only.
- Recipe Lab is read-only.
- Locator repair is preview-only.

## Forbidden Claims

Templates must not claim:

- live browser automation.
- live desktop automation.
- connector execution.
- API/network/webhook listener availability.
- vault access.
- real screenshot, DOM, accessibility tree or HAR capture.
- scheduler, watcher, hook, listener or trigger autorun.
- recorder/replay.
- live locator replay or live locator repair apply.
- product automation availability.
- approval, tool trust, trigger or Recipe Lab unlocking live runtime.

## Phase 9 Boundary

The next phase is Phase 9/9 - Recipe Capture Draft. Phase 9 must remain draft/fixture-safe and must not introduce recorder/replay, live capture, browser automation, desktop automation or real connectors.
