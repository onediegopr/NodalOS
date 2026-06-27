# M5 Reliable Recipe Eval Harness Fixture Scenarios

Decision target: `NODAL_OS_M5_RELIABLE_RECIPE_EVAL_HARNESS_FIXTURE_SCENARIOS`

Decision: `GO_M5_RELIABLE_RECIPE_EVAL_HARNESS_FIXTURE_SCENARIOS_READY`

## Guard

- Repo path: `C:\DESARROLLO\NodalOS\Codigo-m12-audit`
- Branch: `chrome-lab-001-extension-local-ai-bridge`
- Remote: `https://github.com/onediegopr/NodalOS.git`
- Initial HEAD: `004ab7ad998e118d569604f234cf48e31f156a92`
- Required M4 commit in ancestry: yes
- Origin sync at start: `0/0`
- Worktree at start: clean
- Repo confirmed as NODAL OS, not NODRIX.

## Placement Decision

M1 already included a small fixture eval harness. M5 does not replace those M1 contracts. M5 adds a richer fixture-only layer with `ReliableRecipeFixtureEval*` names to avoid collisions and to make the no-runtime boundary explicit.

The M5 runner resolves only existing M3/M4 fixtures:

- M3 read-only Lab recipe fixtures.
- M4 recorder-to-recipe draft fixtures.
- One policy regression fixture that intentionally expects a block for a safe fixture to prove unexpected-pass detection.

## Implemented

- `ReliableRecipeFixtureEvalScenario` catalog with 12 deterministic scenarios.
- `ReliableRecipeFixtureEvalRunner` for repeated fixture-only evaluation.
- Iteration result model with quality, evidence, validation, target, sandbox and human-intervention scores.
- Failure taxonomy including OCR-only sensitive target, validation missing, evidence missing, policy block, risk block, sandbox not ready, recorder draft not reviewed, expected block not occurring and unexpected pass.
- Deterministic flakiness scoring from predefined variants only.
- Product-facing eval report surface.
- Read-only Recipe Lab eval panel with fixture-only notice and safe action labels.
- Focused tests: `ReliableRecipeEvalHarnessFixtureScenarios`, 32 tests.

## Scenario Catalog

- `safe_invoice_download_dry_run_candidate_eval`
- `invoice_download_missing_validation_eval`
- `ocr_only_sensitive_submit_blocked_eval`
- `recorder_password_redaction_eval`
- `captcha_two_factor_handoff_eval`
- `ambiguous_target_needs_review_eval`
- `government_submit_high_risk_blocked_eval`
- `desktop_future_sandbox_blocked_eval`
- `corrected_user_click_review_eval`
- `high_quality_high_risk_still_blocked_eval`
- `unexpected_pass_regression_fixture`
- `predefined_flaky_fixture_eval`

## Safety Boundary

M5 remains fixture-only, deterministic and read-only.

Explicitly not implemented:

- browser execution
- CDP/Playwright/Selenium/Puppeteer/Cloak runtime
- desktop hooks or live desktop control
- real recorder
- no mouse/keyboard/screen/clipboard capture
- screenshot activation
- OCR live activation
- sandbox/VM runtime
- provider/LLM call
- network/API call
- productive filesystem side effects
- CAPTCHA/2FA solver
- credential automation
- payment/publish/send/delete action

## OCR Protected Statement

- OCR files touched: no
- OCR behavior changed: no
- OCR gates changed: no
- OCR live activation changed: no
- OCR used/displayed only as fixture signal/reference: yes

## Recorder Protected Statement

- Recorder runtime added: no
- Mouse/keyboard capture added: no
- Screenshot/screen capture added: no
- Desktop/browser hooks added: no
- Fixture-only eval: yes

## Eval Harness Statement

- Live eval added: no
- Deterministic fixture eval only: yes
- Expected block counts as pass: yes
- Unexpected pass regression detection: yes

## Validation Summary

Required validations are recorded in the final operator report. The focused M5 suite passed before final validation.

## Percentages

Before M5:

- Audit/placement: 100%
- Reliable Recipe contracts: 74%
- Validation/policy gates: 67%
- Evidence/timeline recipe linkage: 57%
- Recorder draft readiness: 63%
- Eval harness readiness: 42%
- Sandbox readiness: 48%
- Perception stack formalization: 64%
- Product surface readiness for Recipe Lab: 54%
- Runtime real autonomy: 0%
- Overall new upgrade: 58%

After M5:

- Audit/placement: 100%
- Reliable Recipe contracts: 78%
- Validation/policy gates: 72%
- Evidence/timeline recipe linkage: 65%
- Recorder draft readiness: 68%
- Eval harness readiness: 70%
- Sandbox readiness: 55%
- Perception stack formalization: 68%
- Product surface readiness for Recipe Lab: 65%
- Runtime real autonomy: 0%
- Overall new upgrade: 68%

## Next Recommended Block

`NODAL_OS_M6_COMPUTER_USE_SANDBOX_READINESS_REPORTS`

Focus M6 on sandbox readiness report expansion, isolation policy checks, rollback/evidence policy scoring and fixture-only product reporting. Keep it design-only and no-runtime.
