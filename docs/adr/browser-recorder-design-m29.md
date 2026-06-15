# ADR: Browser Recorder Design M29

## Status

Accepted for M29.

## Context

The Browser Runtime needs a future recorder, but a script-like recorder would recreate unsafe automation: brittle selectors, hidden secrets, unverified actions, and executable replay. M29 defines the recorder as design-only.

## Decision

The recorder records recipe intent, not executable scripts. It captures:

- Semantic target descriptors.
- Preconditions.
- Risk classification.
- Policy requirements.
- Evidence requirements.
- Verification rules.
- Human approval gates.
- Idempotency requirements for modifying actions.

It must not store:

- Passwords, tokens, secrets.
- Cookies or authorization headers.
- Request/response bodies.
- Sensitive PII.
- Absolute local paths.
- Session values.

## Design-Only Guard

`BrowserRecorderDesign` is disabled by default and does not enable executable replay. `BrowserRecipeDraft` remains `DesignOnly`, and safe drafts require verification rules, redaction policy, approval policy, and versioning policy.

## Redaction

Recipe draft sanitization removes query strings, redacts secret-like values, minimizes URLs, and preserves semantic target names/selectors when safe.

## Out of Scope

- Product recorder.
- Executable replay.
- Auto-submit of risky actions.
- Capturing bodies, cookies, secrets, or PII.

## Consequences

M29 provides guardrails and contracts for M30 read-only recorder prototype without enabling a runnable recorder or product replay.

