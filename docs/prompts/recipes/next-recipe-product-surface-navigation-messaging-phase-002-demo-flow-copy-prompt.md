# NODAL OS - Recipe Product Surface Navigation Messaging - Phase 2/3

Line: `NODAL_RECIPE_PRODUCT_SURFACE_NAVIGATION_MESSAGING_READ_ONLY`

Next phase: 2/3 - Demo Flow Copy

Use this after Phase 1/3 closes with `GO_RECIPE_NAVIGATION_MESSAGING_TAXONOMY_READY`.

## Objective

Add read-only, preview-safe, fixture-safe demo flow copy that explains how an operator safely moves through Recipe Catalog, Recipe Lab, Template Detail, Readiness Explanation, Operator Preview, Handoff/Export Preview, and Safe Demo.

## Boundary

Do not add live recipe execution, live runtime, browser automation, desktop automation, CDP, Playwright, Selenium, Puppeteer, connector/API/network calls, vault or secret reading, scheduler/background worker, watcher/hook/listener, recorder/replay, real capture, automatic workitem processing, fiscal/payment/marketplace/message/delete/write live actions, real export file generation, live locator repair apply, protected browser/live execution changes, or Docker/runner/remote-control/proxy/challenge changes.

## Required Guardrails

- Keep copy read-only / preview-safe / fixture-safe.
- Preserve the allowed final claim.
- Keep the forbidden live automation claim absent from product-facing copy.
- Every disabled action must remain explicitly blocked with a safe next action.
- No navigation or demo flow label may imply live execution or live automation.

## Allowed Claim

NODAL OS has a fixture-safe Recipe Runtime product surface with read-only catalog, lab, templates, readiness explanations, operator previews and handoff/export preview summaries.

## Forbidden Claim

NODAL OS can execute/live automate these recipes.
