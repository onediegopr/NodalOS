# ADR M68 - WebView2/CEF Architecture Decision

## Status

Accepted.

## Context

M68 evaluates whether ONE BRAIN/NEXA should support embedded browser runtimes in addition to the current Chrome/CDP external runtime. The decision is made before any production activation and after the safety, audit, vault, local API, and product/admin foundations were hardened.

## Decision

Chrome/CDP external remains the primary runtime.

WebView2 and CEF are allowed only as sandbox/design candidates. They must not replace Chrome/CDP, must not run sensitive sites, and must not become authoritative.

## Evaluation Criteria

- Safety and authority isolation.
- Profile control and cookie/session containment.
- Network metadata-only capability.
- Download/upload policy compatibility.
- Packaging and update burden.
- Enterprise deployment fit.
- Evidence/audit integration.
- Compatibility with Core/FSM/Safety.
- Risk of runtime authority leak.

## Tradeoff Summary

Chrome/CDP has the strongest existing evidence path, known metadata-only capture, and current gate coverage.

WebView2 is attractive for Windows packaging and controlled local shells, but requires careful profile/session containment and must remain sandbox-only until a future hito.

CEF may help cross-platform embedding, but adds packaging, update, and maintenance risk. It stays design-only.

## Consequences

- No productive WebView2/CEF activation.
- No replacement of Chrome/CDP.
- Embedded runtimes must pass the runtime abstraction compatibility gate before any future expansion.
- Core remains the only authority for Done/Verified decisions.

## Out Of Scope

- Installing WebView2/CEF packages.
- Navigating real sites.
- Sensitive sites.
- Credentials.
- Productive recorder/replay.
- Public SaaS/API.
