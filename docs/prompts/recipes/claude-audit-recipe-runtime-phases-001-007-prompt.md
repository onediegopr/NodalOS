# Claude Deep Audit Prompt - Recipe Runtime Phases 1-7

Audit block: `NODAL_RECIPE_RUNTIME_PHASES_001_007_DEEP_AUDIT`

Target decision: `AUDIT_GO_RECIPE_RUNTIME_READY_FOR_PHASE_8` or `AUDIT_NO_GO_WITH_FINDINGS`.

Audit only. Do not modify files, commit, push, refactor or fix while auditing.

## Scope

Audit Recipe Runtime Phases 1-7:

- Foundation + Workitems.
- Limits / Validation / Risk / Deterministic Policy.
- Evidence Pack + Timeline Projection.
- Human Intervention + Approval Narrative 2.0.
- Tool Trust Registry + Secrets by Reference.
- Trigger / Detector observe-only.
- Recipe Lab + Locator Repair Studio.

## Required Questions

- Are contracts coherent and non-duplicative?
- Does Recipe Lab use `RecipePolicyPreflightEvaluator` as canonical readiness?
- Are evidence, timeline, approval, tool trust, secret refs and triggers summarized safely?
- Do locator repair contracts avoid action authority?
- Are relative coordinate and AI fallback locator paths blocked or human-gated?
- Are raw secrets, raw payloads and real capture paths absent?
- Does any approval, trigger, locator repair or lab model unlock live runtime?
- Are docs and reports consistent with contract/fixture-safe only claims?

## Safety Checks

Confirm no OpenRPA dependency, code copy, XAML import, browser extension/native messaging, real browser automation, real desktop automation, CDP/Playwright/Selenium/Puppeteer, real DOM/accessibility/screenshot/HAR capture, scheduler/background worker, file watcher, OS hook/hotkey listener, browser/desktop listener, network/webhook listener, connector execution, vault, raw secrets, recorder/replay, real locator replay/testing, live locator repair apply, automatic recipe run, automatic workitem processing, approval-triggered live runtime or live runtime was added.

## Recommendation

If no P0/P1 findings are found, recommend proceeding to Phase 8/9 - Global + LATAM Recipe Templates Pack v1 with fixture-safe templates only.
