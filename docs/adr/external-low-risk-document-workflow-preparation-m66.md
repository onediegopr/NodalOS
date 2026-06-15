# M66 - External Low-Risk Document Workflow Preparation

## Status

Accepted as prepared. Live execution remains blocked unless M65 validates a test-owned external target.

## Context

Document workflow must not advance to external execution without target readiness. The allowed scope is low-risk, synthetic, non-sensitive, approval-gated, and safe-download/safe-upload governed.

## Decision

NEXA defines external low-risk document workflow policy, request, readiness, and decision contracts. The evaluator requires M65 target readiness, approval, SafeDownload, SafeUpload, redacted audit, and non-sensitive documents.

If M65 is blocked, M66 returns `PREPARED_BUT_BLOCKED_BY_M65`. This prevents false success while allowing the policy and tests to be completed.

## Guardrails

- Sensitive documents are blocked.
- Workflow cannot bypass SafeDownload or SafeUpload.
- Approval is required.
- Audit remains redacted.
- Live external execution is not allowed without validated target readiness.

## Consequences

The document workflow is ready for future low-risk external validation, but remains blocked in this milestone because M65 has no configured live target.
