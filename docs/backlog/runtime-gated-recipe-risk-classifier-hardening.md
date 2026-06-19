# Runtime-Gated Recipe Risk Classifier Hardening

## Source

Claude M458-M459 MEDIUM-1 identified a pre-runtime risk in the Recipe Risk Classifier.

## Problem

The current classifier is contract-only and uses keyword matching. A benign keyword can match while a dangerous synonym is not covered.

Mandatory adversarial example:

`drop table after reading status`

This can be dangerous if a future runtime or approval gate treats the classifier result as authoritative. Today it does not authorize execution, so this is not a runtime defect in the current system.

## Dangerous Synonyms To Consider

- drop
- purge
- wipe
- truncate
- destroy
- transfer funds
- wire
- charge
- refund
- revoke
- disable
- overwrite
- commit
- push
- deploy

## Decision

Do not implement hardening in M460-M462 because NODRIX has no runtime execution path here.

This backlog item is a runtime-gated blocker before the classifier can be used for real approval gates, execution decisions, or runtime authority.

## Future Hardening Recommended

- Fail closed when intent is ambiguous.
- Require explicit declared risk categories.
- Maintain a separate negative and danger lexicon.
- Never allow benign keywords to reduce risk when dangerous intent is present.
- Add classifier confidence.
- Require human review on ambiguity.
- Add adversarial tests for dangerous synonyms and mixed benign/dangerous instructions.

## Blocks

- Real recorder replay.
- Real browser automation.
- DSL parser runtime.
- Recipe execution.
- Step execution.
- recipe/step execution.
- Approval gates based on classifier authority.

## Does Not Block

- Pause closure.
- Contract-only work.
- Documentation.
- No-runtime design.

## Current Authority

The classifier remains contract-only:

- `RuntimeExecutionAllowed=false`.
- `RuntimeExecutionDeferred=true`.
- `RequiresGlobalPolicyEvaluation=true`.
- `RequiresEvidenceRedaction=true`.
- `CanAuthorizeAction=false`.
