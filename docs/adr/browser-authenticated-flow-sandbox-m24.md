# ADR: Browser Authenticated Flow Sandbox M24

## Status

Accepted for local sandbox proof only.

## Context

M18 proved CDP live read-only against a local fixture. M21/M22 added visible consent and controlled profile activation. M23 adds a sandbox vault boundary. M24 connects these pieces in a local authenticated flow without enabling real sites or real credentials.

## Decision

M24 adds `BrowserAuthenticatedSandboxScenario` and a test-only local HTTP fixture with:

- `/login`
- `/session`
- `/dashboard`
- `/logout`

The flow uses CDP live against `127.0.0.1`, controlled profile activation, scoped consent, the minimal sandbox vault, the phase gate, and semantic verification. The scenario returns success only when execution is followed by verified dashboard proof.

## Flow

1. Core requests consent.
2. Core validates policy and phase gate.
3. Controlled profile is activated under a managed root.
4. Sandbox vault retrieves values only through an internal core boundary.
5. CDP fills the local fixture form.
6. The fixture sets a synthetic local session cookie.
7. Core verifies dashboard text and semantic proof refs.
8. Audit/evidence are emitted without values.
9. Cleanup removes controlled test artifacts.

## Safety Rules

- No external sites.
- No AFIP, banks, ERP, or MercadoLibre account flows.
- No real credentials.
- No raw/personal Chrome profile.
- No request/response bodies captured.
- Sensitive headers remain presence-only.
- Cookie and session values never leave the browser/profile boundary.
- `Executed` is not success.
- `HTTP 200` is not success.
- Session cookie presence is not success.
- Step done requires `Verified + semantic proof`.

## Companion Boundary

Companion remains non-authoritative. It does not receive values, cannot mark success, cannot mark verified, and cannot complete the flow without Core verification.

## Why This Does Not Authorize External Auth

The sandbox proves wiring and boundaries only. External authenticated sites require separate policy, threat model, site-specific risk review, consent UX, credential operations, and live safety audit.

## Future Work

M25/M26 may consider a low-risk external authenticated proof only after post-M24 audit confirms no value leaks, no authority drift, and no unsafe network/body/header capture.
