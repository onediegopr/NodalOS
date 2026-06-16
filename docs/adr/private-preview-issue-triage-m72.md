# ADR M72 - Private Preview Issue Triage

## Status

Accepted.

## Context

Private preview local needs a consistent issue classification model before expanding the product surface. M72 formalizes triage for security, release, product, UX, runtime, vault, API, billing/email sandbox, diagnostics/audit, and documentation/runbook issues.

## Decision

Security blockers and release blockers are separated. Security blockers stop preview progression. Release blockers must be fixed before the next preview stage.

## Security Blockers

- Secret, cookie, or body leak.
- Cross-tenant access.
- Vault raw exposure.
- Support seeing secrets.
- Public API exposure.
- Real billing/email unexpectedly enabled.
- SensitiveRealPilot enabled.

## Release Blockers

- Build/test failure.
- Gate failure.
- Diagnostics unavailable.
- Audit integrity unavailable.
- Runbook missing.
- API role enforcement broken.

## Redaction

Triage decisions and actions are redacted. Reports must not include secret values, cookies, bodies, sensitive header values, vault raw values, payment data, or document contents.
