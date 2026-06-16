# External Chrome/CDP/DOM Read-Only Proof - M103/M104/M105

## Status

Accepted as contract, harness, and M65 candidate-input gate. M65 is not formally closed by this ADR.

## Target

Only test-owned target:

- `https://lab.nodalos.com.ar`

Allowed read-only routes:

- `/`
- `/health/`
- `/ownership/`
- `/products/`
- `/document/`
- `/report/`

Policy-blocked routes:

- `/disabled-form/`
- `/blocked-login/`
- `/blocked-checkout/`
- `/blocked-destructive-action/`

## Difference From M51

M51 is closed with strict HTTP-only scope:

- `ProbeKind=RealHttpClient`
- `Tooling=HttpReadOnlyExternal`
- external HTTP GET/read-only proof
- persisted redacted audit ledger evidence
- no Chrome/CDP/DOM proof

M103-M105 defines the separate Chrome/CDP/DOM evidence track:

- `ProbeKind=RealChromeCdp`
- `Tooling=ChromeCdpExternalReadOnly`
- capabilities: `BrowserNavigationReadOnly`, `DomSnapshotReadOnly`, `PageMetadataReadOnly`, `NetworkMetadataRedacted`, `CoreGoverned`
- DOM/page metadata snapshot only
- no full DOM or body persistence

HttpClient evidence cannot be relabeled as Chrome/CDP evidence.

## What A Real Chrome/CDP Proof Must Prove

A proof can emit `RealChromeCdp` only if:

- Chrome/CDP is actually available and controlled
- navigation reaches the allowed test-owned target
- DOM/page metadata or an allowed redacted snapshot is captured read-only
- no submit, mutation, login, payment, sign, or delete occurs
- no credentials are used
- no personal cookies are persisted
- no tokens or secrets are captured
- no sensitive header values are captured
- no full DOM or response body is persisted
- evidence is redacted
- evidence is persisted to the HMAC audit ledger when candidate evidence is produced

## Default Behavior

The runner is disabled by default. Without explicit opt-in, it returns `SkippedNoOptIn`.

If Chrome/CDP is unavailable, it returns `ChromeCdpUnavailable` and does not fake success.

## M65 Impact

M65 requires dedicated evidence beyond M51. A Chrome/CDP/DOM proof can become M65 candidate input only when:

- `ProbeKind=RealChromeCdp`
- `Tooling=ChromeCdpExternalReadOnly`
- target is verified
- ledger reference exists
- persistence status is `PersistedRedactedLedger`
- no leaks are detected
- allowed routes are read-only
- blocked routes remain policy-blocked

`CandidateCloseM65` is still not automatic closure. Formal M65 closure remains a later review step.

## What This Does Not Enable

This does not enable:

- SaaS public launch
- public API exposure
- real billing
- real email
- real customer credentials
- sensitive sites
- AFIP, banks, ERP, fiscal, financial, or government sites
- submit/pay/sign/delete
- productive recorder/replay
- Chromium fork
- embedded browser runtime

## Current Limitations

The normal test suite does not execute live Chrome/CDP against the internet. It validates contracts, gates, persistence rules, and deterministic proof behavior. A real live Chrome/CDP proof requires explicit opt-in and an available Chrome/CDP runtime.
