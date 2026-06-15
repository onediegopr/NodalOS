# M64 - Billing/Email Private Trial Simulation

## Status

Accepted.

## Context

Email and billing sandbox providers exist, but real charging and real email delivery remain prohibited. M64 simulates a private trial lifecycle to validate product/admin behavior locally without public SaaS activation.

## Decision

M64 defines a private trial simulation with states `Requested`, `AdminApproved`, `LicenseCreated`, `BillingPreviewCreated`, `EmailDraftQueued`, `Active`, `Expiring`, `Expired`, `Revoked`, and `Blocked`.

The simulation requires owner/admin approval, configurable trial features and limits, sandbox/mock billing, sandbox/mock email, and redacted audit events.

## Guardrails

- Real billing providers are blocked.
- Real email providers are blocked.
- `SensitiveRealPilot` is blocked.
- `ProductiveVault` is disabled by default.
- `RecorderProductive` and `ReplayProductive` are blocked.
- Public SaaS activation remains disabled.

## Out Of Scope

- Real charges.
- Real email delivery.
- Real payment cards.
- Real external users.
- Public SaaS activation.
