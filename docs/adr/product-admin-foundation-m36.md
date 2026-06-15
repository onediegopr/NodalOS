# ADR: Product/Admin Foundation M36

## Status

Accepted.

## Context

After M35A the browser runtime is mature enough for sandbox and simulation flows, but real sensitive pilots remain blocked. M36 opens the product/admin line without enabling real sensitive sites, payments, real credentials, raw profiles, or productive replay.

## Decision

Add product identity and admin governance contracts:

- `NexaProductAccount`
- `NexaProductAccountKind`
- `NexaOrganization`
- `NexaPersonAccount`
- `NexaCompanyAccount`
- `NexaWorker`
- `NexaSeat`
- `NexaWorkspace`
- `NexaRole`
- `NexaAccountStatus`
- `NexaAdminCapability`
- `NexaAdminRolePolicy`
- `NexaAdminAction`
- `NexaAdminDecision`

Add `NexaAdminPolicyEvaluator` for fail-closed role decisions.

## Identity Model

The product model supports:

- individual person accounts;
- company accounts;
- organizations;
- one or more workspaces;
- one or more workers/seats;
- worker association to a workspace;
- active, suspended, expired, trial, and free account states.

## Roles

Roles are:

- Owner: can manage account, plan, workers, configuration, allowed flows, read-only views, and audit.
- Admin: can manage workers/configuration where policy allows.
- Operator: can execute allowed flows.
- Viewer: read-only.
- Worker: executes under limits.
- Support: support access and read-only only.

Support does not see secrets. Unknown roles fail closed.

## Admin Audit

Every admin decision emits a redacted audit event with:

- actor;
- role;
- account/organization;
- action;
- decision;
- reason;
- timestamp;
- before/after redacted summaries.

Audit must not contain:

- secret values;
- cookies;
- session material;
- payment card data;
- sensitive document contents.

## What Remains Blocked

M36 does not implement:

- SaaS activation for external users;
- billing;
- payment gateway;
- real license emails;
- sensitive real pilots;
- real credentials;
- raw Chrome profile;
- irreversible submit/pay/sign/delete.

## Future Work

Product/admin persistence, UI, tenant provisioning, support workflows, billing, and external account activation remain future milestones.
