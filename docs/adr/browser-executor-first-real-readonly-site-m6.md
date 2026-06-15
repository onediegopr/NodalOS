# ADR: First Real Read-Only Site Under Core Browser Executor (M6)

## Context

M1-M5 moved the browser runtime away from a Chrome-extension-owned runner and toward a core-governed Browser Executor:

- M1 defined TargetContext, Action, Verification, Evidence, Liveness, Idempotency and launcher policies.
- M2 implemented a CDP-first executor against local fixtures.
- M3 added smoke gates and diagnostics.
- M4 integrated the executor with FSM, Safety and Evidence.
- M5 degraded the Chrome extension to companion/relay/fallback and prevented it from being the authoritative source of success.

M6 introduces the first real public website scenario in read-only mode. This is not a product feature and is not a site-specific executor implementation.

## Site Chosen

The allowed live scenario is a public MercadoLibre Argentina read-only listing/search URL:

- start URL: `https://www.mercadolibre.com.ar/`;
- public search URL: `https://listado.mercadolibre.com.ar/{query}`;
- default query: `sonoff rf bridge`.

This site was chosen because it has public home/listing/search pages that can be verified by URL/title/text without login or account state.

## Read-Only Boundary

Allowed actions:

- navigate to a public HTTP/HTTPS URL;
- observe URL, title and visible text;
- read public DOM text;
- verify public URL/title/text;
- record redacted evidence.

Blocked by default:

- login/account interaction;
- password/credential flows;
- CAPTCHA/anti-bot bypass;
- buying, cart, checkout, favorites, messages or payments;
- click/select/type on live pages unless a future policy explicitly allows a safe read-only interaction.

M6 intentionally uses public URL navigation for the optional live search instead of typing into the live page. This keeps the first real-site gate read-only and avoids treating trusted input as product behavior.

## Architecture Decision

The real-site logic lives in `BrowserRealSiteReadOnlyScenario`, outside `ChromeCdpBrowserExecutor`. The generic CDP executor remains site-neutral.

The scenario uses:

- `ChromeCdpBrowserLauncher` with temporary disposable profile;
- `ChromeCdpPageSession` for observe/navigate;
- `BrowserExecutorStepRunner` for core-governed action execution;
- `BrowserRealSiteReadOnlyPolicy` for read-only policy checks;
- `BrowserVerification` for final gate;
- `BrowserEvidence` for before/after/verification proof.

The Chrome extension and service worker are not used as the brain.

## Verification

Success requires all of:

- final status `Verified`;
- verification status `Verified`;
- host matches the expected public site;
- expected text or URL evidence is present;
- evidence exists;
- no login/CAPTCHA/anti-bot/access-denied signal was detected.

If verification is weak or the site changes, the result is `Uncertain`, `Blocked`, `RequiresHuman`, `TimedOut`, or `Failed`, never success.

## Block Detection

The scenario maps visible login/CAPTCHA/anti-bot/access-denied signals to non-success states:

- CAPTCHA/anti-bot: `RequiresHuman`;
- login wall: `RequiresHuman`;
- access denied/blocking page: `Blocked`;
- weak or missing expected evidence: `Uncertain`.

No CAPTCHA solving, anti-bot bypass or credential handling is attempted.

## Tests

Always-on unit tests cover:

- scenario is external to the generic executor;
- policy blocks sensitive actions;
- policy allows public navigation/read;
- uncertain verification is not success;
- login/CAPTCHA/anti-bot detection;
- evidence shape includes before/action/after/verification;
- extension relay cannot mark success;
- cleanup path is explicit.

The live test is opt-in with:

```text
ONEBRAIN_BROWSER_LIVE_READONLY=1
```

Without that variable, the test is skipped/inconclusive by design to avoid fragile external-site CI behavior.

## Out Of Scope

M6 does not implement:

- product features;
- login;
- cart/buy/payment/account flows;
- site-specific executor hardcode;
- profile/session manager;
- WebView2 or CEF;
- recipe recorder;
- download/upload manager;
- network capture;
- human handoff UI.

## Risks

- Public websites can change or block automation. The correct behavior is non-success with evidence, not bypass.
- Live tests are intentionally opt-in and may be skipped in CI.
- M6 does not prove authenticated or state-changing browser flows.

## Next Step

The next hito should add profile/session and target/frame management foundations, or a controlled M7 read-only expansion with stronger health diagnostics before any state-changing product behavior.
