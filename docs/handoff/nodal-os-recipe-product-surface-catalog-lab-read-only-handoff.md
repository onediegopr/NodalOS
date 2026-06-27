# NODAL OS Recipe Product Surface Catalog + Lab Read-only Handoff

Block: `NODAL_RECIPE_RUNTIME_PRODUCT_SURFACE_001_CATALOG_LAB_READ_ONLY`

Decision: `GO_RECIPE_PRODUCT_SURFACE_CATALOG_LAB_READ_ONLY_READY`

Product-surface phase: 1/4.

Phase name: Recipe Catalog + Lab Read-only Product Surface.

Base Recipe Runtime hardening commit: `409cd6da0ff902287d85b3dbc6a6b6262cd54162`.

## Delivered

- Recipe Catalog read-only product surface contracts/view models.
- Recipe Lab read-only summary surface contracts/view models.
- Safety badges and readiness badges for product cards.
- Product copy policy for forbidden live/action-oriented wording in new Recipe surfaces.
- Fixture-safe tests for catalog cards, lab summaries, copy policy, and no runtime capability exposure.
- Docs, QA report, and next prompt for Phase 2/4.

## Preserved Boundary

- Real recipe execution: NO.
- Browser automation: NO.
- Desktop automation: NO.
- CDP/Playwright/Selenium/Puppeteer: NO.
- Connector/API/network: NO.
- Vault/secrets access: NO.
- Scheduler/watcher/hook/listener: NO.
- Recorder/replay/capture: NO.
- Automatic workitem processing: NO.
- Live runtime enabled: NO.

## Product Claim Guidance

Safe wording: preview, fixture-safe, read-only, template, draft, requires human review, live runtime blocked, connector execution not enabled, browser automation not enabled, desktop automation not enabled, secrets by reference only, evidence by reference only, observe-only trigger.

Do not use wording that implies direct action, live automation, connector connection, credential use, recording, playback, or direct browser/desktop control.

## Next Phase

Phase 2/4: Template Detail + Readiness Explanation UX.

Phase 2 must remain read-only and preview-safe. It should explain readiness issues, blocked modes, missing refs, human-review requirements, and no-live boundaries without adding execution.
