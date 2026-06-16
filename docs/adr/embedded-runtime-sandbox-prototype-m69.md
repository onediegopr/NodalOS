# ADR M69 - Embedded Runtime Sandbox Prototype

## Status

Accepted.

## Context

M69 creates a minimal embedded runtime sandbox prototype without adding a real WebView2 or CEF dependency. The goal is to model the safety boundary and evidence shape, not to replace the current browser runtime.

## Decision

The prototype is fixture-first and model-only. It models WebView2/CEF behavior against local fixture routes:

- `/embedded/status`
- `/embedded/readonly`
- `/embedded/metadata`

The semantic proof is `NEXA_EMBEDDED_RUNTIME_SANDBOX_OK`.

## Safety Rules

- Disabled by default.
- Local fixture only.
- No external sites.
- No production activation.
- No cookies/session exposure.
- No request/response bodies.
- No sensitive header values.
- Downloads disabled by default.
- Uploads disabled by default.
- Runtime cannot be authoritative.
- Chrome/CDP remains primary.

## Evidence

The sandbox can produce evidence refs for semantic proof. These refs are model evidence only and cannot be used to claim productive embedded runtime readiness.

## Out Of Scope

- Real WebView2 process hosting.
- Real CEF integration.
- Real browsing.
- Real download/upload.
- Sensitive simulations.
- External target validation.
