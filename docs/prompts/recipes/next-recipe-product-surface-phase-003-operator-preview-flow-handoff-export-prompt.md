# Next Prompt - Recipe Product Surface Phase 3/4

Block: `NODAL_RECIPE_RUNTIME_PRODUCT_SURFACE_003_OPERATOR_PREVIEW_FLOW_HANDOFF_EXPORT`

Decision target: `GO_RECIPE_PRODUCT_SURFACE_OPERATOR_PREVIEW_HANDOFF_EXPORT_READY`

## Context

Product-surface Phase 1/4 delivered Recipe Catalog and Recipe Lab read-only surfaces.

Product-surface Phase 2/4 delivered Template Detail and Readiness Explanation UX.

Underlying Recipe Runtime line remains fixture-safe design/contracts/templates/capture draft only.

## Objective

Add an Operator Preview Flow + Handoff Export Surface.

The surface should organize:

- selected template summary.
- readiness explanation.
- blocked run modes.
- missing requirements.
- human-review requirements.
- tool trust and secret refs by alias/id only.
- evidence and validation refs.
- trigger observe-only state.
- locator/capture preview summaries.
- safe next action.
- redacted handoff/export summary.

## Hard No-Go

- no real recipe execution.
- no browser automation.
- no desktop automation.
- no CDP/Playwright/Selenium/Puppeteer.
- no connector/API/network calls.
- no vault or secret reading.
- no scheduler/background worker.
- no watcher/hook/listener.
- no recorder/playback/capture.
- no automatic recipe or workitem processing.
- no fiscal/payment/marketplace/message/delete/write live actions.
- no live runtime.

## Required Boundary

The Phase 3 surface must be read-only, preview-safe, fixture-safe, redacted, and reference-only. It must not add action buttons, execution paths, credential prompts, connector activation, locator repair apply, capture start, playback, or live runtime unlock.
