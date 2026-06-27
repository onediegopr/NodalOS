# NODAL OS Recipe Human Intervention + Approval Narrative Handoff

## State

Decision target: `GO_RECIPE_HUMAN_INTERVENTION_APPROVAL_NARRATIVE_READY`.

Phase status:

- Total phases: 9.
- Current phase: 4/9.
- Current phase name: Human Intervention + Approval Narrative 2.0.
- Phase 4 start progress: 0%.
- Phase 4 end progress estimate: 95%.
- Overall Recipe Runtime line completion estimate: 49%.

## Added

- `RecipeHumanInterventionRequest`, refs, reasons, kinds, statuses, safe next actions, and manual resolution contracts.
- `RecipeApprovalNarrative`, decision options, decision records, consequence summaries, risk explanations, evidence summaries, rollback boundaries, and limit summaries.
- `RecipeHumanBlockingScenarioCatalog` for critical human-review cases.
- Fixture-safe readiness checks for high/critical risk, missing human paths, live action blocks, sensitive AI fallback, and critical safe-next/rollback metadata.
- Timeline projection helpers for intervention, approval required, approval decisions, rejection, more evidence, and manual resolution.
- Handoff summary contract for approval narrative summaries.

## Boundaries

No OpenRPA dependency, no code copy, no XAML, no extension/native messaging, no real browser automation, no desktop/computer-use automation, no CDP/Playwright/Selenium/Puppeteer, no scheduler, no recorder/replay, no file watcher, no OS hook, no network/API call, no real screenshot/HAR/DOM/accessibility capture, no approval-triggered execution, no secret exposure, and no live runtime.

Approval does not unlock live runtime. Human intervention does not solve CAPTCHA, 2FA, or challenge states automatically.

## Next Phase

Phase 5/9: Tool Trust Registry + Secrets by Reference.

Claude audit recommendation: run a deep audit before Phase 5 if this phase closes cleanly.
