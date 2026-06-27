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

Audit cleanup carry-forward:

- Approval decisions are narrative-bound; Phase 5 must preserve that no caller can approve an option that was not offered by the approval narrative.
- `RecipePolicyPreflightEvaluator` remains the canonical readiness path for Phase 2+ policy checks; do not rely on foundation-only readiness alone.
- Sensitive categories introduced or refined in Phase 5 must require approval/human paths unless explicitly blocked.
- `FutureConnectorRuntime` remains descriptive and must be blocked or explicitly gated until Tool Trust + Secrets policy exists.
- Failed blocking validation evidence must not be treated as complete evidence.
