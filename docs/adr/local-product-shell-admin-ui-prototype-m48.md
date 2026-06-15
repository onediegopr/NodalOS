# ADR M48: Local Product Shell / Admin UI Prototype

## Status

Accepted for M48.

## Context

The platform now has product/admin, licensing, tenant governance, diagnostics, packaging dry-run, configuration profiles, release models, and a public API boundary design. It still must not expose public SaaS or a public API.

## Decision

M48 introduces a local product shell render model for an admin UI prototype. It is local-only, in-memory/model-first, redacted, tenant-scoped, role-filtered, license-aware, and feature-aware.

The shell defines routes for:

- dashboard;
- accounts;
- organization;
- workers;
- licenses;
- features;
- usage;
- diagnostics;
- support;
- audit;
- release;
- configuration;
- readiness.

## Allowed Actions

The prototype can view dashboard, license, feature flags, diagnostics, audit export summary, release status, configuration profile, and request mock onboarding or mock diagnostics/export actions.

## Blocked Actions

The shell blocks enabling SensitiveRealPilot, RecorderProductive, ReplayProductive, ProductiveVault without entitlement/admin override/gate, real billing, real email, public SaaS activation, and real deploy/update.

## Data Safety

Render models must not expose secret values, cookies, session material, raw vault payloads, payment data, document contents, request/response bodies, sensitive header values, or full sensitive local paths.

## Out of Scope

M48 is not a final web UI, not SaaS, not a public API, and does not perform real deploy, billing, email, sensitive automation, or update execution.
