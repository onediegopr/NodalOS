# M112-M114 - M65 Formal Closure With Scope Lock

## Status

M65 is formally closed with limited scope:

`FormallyClosedTargetOwnedReadOnlyCdp`

## Evidence Used

- Target: `https://lab.nodalos.com.ar`
- Opt-in: `NODAL_OS_EXTERNAL_LIVE_PROOF_OPT_IN=true`
- Chrome/CDP: available
- Profile: isolated temporary profile, no personal/default profile
- CDP session: controlled through loopback DevTools
- `ProbeKind=RealChromeCdp`
- `Tooling=ChromeCdpExternalReadOnly`
- Capabilities: `BrowserNavigationReadOnly`, `DomSnapshotReadOnly`, `PageMetadataReadOnly`, `NetworkMetadataRedacted`, `CoreGoverned`

Routes navigated read-only:

- `/`
- `/health/`
- `/ownership/`
- `/products/`
- `/document/`
- `/report/`

Routes verified as policy-blocked:

- `/disabled-form/`
- `/blocked-login/`
- `/blocked-checkout/`
- `/blocked-destructive-action/`

Ledger evidence:

- `PersistenceStatus=PersistedRedactedLedger`
- `LedgerRef=audit-ledger-edb3e2fbb0a0446788dae17a269c0058`
- `LedgerHash=61f52af1eebf08d59a24e5fbb72e70acf0038e7a329bff6599a0ac00c757f03e`

Safety confirmations:

- no full DOM persisted
- no full body persisted
- no cookies persisted
- no tokens or secrets persisted
- no credentials used
- no submit, mutation, payment, login, sign, or delete

## What M65 Proves

M65 proves that NODAL OS can run a target-owned external low-risk Chrome/CDP/DOM read-only proof against `lab.nodalos.com.ar`, with isolated profile and redacted ledger evidence.

## What M65 Does Not Prove

M65 does not prove or unlock:

- external CDP general-ready
- production external browser automation
- third-party sites
- sensitive sites
- real credentials
- login, checkout, submit, pay, sign, delete
- SaaS public
- public API
- billing real
- email real
- recorder/replay productive mode
- embedded runtime
- Chromium fork

## Scope Lock

External CDP remains locked to `TargetOwnedProofOnly`.

Any new external target, third-party host, sensitive category, credentialed flow, mutation, production mode, or general external CDP claim requires dedicated approval, new evidence, and a separate review.

## Decision

M65 is closed with limited scope:

`FormallyClosedTargetOwnedReadOnlyCdp`

Browser Runtime external general-ready remains false.
