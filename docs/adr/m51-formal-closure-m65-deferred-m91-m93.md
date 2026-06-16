# M91-M93: M51 Formal Closure Review and M65 Deferred Evidence

## Status

Accepted for M91-M93.

## Context

M90 hardened the external read-only proof path after audit findings:

- HttpClient proof is not Chrome/CDP proof.
- HttpClient proof must use `ProbeKind=RealHttpClient`.
- Tooling must be `HttpReadOnlyExternal`.
- Capabilities must be limited to `HttpGetReadOnly`, `NetworkMetadataOnly`, and `CoreGoverned`.
- Evidence must persist to the M50 `BrowserPersistentAuditLedger` before closure review can accept it.
- Response bodies may be fetched transiently for safety scan, but must not be persisted.

The previous live host `nexalab.nodalos.com.ar` was deactivated and returned `DEPLOYMENT_NOT_FOUND`.

## Current Target

- Target: `https://lab.nodalos.com.ar`
- Vercel project: `lab`
- Vercel scope: `Shift Evidence`
- DNS: `A lab -> 76.76.21.21`
- Required paths: `/`, `/health/`, `/ownership/`, `/products/`, `/document/`, `/report/`
- Blocked fixture paths: `/disabled-form/`, `/blocked-login/`, `/blocked-checkout/`, `/blocked-destructive-action/`

## Legacy Host

- Previous host: `nexalab.nodalos.com.ar`
- Status: deactivated legacy host
- Expected behavior: `DEPLOYMENT_NOT_FOUND`
- It must not be used as an operational live proof target.

## What M51 Proof Can Establish

A passed M51 proof can establish only HTTP read-only external reachability against a verified test-owned target:

- HTTPS/health/ownership verified.
- Only GET/read-only route checks executed.
- No submit, mutation, payment, checkout, login, or credential flow.
- No cookies, tokens, secrets, sensitive headers, or body persisted.
- Evidence pack persisted to the HMAC ledger with `LedgerRef`, `LedgerSequence`, `LedgerHash`, and `PersistedAtUtc`.

## What It Does Not Establish

This proof does not establish:

- Chrome/CDP live navigation.
- DOM read-only proof.
- Browser process/profile cleanup.
- Auth target readiness.
- Document workflow readiness.
- Low-risk external auth closure under M65.
- SaaS public readiness.
- Billing or email real readiness.
- Real credential readiness.

## M51 Closure Rule

M51 may be marked closed only after explicit review accepts a proof with:

- `ProofStatus=PassedReadOnlyProof`
- `ProbeKind=RealHttpClient` or `RealChromeCdp`
- honest tooling/capabilities
- `PersistenceStatus=PersistedRedactedLedger`
- non-empty `LedgerRef`
- target verified as `lab.nodalos.com.ar`
- no persisted secrets, cookies, tokens, or body
- no mutation, submit, payment, checkout, or real login

## M65 Decision

M65 remains `DeferredNeedsDedicatedEvidence`.

The M51 HTTP read-only proof is not enough to close M65 because M65 is about external low-risk auth/live target setup and requires dedicated evidence beyond a basic HTTP read-only proof.

## Remaining Blocks

- No SaaS public activation.
- No public API exposure.
- No real billing.
- No real email.
- No real credentials.
- No sensitive sites.
- No submit/pay/sign/delete.
- No Productive recorder/replay.
- No UI/Companion/Admin authority.

