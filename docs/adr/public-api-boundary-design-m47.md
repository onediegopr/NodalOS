# ADR M47: Public API Boundary Design

## Status

Accepted for M47.

## Context

The product/admin foundation now has internal service layers, tenant governance, licensing, diagnostics, onboarding mock, configuration profiles, and release models. The next step is to define a future public API boundary without exposing a network service.

## Decision

M47 introduces design-only public API contracts:

- API boundary;
- endpoint catalog;
- operation categories;
- request/response DTO model;
- auth context;
- tenant context;
- decision model;
- audit event;
- auth, tenant, permission, and rate-limit policies.

API categories include Admin, Licensing, Onboarding, Diagnostics, AuditExport, RuntimeStatus, WorkflowRequest, FeatureFlags, and Support.

## DTO Rules

Public DTOs may contain IDs, redacted summaries, status, decisions, reason codes, audit refs, feature flags, limits, counters, and tenant-scoped metadata.

Public DTOs must never contain secret values, cookies, session material, request/response bodies, sensitive header values, raw vault data, payment card data, sensitive document contents, or full sensitive local paths.

## Boundary Rules

The boundary remains:

- DesignOnly;
- NotPubliclyExposed;
- NoNetworkListener.

No HTTP server, public port, SaaS endpoint, or external activation is introduced in M47.

## Auth, Tenant, Licensing

The evaluator fails closed for missing auth, unknown tenants, cross-tenant requests, unauthorized workers, disabled license features, sensitive capabilities without compliance, and support requests for secret-bearing data.

## Rate Limits

The rate-limit model supports per-tenant, per-worker, per-endpoint, per-plan burst, daily, and monthly limits. It only makes decisions; it does not expose a public service.

## Out of Scope

M47 does not expose an API publicly, start a listener, provide real SaaS activation, accept real external users, enable real billing, or bypass tenant/licensing controls.
