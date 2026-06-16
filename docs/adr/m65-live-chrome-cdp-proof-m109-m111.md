# M109-M111 - M65 Live Chrome/CDP Proof

## Status

Live Chrome/CDP proof executed with explicit opt-in.

## Target

- Target: `https://lab.nodalos.com.ar`
- Allowed routes: `/`, `/health/`, `/ownership/`, `/products/`, `/document/`, `/report/`
- Policy-blocked routes: `/disabled-form/`, `/blocked-login/`, `/blocked-checkout/`, `/blocked-destructive-action/`

## Opt-In

Live Chrome/CDP proof must not run by default.

Supported opt-in variables:

- `NODAL_OS_EXTERNAL_LIVE_PROOF_OPT_IN=true`
- `NEXA_EXTERNAL_LIVE_PROOF_OPT_IN=true` as a temporary legacy alias

## Execution Rules

The proof is valid only when:

- Chrome/CDP is launched from an available executable.
- The browser uses an isolated temporary profile.
- No personal/default Chrome profile is used.
- No credentials, saved passwords, personal extensions, cookies, tokens, sensitive headers, full DOM, or response body are persisted.
- Navigation remains read-only and limited to the allowlisted target host and paths.
- Blocked routes are recorded as policy-blocked and are not mutated.
- Evidence is redacted and persisted to the `BrowserPersistentAuditLedger`.

## Evidence

Successful live proof must emit:

- `ProbeKind=RealChromeCdp`
- `Tooling=ChromeCdpExternalReadOnly`
- Capabilities: `BrowserNavigationReadOnly`, `DomSnapshotReadOnly`, `PageMetadataReadOnly`, `NetworkMetadataRedacted`, `CoreGoverned`
- `PersistenceStatus=PersistedRedactedLedger`
- `LedgerRef` and ledger hash metadata

Observed opt-in run:

- Opt-in: `NODAL_OS_EXTERNAL_LIVE_PROOF_OPT_IN=true`
- Status: `PassedReadOnlyProof`
- `ProbeKind=RealChromeCdp`
- `Tooling=ChromeCdpExternalReadOnly`
- `PersistenceStatus=PersistedRedactedLedger`
- `LedgerRef=audit-ledger-edb3e2fbb0a0446788dae17a269c0058`
- `LedgerHash=61f52af1eebf08d59a24e5fbb72e70acf0038e7a329bff6599a0ac00c757f03e`
- Routes visited: `/`, `/health/`, `/ownership/`, `/products/`, `/document/`, `/report/`
- Policy-blocked routes: `/disabled-form/`, `/blocked-login/`, `/blocked-checkout/`, `/blocked-destructive-action/`

## M65 Review

M65 can become `CandidateCloseM65` only when the live Chrome/CDP proof passes with persisted ledger evidence and no leaks or mutations.

Observed review result: `CandidateCloseM65`.

M65 is not formally closed by this ADR. Formal closure requires a separate operator decision.

## Non-Goals

This does not enable SaaS public access, public API, billing, email, real credentials, sensitive sites, login, checkout, submit, pay, sign, delete, recorder/replay productive mode, embedded runtime, Chromium fork, Vercel changes, or DNS changes.
