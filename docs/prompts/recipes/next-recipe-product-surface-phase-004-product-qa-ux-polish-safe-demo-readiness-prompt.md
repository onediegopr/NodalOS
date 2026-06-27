# NODAL OS - Recipe Runtime Product Surface - Phase 4/4 Prompt

Block: `NODAL_RECIPE_RUNTIME_PRODUCT_SURFACE_004_PRODUCT_QA_UX_POLISH_SAFE_DEMO_READINESS`

Decision target: `GO_RECIPE_PRODUCT_SURFACE_QA_UX_POLISH_SAFE_DEMO_READY`

You are working on NODAL OS in:

`C:\DESARROLLO\NodalOS\Codigo-m12-audit`

Branch:

`chrome-lab-001-extension-local-ai-bridge`

Previous product-surface phases:

- Phase 1/4 - Recipe Catalog + Lab Read-only Product Surface - commit `2b93eb4392f7817d9e13550a9aff83df246f5cb9`
- Phase 2/4 - Template Detail + Readiness Explanation UX - commit `a8993e132999b7e004ee67bcc9393c158cb79812`
- Phase 3/4 - Operator Preview Flow + Handoff Export Surface - commit TBD

Underlying Recipe Runtime final hardening commit:

`409cd6da0ff902287d85b3dbc6a6b6262cd54162`

## Objective

Finalize the read-only Recipe Runtime Product Surface line with QA, UX polish, safe demo readiness, final docs, and final safety checks.

## Absolute Boundary

This phase remains read-only, preview-safe, fixture-safe, and reference-only.

Do not add real recipe execution, browser automation, desktop automation, CDP, Playwright, Selenium, Puppeteer, connector/API/network calls, vault access, secret reading, scheduler/background worker, watcher/hook/listener, recorder/playback, real capture, live locator repair activation, automatic recipe run, automatic workitem processing, fiscal/payment/marketplace/message/delete/write live actions, or protected browser/live execution changes.

## Required Work

- Review Product Surface phases 1-3 for consistency.
- Harden product copy so it cannot overclaim live automation.
- Confirm catalog, template detail, recipe lab, operator preview, and handoff export previews remain read-only.
- Confirm export is preview-only and writes no file.
- Confirm disabled action states are clear.
- Add final safe demo readiness docs/report.
- Run full Recipe Product Surface and Recipe Runtime relevant tests.

## Required Final Claim

Correct product claim:

`NODAL OS has a fixture-safe Recipe Runtime product surface with read-only catalog, lab, templates, readiness explanations, operator previews, and handoff/export preview summaries.`

Forbidden product claim:

`NODAL OS can execute/live automate these recipes.`
