# M67 - Private Local API Role And Leak Hardening

## Status

Accepted.

## Context

The post-adjustment audit found three hardening gaps: `NexaPrivateLocalApiRoute.MinimumRole` was declared but not enforced, the skipped-tests audit report was stale, and M52 leak-hardening checked hand-built safe artifacts instead of real service outputs.

## Decision

M67 applies explicit role hierarchy enforcement in the private local API, synchronizes the skipped-tests audit list to the current 29 known skipped tests, and adds real-surface leak-hardening for outputs produced by M53-M63 services.

Role hierarchy is:

- Owner satisfies all non-prohibited roles.
- Admin satisfies Admin, Operator, Worker, and Viewer.
- Operator satisfies Operator, Worker, and Viewer.
- Worker satisfies Worker and Viewer.
- Viewer is read-only.
- Support is metadata-only and cannot access non-support-safe routes or secrets.
- Unknown fails closed.

## Leak Hardening Scope

Real-surface checks cover private preview, feedback/session summary, private local API responses, API diagnostics, email sandbox/outbox, billing sandbox ledger, diagnostics bundle, support bundle, audit export, local product shell, installer/release reports, and pre-production checkpoint reports.

## Out Of Scope

M67 does not introduce public API exposure, real billing, real email, real users, external targets, sensitive sites, real customer credentials, or ProductiveVault with real secrets.
