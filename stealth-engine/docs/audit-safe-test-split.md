# Stealth Engine Test Split

## Purpose

This note separates Stealth Engine validation into an audit-safe default path and explicit live opt-in paths.

## Audit-safe default

Use:

```bash
npm test
```

or:

```bash
npm run test:audit-safe
```

Both commands run only `tests/stealth-suite.test.js`. That suite covers deterministic construction, profile coherence, behavior profiles, proxy pool logic, blacklist logic, adaptive behavior calculations, and disabled visual captcha solver behavior. It does not launch a browser, open CDP, call external sites, start the Bridge, or require network access.

## Live opt-in tests

Use only with explicit operator approval:

```bash
npm run test:live
```

The live suite includes:

- `tests/anti-bot-stress.test.js`: launches Playwright Chromium and calls external anti-bot demo sites. The hCaptcha and Cloudflare Turnstile demo checks are external-site dependent and may fail because the demo changed or DNS/network is unavailable.
- `tests/bridge-integration.test.js`: starts the local Bridge process, starts the Stealth Engine process, opens WebSocket/HTTP endpoints on localhost, and requires Playwright Chromium.

Live tests are not an audit-safe gate and must not be reported as PASS unless they are actually executed in an explicitly authorized live validation session.

## Protected scope

This split does not change Stealth Core runtime behavior. It only changes package scripts and documentation. The protected runtime implementation remains untouched:

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
