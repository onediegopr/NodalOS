# ADR M42: Packaging and Diagnostics Foundation

## Status

Accepted for M42.

## Context

The product/admin foundation needs installability and support diagnostics before any public SaaS activation. M42 creates contracts and internal collectors only; it does not build a real installer.

## Decision

M42 adds package, readiness, health, diagnostics, and support-mode contracts and services:

- package manifest and components;
- environment readiness checks;
- health report;
- diagnostics bundle with manifest/hash;
- strict diagnostics redaction policy;
- metadata-only support mode;
- support bundle generation.

## Diagnostics Bundle

Diagnostics includes redacted sections for:

- environment summary;
- package manifest;
- health checks;
- feature flags;
- license status summary;
- admin/tenant summary;
- browser runtime status;
- vault provider health;
- recent audit summary;
- recent errors.

Diagnostics excludes:

- secret values;
- cookies;
- session material;
- request/response bodies;
- sensitive header values;
- payment data;
- document contents;
- full sensitive local paths;
- raw vault payloads.

## Support Mode

Support mode is metadata-only, read-only, time-limited, audited, and blocked from secrets, vault raw values, cookies, sessions, and cross-tenant access.

## Health Checks

Minimum checks cover runtime build info, licensing, admin runtime, tenant governance, audit export, browser gate, vault provider health, and diagnostics redaction.

## Out of Scope

M42 does not implement:

- a real installer;
- auto-update mechanism;
- public diagnostic upload;
- external storage;
- support impersonation;
- secret or session inspection.

## Consequences

Support and packaging can now be tested through safe bundles before public distribution work starts.
