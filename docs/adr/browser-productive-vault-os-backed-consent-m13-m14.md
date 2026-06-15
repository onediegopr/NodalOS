# ADR: Browser Productive Vault, OS-backed Provider Design, and Consent UI M13/M14

## Context

M12 created the Secret Boundary and Profile Real Consent model. M12.5 hardened that boundary by removing productive forced decisions, validating identifiers, canonicalizing the synthetic test vault, and requiring authority/proof/challenge for profile consent.

M13/M14 introduces productive vault contracts and consent presentation without enabling real secret storage, real credentials, real login, or real profile launch.

## Decision

Browser Runtime now has a productive vault abstraction:

- `BrowserProductiveVaultProvider`;
- provider kind/capability/policy/configuration contracts;
- storage, retrieval, use, rotation, deletion, and export operation contracts;
- vault consent contracts and presentation;
- audit events for every vault operation.

The implementation uses a safe route:

- `NullBrowserProductiveVaultProvider` denies/fails closed;
- `UnsupportedBrowserProductiveVaultProvider` fails closed;
- `WindowsDpapiSecretVaultProvider` is design-only and fails closed;
- `WindowsCredentialManagerSecretVaultProvider` is design-only and fails closed;
- `InMemoryTestOnlyProductiveVaultProvider` stores synthetic references only and never stores or returns secret values.

## Why M13/M14 Are Grouped

Vault storage and consent UI cannot be safely designed independently. The vault must know whether a scoped, authoritative consent exists, and the UI/Companion must be explicitly non-authoritative.

This hito defines both sides together, but it does not enable real credentials.

## DPAPI vs Windows Credential Manager vs External Vault

### DPAPI

Suitable for local OS-bound encryption, but it requires decisions about user/machine scope, backup, roaming, and recovery. It remains design-only.

### Windows Credential Manager

Suitable for OS-managed credential items, but it requires naming, scoping, lifecycle, and operator consent policy. It remains design-only.

### External Vault Future

Reserved for enterprise vaults. It is unsupported in M13/M14 and fails closed.

## Consent Separation

Consent types are intentionally separate:

- profile real consent does not authorize secret access;
- secret storage consent does not authorize secret use;
- secret retrieval consent does not authorize export;
- secret use consent does not authorize credential autofill;
- cookie access consent does not authorize password/token access;
- export is prohibited by default.

Every grant requires:

- trusted authority: Core, Admin, or TestHarness;
- approving actor;
- approval source;
- proof ref;
- challenge binding;
- expiry handling;
- non-companion authority.

Companion can send intent only. It cannot grant.

## Companion Surface

M13/M14 adds contract/presentation for consent:

- `BrowserVaultConsentPresentation`;
- `BrowserVaultUiEvent`;
- companion events are `authoritative:false`;
- companion events are redacted;
- companion events cannot be `Verified`;
- companion never receives secret values.

Physical sidepanel changes are deferred to avoid mixing with Browser-004.x legacy debt.

## Audit Trail

Every vault decision creates `BrowserSecretVaultAuditEvent` with:

- operation;
- provider kind;
- decision;
- secret reference metadata;
- consent id/proof ref if present;
- run/action/correlation/profile/session ids;
- timestamp;
- redacted summary.

Audit events never include secret values and validate IDs through `BrowserSafeIdentifierValidator`.

## Out Of Scope

- real credentials;
- real login;
- profile real launch;
- real cookies;
- DPAPI writes;
- Windows Credential Manager writes;
- AFIP, banks, ERP, or authenticated MercadoLibre;
- CAPTCHA solving;
- 2FA automation;
- WebView2/CEF;
- recorder;
- network capture;
- download/upload manager;
- export/session replay.

## Future Activation Criteria

Before enabling real OS-backed storage:

- implement explicit consent UI with Core authority;
- implement persistent audit ledger;
- bind consent to tenant/person/profile/session/portal;
- implement secure provider configuration;
- prove no secret values reach logs/evidence/Companion;
- add destructive operation approval and recovery model;
- pass fixture-only and synthetic-only tests.

## Percentages

After M13/M14:

- Browser Executor CDP read-only core-governed: 95%.
- Browser Runtime Layer productive completeness: 90%.
- ONE BRAIN global: 84%.
