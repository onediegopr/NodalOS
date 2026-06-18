# ADR: OCR Observation Isolation And Confidence Policy M322-M324

## Status

Accepted for internal development.

## Context

M319-M321 proved the evidence-envelope model but showed that a bounded desktop capture could still observe unrelated content. That made the OCR result unusable as evidence even though the OCR pipeline itself was healthy.

## Decision

For low-risk internal OCR observation:

- evidence remains non-authoritative
- actions remain blocked
- acceptance requires explicit region verification
- acceptance requires explicit confidence gating
- screen-copy capture is not sufficient for QA-window evidence
- QA-window observation uses a capture derived from the QA window client surface

## Acceptance policy

An observation may be marked `AcceptedEvidence` only when all of the following are true:

- `ActionsAllowed=false`
- `NoAuthority=true`
- `EvidenceOnly=true`
- window title and process match the QA host
- region bounds match the expected bounded capture
- capture fingerprint matches
- capture is not full-screen
- confidence gate passes

## Confidence policy

The current internal gate passes when:

- detector boxes exist
- guard survives and does not time out
- confidence is at or above the configured threshold, or remains safe to interpret
- exact/normalized match succeeds, or edit distance stays within the bounded threshold for the controlled QA fixture

This is explicitly an internal evidence gate, not a general OCR correctness claim.

## Consequences

- low-risk OCR observation can now feed internal evidence workflows
- envelopes keep rejection/uncertain states when verification or confidence fails
- the system preserves PaddleOCR policy:
  - official space token
  - no softmax reapply
  - no dictionary invention

