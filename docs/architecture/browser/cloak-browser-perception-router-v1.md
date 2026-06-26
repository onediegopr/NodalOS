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

### Future Layers

The following are deliberately out of scope:

- Locator Engine
- Blockage Detector deep implementation
- Safe Injection
- Controlled Action Controller integration
- Productive actions
- External page navigation

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
