# M65 - External Low-Risk Auth Live Target Setup

## Status

Accepted for setup. Live validation remains blocked until a test-owned external target is configured.

## Context

M51 is deferred because no external test-owned target has been validated. M65 prepares the low-risk external target model needed for future auth proof without using third-party sites, customer accounts, real credentials, sensitive categories, 2FA/CAPTCHA, payments, or irreversible actions.

## Decision

NEXA defines an external low-risk target setup contract with base URL, allowlisted host, ownership proof, risk classification, semantic proof, read-only paths, and disallowed actions. Missing target configuration is an explicit `BLOCKED_NO_TEST_OWNED_EXTERNAL_TARGET` result, not a success.

Live validation is opt-in through `ONEBRAIN_EXTERNAL_LOW_RISK_TARGET_BASE_URL` and `ONEBRAIN_RUN_EXTERNAL_LOW_RISK_TARGET_TESTS`. Without those values, only contract and negative tests run.

## Guardrails

- Only test-owned or explicitly controlled targets are allowed.
- Sensitive hosts and categories are blocked.
- Payment, upload, irreversible actions, 2FA/CAPTCHA, real customer data, and real credentials are blocked.
- Network capture remains metadata-only.
- Cookie values, opaque query values, bodies, and sensitive header values must not persist.

## Consequences

The product can model and evaluate external low-risk readiness while preserving the M51 deferred status. Future closure requires a real test-owned target and live proof.
