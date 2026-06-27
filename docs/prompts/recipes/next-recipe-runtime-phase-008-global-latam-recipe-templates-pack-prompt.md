# NODAL OS Recipe Runtime Phase 8 Prompt

Block: `NODAL_RECIPE_RUNTIME_008_GLOBAL_LATAM_RECIPE_TEMPLATES_PACK`

Objective: add fixture-safe Global + LATAM Recipe Templates Pack v1.

Scope:

- Template metadata.
- Region/country classification.
- Risk and approval requirement refs.
- Tool trust and secret refs by id only.
- Evidence and timeline expectations.
- Trigger observe-only associations.
- Recipe Lab preview compatibility.
- No live browser automation.
- No live desktop automation.
- No CDP/Playwright/Selenium/Puppeteer.
- No connector execution.
- No network/API calls.
- No vault or raw secrets.
- No scheduler/watcher/hook/listener.
- No automatic recipe run or workitem processing.

Required guardrails:

- Templates are catalog/preview/fixture-safe only.
- Phase 8 must add a composite template readiness evaluator that composes `RecipePolicyPreflightEvaluator`, tool/secret readiness, trigger observe-only readiness, locator/lab safety, and evidence/approval readiness.
- Template readiness must block live-blocked, future-gated, disabled, browser-runtime, desktop-runtime, or connector-runtime-backed tool entries even when a tool entry is otherwise approved for fixture use.
- Payment/fiscal/legal/message/delete/publication templates remain approval/human gated.
- LATAM fiscal and marketplace templates must not include real integrations.
- Recipe Lab can inspect templates but cannot execute them.
- Tool trust and secret refs must remain by-reference only; no real vault/API/network/connector execution is allowed.

Expected phase:

- Total phases: 9.
- Current phase: 8/9.
- Phase name: Global + LATAM Recipe Templates Pack v1.
