# Phase D Workspace Context Packet Surface Read-Only

Decision target: `GO_PHASE_D_WORKSPACE_CONTEXT_PACKET_SURFACE_READ_ONLY_READY`

## Decision

Add a deterministic, fixture-safe, in-memory surface presenter for the Workspace Context Packet.

The surface is a read-only aggregation over:

- `WorkspaceContextReadOnlyPresenter.CreateFixture()`;
- `WorkspaceContextAuthorityFreshnessGuard`;
- `WorkspaceContextSelectionLockExclusionGuard`;
- `WorkspaceMemoryCandidateContradictionRiskGuard`.

It exposes sections/cards for the packet, selected/locked/excluded context, guard summaries, memory candidate previews, disabled capability notices, blockers, warnings, human-review requirements, no-side-effect proof, documented debt, and the next recommended block.

## Scope

This ADR covers presenter/contract surface only. It does not add a sidepanel mount, product actions, export actions, filesystem export, workspace reads, workspace indexing, durable memory, DB usage, provider/cloud calls, semantic/vector backend, LLM live calls, migration runners, browser/CDP automation, WCU/OCR live behavior, runtime actions, or service registration.

## Safety Rules

- Surface is not action.
- Packet is not durable memory.
- Candidate is not memory.
- Product actions count must remain `0`.
- Export actions count must remain `0`.
- Unsafe or missing state is shown as warning/blocker, never hidden.
- Provider/cloud, semantic/vector, durable memory, runtime/live and export remain disabled.

## No-Side-Effect Proof

Every section carries `WorkspaceContextNoSideEffectProof.FixtureReadOnly()`. Tests assert no workspace filesystem reads, no filesystem writes, no database touch, no durable persistence, no durable memory, no vector/semantic backend, no LLM/provider, no provider/cloud, no migration runner/execution, no runtime/live, no browser/CDP, no WCU/OCR, no product action, and no product service registration.

## Future Unlock Requirements

Any visible sidepanel mount, physical export, durable memory, real workspace source, provider/semantic backend, or product action must be a separate explicit milestone with source policy, manual QA, no-runtime proof, and closeout audit.
