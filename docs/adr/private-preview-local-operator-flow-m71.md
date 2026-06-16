# ADR M71 - Private Preview Local Operator Flow

## Status

Accepted.

## Context

M71 defines the local operator flow for NEXA private preview. The system is mature enough for local, single-tenant operation, but not for public SaaS, real billing, real email, external user activation, or sensitive real sites.

## Decision

The operator flow is local-only and in-process. It chains session start, profile/config validation, mock license validation, tenant/workspace validation, local shell model access, diagnostics, feature flag review, audit integrity check, private local API usage, summary generation, and safe session close.

## Safety Rules

- Local machine only.
- Single tenant.
- Synthetic data only.
- Mock billing/email only.
- Private local API in-process only.
- Diagnostics and audit refs only.
- No public SaaS.
- No real billing/email.
- No SensitiveRealPilot.
- No productive recorder/replay.
- M51 remains explicit and deferred for external mode.

## Out Of Scope

- Public SaaS.
- External users.
- Real credentials.
- Real external sites.
- Billing/email production.
- Embedded runtime production.
