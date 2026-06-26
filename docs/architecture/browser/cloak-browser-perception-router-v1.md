# Cloak Browser Perception Router V1

Date: 2026-06-26

Status: foundation, read-only, no productive automation.

## Problem

NODAL OS needs CloakBrowser to become a native browser perception runtime, not a pile of page-specific hacks. The product needs a safe foundation that can classify page capabilities and choose a future strategy before any locator, action, injection, or automation layer is allowed to run.

## Boundary

This layer is not the existing stealth fingerprinter. The stealth/fingerprinting code measures and controls browser/runtime surface. The new `PageCapabilityClassifier` classifies page capability and blockage signals from metadata-only snapshots.

Names deliberately used for this line:

- `BrowserPerceptionSnapshot`
- `PerceptionSignal`
- `PageCapabilityClassifier`
- `PageTechnologyProfile`
- `StrategyRouterDecision`
- `BrowserEvidencePack`
- `BlockageReport`

The name `PageFingerprinter` is intentionally not used.

## Architecture

```text
Browser page
  -> Multi-Signal Snapshot
  -> Page Capability Classifier
  -> Strategy Router
  -> Locator Engine future layer
  -> Blockage Detector future layer
  -> Evidence Pack
```

The current block implements only contracts, fixture-safe snapshot V0, classifier V1, router V1, placeholders for evidence/blockage, tests, and QA reports.

## Components

### Multi-Signal Snapshot

`BrowserPerceptionSnapshot` represents metadata-only page perception:

- redacted page URL
- title
- viewport
- frame summary
- shadow DOM marker
- canvas marker
- forms summary
- interactive element summary
- accessibility summary
- layout and visibility summary
- console summary
- network summary
- lifecycle state
- screenshot metadata only
- storage metadata counts only
- redaction status

It must not store raw DOM, raw HTML, cookies, localStorage/sessionStorage values, input values, credentials, screenshots inline, or external page payloads.

### Locator Engine

`LocatorEngine` is a read-only candidate generator. It chooses the safest locator strategy from `PageTechnologyProfile` and `BrowserPerceptionSnapshot` metadata, then emits locator candidates for future layers. It does not execute locators, click, type, validate against real pages, pierce shadow DOM, inspect live frames, run JavaScript, or perform visual recognition.

Initial locator strategies:

- `Css`
- `XPath`
- `Accessibility`
- `Text`
- `Visual`
- `Hybrid`
- `FrameTargetRequired`
- `ShadowPiercingRequired`
- `HumanHandoff`

Locator routing rules:

- Modern SPA or semantic UI -> `Accessibility`
- Legacy/simple forms -> `Css`
- Relevant iframe metadata -> `FrameTargetRequired`
- Shadow DOM marker -> `ShadowPiercingRequired`
- Canvas/visual-only marker -> `Visual`
- Auth, CAPTCHA, 2FA, anti-bot, or low-confidence/contradictory signals -> `HumanHandoff`

All locators are candidates only. They include `CandidateOnly=true` and `ExecutesAction=false`.

### Page Capability Classifier

`PageCapabilityClassifier` is a pure deterministic classifier over a `BrowserPerceptionSnapshot`. It returns a `PageCapabilityProfile` with:

- detected page kind
- detected signals
- confidence
- reasons
- recommended strategy

Initial page kinds:

- `legacy_html_form`
- `modern_spa`
- `ssr_app`
- `iframe_heavy`
- `shadow_dom_app`
- `canvas_visual_app`
- `auth_required`
- `human_verification_required`
- `broken_or_unstable`
- `unknown`

### Strategy Router

`StrategyRouter` converts a capability profile into a safe strategy decision. It does not execute actions.

Initial strategies:

- `DOM_FIRST`
- `ACCESSIBILITY_FIRST`
- `FRAME_TARGET_REQUIRED`
- `SHADOW_DOM_REQUIRED`
- `VISUAL_REQUIRED`
- `NETWORK_DIAGNOSIS_REQUIRED`
- `CONSOLE_DIAGNOSIS_REQUIRED`
- `HUMAN_HANDOFF_REQUIRED`
- `UNSUPPORTED_OR_HIGH_RISK`

Priority rules:

- Human handoff wins over every automated or diagnostic strategy.
- Auth, CAPTCHA, 2FA, anti-bot, login, and credential-required signals route to `HUMAN_HANDOFF_REQUIRED`.
- Critical network failures route to `NETWORK_DIAGNOSIS_REQUIRED` unless they are auth/human handoff.
- Critical console/runtime failures route to `CONSOLE_DIAGNOSIS_REQUIRED`.
- Unknown or low-confidence pages route to `UNSUPPORTED_OR_HIGH_RISK`.

From CBPR-005/006 forward, `StrategyRouter` consults `BlockageDetector` before final routing. If a blockage requires human handoff, final routing is `HUMAN_HANDOFF_REQUIRED`. If no critical blockage exists and a snapshot is available, the router enriches the decision with locator strategy metadata.

### Blockage Detector

`BlockageDetector` detects metadata-only obstacles that block or condition future automation. It does not solve, bypass, dismiss, click, submit, wait out, or mutate anything.

Initial blockage kinds:

- `Captcha`
- `Login`
- `TwoFactor`
- `AntiBot`
- `RateLimit`
- `AccessDenied`
- `Popup`
- `CookieWall`
- `BrokenPage`
- `NetworkFailure`
- `ConsoleError`
- `Unknown`

Rules:

- CAPTCHA, 2FA, anti-bot, login, and credential entry always require human handoff.
- HTTP 403 blocks automatic continuation.
- HTTP 429 blocks automatic continuation and may require future backoff policy, never bypass.
- Cookie walls and popups are warnings only in this block; no dismissal is executed.
- Critical console and network failures block automatic continuation.

### Safe Action Planner

`SafeActionPlanner` creates theoretical, fixture-only action plans from `PageTechnologyProfile`, `BrowserPerceptionSnapshot`, and a requested objective. It does not execute actions, navigate, click, type, select, call CDP, call WebSocket, inject JavaScript, or mutate page state.

Initial action kinds:

- `Scroll`
- `Focus`
- `Click`
- `Type`
- `Select`
- `Wait`
- `HumanHandoff`

Planning rules:

- CAPTCHA, 2FA, anti-bot, login, credential entry, sensitive data, or bypass objectives return only `HumanHandoff`.
- Low-confidence or contradictory snapshots return `HumanHandoff`.
- Legacy form fixtures may produce theoretical `Type` and `Click` plans.
- SPA fixtures may produce theoretical accessibility-based `Focus` and `Click` plans.
- Visual/canvas surfaces return `HumanHandoff` in this block.
- Every plan is `PlanOnly`, `CanExecuteInFixtureOnly`, `ProhibitedOnExternalPages`, and metadata-only.

### Pre/Post Verification Contracts

`BrowserActionVerifier` evaluates preconditions and postconditions against snapshots only. It does not execute the action being verified.

Initial preconditions:

- `NoHumanHandoffBlockage`
- `TargetLocatorPresent`
- `TargetVisible`
- `TargetEnabled`
- `PageStable`
- `ConfidenceAboveThreshold`
- `FixtureOrControlledPageOnly`
- `LiveExecutionDisabled`
- `SensitiveInputSafe`
- `SupportedAction`

Initial postconditions:

- `UrlChanged`
- `DomChanged`
- `ElementAppeared`
- `ElementDisappeared`
- `InputValueChanged`
- `NetworkSettled`
- `NoCriticalConsoleError`
- `ExpectedStateObserved`

Verification contracts are planning gates for future action layers. They do not grant execution authority.

### Future Layers

The following are deliberately out of scope:

- Locator execution or live locator validation
- Blockage Detector deep implementation
- Safe Injection
- Controlled Action Controller integration
- Productive actions
- External page navigation

After CBPR-005/006, Locator Engine V1 and Blockage Detector V1 exist only as read-only candidate/diagnostic layers. Productive actions, live page mutation, safe injection, and external navigation remain out of scope.

After CBPR-007/008, Safe Action Planner V1 and Pre/Post Verification Contracts V1 exist only as read-only fixture planning and snapshot comparison layers. Controlled action execution is explicitly deferred to CBPR-009 and must not run without human confirmation.

After CBPR-009, Controlled Action Executor V0 exists only as a fixture/in-memory executor. It accepts `FixtureOnly` mode, aborts all live modes, mutates only `FixturePageState`, and records metadata-only evidence flags proving no CDP, WebSocket, browser launch, extension, external navigation, product file write, or system browser path was invoked. CBPR-010 is not implemented by CBPR-009.

After CBPR-010, Browser Evidence Pack V1 closes the fixture-safe line with metadata-only evidence for plan-only decisions, fixture execution success, fixture execution failure, blockage, human handoff, and verification failure. The evidence collector works only over snapshots, strategy decisions, plans, blockages, verification results, and fixture execution results. It does not collect live browser state, call CDP, call WebSocket, open a browser, invoke the Chrome Extension, navigate externally, or enable product actions.

CBPR-010 also adds defensive redaction for sensitive field names and secret-like text patterns including passwords, tokens, API keys, client secrets, bearer/authorization values, session/cookie values, OTP/2FA/MFA markers, captcha markers, GitHub token patterns, OpenAI-style `sk-` patterns, and JWT-like strings. The next step after CBPR-010 is a deep audit of CBPR-001/010 before any future live CDP design.

## Guardrails

- No CAPTCHA, 2FA, anti-bot, paywall, or login bypass.
- No stealth-core/fingerprinter modification.
- No protected isolated browser executor modification.
- No header, user-agent, WebGL, canvas, profile, proxy, anti-detect, launch hardening, or runtime profile changes.
- No production injection.
- No productive browser actions.
- No real external pages.
- No filesystem writes outside docs/tests/new modules/artifacts explicitly allowed for this block.
- Human handoff is mandatory for auth, CAPTCHA, 2FA, anti-bot, and credential-required pages.

## Definition Of Done For CBPR-001/004

- Browser runtime inventory exists.
- ADR exists.
- New contracts exist in an isolated module.
- Multi-Signal Snapshot V0 can be built from fixtures.
- Page Capability Classifier V1 exists.
- Strategy Router V1 exists.
- Tests cover major fixture classes and priority rules.
- Evidence/report artifacts exist.
- Protected scope has no diff.
- No productive automation or safe injection is added.
