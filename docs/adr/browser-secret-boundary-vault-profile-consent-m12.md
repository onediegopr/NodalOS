# ADR: Browser Secret Boundary, Vault Policy, and Profile Consent M12

## Context

M9 established the Credential Boundary and Human Handoff model. M10 added the presentation contract, and M11 connected the Chrome Companion UI as a non-authoritative handoff surface.

M12 defines the next boundary: how secrets and real browser profiles will be referenced, authorized, audited, and denied by default.

## Decision

Browser Runtime introduces a formal Secret Boundary:

- secret values must not circulate as normal strings;
- runtime contracts use `BrowserSecretReference`, not raw values;
- secret access requires an explicit `BrowserSecretAccessRequest`;
- policy returns `BrowserSecretAccessDecision`;
- every access attempt emits `BrowserSecretAuditEvent`;
- unknown secret usage fails closed;
- diagnostics, evidence, handoff presentation, and companion protocol must be redacted.

## Vault Interface

M12 adds `IBrowserSecretVault` with two safe implementations:

- `NullBrowserSecretVault`: denies every request by default.
- `InMemoryTestSecretVault`: accepts only synthetic test secrets and returns references only.

There is no productive vault in M12. There is no DPAPI, Windows Credential Manager, cloud vault, or persistent credential storage yet.

## Secret References

A `BrowserSecretReference` contains metadata only:

- secret id;
- kind;
- scope;
- owner;
- portal;
- redacted label.

It does not contain the secret value.

## Chrome Companion Boundary

Chrome Companion must not receive secret values. It can show safe handoff instructions and send non-authoritative UI events, but it cannot request, store, or transmit passwords, tokens, cookies, OTPs, or clave fiscal values.

## Profile Real Consent

M12 adds a formal profile consent model:

- consent request;
- scoped consent;
- granted/denied/expired/revoked status;
- audit event.

Real profile consent does not authorize secret access. It only gates future real profile usage. Secret access still requires vault policy and audit.

M12 does not launch a real user profile. `BrowserProfileManager` can validate scoped consent, but real profile launch remains unsupported until a future milestone.

## Fail-Closed Rules

- unknown secret => fail closed;
- password/clave fiscal/bank credential => human handoff or approval;
- cookie/session access denied by default;
- missing consent => fail closed;
- expired/revoked consent => fail closed;
- user completed handoff does not create or store a secret.

## Out Of Scope

- real login;
- real credentials;
- real user profile launch;
- DPAPI / Windows Credential Manager;
- vault product implementation;
- AFIP/banks/ERP/authenticated sites;
- CAPTCHA solving;
- 2FA automation;
- WebView2/CEF;
- network capture;
- download/upload manager;
- export/replay;
- recorder.

## Future Work

- productive vault implementation;
- OS-backed secret storage;
- explicit user consent UI;
- real profile launch with scoped consent;
- authenticated-site smoke gates;
- deeper audit ledger persistence;
- vault-backed human handoff resume.
