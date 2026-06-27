# Next Prompt - Recipe Product Surface Phase 2/4

Block: `NODAL_RECIPE_RUNTIME_PRODUCT_SURFACE_002_TEMPLATE_DETAIL_READINESS_EXPLANATION_UX`

Decision target: `GO_RECIPE_PRODUCT_SURFACE_TEMPLATE_DETAIL_READINESS_EXPLANATION_READY`

## Context

Product-surface Phase 1/4 closed with Recipe Catalog and Recipe Lab read-only surfaces.

Base Recipe Runtime line is complete as fixture-safe design/contracts/templates/capture draft.

Final Recipe Runtime hardening commit: `409cd6da0ff902287d85b3dbc6a6b6262cd54162`.

## Objective

Add read-only Template Detail and Readiness Explanation UX contracts/view models.

The surface should explain:

- template overview.
- readiness status.
- policy preflight issues.
- missing limits, validation, evidence, approval, tool trust, and secret refs.
- live-blocked and future-gated modes.
- human-review requirements.
- trigger observe-only status.
- locator repair preview status.
- capture draft mapping status.
- safe next action.
- why the template remains preview-safe.

## Hard No-Go

- no real recipe execution.
- no browser automation.
- no desktop automation.
- no CDP/Playwright/Selenium/Puppeteer.
- no connector/API/network calls.
- no vault or secret reading.
- no scheduler/background worker.
- no watcher/hook/listener.
- no recorder/replay/capture.
- no automatic recipe or workitem processing.
- no fiscal/payment/marketplace/message/delete/write live actions.
- no live runtime.

## Required Boundary

The UX must be read-only, preview-safe, fixture-safe, and reference-only. It must not add action buttons, execution paths, credential prompts, connector activation, locator repair apply, capture start, playback, or live runtime unlock.
