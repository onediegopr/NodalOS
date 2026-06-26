# CBPR-007/008 Safe Action Planner + Pre/Post Verification Contracts

Date: 2026-06-26

Decision: `GO_CLOAK_BROWSER_SAFE_ACTION_PLANNER_CONTRACTS`

Project: NODAL OS

Branch: `chrome-lab-001-extension-local-ai-bridge`

Initial HEAD: `177c6d3505961b4ea5e26507f3f52b9abb388fc6`

Final HEAD: recorded in final response after commit.

Mode: read-only, fixture-safe, no action execution.

## What Changed

- Added `SafeActionPlanner` V1 under `src/OneBrain.BrowserPerception/Actions/`.
- Added safe action planning contracts:
  - `SafeBrowserActionKind`
  - `SafeBrowserActionPlan`
  - `SafeBrowserActionEvidencePlan`
- Added pre/post verification contracts under `src/OneBrain.BrowserPerception/Verification/`.
- Added `BrowserActionVerifier` for snapshot-only pre/post verification.
- Added CBPR-007/008 tests in `tests/OneBrain.Safety.Tests/CloakBrowserSafeActionPlannerTests.cs`.
- Updated the browser perception ADR.

## What Did Not Change

- No protected Stealth Core files changed.
- No isolated browser executor changed.
- No Bridge/WebSocket protected channel changed.
- No CDP live action was added.
- No WebSocket live action was added.
- No click, type, select, scroll, focus, wait, navigation, or injection is executed.
- No external page navigation was added.
- No CAPTCHA/2FA/anti-bot/paywall/login bypass was added.

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
| CAPTCHA marker returns `HumanHandoff` | implemented |
| 2FA marker returns `HumanHandoff` | implemented |
| Anti-bot marker returns `HumanHandoff` | implemented |
| Login/auth returns `HumanHandoff` | implemented |
| Legacy form returns theoretical `Type` + `Click` | implemented |
| SPA returns accessibility locator plans | implemented |
| Low confidence returns `HumanHandoff` | implemented |
| Preconditions can pass | implemented |
| Missing target locator blocks | implemented |
| Human handoff blockage blocks | implemented |
| Matching postcondition succeeds | implemented |
| Mismatched postcondition fails | implemented |
| Plans are prohibited on external pages | implemented |
| No action execution side effects | implemented |

## Validation Summary

- `dotnet build .\OneBrain.slnx --no-restore`: PASS
- `TestCategory=CloakBrowserPerceptionRouter`: PASS 46/46
- `TestCategory=CloakBrowserRuntime`: PASS 50/50
- `TestCategory=CloakBrowserRuntimeLive`: PASS 18/18
- `TestCategory=CdpBrowserSkills`: PASS 19/19
- `TestCategory=CdpBrowserSkillsSession`: PASS 12/12
- `TestCategory=CdpBrowserSkillsProductSurface`: PASS 8/8
- `TestCategory=CdpUiRuntimeBoundary`: PASS 14/14
- `TestCategory=CdpUiLiveStatusAdapter`: PASS 16/16
- `TestCategory=CdpUiStatusRefresh`: PASS 11/11
- `TestCategory=CdpSafeLocalStatusChannel`: PASS 11/11
- `TestCategory=NoExtensionDefaultHarness`: PASS 7/7
- `TestCategory=MinimalNoExtensionProductSurface`: PASS 9/9
- `TestCategory=ExtensionDeprecationHardening`: PASS 7/7
- `TestCategory=ForkUpdateReleasePipeline`: PASS 7/7
- `node --check browser-extension/onebrain-chrome-lab/sidepanel.js`: PASS
- `node --check scripts/verify-installed-sidepanel.mjs`: PASS
- `node scripts/verify-installed-sidepanel.mjs`: PASS, legacy compat only
- CloakBrowser CDP no-extension/default scripts: PASS
- `git diff --check`: PASS
- Secret scan changed/new: PASS
- Protected scope scan changed/new: PASS
- Forbidden browser usage scan changed/new: PASS
- Bad UX wording scan changed/new: PASS, no changed product UI files

## Hard Stop

CBPR-009 and CBPR-010 were not implemented. This block stops after planner and verification contracts.

## Risks

- Safe action plans are theoretical and fixture-only.
- Postcondition verification compares snapshots only; it does not observe live browser state.
- Future CBPR-009 execution must remain fixture-only until separately authorized and audited.

## Updated Percentages

- CloakBrowser runtime base: 100%
- Perception Router: 50%
- Browser diagnosis: 40%
- Locator Engine: 30%
- Blockage Detector: 30%
- Safe actions: 15%
- Browser automation productiva: 0%

## Next Recommended Block

`CBPR-009 — Controlled Action Executor V0`, only after explicit human confirmation and still fixture-only.
