# M65 Real Chrome/CDP Evidence Review - M106/M107/M108

## Status

Accepted as live-proof preflight, runner, and review policy. M65 is not formally closed by this ADR.

## Target

Only allowed external target:

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

## Opt-In

Preferred:

- `NODAL_OS_EXTERNAL_LIVE_PROOF_OPT_IN=true`

Temporary compatibility alias:

- `NEXA_EXTERNAL_LIVE_PROOF_OPT_IN=true`

Without opt-in, no Chrome/CDP live proof is launched.

## Preflight Requirements

The preflight must verify:

- Chrome/Edge executable availability
- isolated temporary profile
- no personal Chrome profile
- no default user-data-dir
- no persisted cookies
- no credentials or saved passwords
- no personal extensions
- controlled loopback CDP session
- target host is `lab.nodalos.com.ar`
- read-only only

Unsafe profiles and non-allowlisted hosts are blocked before launch.

## Real Chrome/CDP Proof Requirements

A proof may emit `ProbeKind=RealChromeCdp` only when:

- Chrome/CDP is really available and controlled
- navigation reaches the test-owned target
- DOM/page metadata is captured read-only
- full DOM/body is not persisted
- no submit, mutation, login, checkout, payment, sign, delete, download, or credential entry occurs
- no personal cookies, tokens, secrets, or sensitive headers are captured
- evidence is redacted
- evidence is persisted to `BrowserPersistentAuditLedger`

## M65 Review

M65 can become `CandidateCloseM65` only with:

- `ProbeKind=RealChromeCdp`
- `Tooling=ChromeCdpExternalReadOnly`
- target verified
- persisted redacted ledger ref
- no leaks
- no mutation
- read-only routes passed
- blocked routes policy-passed

This is candidate input only. It does not formally close M65.

## What Was Not Opened

This does not enable:

- public SaaS
- public API
- real billing
- real email
- real customer credentials
- sensitive sites
- AFIP, bank, ERP, fiscal, financial, or government targets
- submit/pay/sign/delete
- productive recorder/replay
- Chromium fork
- embedded browser runtime

## Current Execution Result

Normal test execution remains deterministic and does not launch live Chrome/CDP without opt-in. If Chrome/CDP is unavailable, the runner reports `ChromeCdpUnavailable` and does not fake success.
