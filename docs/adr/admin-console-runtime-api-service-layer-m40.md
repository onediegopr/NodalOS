# ADR M40: Admin Console Runtime/API Service Layer

## Status

Accepted for M40.

## Context

M38 created Admin Console UX/API contracts. M40 converts those contracts into an internal runtime service layer without exposing a public SaaS API, billing flow, real license emails, or external user activation.

## Decision

M40 introduces internal in-memory runtime services:

- `NexaAdminConsoleService`;
- `NexaAdminStateStore`;
- `NexaAdminAuditStore`;
- `NexaAdminPolicyService`;
- `NexaAdminQueryHandler`.

The store is fixture-first and replaceable by future persistence. It is intentionally not an HTTP API and not a public control plane.

## Command Execution

Every admin command follows:

1. Actor and role validation.
2. Role policy evaluation.
3. License/plan/feature compatibility checks.
4. Decision.
5. State mutation only when allowed.
6. Redacted audit event.
7. Redacted command result.

Viewer mutation, unknown roles, support secret access, worker plan administration, incompatible features, sensitive real pilot without compliance, productive vault without explicit entitlement/admin override, and productive recorder/replay are blocked.

## Query Model

The dashboard query returns:

- account summary;
- organization/workspace summary;
- workers/seats;
- license and plan status;
- feature flags;
- usage limits/counters;
- blocked capabilities;
- warnings;
- redacted audit summary.

It does not return secret values, vault raw data, cookies, session material, payment card data, document contents, or full sensitive paths.

## Audit

Admin mutations write redacted `NexaAdminAuditEvent` entries with actor, role, account, organization, action, decision, reason, timestamp, and redacted before/after summaries.

## Out of Scope

M40 does not implement:

- public SaaS API;
- production admin web app;
- real billing;
- payment gateway;
- real license emails;
- external user activation;
- sensitive real pilots.

## Consequences

The product/admin foundation now has an internal service layer that can be tested end-to-end before any public API or SaaS surface is built.
