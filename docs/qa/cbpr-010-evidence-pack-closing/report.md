# CBPR-010 Browser Evidence Pack V1 + Fixture-Safe Closing

Date: 2026-06-26

Decision: `GO_CLOAK_BROWSER_EVIDENCE_PACK_V1_FIXTURE_SAFE_CLOSING`

Project: NODAL OS

Branch: `chrome-lab-001-extension-local-ai-bridge`

Initial HEAD: `d240e38f8db1561761edcd865c4dffcdf4d7066c`

Final HEAD: `6074089468970758f71d32e93430c6122dbd23c7`

Origin sync at start: `0 0`

Mode: fixture-safe, metadata-only evidence, no live execution.

## CBPR Line Summary

- CBPR-001/004: Perception Router foundation, fixture-safe snapshots, classifier, router, inventory, ADR.
- CBPR-005/006: Locator Engine V1 and Blockage Detector V1, no actions.
- CBPR-007/008: Safe Action Planner and Pre/Post Verification contracts, plan-only.
- CBPR-009: Controlled Action Executor V0, fixture/in-memory only.
- CBPR-010: Browser Evidence Pack V1 and redaction, fixture-safe line closing.

## What Changed

- Added `BrowserEvidencePack` V1 fields to the existing evidence contract.
- Added `BrowserEvidenceCollector` for:
  - plan-only evidence
  - fixture execution success
  - fixture execution failure
  - blockage evidence
  - human handoff evidence
  - verification failure evidence
- Added `BrowserEvidenceRedactor` for defensive field and text redaction.
- Updated the legacy placeholder `BrowserEvidencePackBuilder` to emit V1 metadata-only packs.
- Added CBPR-010 tests in `tests/OneBrain.Safety.Tests/CloakBrowserEvidencePackTests.cs`.
- Updated the browser perception ADR.

## What Did Not Change

- No protected Stealth Core files changed.
- No isolated browser executor changed.
- No Bridge/WebSocket protected channel changed.
- No CDP live action was added.
- No WebSocket live action was added.
- No browser launch was added.
- No Chrome Extension path was added.
- No external navigation was added.
- No Safe Injection live path was added.
- No collector live action path was added.
- No product integration that enables real actions was added.

## Protected Paths Verified

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

Protected scope scan result: PASS, no changed/new protected paths.

## Test Matrix

| Requirement | Status |
| --- | --- |
| Plan-only evidence has no execution result | implemented |
| Fixture execution success evidence includes after snapshot | implemented |
| Fixture execution failure evidence records failed state | implemented |
| Blockage evidence records CAPTCHA handoff | implemented |
| Blockage evidence records 2FA/anti-bot/login handoff | implemented |
| Human handoff evidence records decision and blockage | implemented |
| Verification failure evidence records failed verification | implemented |
| Password field value is redacted from serialized JSON | implemented |
| Token/API key/authorization/session values are redacted | implemented |
| OTP contextual field is redacted | implemented |
| Non-sensitive field remains visible | implemented |
| Serialization round-trip does not reintroduce secrets | implemented |
| Collector evidence flags prove no live invocation | implemented |
| Summary containing `sk-` token is redacted | implemented |
| Normal field text is not over-redacted | implemented |

## Validation Summary

- `dotnet build .\OneBrain.slnx --no-restore`: PASS
- `dotnet test .\OneBrain.slnx --filter TestCategory=CloakBrowserPerceptionRouter`: PASS 75/75
- `TestCategory=CloakBrowserRuntime`: PASS 50/50
- `TestCategory=CloakBrowserRuntimeLive`: PASS 18/18
- `TestCategory=CdpBrowserSkills`: PASS 19/19
- `TestCategory=CdpBrowserSkillsSession`: PASS 12/12
- `TestCategory=CdpBrowserSkillsProductSurface`: PASS 8/8
- `TestCategory=CdpUiRuntimeBoundary`: PASS 14/14
- `TestCategory=NoExtensionDefaultHarness`: PASS 7/7
- `TestCategory=MinimalNoExtensionProductSurface`: PASS 9/9
- `git diff --check`: PASS
- Protected scope scan: PASS, no protected paths changed.
- Forbidden browser usage scan: PASS, no forbidden browser/runtime usage in changed files.
- Secret scan: PASS, only fake redaction-test literals detected in evidence tests.
- Bad UX wording scan: PASS, no changed product UI files with blocked wording.

## No Live Execution Proof

Every `BrowserEvidencePack` V1 emitted by the collector records:

- `MetadataOnly=true`
- `FixtureOnly=true`
- `CdpInvoked=false`
- `WebSocketInvoked=false`
- `BrowserLaunched=false`
- `SystemBrowserUsed=false`
- `ExtensionInvoked=false`
- `ExternalNavigationAttempted=false`
- `ProductFilesModified=false`
- `LiveExecutionDisabled=true`

## Redaction Proof

`BrowserEvidenceRedactor` redacts sensitive field names and secret-like text patterns:

- password/passwd/pwd
- token/access_token/refresh_token
- api_key/apikey/API key
- secret/client_secret
- bearer/authorization
- credential
- otp/2fa/mfa
- captcha
- session/cookie
- `sk-...`
- `ghp_...`
- JWT-like strings
- long opaque key-like strings

Sensitive values are replaced with `[REDACTED]`; `SensitiveFieldsRedacted` stores field/category names, not values.

## Hard Stop

The fixture-safe CBPR line is closed. No live CDP action executor, WebSocket live bridge, Safe Injection live, external navigation, real-page actions, live collector actions, or productive Mission Control action integration was implemented.

## Risks

- Evidence packs are metadata-only and fixture-safe; they do not prove live execution safety.
- Redaction is defensive but not a substitute for a future formal data-classification audit before live use.
- Any future live CDP phase requires a new architecture, new guardrails, new prompt, human approval, and a deep audit first.

## Final Percentages

- CloakBrowser runtime base: 100%
- Perception Router: 65%
- Browser diagnosis: 60%
- Locator Engine: 40%
- Blockage Detector: 45%
- Safe actions: 45%
- Browser automation productiva: 0%

## Next Mandatory Step

Run GPT-5.5 XHigh + Kimi 2.7 deep audit over CBPR-001/010 before any future live CDP design.
