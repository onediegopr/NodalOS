# Phase D Memory Candidate Contradiction Risk Read-Only Guards

Decision target: `GO_PHASE_D_MEMORY_CANDIDATE_CONTRADICTION_RISK_READ_ONLY_READY`

## Decision

Phase D memory candidates remain read-only previews. They are not durable memory, not trusted facts, and do not enable decision or safe-next-step use. Evidence, authority, freshness, selection, lock, exclusion, contradiction, risk, confidence, and human-review requirements only determine whether a candidate may appear as a read-only warning/preview or must be blocked.

The guard is deterministic and fail-closed:

- candidate without evidence is blocked;
- stale, excluded, or locked-unsafe dependency is blocked;
- contradiction requires human review;
- risk cannot become decision memory;
- critical risk blocks safe next step until reviewed;
- unresolved contradiction blocks safe next step;
- decision candidate without human review is blocked;
- claim candidate without confidence is blocked;
- action candidate without required human action is blocked;
- provider/cloud-derived candidate is blocked while provider/cloud is disabled;
- semantic/vector-derived candidate is blocked while semantic/vector is disabled;
- legacy candidate without provenance is blocked;
- fixture-only candidate is warning/read-only and not durable/trusted;
- duplicate candidates with conflicting conclusions are blocked;
- raw/sensitive payload candidate is excluded.

## Scope

This ADR covers read-only memory candidate guard hardening only. It does not add durable memory, workspace reads, filesystem writes, DB usage, provider/cloud calls, semantic/vector backend, LLM live calls, migration runners, browser/CDP automation, WCU/OCR live behavior, runtime actions, or UI actions.

## Fixture Strategy

`WorkspaceMemoryCandidateContradictionRiskGuard.CreateFixtureCatalog()` covers 24 deterministic fixtures across contradiction, risk, decision, claim, action, safe-next-step, provider/semantic, legacy, fixture-only, duplicate, raw/sensitive, unknown authority, and missing freshness cases.

## No-Side-Effect Proof

All fixtures carry `WorkspaceContextNoSideEffectProof.FixtureReadOnly()` and tests assert no workspace filesystem reads, no filesystem writes, no database touch, no durable persistence, no durable memory, no vector/semantic backend, no LLM/provider, no provider/cloud, no migration runner/execution, no runtime/live, no browser/CDP, no WCU/OCR, no product action, and no product service registration.

## Future Unlock Requirements

Any future durable memory, real workspace source, provider/semantic backend, or UI influence must be a separate explicit milestone with source policy, human-review workflow, no-runtime proof, and closeout audit.
