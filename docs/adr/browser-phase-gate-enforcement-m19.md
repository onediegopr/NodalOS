# M19 - Browser Runtime Phase Gate Enforcement Real

## Context

M16 introduced `BrowserRuntimePhaseCloseGate` as a useful checklist, but it still accepted critical booleans from the caller:

- companion authoritative;
- real profile active;
- real vault active;
- login real active;
- service worker/legacy runner state.

That is not strong enough for a phase gate. A caller should request evaluation, not declare that the runtime is safe.

## Decision

M19 introduces observed runtime state and probe-based evaluation:

- `BrowserRuntimeObservedState`;
- `BrowserRuntimeCapabilityState`;
- `BrowserRuntimePhaseGateProbeResult`;
- `IBrowserRuntimeSecurityProbe`.

The new gate overload receives a probe and derives the gate result from observed state plus runtime artifacts. The old flag-based overload is marked obsolete and retained only for M16 compatibility tests.

## Required Observed State

The gate observes or derives:

- companion authority;
- legacy runner state;
- real profile activity;
- real vault activity;
- real login activity;
- network capture mode;
- request/response body capture support;
- sensitive header value capture support;
- executable replay state;
- download/upload mode;
- target/frame manager health;
- audit ledger integrity provider;
- audit ledger head seal availability and validity;
- CDP live proof availability;
- Browser-004.x legacy isolation.

## Fail Conditions

The gate fails if any of these are observed:

- companion authoritative;
- legacy runner enabled;
- real profile active;
- real vault active;
- real login active;
- network capture not metadata-only;
- request or response bodies supported;
- sensitive header values supported;
- executable replay enabled;
- audit ledger not HMAC-backed;
- missing or invalid audit head seal;
- unhealthy target/frame manager;
- missing M18 CDP live proof;
- Browser-004.x legacy mixed into runtime.

## Pass Conditions

The gate passes only when observed runtime state confirms:

- companion is non-authoritative;
- legacy runner is disabled/isolated;
- no real profile, vault, or login is active;
- network capture is metadata-only;
- body capture and sensitive header value capture are unsupported;
- replay is diagnostic-only;
- audit ledger uses HMAC integrity with a valid head seal;
- target/frame manager is healthy;
- M18 CDP live proof is available.

## What Remains Blocked

Passing M19 does not enable:

- vault real;
- profile real;
- login real;
- cookies real;
- AFIP, banks, ERP, or sensitive external sites;
- CAPTCHA or 2FA automation;
- request/response body capture;
- executable replay;
- companion authority.

## Next Step

M19 enables planning for M21/M22 enforcement and future controlled runtime work. It does not authorize productive credential or profile flows.
