# M22 - Profile Real Controlled Activation

## Context

The runtime needs a path toward real profile handling, but raw user profile launch remains too risky before vault real, login policy, cookie/session boundary review, and production hardening.

M22 therefore activates only controlled browser profiles under ONE BRAIN policy. It does not launch a personal Chrome profile and does not use sites requiring authentication.

## Decision

M22 introduces controlled profile activation:

- `BrowserControlledProfileMode`;
- `BrowserControlledProfileLifecycleState`;
- `BrowserControlledProfileActivationPolicy`;
- `BrowserControlledProfileActivationRequest`;
- `BrowserControlledProfileActivationResult`;
- `BrowserControlledProfileActivationService`.

The only activatable mode in M22 is `PersistentControlled`, backed by `BrowserProfileKind.PersistentControlled` under a ONE BRAIN controlled root directory.

## Profile Modes

- `Disposable`: default for fixture and CDP tests.
- `PersistentControlled`: allowed only with policy, gate, and consent.
- `UserProfileBlocked`: raw user profile is blocked by default.
- `UserProfileControlledWithConsent`: modeled in the gate but raw user profile launch remains disabled in M22.

## Activation Requirements

Controlled activation requires:

- valid consent for `ProfileControlledActivation`;
- profile scope;
- policy allowing persistent controlled profiles;
- phase gate passed;
- network metadata-only;
- body capture unsupported;
- sensitive header value capture unsupported;
- companion non-authoritative;
- no real vault active;
- audit event.

## Fail Conditions

Activation fails if:

- consent is missing;
- consent is expired;
- consent is revoked;
- scope is wrong;
- phase gate fails;
- companion is authoritative;
- real vault is active;
- network capture is unsafe;
- request/response bodies are supported;
- sensitive header values are supported;
- raw user profile is requested.

## Cookie And Session Boundary

M22 does not expose cookie or session material:

- no cookies in UI;
- no cookies in protocol;
- no cookies in logs;
- no cookies in audit;
- no Set-Cookie values;
- no Authorization values;
- no session storage values.

The activation result records `CookiesExposed=false` and `SessionStorageExposed=false`, and tests assert that audit data remains clean.

## Lifecycle

Controlled profiles can be:

- created;
- activated;
- in use;
- expired;
- revoked;
- cleanup requested;
- cleaned;
- blocked.

Cleanup can remove controlled temp artifacts when expiration or revocation requires it.

## Phase Gate Update

The M19 gate now distinguishes:

- raw user profile active: fail;
- controlled profile with valid consent: allowed if all other checks pass;
- controlled profile without valid consent: fail.

## Out Of Scope

M22 does not implement:

- vault real;
- login real;
- cookies real in logs/evidence/UI;
- raw user Chrome profile launch;
- AFIP, banks, ERP, MercadoLibre account flows;
- CAPTCHA or 2FA automation.

## Next

M22 enables M23/M24 planning for vault real minimum and stricter authenticated-flow preconditions, but not authenticated real sites yet.
