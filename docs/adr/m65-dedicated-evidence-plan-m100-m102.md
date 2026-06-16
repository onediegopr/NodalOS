# M65 Dedicated Evidence Plan - M100/M101/M102

## Status

Accepted as planning and gating work. M65 remains `DeferredNeedsDedicatedEvidence`.

## Context

NODAL OS M51 is closed with strict scope:

- external HTTP read-only proof
- test-owned target: `https://lab.nodalos.com.ar`
- `ProbeKind=RealHttpClient`
- `Tooling=HttpReadOnlyExternal`
- evidence persisted to the HMAC audit ledger
- no external Chrome/CDP/DOM proof

That evidence is sufficient for M51 HTTP read-only proof, but it is not sufficient for M65.

## Why M51 Does Not Close M65

M51 proved a narrow external HTTP read-only path. It did not prove:

- browser runtime navigation against external pages
- CDP/DOM read-only extraction on the external target
- multi-page low-risk external workflow behavior
- policy-blocked unsafe routes under a dedicated M65 review
- safe handling of external low-risk document/workflow surfaces

M65 must not close by inheritance from M51.

## M65 Scope

M65 means a dedicated external low-risk target evidence track:

- target is test-owned
- target is external
- surface is low-risk and synthetic
- read-only first
- unsafe routes are policy-blocked
- no credentials
- no personal cookies
- no secrets
- no submit
- no mutation
- no payment
- no real login
- evidence ledger required
- reviewer is separate from M51

## Out Of Scope

M65 does not enable:

- SaaS public launch
- public API exposure
- billing real
- email real
- real customer credentials
- sensitive sites
- AFIP, banks, ERP, fiscal, financial, or government surfaces
- submit/pay/sign/delete
- productive recorder/replay

## Minimum Evidence Required

M65 requires:

- scenario plan for low-risk external target
- test-owned target verification
- redacted persistent ledger evidence
- no secrets/cookies/tokens
- no persisted bodies
- no submit/mutation/payment/login
- policy evidence for blocked login, checkout/payment, disabled form, and destructive action surfaces
- at least one dedicated external browser/CDP/DOM read-only proof, unless a future ADR explicitly narrows M65 to HTTP-only

## Sufficient Evidence

Candidate M65 closure may be emitted only when:

- dedicated scenario plan is ready
- target is verified test-owned
- read-only proof passes
- `ProbeKind=RealChromeCdp` or future explicitly approved equivalent
- `Tooling=ChromeCdpExternal` or future explicitly approved equivalent
- ledger reference exists
- redaction is clean
- blocked routes remain blocked by policy
- no sensitive surface is enabled

Candidate closure is not automatic closure.

## Insufficient Evidence

The following are not sufficient:

- M51 evidence alone
- HTTP GET proof alone
- dry-run/model-only proof
- fake probe evidence
- evidence without ledger ref
- evidence with secrets/cookies/tokens
- evidence with submit/mutation/payment/login
- policy-only blocked routes without read-only external browser evidence

## M101 Scenario Expansion

Dedicated low-risk scenarios:

- read-only landing verification
- read-only document verification
- read-only structured table/report
- disabled form policy verification
- blocked login policy verification
- blocked checkout/payment policy verification
- blocked destructive action policy verification
- synthetic multi-page workflow read-only
- optional safe search/filter UI if static and non-mutating
- optional synthetic download metadata-only without real file download

The scenario plan can become ready without closing M65.

## M102 Closure Gate

The M65 gate rules:

- M51 closed alone => `DoNotClose`
- HTTP proof only => `RequiresChromeCdpDomProof`
- no ledger => `DoNotClose`
- fake/model-only => `DoNotClose`
- secrets/cookies/tokens => `DoNotClose`
- submit/mutation/payment/login => `DoNotClose`
- CDP/DOM required but missing => `RequiresChromeCdpDomProof`
- all dedicated evidence present => `CandidateCloseM65`

## Current Decision

M65 scope is defined and the scenario plan is ready. M65 remains deferred until dedicated evidence is collected.

## Security State

SaaS, public API, billing real, email real, real credentials, sensitive sites, submit/pay/sign/delete, and productive recorder/replay remain blocked.
