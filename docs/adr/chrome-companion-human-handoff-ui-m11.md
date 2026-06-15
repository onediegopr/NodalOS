# ADR: Chrome Companion Human Handoff UI M11

## Context

M9 introduced the formal Credential Boundary and Human Handoff contracts. M10 added the presentation and protocol models for `handoff.*` events, but did not wire the Chrome Companion UI to avoid mixing legacy Browser-004.x debt.

M11 connects the real Chrome Companion side panel to that protocol with a minimal, safe surface.

## Decision

The Chrome Companion can display handoff state and relay two user intents:

- `handoff.userCompleted`
- `handoff.cancelled`

Every event emitted by the companion is non-authoritative:

- `source = chrome-companion`
- `runtimeKind = core-governed-companion`
- `authoritative = false`
- `verificationStatus = NotVerified`
- no success evidence or proof is fabricated by the extension

The companion cannot mark a run as `Verified`, `Done`, or successful. `UserCompleted` only means the user claims to have completed the manual step. Core must re-observe, verify, and produce semantic proof/evidence before resuming.

## UI Scope

The side panel shows:

- safe reason
- safe/redacted URL
- instruction
- expected user action
- allowed options
- current display state

It exposes:

- "Ya lo hice, continuar"
- "Cancelar"
- "Copiar LOG"

The UI is intentionally minimal. It does not read password fields, solve CAPTCHA, handle 2FA, or store credentials.

## Redaction

Visible presentation and local diagnostics redact credential-like material, including password, token, bearer, authorization, cookie, OTP/code, and clave fiscal patterns. The companion must not log raw secrets.

## Why Companion Is Not Authority

The service worker was previously a source of false completion risk. M11 preserves the M5 decision: the extension is a companion, relay, and human-intervention surface only. Core/FSM/Browser Executor remains the source of truth.

## Out Of Scope

- real login
- CAPTCHA solving
- 2FA automation
- vault or credential storage
- real user profile
- authenticated sites
- full handoff workflow UI
- WebView2/CEF
- network capture
- download/upload manager

## Future Work

- Vault / Secret Boundary
- profile real consent flow
- richer handoff UI
- authenticated-site smoke gates
- replay/export session support
