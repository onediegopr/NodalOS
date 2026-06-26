# Live CDP Design Gate ADR

Date: 2026-06-26

Project: NODAL OS

Status: design-only. No implementation allowed by this ADR.

## Context

CBPR-001/010 closed the `CLOAK_BROWSER_PERCEPTION_ROUTER` line as fixture-safe. It provides perception, classification, locator strategy, blockage detection, safe action planning, verification contracts, fixture-only execution, and evidence packs over metadata, snapshots, and in-memory state.

Live remains `NO-GO`.

The Stealth Core remains protected:

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

## Proposed Future Architecture, Conceptual Only

### Live Read-Only Collector

A future live read-only collector may be designed to gather:

- CDP DOM snapshot.
- AX tree.
- Layout boxes.
- Screenshot metadata.
- Console summary.
- Network summary.
- Lifecycle state.

The collector must be read-only first. It must not click, type, select, scroll, inject script for mutation, navigate, submit forms, solve challenges, enter credentials, or modify page state.

### Future Live Action Gateway

If a later phase is approved, a live action gateway must be:

- Disabled by default.
- Explicitly enabled by a dedicated gate.
- Approval required.
- Target page allowlist required.
- No credentials.
- No CAPTCHA, 2FA, or anti-bot handling.
- Evidence before and after.
- Postcondition verification required.
- Immediate human handoff if ambiguous.

This ADR does not authorize building that gateway.

## Gating Model

- Fixture-safe layer remains separate.
- Live collector must start read-only.
- Action executor is a later phase only.
- Product UI cannot enable live actions until a gate is explicitly unlocked.
- No bridge, WebSocket, CDP action, Safe Injection, or runtime change is allowed by this document.

## Threat Model

Future live work must address:

- Accidental live execution.
- Secret leakage.
- Selector wrong target.
- Auth/session risk.
- Anti-bot, CAPTCHA, and 2FA encounters.
- User misunderstanding.
- Site policy violation.
- Regression into extension or system browser fallback.
- Evidence integrity and redaction drift.
- Protected scope drift.

## Required Future Tests

Before any live implementation:

- Live disabled by default.
- No external navigation unless explicit test fixture.
- Action blocked without approval.
- CAPTCHA and 2FA route to human handoff.
- Credentials blocked.
- Redaction on live summaries.
- No protected scope changes.
- No system browser fallback.
- No Chrome Extension default fallback.
- No Safe Injection live unless separately approved.

## Decision

- `LIVE_CDP_DESIGN_ONLY: YES`
- `LIVE_CDP_IMPLEMENTATION_ALLOWED: NO`
- `SAFE_INJECTION_LIVE_ALLOWED: NO`
- `PRODUCTIVE_BROWSER_AUTOMATION_ALLOWED: NO`

Any live work requires a new human decision, new prompt, new ADR, new threat model, new tests, and new audit.
