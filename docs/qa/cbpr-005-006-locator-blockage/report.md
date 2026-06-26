# CBPR-005/006 Locator Engine V1 + Blockage Detector V1

Date: 2026-06-26

Decision: `GO_CLOAK_BROWSER_LOCATOR_BLOCKAGE_FOUNDATION`

Project: NODAL OS

Branch: `chrome-lab-001-extension-local-ai-bridge`

Initial HEAD: `06191f14f7a59cf68e7e0939ac1366b9c7878633`

Final HEAD: recorded in final task response after commit.

Mode: read-only, fixture-safe, no productive automation.

## What Changed

- Added `LocatorEngine` V1 in the isolated `OneBrain.BrowserPerception` project.
- Added read-only locator candidate contracts:
  - `LocatorStrategyKind`
  - `LocatorStrategy`
  - `InteractiveElementSnapshot`
  - `ElementLocator`
- Added `BlockageDetector` V1 in the isolated `OneBrain.BrowserPerception` project.
- Extended blockage contracts while keeping existing foundation compatibility.
- Updated `StrategyRouter` to consult blockage detection before final routing.
- Added optional locator/blockage metadata to `StrategyRouterDecision` without changing its existing positional constructor.
- Added CBPR-005/006 tests in the existing perception router category.
- Updated the browser perception ADR.

## What Did Not Change

- No protected Stealth Core files changed.
- No isolated browser executor changed.
- No Bridge/WebSocket protected channel changed.
- No CloakBrowser runtime launch/profile/fingerprint/evasion behavior changed.
- No Chrome Extension legacy path changed.
- No productive actions were added.
- No external navigation was added.
- No safe injection productive path was added.
- No CAPTCHA/2FA/anti-bot/paywall/login bypass was added.

## Protected Paths Verified

The following paths are protected and must remain diff-free:

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

## Locator Strategy Matrix

| Fixture | Expected Locator Strategy | Status |
| --- | --- | --- |
| SPA semantic UI | `Accessibility` | implemented |
| Legacy form | `Css` | implemented |
| Iframe page | `FrameTargetRequired` | implemented |
| Shadow DOM page | `ShadowPiercingRequired` | implemented |
| Canvas/visual page | `Visual` | implemented |
| CAPTCHA/auth page | `HumanHandoff` | implemented |
| Contradictory/complex signals | `HumanHandoff` | implemented |
| Element locator generation | candidates only, no action execution | implemented |

## Blockage Matrix

| Fixture | Expected Blockage | Status |
| --- | --- | --- |
| CAPTCHA marker | `Captcha`, human handoff | implemented |
| Login form | `Login`, human handoff | implemented |
| 2FA marker | `TwoFactor`, human handoff | implemented |
| Anti-bot marker | `AntiBot`, human handoff | implemented |
| HTTP 403 | `AccessDenied`, automatic false | implemented |
| HTTP 429 | `RateLimit`, automatic false, no bypass | implemented |
| Cookie wall | `CookieWall`, warning | implemented |
| Console critical error | `ConsoleError`, automatic false | implemented |
| Network 500 | `NetworkFailure`, automatic false | implemented |
| Router priority | human handoff wins over locator strategy | implemented |

## Validation Summary

Final validation commands and PASS/FAIL status are recorded in the final task response. The intended minimum validation set includes:

- `dotnet build .\OneBrain.slnx --no-restore`
- `dotnet test .\tests\OneBrain.Safety.Tests\OneBrain.Safety.Tests.csproj --no-build --filter "TestCategory=CloakBrowserPerceptionRouter"`
- relevant CloakBrowser/CDP runtime categories
- `git diff --check`
- secret scan changed/new
- protected scope scan changed/new
- forbidden browser usage scan changed/new
- bad UX wording scan changed/new

## Risks

- Locator Engine V1 emits candidates only; it does not validate against live pages.
- Blockage Detector V1 is metadata-based and fixture-safe; deeper live blockage analysis remains future work.
- Future Safe Actions must consume these outputs as planning inputs, not as execution authority.
- Legacy installed sidepanel compatibility harness hit one transient `Inspected target navigated or closed` failure and passed on retry. This harness remains compat-only and is not the runtime default proof.

## Updated Percentages

- CloakBrowser runtime base: 100%
- Perception Router: 40%
- Browser diagnosis: 35%
- Locator Engine: 25%
- Blockage Detector: 25%
- Safe actions: 0%
- Browser automation productiva: 0%

## Next Recommended Block

`CBPR-007/008 — Safe Actions Planning + Pre/Post Verification Contracts`, still without productive external actions.
