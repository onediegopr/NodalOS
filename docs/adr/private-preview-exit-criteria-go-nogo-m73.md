# ADR M73 - Private Preview Exit Criteria / Go-No-Go

## Status

Accepted.

## Context

M73 defines how NEXA decides whether a local private preview can continue or move to a next stage. It does not enable public SaaS, real billing, real email, or external sensitive workflows.

## Decision

A Go/No-Go report evaluates build, suite, gate, blockers, audit key custody, diagnostics redaction, tenant governance, private local API role enforcement, support metadata-only, billing/email sandbox status, public SaaS disablement, public API listener disablement, and explicit M51 status.

## Go Criteria

- Build OK.
- Suite OK.
- Gate OK.
- No critical/high security blockers.
- No unresolved release blockers.
- Audit key custody OK.
- Diagnostics redaction OK.
- Tenant governance OK.
- Private local API role enforcement OK.
- Support metadata-only.
- Billing/email mock or sandbox only.
- Public SaaS disabled.
- Public API listener disabled.
- M51 external proof status explicit.

## Decisions

The report can allow next local preview, recommend external target setup, or block on security, release, or missing evidence.

## Non-Goals

- No automatic SaaS activation.
- No real billing/email.
- No sensitive real pilot.
- No external target validation claim while M51 remains deferred.
