# Mistral OCR 4 Provider Router Design-Only Report

Decision: `GO_MISTRAL_OCR_PROVIDER_ROUTER_DESIGN_ONLY_READY_WITH_KNOWN_OUT_OF_SCOPE_UNTRACKED_WCU_FILES`

Mode: design-only, fixture-safe, no-live, no-billing, no browser or desktop automation.

Branch: `chrome-lab-001-extension-local-ai-bridge`

Initial HEAD: `1aadf7543afa25c50816bf05b93cf4f9d8c53d8c`

Final HEAD: recorded in final operator response after commit. A commit cannot self-reference its own final hash.

## Scope

Implemented an isolated OCR / Document Intelligence provider router foundation in `OneBrain.DocumentIntelligence`.

The block adds:

- provider registry contracts,
- Mistral OCR 4 provider candidate,
- Mistral Document AI provider candidate,
- local OCR fixture provider candidate,
- human review fallback,
- fixture-only routing decisions,
- confidence policy,
- evidence/redaction contracts,
- synthetic fixtures,
- tests and architecture documentation.

## Safety

- No real API calls.
- No Mistral API key usage.
- No network requests.
- No billing integration.
- No live provider client.
- No browser automation.
- No desktop automation.
- No CDP live execution.
- No Safe Injection live.
- No screenshot/document raw evidence persistence.
- No OCR action authority.

## Known Out-Of-Scope Worktree State

Unexpected untracked Windows Computer Use / computer-use interop files are intentionally preserved out of scope:

- `docs/architecture/computer-use/windows-computer-use-robust-perception-interop-v1.md`
- `docs/qa/computer-use/wcu-008-014-uia-ocr-interop/`
- `src/OneBrain.WindowsComputerUse/ComputerUsePerceptionFusion.cs`
- `src/OneBrain.WindowsComputerUse/VisualPerceptionInterop.cs`
- `src/OneBrain.WindowsComputerUse/WindowsUiAutomationReadOnlyCollector.cs`
- `tests/OneBrain.Safety.Tests/WindowsComputerUseOcrInteropTests.cs`

They are not modified, not deleted, not staged, and not committed by this OCR / Document Intelligence block.

## Protected Scope

Protected scope was not modified:

- `stealth-engine/src/evasion/**`
- `stealth-engine/src/captcha/**`
- `stealth-engine/src/fingerprint/**`
- `stealth-engine/src/behavior/**`
- `stealth-engine/src/proxy/**`
- `stealth-engine/src/antiBlocking/**`
- `stealth-engine/src/handoff/**`
- `stealth-engine/src/StealthSession.js`
- `stealth-engine/src/StealthBrowserManager.js`
- `stealth-engine/src/index.js`
- `stealth-engine/tests/stealth-suite.test.js`

## Provider Candidates

- `local.onnx_ocr_fixture`: fixture-only local OCR candidate.
- `cloud.mistral_ocr_4`: paid OCR candidate, live candidate blocked.
- `cloud.mistral_document_ai`: document AI candidate, live candidate blocked.
- `human.review`: fixture-only human review fallback.

## Fixtures

- `simple_invoice_fixture`
- `table_fixture`
- `low_confidence_fixture`
- `sensitive_document_fixture`
- `conflicting_fields_fixture`
- `screen_crop_fixture`
- `captcha_like_fixture`
- `login_like_fixture`
- `payment_like_fixture`
- `fiscal_submission_like_fixture`

All fixtures are synthetic and contain fake/non-real data.

## Validation Results

Recorded validation results:

- `dotnet restore .\OneBrain.slnx`: PASS, required because the new project needed an assets file before no-restore build.
- `dotnet build .\OneBrain.slnx --no-restore`: PASS.
- `dotnet test .\tests\OneBrain.Safety.Tests\OneBrain.Safety.Tests.csproj --no-build --filter TestCategory=MistralOcrProviderRouterDesignOnly`: PASS, 14/14.
- `git diff --check`: PASS.
- JSON validation: PASS.
- Protected scope scan: PASS, no diff.
- Secret scan changed/new: PASS with allowed synthetic redaction-test literals only.
- Forbidden live/network/API usage scan changed/new: PASS.
- No browser/desktop automation change scan: PASS.

## Decisions Preserved

- `LIVE_MISTRAL_OCR_CALLS: NO-GO`
- `PAID_PROVIDER_EXECUTION: NO-GO`
- `OCR_ACTION_AUTHORITY: NO-GO`
- `BROWSER_LIVE_AUTOMATION: NO-GO`
- `DESKTOP_LIVE_AUTOMATION: NO-GO`
- `CAPTCHA_LOGIN_2FA_PAYMENT_FISCAL_AUTOMATION: NO-GO`
- `PUBLIC_RELEASE_UNLOCK: NO-GO`
- `PAID_BETA_UNLOCK: NO-GO`
- `WCU_UNTRACKED_FILES_TOUCHED: NO`
- `WCU_UNTRACKED_FILES_COMMITTED: NO`
- `WCU_UNTRACKED_FILES_DELETED: NO`

## Risks

- Live Mistral OCR remains unimplemented and blocked.
- Provider billing, BYOK, retention, consent, timeout/retry, allowlist and audit policy must be designed before any live provider implementation.
- OCR remains observation-only and cannot unlock action execution.
- Worktree remains not fully clean because known out-of-scope WCU/computer-use interop untracked files are preserved.
