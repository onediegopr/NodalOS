# M77-M79 External Test-Owned Proof Preparation

## Status

Accepted as preparation only. M51 and M65 remain deferred until a real test-owned external target exists and opt-in live proof is executed.

## Context

NEXA local private preview is valid, but external/live browser proof is still blocked by lack of a controlled external target. This block must not be bypassed with third-party, fiscal, financial, government, or sensitive sites.

## Decision

M77 defines the `ExternalTestOwnedTarget` contract and approval states. A valid target must be explicitly test-owned, approved, read-only, HTTP/HTTPS, allowlisted, non-sensitive, no credentials, no personal data, metadata-only, and GET/HEAD-only.

M78 defines an opt-in external proof harness. It is disabled by default and blocks before execution when opt-in, target approval, host/path/method, cookie, body, sensitive header, or submit policies fail.

M79 defines the external read-only evidence pack. It can represent prepared, skipped, blocked, executed, failed, and passed proof states. A passed proof may be a candidate for M51/M65 closure review, but it does not close M51/M65 automatically.

## Non-goals

- No real external proof execution in this milestone.
- No third-party targets.
- No sensitive sites.
- No credentials.
- No submit/pay/sign/delete.
- No SaaS public activation.
- No M51/M65 closure.

## Consequences

The next real external step is to provision a test-owned target, then run opt-in live proof. Until then, external/live readiness remains blocked or deferred.
