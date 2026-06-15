# ADR: Browser External Low-Risk Auth Live Target M25B

## Status

Accepted for M25B.

## Context

M25 created the external low-risk authentication contract, policy, read-only guard, and failure model. It did not close live external validation because no safe, test-owned external target was configured. M25B exists to make that distinction explicit and prevent false success.

## Decision

External live auth may only run against a target configured through `BrowserExternalLowRiskTargetConfig`, `BrowserExternalLowRiskTargetAllowlist`, and `BrowserExternalLowRiskCredentialBinding`.

A valid target must be:

- Explicitly allowlisted by host.
- Test-owned.
- Backed by a dedicated testing account.
- Free of personal, customer, commercial, fiscal, financial, or sensitive data.
- Free of payments and irreversible actions.
- Free of 2FA/CAPTCHA automation.
- Verifiable by read-only dashboard proof.

If no such target exists, M25B reports `BlockedNoSafeExternalTarget` and is not considered live-validated.

## Credential Binding

Credentials must be test/sandbox references from the vault boundary. Personal or commercial credentials are prohibited. Companion never receives values.

## Read-Only Guard

Post-login execution remains read-only. Mutations, submissions, uploads, payments, settings changes, deletes, publishes, and confirmations are blocked unless another milestone explicitly authorizes them.

## Out of Scope

- AFIP, banks, ERP, fiscal portals, payment providers.
- Personal Google/Gmail or company accounts.
- Customer accounts or accounts with real data.
- CAPTCHA/2FA automation.
- Sensitive request/response body capture.
- Sensitive header value capture.

## Consequences

M25B can close as blocked without target, or close as live-validated only when a test-owned external target is configured and verified. This prevents treating policy-only auth as real external auth.

