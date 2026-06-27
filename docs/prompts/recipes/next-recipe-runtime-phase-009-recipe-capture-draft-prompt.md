# Next Prompt - Recipe Runtime Phase 9/9

Block: `NODAL_RECIPE_RUNTIME_009_RECIPE_CAPTURE_DRAFT`

Decision target: `GO_RECIPE_CAPTURE_DRAFT_READY`

Objective: add fixture-safe Recipe Capture Draft contracts for authoring recipe drafts from operator-provided descriptions, fixture observations, evidence refs and template refs.

Required guardrails:

- no real recorder.
- no replay.
- no browser automation.
- no desktop automation.
- no CDP/Playwright/Selenium/Puppeteer.
- no extension/native messaging.
- no real DOM/accessibility/screenshot/HAR capture.
- no scheduler/background worker.
- no watcher/hook/listener.
- no network/API/webhook calls.
- no connector execution.
- no vault access.
- no raw secrets.
- no automatic recipe run.
- no automatic workitem processing.
- no locator replay or live locator repair apply.
- no CAPTCHA/2FA/challenge bypass.
- no live runtime.

Phase 9 should build on Phase 8 template catalog and composite readiness. Capture output must be draft-only, reference-only, redacted and routed through readiness before any fixture-ready claim.

Expected output:

- Recipe Capture Draft contracts.
- Draft-to-template linkage.
- Draft evidence refs and redaction summaries.
- Draft readiness projection using composite template readiness.
- Fixture-safe tests.
- Docs, QA report, handoff and final audit recommendation for Recipe Runtime Phases 1-9.
