# Claude Final Audit Prompt - Recipe Product Surface Read-only Safe Demo

Audit block: `NODAL_RECIPE_PRODUCT_SURFACE_FINAL_READ_ONLY_SAFE_DEMO_AUDIT`

Target decision: `AUDIT_GO_RECIPE_PRODUCT_SURFACE_READ_ONLY_SAFE_DEMO_COMPLETE` or `AUDIT_NO_GO_WITH_FINDINGS`

You are auditing NODAL OS.

This is a final audit of the Recipe Runtime Product Surface line.

IMPORTANT:

- Audit only.
- Do not modify files.
- Do not commit.
- Do not push.
- Return findings only.

## Scope

Audit the read-only Recipe Runtime Product Surface line:

- Phase 1/4 - Recipe Catalog + Lab Read-only Product Surface - `2b93eb4392f7817d9e13550a9aff83df246f5cb9`
- Phase 2/4 - Template Detail + Readiness Explanation UX - `a8993e132999b7e004ee67bcc9393c158cb79812`
- Phase 3/4 - Operator Preview Flow + Handoff Export Surface - `8d042126e44d625c71367e421443445041b13a35`
- Phase 4/4 - Product QA / UX Polish / Safe Demo Readiness - latest commit

Review:

- `src/OneBrain.Core/Recipes/RecipeProductSurfaceContracts.cs`
- Product surface tests under `tests/OneBrain.Recipes.Tests`
- Product surface docs under `docs/recipes`
- Product surface QA reports under `docs/qa`
- Product surface handoffs and prompts

## Audit Questions

1. P0/P1/P2/P3 findings.
2. Scope drift check.
3. Live execution leakage check.
4. Product overclaim check.
5. Test quality review.
6. Docs consistency review.
7. Protected scope review.
8. Safety matrix review.
9. GO/NO-GO decision for closing the Product Surface line.

## Safety Assertions To Verify

- Real recipe execution: NO.
- Live recipe runtime: NO.
- Browser automation: NO.
- Desktop automation: NO.
- CDP/Playwright/Selenium/Puppeteer: NO.
- Connector/API/network: NO.
- Vault/secrets access: NO.
- Scheduler/watcher/hook/listener: NO.
- Recorder/playback/capture: NO.
- Automatic workitem processing: NO.
- Fiscal/payment/marketplace/message/delete/write live actions: NO.
- Real export file generation: NO.
- Product copy overclaims live automation: NO.

## Correct Product Claim

`NODAL OS has a fixture-safe Recipe Runtime product surface with read-only catalog, lab, templates, readiness explanations, operator previews and handoff/export preview summaries.`

## Forbidden Product Claim

`NODAL OS can execute/live automate these recipes.`

## Final Report Format

1. Audit decision.
2. Current git state.
3. Commands run.
4. Safety matrix.
5. Findings with severity P0/P1/P2/P3.
6. Test quality review.
7. Documentation review.
8. Protected scope review.
9. Final recommendation.
