# ADR M70 - Runtime Abstraction Compatibility Gate

## Status

Accepted.

## Context

M70 defines a compatibility gate so that multiple browser runtime providers can be evaluated without weakening Core authority. This is required before any future embedded runtime work.

## Decision

Every runtime provider must declare capabilities and a safety profile. The gate allows Chrome/CDP as primary and marks WebView2/CEF as sandbox-only/design-only.

## Compatibility Requirements

A runtime fails compatibility if it:

- Attempts to be authoritative.
- Does not keep Core/FSM/Safety as authority.
- Lacks network metadata-only capability.
- Exposes cookies/session.
- Captures request/response bodies.
- Captures sensitive header values.
- Allows download/upload without policy.
- Allows submit/pay/sign/delete or other irreversible actions.
- Does not produce evidence refs.
- Replaces Chrome/CDP without a future decision.
- Runs embedded production mode.

## Capability Mapping

Capabilities are represented as supported, unsupported, design-only, sandbox-only, requires-gate, or requires-approval.

Mapped capabilities include read-only navigation, DOM read-only, network metadata-only, safe download, safe upload, controlled profile, vault boundary, recorder read-only, replay safe mode, sensitive simulation, and external read-only.

## Consequences

- Core remains authority.
- Embedded runtimes are not productive.
- Any future runtime must pass compatibility before integration.
- Gate/readiness can detect unsafe embedded runtime activation.

## Out Of Scope

- Public API changes.
- Real embedded runtime hosting.
- External site access.
- Credentials.
- Sensitive pilots.
