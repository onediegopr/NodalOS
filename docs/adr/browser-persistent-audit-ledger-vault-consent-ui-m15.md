# M15 - Persistent Audit Ledger and Vault Consent UI

## Context

M13/M14 defined the productive vault and consent contracts, but intentionally left real secret storage disabled. Before enabling any OS-backed provider, ONE BRAIN needs two missing controls:

- a persistent audit ledger for vault, consent, handoff, policy, and redaction events;
- a physical Chrome Companion surface for vault/profile consent requests.

The service worker and sidepanel remain companion surfaces only. Core/FSM/Safety/Evidence remains authoritative.

## Decision

M15 adds a local persistent audit ledger and a minimal vault/profile consent UI in Chrome Companion.

The ledger stores redacted audit events, correlation metadata, decisions, reasons, timestamps, and integrity metadata. It does not store passwords, tokens, cookies, credential material, localStorage, IndexedDB, screenshots, or secret values.

The Companion UI can show consent requests and send user intent events. It cannot grant consent, mark success, emit `Verified`, emit `Done`, or become the final authority.

## Persistent Audit Ledger

The ledger supports events for:

- vault storage, retrieval, use, deletion, rotation, export attempts, provider selection, denial, and fail-closed decisions;
- consent requested, user approved intent, user denied intent, core grant/deny, expiry, and revocation;
- profile real consent requested, granted, and denied;
- human handoff creation, user completed intent, resume verified, and resume rejected;
- policy blocked and redaction applied.

Each persisted event includes:

- event id;
- event kind;
- sequence number;
- timestamp;
- redacted secret reference metadata where applicable;
- run/action/correlation/profile/session ids where applicable;
- decision and reason;
- provider kind where applicable;
- redaction status;
- previous hash;
- event hash.

## Integrity

M15 uses a lightweight tamper-evident chain:

- each event has a deterministic SHA-256 hash over its normalized payload;
- each event can include the previous event hash;
- changing the payload changes the event hash.

This is not a full cryptographic ledger or notarization system. It is sufficient for local regression protection and audit continuity until a stronger audit backend exists.

## Redaction

All ledger writes and Companion protocol messages are redacted before persistence or display. Secret-like identifiers and metadata are rejected or redacted using the existing browser secret boundary rules.

The ledger export is safe by construction: it serializes only redacted ledger events and revalidates events before writing.

## Chrome Companion Consent UI

The sidepanel now has a minimal consent surface for:

- vault configuration;
- future secret storage;
- future secret use;
- cookie/session access requests;
- real profile consent requests;
- deletion, rotation, revocation, and cancellation.

The UI exposes only user intent:

- `vaultConsent.userApproved`;
- `vaultConsent.userDenied`;
- `vaultConsent.cancelled`;
- `profileConsent.userApproved`;
- `profileConsent.userDenied`;
- `profileConsent.cancelled`.

Every Companion-originated consent message is:

- `authoritative:false`;
- `source:"chrome-companion"`;
- `runtimeKind:"core-governed-companion"`;
- `verificationStatus:"NotVerified"`;
- `redacted:true`.

## Out Of Scope

M15 does not implement:

- real secret storage;
- real secret retrieval;
- DPAPI provider activation;
- Windows Credential Manager provider activation;
- login automation;
- real profile launch;
- cookies from real profiles;
- AFIP, banks, MercadoLibre account flows, or any new real site;
- WebView2, CEF, recorder, network capture, download/upload manager, or export/replay.

## Why This Comes Before Vault Activation

Vault activation without persistent audit and visible consent would create an unsafe accountability gap. M15 closes that gap without enabling secret material handling.

## Risks

- The ledger is local and lightweight; it is not tamper-proof against an attacker with filesystem control.
- The Companion UI is intentionally minimal and does not replace a full consent UX.
- Physical Companion events are user intent only; Core must continue validating authority, proof, challenge, scope, expiry, and revocation.

## Next

M16 should focus on Download/Upload Manager, Network Capture, Export Session / Replay, and final hardening before enabling any real productive vault provider.

## Percentages

- Browser Executor CDP read-only core-governed: 95%.
- Browser Runtime Layer productiva completa: 92-93%.
- ONE BRAIN global: 85-86%.
