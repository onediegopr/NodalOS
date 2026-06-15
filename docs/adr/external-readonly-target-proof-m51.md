# ADR M51: External Read-Only Target Proof

## Status

Accepted as M51 design and guardrail implementation. Live closure requires a configured test-owned external target.

## Context

External audit found that the browser runtime was proven against local fixtures and sandbox flows, but not against a real external target controlled by the team. Local/sandbox validation is necessary, but it does not prove external DNS/TLS/network/CDP behavior.

## Decision

M51 defines an external read-only target proof model:

- target configuration;
- host allowlist;
- ownership classification;
- low-risk classification;
- read-only verification rules;
- metadata-only network policy;
- read-only guard;
- audit/evidence result.

The target must be test-owned or controlled, low-risk, non-sensitive, non-authenticated by default, and free of real personal/commercial/customer data.

## Prohibited Targets

The policy blocks AFIP, banks, ERP, fiscal/financial/government sites, third-party uncontrolled sites, personal/commercial/customer accounts, real client credentials, 2FA/CAPTCHA automation, irreversible actions, upload, sensitive downloads, request/response bodies, sensitive header values, cookies in evidence, profile raw, productive recorder/replay, public SaaS/API exposure, and Companion authority.

## Read-Only Proof

A valid proof requires:

- allowlisted test-owned host;
- CDP live external navigation;
- DOM/title/text semantic proof;
- network metadata-only;
- no opaque query persistence;
- no cookies/session persistence;
- no bodies;
- no sensitive header values;
- external read-only guard active;
- browser/profile cleanup;
- M50 audit key custody.

Done is only valid with executed + verified + semantic proof + audit key custody.

## Key Custody

M51 requires M50 key custody. Dev fixture implicit key use is not allowed. Audit/evidence must include key provider kind and key id metadata without exposing raw key material.

## Blocked State

If `ONEBRAIN_EXTERNAL_READONLY_TARGET_BASE_URL` is not configured with a test-owned external target, M51 must be reported as `BLOCKED_NO_TEST_OWNED_EXTERNAL_TARGET`. Passing local/contract tests alone is not M51 live closure.

## What Remains Blocked

External authenticated flow M25B, M28 external workflow, sensitive real pilots, AFIP/banks/ERP, public SaaS/API, real billing/email, profile raw, productive recorder/replay, and irreversible submit/pay/sign/delete remain blocked after M51 unless a future hito explicitly unlocks them.
