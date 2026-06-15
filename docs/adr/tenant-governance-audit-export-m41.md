# ADR M41: Tenant Governance and Audit Export

## Status

Accepted for M41.

## Context

Product/admin now needs multi-tenant separation and safe audit export before any public SaaS, billing, or customer onboarding work. M41 formalizes tenant governance and exportable redacted audit data.

## Decision

M41 introduces:

- `NexaTenant`;
- tenant boundaries and scopes;
- tenant data classification;
- strict tenant policy;
- `NexaTenantGovernanceEvaluator`;
- audit export request/result/manifest contracts;
- `NexaAuditExportService`.

## Tenant Isolation

The evaluator blocks:

- cross-account access;
- cross-organization access;
- worker access to another workspace;
- support access to sensitive or secret data;
- viewer mutation;
- unknown/invalid tenant ids;
- secret access without explicit policy.

Support mode is metadata-only and redacted.

## Audit Export

Audit export supports JSON and JSONL. Each export includes:

- export id;
- scope;
- time range;
- actor;
- account/organization/workspace;
- event count;
- redaction policy id;
- SHA256 hash;
- source audit refs;
- redacted events.

Export excludes:

- secret values;
- cookies;
- session values;
- request/response bodies;
- sensitive header values;
- payment data;
- document contents;
- full sensitive local paths.

## Integrity

M41 uses a deterministic SHA256 hash over the redacted export payload. This is compatible with future HMAC sealing but does not replace the M17 persistent ledger HMAC chain.

## Gate Integration

Runtime phase gate now tracks:

- admin runtime service defined;
- tenant governance defined;
- audit export defined;
- audit export redacted;
- cross-tenant isolation enabled;
- support cannot view secrets;
- sensitive features require tenant policy.

The gate fails if audit export leaks secrets, cross-tenant isolation is disabled, support can view secrets, or sensitive features are enabled without tenant policy.

## Out of Scope

M41 does not implement:

- external storage;
- public audit API;
- customer-facing export portal;
- billing or payment data export;
- real SaaS activation;
- sensitive real site pilots.

## Consequences

The product/admin track can now reason about account, organization, workspace, and worker separation with testable audit exports before any public SaaS rollout.
