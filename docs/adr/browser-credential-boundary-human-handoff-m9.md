# ADR: Browser Credential Boundary and Human Handoff M9

## Status

Accepted for M9.

## Context

The Browser Runtime Layer can now execute read-only and fixture-first browser work through the Core-governed CDP executor. Before any authenticated or sensitive flows, the runtime needs a formal boundary for credentials and human intervention.

The system must fail closed around passwords, OTP/2FA, CAPTCHA, clave fiscal, tokens, account-private areas, financial actions, and sensitive submits. The executor must not type secrets, solve challenges, submit credentials, or treat human completion as success without re-observation and verification.

## Decision

M9 introduces formal contracts for:

- `BrowserCredentialBoundary`
- `BrowserCredentialSignal`
- `BrowserCredentialBoundaryDecision`
- `BrowserHumanHandoffRequest`
- `BrowserHumanHandoffContext`
- `BrowserHumanHandoffResumeToken`
- `BrowserHumanHandoffInstruction`
- `BrowserHumanHandoffResumeDecision`

The runtime detects credential signals from observations and actions. Strong signals such as password fields, OTP, CAPTCHA, clave fiscal, login submit, and financial actions produce `RequiresHuman` or `FailClosed`. Unknown sensitive prompts fail closed.

The FSM adapter accepts a credential boundary decision and stops before dispatching browser actions. It returns `RequiresHuman` with a formal handoff request when a boundary needs user intervention.

## What Is Detected

- login forms
- password fields
- OTP and two-factor prompts
- CAPTCHA prompts
- token prompts
- clave fiscal prompts
- credential submit controls
- payment and financial actions
- sensitive account/private data areas
- unknown sensitive prompts

## What Is Blocked

- automatic typing into password fields
- automatic OTP entry
- automatic CAPTCHA solving
- login submit without human handoff
- payment/financial action without human handoff
- unknown sensitive prompts

## Resume Rules

Human handoff can resume only if all are true:

- user status is `UserCompleted`;
- resume token is not expired;
- browser session remains alive;
- target/frame context is not stale;
- runtime re-observes after handoff;
- verification is `Verified`;
- verification includes semantic proof refs;
- evidence refs are available.

`UserCompleted` alone is not success.

## Redaction

M9 redacts secret-like values before diagnostics/evidence. This includes password, passwd, pass, secret, token, access token, refresh token, id token, API key, cookies, authorization, bearer, OTP/code, clave/clave fiscal, CUIT and DNI when they appear in sensitive context.

Screenshots of sensitive handoff contexts remain disabled by default. Credentials are not stored.

## Non-Goals

M9 does not implement:

- real login automation;
- CAPTCHA solving;
- real 2FA handling;
- AFIP, banks, ERP or authenticated MercadoLibre flows;
- real user profile usage;
- vault storage;
- full UI handoff;
- WebView2 or CEF;
- recorder, network capture, download/upload manager.

## Chrome Companion

The extension may later display handoff instructions and resume/cancel options, but it remains non-authoritative. Companion relay completion cannot mark the run successful. Core requires post-handoff observation and verification.

## Consequences

Authenticated flows are now structurally blocked until a human handoff path exists. This is intentional. The next layer can build UI/vault/profile consent on top of this boundary without weakening the Core invariants.
