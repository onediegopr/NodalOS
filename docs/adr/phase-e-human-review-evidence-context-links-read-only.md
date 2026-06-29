# ADR: Phase E Human Review Evidence Context Links Read Only

Decision target: `GO_PHASE_E_HUMAN_REVIEW_EVIDENCE_CONTEXT_LINKS_READ_ONLY_READY`

## Status

Accepted as read-only link hardening.

## Context

Phase E already has a deterministic Approval/Human Review read-only foundation and risk/decision guards. This milestone hardens the links between human review packets, Phase C evidence labels and Phase D context labels.

The link model must not treat a reference as durable evidence, trusted context or approval execution. Links are fixture-safe pointers for preview and audit only.

## Decision

Add `HumanReviewEvidenceContextLinkReadOnlyGuard` as a deterministic in-memory guard.

It models:

- evidence link state;
- context link state;
- source kind;
- authority/freshness failures;
- confidence failures;
- risk and contradiction state;
- human review state;
- decision, candidate-action and safe-next-step usage;
- product action and state mutation counts;
- blockers and warnings;
- no-side-effect proof.

## Guard Rules

- Missing evidence blocks.
- Missing context blocks.
- Stale, excluded or unknown context blocks.
- Unknown authority blocks.
- Missing freshness blocks.
- Missing confidence blocks.
- Unresolved contradiction blocks.
- Critical risk blocks.
- Raw or secret-like links are excluded.
- Disabled provider/cloud, semantic/vector, LLM-live, persistence-store and durable-memory sources block.
- Product action count greater than zero blocks.
- State mutation count greater than zero blocks.
- Decision option, candidate action and safe next step usages block when they depend on invalid links.
- Fixture-only evidence is warning-only and not production trusted.
- Context requiring human review remains preview-only and requires human review.

## Invariants

- Evidence link is not durable evidence.
- Context link is not trusted context by default.
- Approval preview is not approval execution.
- Human review packet is not state mutation.
- No product action is exposed.
- No service registration is added.
- Phase E read-only files must not reference `ApprovalArtifactWriter`, `ApprovalPolicy`, `ApprovalBindingValidator`, `Pilot`, `AgentOperations` or writer/policy execution paths.

## No Goals

- No approval execution.
- No approval state mutation.
- No product action command.
- No runtime/live.
- No filesystem product read/write.
- No workspace scan.
- No DB or dependency.
- No provider/cloud/network.
- No semantic/vector backend.
- No LLM live.
- No durable memory.
- No migration runner.
- No physical export, clipboard or download.

## Future Unlock Requirements

Any future executable approval flow must be handled in a separate protected milestone with explicit state mutation design, durable audit trail design, non-fixture evidence/context compatibility, runtime gate review and re-audit of writer/policy/service registration paths.
