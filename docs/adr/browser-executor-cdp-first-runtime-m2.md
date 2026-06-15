# ADR: Browser Executor CDP-first runtime M2

## Status

Accepted for M2. This ADR documents the first real CDP Browser Executor implementation inside the local runtime layer. It is fixture-first and intentionally not connected to the existing Chrome Lab product flow.

## Context

M1 introduced formal Browser Executor contracts for target context, observations, actions, verification, evidence, liveness, idempotency, capabilities, and Chrome launcher policy.

M2 validates that those contracts can back a real executor without depending on the MV3 service worker as the execution brain.

## Decision

Add `OneBrain.BrowserExecutor.Cdp` as an isolated runtime module.

The M2 executor:

- launches Chrome/Edge with CDP bound to `127.0.0.1`;
- uses a temporary disposable profile by default;
- creates and connects to CDP page targets;
- observes a local HTML fixture;
- performs safe fixture actions through CDP input events;
- verifies postconditions separately from action execution;
- emits minimal non-sensitive evidence records;
- probes target liveness with `BrowserHeartbeat`;
- blocks stale target actions;
- enforces idempotency for modifying actions;
- closes Chrome and deletes the temporary profile on dispose.

## What M2 Does Not Do

M2 does not:

- automate MercadoLibre or any real external site;
- use a real user profile;
- touch credentials, login, CAPTCHA, 2FA, banks, or government portals;
- migrate the service worker;
- change side panel UX;
- implement M3 smoke gates;
- integrate deeply with `SafeExecutionFsm` or `EvidenceLedger`;
- implement WebView2, CEF, or Chrome Extension Runtime.

## Launcher Policy

Chrome is launched with:

- `--remote-debugging-address=127.0.0.1`;
- an allocated localhost CDP port;
- `--user-data-dir` pointing to a `onebrain-cdp-*` temp directory;
- sync, extension loading, component updates, first-run, and default-browser prompts disabled.

The test executor uses headless Chrome by default. Real profile use remains blocked until an explicit consent flow exists.

## Fixture-First Scope

The fixture lives under `tests/fixtures/browser-executor/basic-form.html` and covers:

- navigation;
- title/text observation;
- actionable element extraction;
- input typing;
- button clicking;
- DOM mutation verification;
- iframe presence for frame counting.

This avoids external network dependence and avoids user data.

## Verification Rule

M2 preserves the M1 rule:

```text
Executed != Verified
Uncertain != Done
```

`ChromeCdpActionResult.Executed=true` only means the CDP action was dispatched. Completion still requires `BrowserVerification.Status == Verified`.

## Evidence

M2 emits minimal `BrowserEvidence` records for action metadata. Evidence is non-sensitive and uses redaction flags. Screenshots are intentionally omitted.

## Cleanup

`ChromeCdpBrowserSession.DisposeAsync` kills the managed Chrome process tree and retries temp profile deletion. Tests assert no managed process remains and the temp profile path is removed.

## Risks

- CDP event routing is still minimal and synchronous.
- Target visibility/active-window semantics are not complete.
- Browser process cleanup is best-effort on hostile OS conditions.
- The executor is not yet wired to the core run FSM.
- The current extension-first path still exists as technical debt.

## Next Milestones

- M3: runtime smoke gates and executor diagnostics.
- M4: integrate Browser Executor outcomes with FSM/Safety/Evidence.
- M5: start extracting execution ownership from the MV3 service worker.
