# NODAL OS Recipe Runtime Phase 7 Prompt

Block: `NODAL_RECIPE_RUNTIME_007_RECIPE_LAB_LOCATOR_REPAIR_STUDIO`

Objective: add fixture-safe Recipe Lab + Locator Repair Studio contracts.

Scope:

- Recipe lab metadata.
- Draft repair suggestions.
- Locator confidence review by reference.
- Evidence/timeline linkage.
- Human review and approval narrative refs.
- No live browser automation.
- No live desktop automation.
- No CDP/Playwright/Selenium/Puppeteer.
- No browser extension/native messaging.
- No scheduler/background worker.
- No file watcher, OS hook, browser listener or desktop listener.
- No network/API/connector execution.
- No automatic recipe run.
- No automatic workitem processing.
- No secret value access.

Required guardrails:

- Phase 7 must remain contract/fixture-safe.
- Locator repair confidence cannot authorize execution.
- Trigger observations cannot unlock autorun.
- Approval decisions remain narrative-bound.
- Tool trust and secrets remain by reference only.

Expected phase:

- Total phases: 9.
- Current phase: 7/9.
- Phase name: Recipe Lab + Locator Repair Studio.

Claude audit recommendation: if Phase 6 closed cleanly, either audit Phases 1-6 now or proceed one more phase and audit Phases 1-7.
