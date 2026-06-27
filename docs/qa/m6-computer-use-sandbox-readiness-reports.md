# M6 Reliable Recipe Computer-Use Sandbox Readiness Reports QA

Decision target: `NODAL_OS_M6_COMPUTER_USE_SANDBOX_READINESS_REPORTS`

## Status

Implementation status: ready for validation.

M6 adds fixture-only/read-only sandbox readiness reports for Reliable Recipes, Recorder Drafts and Eval Scenarios. It does not create any sandbox or runtime.

## Guard Baseline

- Repo: `C:\DESARROLLO\NodalOS\Codigo-m12-audit`
- Branch: `chrome-lab-001-extension-local-ai-bridge`
- Remote: `https://github.com/onediegopr/NodalOS.git`
- Initial HEAD: `4db6894b67b235a930e3b0e639edb8348ab593dc`
- Origin sync at start: `0/0`
- Worktree at start: clean
- M5 commit in ancestry: yes

## Files Added or Updated

- `src/OneBrain.Core/Recipes/ReliableRecipeSandboxReadinessReports.cs`
- `src/OneBrain.Core/Recipes/ReliableRecipeEvalHarnessFixtureContracts.cs`
- `src/OneBrain.Core/Recipes/ReliableRecipeLabViewModels.cs`
- `tests/OneBrain.Recipes.Tests/ComputerUseSandboxReadinessReportsTests.cs`
- `tests/OneBrain.Recipes.Tests/ReliableRecipeEvalHarnessFixtureScenariosTests.cs`
- `docs/architecture/automation/computer-use-sandbox-readiness-reports-v1.md`
- `docs/qa/m6-computer-use-sandbox-readiness-reports.md`

## What Changed

- Added `ComputerUseSandboxReadinessReport`.
- Added subject kinds, decisions, assessment modes, required isolation modes, surface readiness, missing requirement taxonomy and blocked capability taxonomy.
- Added deterministic `ComputerUseSandboxReadinessEvaluator`.
- Added `ComputerUseSandboxReadinessScenarioCatalog` with 11 stable scenarios.
- Added sandbox readiness summary into M5 eval reports.
- Added sandbox readiness panel into M3 Recipe Lab view model.
- Added 30 focused tests under `ComputerUseSandboxReadinessReports`.

## What Did Not Change

- No real sandbox.
- No VM/container/Docker.
- No browser execution.
- No desktop execution.
- No CDP live path.
- No Playwright/Selenium/Puppeteer runtime.
- No Cloak mutation.
- No UIA/Win32 live.
- No mouse/keyboard/screen/clipboard capture.
- No OCR live activation.
- No screenshot activation.
- No recorder runtime.
- No provider/LLM call.
- No network call.
- No shell/process runner.
- No productive filesystem write.

## Protected Statements

OCR:

- OCR files touched: no.
- OCR behavior changed: no.
- OCR gates changed: no.
- OCR live activation changed: no.
- OCR used/displayed only as fixture signal/reference: yes.

Recorder:

- Recorder runtime added: no.
- Mouse/keyboard capture added: no.
- Screenshot/screen capture added: no.
- Desktop/browser hooks added: no.
- Fixture-only readiness: yes.

Sandbox:

- Sandbox runtime added: no.
- VM/container/Docker added: no.
- Browser/desktop live launched: no.
- Network/shell/process runner added: no.
- Readiness reports only: yes.

## Scenario Coverage

- `safe_invoice_fixture_ready`
- `invoice_missing_validation_needs_review`
- `ocr_only_sensitive_blocked`
- `password_credential_policy_blocked`
- `captcha_two_factor_handoff_blocked`
- `ambiguous_target_needs_review`
- `government_submit_policy_blocked`
- `desktop_future_live_blocked`
- `browser_future_profile_required`
- `high_quality_high_risk_still_blocked`
- `unexpected_pass_regression_blocked`

## Validation Plan

Required commands:

- `dotnet restore .\OneBrain.slnx`
- `dotnet build .\OneBrain.slnx --no-restore`
- `dotnet test .\tests\OneBrain.Recipes.Tests\OneBrain.Recipes.Tests.csproj --no-build --filter TestCategory=ComputerUseSandboxReadinessReports`
- `dotnet test .\tests\OneBrain.Recipes.Tests\OneBrain.Recipes.Tests.csproj --no-build`
- `dotnet test .\tests\OneBrain.Safety.Tests\OneBrain.Safety.Tests.csproj --no-build --filter FullyQualifiedName~Recipe`
- `git diff --check`
- `git diff --cached --check`
- protected scope scan
- OCR protected scope scan
- recorder no-live scan
- sandbox no-runtime scan
- secret scan
- no-live/no-action scan
- dependency scan

## Expected Percentage Movement

Before M6:

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

Target after M6:

- Audit/placement: 100%
- Reliable Recipe contracts: 82%
- Validation/policy gates: 76%
- Evidence/timeline recipe linkage: 70%
- Recorder draft readiness: 72%
- Eval harness readiness: 75%
- Sandbox readiness: 80%
- Perception stack formalization: 72%
- Product surface readiness for Recipe Lab: 74%
- Runtime real autonomy: 0%
- Overall new upgrade: 76%

## Next Block

Recommended M7: `NODAL_OS_M7_PERCEPTION_STACK_FORMAL_INTEGRATION_REPORTS`.
