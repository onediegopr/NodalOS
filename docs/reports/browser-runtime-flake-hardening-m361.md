# Browser Runtime Flake Hardening M361

## Problem

Browser/CDP smoke tests intermittently failed in full-suite runs after all functional gates passed. The failing area was cleanup: managed browser process, CDP port or temporary profile appeared to remain briefly after dispose.

## Probable Root Cause

The cleanup assertion was more eager than the environment. The smoke runner performed a one-shot process/port/profile check after disposal, while Windows and Chrome can release sockets/profile locks slightly later. WebSocket close also lacked cleanup-specific exception handling.

## Implemented Changes

- Added `BrowserRuntimeSmokeCleanupProbe`.
- Added bounded polling for process, CDP port and profile cleanup state.
- Re-attempted profile deletion only for owned `%TEMP%\onebrain-cdp-*` directories.
- Preserved owned-process-only cleanup.
- Hardened `ChromeCdpPageSession.DisposeAsync` with bounded WebSocket close and cleanup-only exception filtering.
- Made Gate 10 cleanup diagnostics component-specific.

## Diagnostics Added

Gate 10 now reports:

- process outcome;
- profile delete outcome;
- CDP port outcome;
- WebSocket close outcome;
- leftover process flag;
- profile directory existence flag;
- CDP port open flag;
- redacted profile leaf;
- sanitized cleanup warnings.

## Tests Added Or Updated

Updated `BrowserRuntimeSmokeTests` with:

- `BrowserRuntimeSmokeCleanup_IsIdempotent`
- `BrowserRuntimeSmokeCleanup_ReportsProfileDeleteOutcome`
- `BrowserRuntimeSmokeCleanup_ReportsProcessOutcome`
- `BrowserRuntimeSmokeCleanup_DoesNotKillUnownedBrowserProcesses`
- `BrowserRuntimeSmokeUsesUniqueProfilePathPerRun`
- `BrowserRuntimeSmokeCleanupHandlesAlreadyExitedProcess`
- `BrowserRuntimeSmokeCleanupHandlesMissingProfileDirectory`
- `BrowserRuntimeSmokeDiagnosticsDoNotContainSecrets`

Added `NodalOsBrowserRuntimeFlakeHardeningM359M361Tests` for M361 report/artifact flags.

## Repeated Runs

- Specific smoke filter run 1: `17 passed`, `0 failed`.
- Specific smoke filter run 2: `17 passed`, `0 failed`.
- Specific smoke filter run 3: `17 passed`, `0 failed`.
- Browser/smoke related filter: `261 passed`, `11 skipped`, `0 failed`.
- Final browser/smoke related filter after artifact tests: `264 passed`, `11 skipped`, `0 failed`.
- Full suite: `2911 passed`, `37 skipped`, `0 failed`.

## Known Limits

No known remaining environment limit was reproduced after the hardening runs. The cleanup probe remains bounded and diagnostic rather than infinite.

## Recommendation

Proceed to `M362-M364 Verification Before Done Gate`.

Decision: `M359+M360+M361 CERRADO / BROWSER_RUNTIME_FLAKE_HARDENED`.
