# NODAL OS Recipe Runtime Phase 5 Prompt

Block: `NODAL_RECIPE_RUNTIME_005_TOOL_TRUST_SECRETS_BY_REFERENCE`

Objective: add fixture-safe tool trust registry and secrets-by-reference contracts for Recipe Runtime.

Constraints:

- Do not add live execution.
- Do not add browser extension/native messaging.
- Do not add real browser automation.
- Do not add desktop/computer-use automation.
- Do not add CDP/Playwright/Selenium/Puppeteer.
- Do not add scheduler/background worker.
- Do not add recorder/replay.
- Do not add file watcher or OS hook.
- Do not add network/API calls.
- Do not access a real secret vault.
- Do not capture real screenshots, DOM, accessibility trees, HAR, or network logs.
- Do not expose secrets.
- Approval must not unlock live runtime.

Expected phase:

- Total phases: 9.
- Current phase: 5/9.
- Phase name: Tool Trust Registry + Secrets by Reference.

Recommended pre-step: run a Claude deep audit of Phases 1-4 before implementing Phase 5.
