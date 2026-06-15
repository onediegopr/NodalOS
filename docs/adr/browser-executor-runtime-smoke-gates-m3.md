# ADR: Browser Executor runtime smoke gates M3

## Status

Accepted for M3. This ADR documents repeatable smoke gates and diagnostics for the CDP-first Browser Executor introduced in M2.

## Context

M2 proved that the runtime can launch a controlled Chrome/Edge instance with CDP, use a temporary profile, observe a local fixture, dispatch trusted input through CDP, verify results separately from action execution, enforce idempotency, and clean up resources.

M3 turns that capability into repeatable runtime gates with structured diagnostics. It remains fixture-first and does not touch product flows, real sites, the side panel, or the MV3 service worker.

## Implemented Gates

1. **Control Plane / Launcher**
   - Launches Chrome/Edge with CDP on `127.0.0.1`.
   - Uses a disposable `onebrain-cdp-*` temporary profile.
   - Confirms process and endpoint health.

2. **Target Discovery**
   - Creates a fixture page target.
   - Confirms the target is discoverable and has a `TargetId`.

3. **TargetContext**
   - Builds a real `BrowserTargetContext`.
   - Confirms generation/liveness token validity.
   - Reports active/user-facing as limited where CDP does not provide a direct field.

4. **Observe**
   - Observes the local fixture.
   - Captures URL, title, visible text, actionables, timestamp, target context, and evidence refs.
   - Does not require the service worker.

5. **Act**
   - Executes `TypeText`, `Click`, and controlled `Wait`.
   - Confirms modifying actions require idempotency.
   - Confirms action execution is not verification.

6. **Verify**
   - Verifies an expected DOM text change.
   - Confirms impossible/ambiguous expectations become `Uncertain`, not done.

7. **Liveness / Stale**
   - Probes target liveness.
   - Forces a generation change through controlled fixture navigation.
   - Confirms stale target blocks modifying actions.

8. **Timeout / Cancel**
   - Runs a controlled wait timeout.
   - Confirms the suite does not hang and returns a classified failure.

9. **Idempotency / Replay Safety**
   - Rejects duplicate modifying actions.
   - Blocks same idempotency key with a different fingerprint.
   - Allows repeated read-only action.

10. **Cleanup**
    - Confirms no managed Chrome/Edge process remains.
    - Confirms no CDP port remains open.
    - Confirms no `onebrain-cdp-*` temporary profile remains.

## Diagnostics

M3 introduces:

- `BrowserRuntimeSmokeReport`
- `BrowserRuntimeGateResult`
- `BrowserRuntimeDiagnostic`
- `BrowserRuntimeHealthSnapshot`
- `BrowserRuntimeErrorCode`

The report includes gate status, runtime kind, browser executable identity without sensitive paths, CDP endpoint, port, profile mode, process id, target id, target URL/title, liveness status, evidence refs, cleanup status, timestamps, durations, and error classification.

## Error Classification

The minimum classification set is:

- `LauncherFailed`
- `CdpEndpointUnavailable`
- `TargetDiscoveryFailed`
- `TargetStale`
- `TargetDetached`
- `ActionRejected`
- `ActionTimeout`
- `VerificationFailed`
- `VerificationUncertain`
- `IdempotencyRejected`
- `CleanupFailed`
- `UnexpectedException`
- `EnvironmentUnsupported`

If Chrome/Edge is unavailable, gates report `EnvironmentUnsupported` instead of false green.

## Cleanup

The runner always disposes page/session in `finally`. `ChromeCdpBrowserSession.DisposeAsync` terminates the managed browser process tree and retries temporary profile deletion. The cleanup gate independently checks process id, CDP port, and temp profile path.

## Out of Scope

M3 does not:

- touch MercadoLibre or any external site;
- automate login, CAPTCHA, 2FA, banks, AFIP, or real portals;
- migrate the service worker;
- change UX or side panel;
- add product features;
- implement WebView2, CEF, download/upload, network capture, session export/replay, recorder, or profile manager;
- integrate deeply with FSM/Safety/Evidence.

## What This Prepares

M3 gives M4 a repeatable health gate for the Browser Executor before integrating it into FSM/Safety/Evidence. It also creates the diagnostic shape needed for future Browser Health Monitor and runtime smoke gates.

## Progress

Browser Executor / Browser Runtime Layer after M3: approximately 56%.

ONE BRAIN global after M3: approximately 67%.
