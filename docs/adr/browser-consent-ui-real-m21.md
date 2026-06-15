# M21 - Browser Consent UI Real

## Context

Previous milestones created consent and handoff contracts, but M21 makes consent a real, renderable, testable surface. Consent must be understandable to an operator and auditable by Core.

Consent remains a permission to evaluate policy later. It is not action authorization.

## Decision

M21 introduces a generic browser consent model:

- `BrowserConsentRequest`;
- `BrowserConsentGrant`;
- `BrowserConsentScope`;
- `BrowserConsentUiModel`;
- `BrowserConsentDecision`;
- `BrowserConsentAuditEvent`;
- `BrowserConsentService`.

The UI model is companion-safe and non-authoritative. It can be rendered by Chrome Companion or another local surface without exposing secrets or becoming the source of truth.

## What The User Sees

The UI model includes:

- requested capability;
- scope;
- purpose;
- TTL and expiration;
- requester;
- human-readable explanation;
- risks;
- what remains blocked;
- revoke instructions;
- options: approve, deny, revoke, copy diagnostic log.

It explicitly says that consent does not authorize sensitive actions automatically.

## What The User Does Not See

The consent surface must not show:

- secret values;
- cookies;
- tokens;
- authorization headers;
- sensitive paths;
- request or response bodies;
- profile storage internals.

All fields are redacted before presentation and audit.

## Expiration And Revocation

Consent grants:

- expire by TTL;
- can be revoked;
- can be queried by capability and scope;
- emit audit events;
- block policy evaluation when expired or revoked.

## Handoff Integration

`RequiresHuman` can request consent, but:

- `UserApprovedConsent != StepDone`;
- `ConsentGranted != ActionAuthorized`;
- resume still requires Core policy, re-observation, verification, and proof.

## Out Of Scope

M21 does not implement:

- vault real;
- profile raw launch;
- login real;
- credential capture;
- companion authority;
- automatic sensitive actions.

## Next

M21 enables M22 controlled profile activation under policy and gate checks.
