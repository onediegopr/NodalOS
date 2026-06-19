# Browser Runtime Flake Root Cause M359

## Affected Tests

- `BrowserRuntimeSmokeRunnerExecutesAllGatesOnFixture`
- `BrowserRuntimeSmokeReportContainsStructuredDiagnostics`

## Observed Symptom

The full suite intermittently failed in Gate 10 cleanup while isolated reruns passed. The repeated failure shape was:

- all functional smoke gates passed;
- cleanup gate failed;
- structured diagnostics reported `CleanupCompleted=false`;
- isolated rerun passed.

## Root Cause Hypothesis

The likely root cause was a cleanup race around CDP/browser teardown:

- the smoke runner checked process, CDP port and profile directory immediately after disposing page/session;
- Windows can keep the CDP port or temporary profile locked briefly after browser process termination;
- `ChromeCdpPageSession.DisposeAsync` could throw during WebSocket close if the socket was already closed/aborted, preventing deterministic session cleanup from being the only cleanup concern;
- the final cleanup diagnostic did not identify which component remained live.

## Evidence Found

- `BrowserRuntimeSmoke.cs` created Gate 10 based on a one-shot `CleanupLooksComplete(processId, port, profileDir)`.
- `ChromeCdpPageSession.DisposeAsync` closed the WebSocket with `CancellationToken.None` and no cleanup exception filter.
- `ChromeCdpBrowserSession.DisposeAsync` killed only the launched process PID, which is the correct ownership boundary.
- `BrowserProfileManager.CleanupProfileAsync` already retried profile deletion, but Gate 10 still did not retry the post-dispose state check.

## Changes Proposed

- Add a bounded cleanup probe after page/session dispose.
- Retry observation of process, port and profile state without long sleeps.
- Re-attempt deletion only for owned temporary profiles named `onebrain-cdp-*` under `%TEMP%`.
- Keep process cleanup owned-PID only.
- Harden WebSocket cleanup so socket-close exceptions do not prevent session cleanup.
- Add structured diagnostics for process/profile/port/WebSocket outcomes.

## What Will Not Be Touched

- Browser action behavior.
- CDP command semantics.
- Runtime policy.
- OCR.
- Agent Operations domain.
- UI.
- Recipe execution.
- Orchestration.

## Risk Of Hiding Bugs

This change does not retry action, observation, verification or idempotency gates. It only waits bounded time for cleanup state to converge and reports detailed cleanup state. Functional failures remain failures.

## Hardening Plan

1. Add `BrowserRuntimeSmokeCleanupProbe`.
2. Use it in Gate 10 after page/session disposal.
3. Catch only cleanup-specific WebSocket/IO/ObjectDisposed exceptions during cleanup.
4. Add unit tests for idempotency, missing profiles, profile delete outcome, process outcome, no user-process kill and secret-free diagnostics.
5. Run repeated smoke filters and full suite.
