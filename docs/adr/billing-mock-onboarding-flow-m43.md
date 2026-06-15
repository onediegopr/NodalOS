# ADR M43: Billing Mock and Onboarding Flow

## Status

Accepted for M43.

## Context

The product track needs onboarding and commercial flow modeling, but real billing, payment providers, emails, and public SaaS activation remain prohibited.

## Decision

M43 introduces mock-only onboarding, billing, and email outbox services:

- Free plan signup mock;
- admin-approved Trial mock;
- Pro and Enterprise interest/mock checkout;
- mock billing provider;
- invoice preview;
- mock email outbox drafts;
- onboarding audit events.

## Billing

The only allowed provider is `MockOnly`. Real Stripe, Redsys, PayPal, bank, or manual real charge providers are explicitly modeled as prohibited and not implemented.

The mock provider creates invoice previews only and never creates a real charge.

## Email

Email delivery is `MockOutboxOnly`. The outbox stores drafts only and never sends real email.

## Free and Trial

Free:

- requires email;
- allows one Free license per email window;
- expires after 7 days;
- uses strong limits;
- disables SensitiveRealPilot, ProductiveVault, RecorderProductive, and ReplayProductive.

Trial:

- requires admin approval;
- supports custom features and limits;
- is revocable;
- is audited.

## Out of Scope

M43 does not implement:

- real billing;
- payment gateway;
- real emails;
- public SaaS signup;
- external user activation;
- customer credentials;
- sensitive sites;
- productive recorder/replay.

## Consequences

The commercial lifecycle is now testable without money movement, external users, or outbound email.
