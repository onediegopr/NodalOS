# ADR: NODAL OS Durable Approval Audit Trail Design-Only Protected

Decision target: `GO_NODAL_OS_DURABLE_APPROVAL_AUDIT_TRAIL_DESIGN_ONLY_PROTECTED_READY`

## Status

Accepted as protected design-only durable approval audit trail specification if validation passes.

## Context

The controlled execution readiness design track and Approval Mutation Store design established that future approval mutation and controlled execution require durable evidence before any real state change can exist. The missing design layer is a dedicated durable approval audit trail specification.

Control phrase:

`Durable audit trail design readiness may increase. Durable audit trail implementation readiness remains 0%.`

## Decision

Add a deterministic read-only audit trail design fixture:

- `ApprovalDurableAuditTrailDesignOnlyProtected`
- `ApprovalAuditTrailReadiness`
- `ApprovalAuditTrailCapabilityStatus`
- `ApprovalAuditEventPreview`
- `ApprovalAuditEventKind`
- `ApprovalAuditEventFieldRequirement`
- `ApprovalAuditTrailBlockedReason`
- `ApprovalAuditTrailRedactionRequirement`
- `ApprovalAuditTrailRetentionRequirement`
- `ApprovalAuditTrailDeletionRequirement`
- `ApprovalAuditTrailHashChainDesign`
- `ApprovalAuditTrailReplayProtectionDesign`
- `ApprovalAuditTrailExternalAuditRequirement`
- `ApprovalAuditTrailAntiCapabilityProof`
- `ApprovalAuditTrailFutureImplementationChecklist`

The fixture is produced by `ApprovalDurableAuditTrailDesignOnlyProtectedPresenter.CreateFixture()`. It is an in-memory DTO model. It does not expose a durable audit trail, append-only ledger, audit repository, DB, filesystem product IO, file read, real file hash, event persistence, command handler, product service, runtime/live, approval mutation, approval execution, writer/policy productive path, physical export or release/commercial readiness.

## Design Areas

1. Audit trail readiness: design range only; implementation, durable store, append-only ledger, database, filesystem, hash-chain, redaction runtime, retention workflow, deletion workflow and runtime/live readiness remain `0%`.
2. Audit event previews: approval, human review, mutation, policy, runtime gate, export, rollback and external audit events are future/conceptual previews with `CanAppend=false`, `IsPersisted=false` and `IsDurable=false`.
3. Field requirements: event id, actor refs, approval refs, mutation attempt refs, evidence refs, policy version, fingerprints, hashes, redaction status, retention class, deletion eligibility, replay nonce and idempotency key are future-only requirements.
4. Redaction, retention and deletion: future governance requirements are modeled without redaction runtime, secret scan, PII scan, retention store, deletion workflow, privacy export or data write.
5. Hash-chain and replay: previous event hash, event hash, chain validation, tamper evidence, nonce, idempotency and duplicate detection are requirements only; no crypto/hash runtime or replay store exists.
6. External audit: future audit requirements are listed with no external provider, network, service registration or automatic audit execution.
7. Negative capability proof: explicit flags keep append, persistence, ledger, repository, DB, filesystem, workspace reads, real hashes, redaction, retention, deletion, services, commands, mutation, execution, writer/policy, runtime, export, provider/cloud, LLM, vector, memory, browser/CDP, WCU/OCR, recipes and release claims unavailable.

## Non-Goals

- No durable audit trail real.
- No append-only ledger real.
- No audit repository or audit store.
- No DB or migration runner.
- No filesystem product IO.
- No file read or file hash real.
- No event persistence.
- No state mutation.
- No approval mutation.
- No approval execution.
- No runtime/live.
- No writer/policy productive integration.
- No command handler or product service registration.
- No product actions or enabled UI controls.
- No physical export, clipboard or download.
- No provider/cloud/network.
- No semantic/vector backend.
- No LLM live.
- No durable memory.
- No browser/CDP live, WCU live or OCR live.
- No recipe execution.
- No release/commercial readiness claim.

## Safety Proof

- Durable audit trail implementation readiness: `0%`.
- Durable store readiness: `0%`.
- Append-only ledger readiness: `0%`.
- Audit repository readiness: `0%`.
- DB readiness: `0%`.
- Filesystem readiness: `0%`.
- File hash/read readiness: `0%`.
- Redaction runtime readiness: `0%`.
- Retention workflow readiness: `0%`.
- Deletion workflow readiness: `0%`.
- Runtime/live readiness: `0%`.
- Approval mutation readiness: `0%`.
- Approval execution readiness: `0%`.
- Product action count: `0`.
- State mutation count: `0`.
- Audit append count: `0`.
- Persisted event count: `0`.
- Export action count: `0`.
- Release/commercial readiness: `NO-GO`.

## Future Requirements

Any move from this design to a real durable audit trail requires a separate protected sequence and explicit user approval. Future work must cover storage architecture, append-only ledger policy, actor identity and authority, policy versioning, redaction runtime, retention/deletion/privacy policy, hash-chain implementation, replay/idempotency protection, mutation store integration, writer/policy boundary audit, runtime gate audit and external audit approval.
