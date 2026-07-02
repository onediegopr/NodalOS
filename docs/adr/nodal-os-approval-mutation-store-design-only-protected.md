# ADR: NODAL OS Approval Mutation Store Design-Only Protected

Decision target: `GO_NODAL_OS_APPROVAL_MUTATION_STORE_DESIGN_ONLY_PROTECTED_READY`

## Status

Accepted as protected design-only mutation store specification if validation passes.

## Context

The controlled execution readiness macro-track designed the future bridge for approval execution, mutation boundary, writer/policy boundary, durable audit trail, physical export policy, runtime gate and negative capabilities. The next critical design gap is the approval mutation store: what a future system would need before any approval state mutation could be recorded safely.

Control phrase:

`Mutation store design readiness may increase. Approval state mutation readiness remains 0%.`

## Decision

Add a deterministic read-only mutation store design fixture:

- `ApprovalMutationStoreDesignOnlyProtected`
- `ApprovalMutationStoreReadiness`
- `ApprovalMutationStoreCapabilityStatus`
- `ApprovalMutationAttemptPreview`
- `ApprovalMutationRecordPreview`
- `ApprovalMutationActorBoundaryDesign`
- `ApprovalStalenessDesign`
- `ApprovalInvalidationDesign`
- `ApprovalSupersedingDesign`
- `ApprovalMutationReplayProtectionDesign`
- `ApprovalMutationConcurrencyModelDesign`
- `ApprovalMutationIdempotencyDesign`
- `ApprovalMutationEvidenceRequirementDesign`
- `ApprovalMutationStoreAntiCapabilityProof`
- `ApprovalMutationStoreFutureImplementationChecklist`

The fixture is produced by `ApprovalMutationStoreDesignOnlyProtectedPresenter.CreateFixture()`. It is an in-memory DTO model. It does not expose a real store, repository, DB, filesystem product IO, durable audit trail, command handler, product service, runtime/live, approval execution, writer/policy productive path, physical export or release/commercial readiness.

## Design Areas

1. Mutation store readiness: design range only; implementation, runtime, durable store, database and filesystem write readiness remain `0%`.
2. Mutation attempts and record previews: every mutation kind is a future/conceptual preview with `CanMutate=false`, `IsPersisted=false` and `IsDurable=false`.
3. Actor/identity boundary: human actor proof is required in the future; anonymous, automation and service actors remain blocked and no identity provider is implemented.
4. Stale approval, invalidation and superseding: future context, policy, target and evidence checks are required; no current scan, hash, invalidation or state update exists.
5. Replay, concurrency and idempotency: future nonce, idempotency key, expected state and previous event hash requirements are modeled; no lock, transaction, DB or mutable global state exists.
6. Evidence and audit requirements: future approval packet, human review packet, policy decision, actor proof, fingerprints and audit requirements are enumerated; no evidence is persisted and no audit trail is written.
7. Negative capability proof: explicit flags keep mutation persistence, commits, replays, state updates, audit writes, DB, filesystem, services, commands, writer/policy, runtime, export, provider/cloud, LLM, memory, browser/CDP, WCU/OCR, recipes and release claims unavailable.

## Non-Goals

- No approval mutation.
- No approval execution.
- No real mutation store.
- No repository.
- No DB or migration runner.
- No filesystem product IO.
- No durable audit trail implementation.
- No writer/policy productive integration.
- No command handler or product service registration.
- No product actions or enabled UI controls.
- No runtime/live.
- No physical export, clipboard or download.
- No provider/cloud/network.
- No semantic/vector backend.
- No LLM live.
- No durable memory.
- No browser/CDP live, WCU live or OCR live.
- No recipe execution.
- No release/commercial readiness claim.

## Safety Proof

- Mutation implementation readiness: `0%`.
- Store implementation readiness: `0%`.
- Repository readiness: `0%`.
- DB readiness: `0%`.
- Filesystem write readiness: `0%`.
- Runtime/live readiness: `0%`.
- Approval execution readiness: `0%`.
- Product action count: `0`.
- State mutation count: `0`.
- Export action count: `0`.
- Release/commercial readiness: `NO-GO`.
- All blocked reasons remain present.
- All anti-capabilities pass.

## Future Requirements

Any move from this design to a real mutation store requires a separate protected sequence and explicit user approval. Future work must cover durable audit trail design audit, mutation policy, actor identity and authority, replay protection, invalidation policy, concurrency model, writer/policy boundary audit, runtime gate audit, state storage architecture, security review and external audit.
