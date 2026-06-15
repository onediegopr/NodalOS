# ADR: Browser Human Handoff Companion Surface M10

## Status

Accepted for M10.

## Context

M9 introduced the formal credential boundary and human handoff contracts. The runtime can stop on password, login, OTP, CAPTCHA, clave fiscal, token prompts, sensitive submits and financial actions. The missing piece is a safe presentation/protocol surface so the user understands why automation stopped and how to proceed.

The Chrome extension remains a companion. It must not regain run ownership or mark success.

## Decision

M10 adds a handoff presentation and companion protocol contract:

- `BrowserHumanHandoffPresentation`
- `BrowserHumanHandoffDisplayState`
- `BrowserHumanHandoffUserOption`
- `BrowserHumanHandoffUiEvent`
- `BrowserHumanHandoffProtocolEnvelope`
- `BrowserHumanHandoffCompanionAdapter`

The companion surface may display:

- handoff id, run id, action id and correlation id;
- reason and display state;
- redacted title and URL;
- clear instruction;
- expected user action;
- allowed safe options;
- blocked unsafe options;
- expiry;
- evidence/proof refs;
- diagnostics.

All companion-originated messages are `authoritative:false`.

## Protocol Events

The formal events are:

- `handoff.created`
- `handoff.updated`
- `handoff.userCompleted`
- `handoff.cancelled`
- `handoff.expired`
- `handoff.resumeRequested`
- `handoff.resumeVerified`
- `handoff.resumeRejected`

`handoff.userCompleted` from the UI is a signal only. It does not mark success. Core must re-observe, verify and attach semantic proof/evidence before emitting a verified resume result.

## UI Contract

The minimal UI should explain:

- what sensitive boundary was detected;
- that NEXA stopped;
- what the user can do manually;
- that NEXA will re-observe and verify before continuing;
- how to cancel;
- how to copy diagnostics.

Example for password/login:

> Se detectó un paso sensible: contraseña/login. NEXA se detuvo para que lo completes manualmente. Cuando termines, presioná "Ya lo hice, continuar". NEXA volverá a observar la página y sólo continuará si puede verificar el estado.

Example for CAPTCHA:

> Se detectó CAPTCHA o verificación anti-bot. NEXA no intentará resolverlo automáticamente. Resolvelo manualmente en el navegador y luego presioná "Ya lo hice, continuar".

## Redaction

Presentation and protocol diagnostics must pass through credential redaction. Passwords, tokens, cookies, authorization headers, bearer values, OTP/code, clave fiscal, CUIT/DNI in sensitive context and sensitive query params must not be displayed or logged.

## Non-Goals

M10 does not implement:

- real login;
- credential storage or vault;
- CAPTCHA solving;
- real 2FA;
- real profile consent;
- full sidepanel redesign;
- WebView2/CEF;
- recorder, network capture, download/upload manager.

## Extension Boundary

The sidepanel/service worker may show handoff status and send user intent events such as `userCompleted` or `cancelled`. These events are non-authoritative. They cannot set `Verified`, cannot complete a run and cannot bypass Core/FSM/Safety/Evidence.

Success remains:

`UserCompleted + re-observation + verification Verified + semantic proof + evidence`

Anything else is pending, cancelled, expired, rejected or failed.
