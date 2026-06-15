# ADR M38: Admin Console UX/API Contracts

## Status

Accepted for M38.

## Context

M36/M37 introduced product accounts, organizations, workers, roles, plans, feature flags, usage limits, and licensing governance. M38 formalizes the admin console contract layer without shipping a public SaaS admin UI, billing flow, or live customer activation.

## Decision

The admin console is represented by redacted view models and command/query contracts:

- dashboard, account, organization, worker, license, feature flag, usage, and audit view models;
- create/update account, worker management, role update, license assignment, feature flag, usage limit, suspend account, and audit query commands;
- command results with policy decisions and audit references.

The API contracts always include actor, role, target account/organization, requested action, decision, and audit references. Mutating commands are evaluated by admin role policy and fail closed for unknown roles.

## UX Safety

The console must show:

- plan and license status;
- enabled and blocked features;
- usage counters and limits;
- expiration and warnings;
- blocked capability reasons;
- redacted audit summaries.

The console must not expose:

- secret values;
- cookies or session material;
- payment card data;
- sensitive document contents;
- full sensitive local paths.

Sensitive features are disabled by default. `SensitiveRealPilot`, `ProductiveVault`, `RecorderProductive`, and `ReplayProductive` appear as blocked unless a future controlled policy explicitly permits them. `ProfileRaw` is not modeled as an enableable feature.

## Admin Policy

Base roles remain:

- Owner;
- Admin;
- Operator;
- Viewer;
- Worker;
- Support.

Viewer cannot mutate. Support cannot view secrets. Productive recorder/replay cannot be enabled from M38 contracts. Sensitive real pilot requires compliance approval and still remains blocked by broader runtime gates until a future milestone.

## Audit

Admin actions produce redacted audit views with actor, role, account, organization, action, decision, reason, timestamp, and redacted before/after summaries. Audit views reject secret-like content.

## Out of Scope

M38 does not implement:

- production web admin UI;
- SaaS public activation;
- real billing or payment gateway;
- real license emails;
- customer data onboarding;
- sensitive real pilots.

## Consequences

Product/admin now has stable UI/API contracts that can be wired to a future admin console without weakening browser-runtime safety gates.
