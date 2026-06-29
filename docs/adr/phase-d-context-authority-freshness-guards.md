# Phase D Context Authority Freshness Guards

Decision target: `GO_PHASE_D_CONTEXT_AUTHORITY_FRESHNESS_GUARDS_READY`

## Decision

Phase D context and memory previews use a deterministic, in-memory authority/freshness guard before any context can be treated as usable for read-only summaries, decision previews, safe-next-step previews, or memory candidate previews.

The guard is fail-closed:

- unknown authority is blocked;
- missing freshness is blocked;
- stale context cannot feed decisions or safe next steps;
- contradictory context is blocked;
- selected excluded context is blocked;
- raw payload context is excluded;
- sensitive context without human clearance is blocked;
- provider/cloud-derived context is blocked while provider/cloud is disabled;
- semantic/vector-derived context is blocked while semantic/vector backend is disabled;
- memory candidates without evidence are blocked;
- decision memory without human review is blocked.

## Scope

This ADR covers read-only guard hardening only. It does not implement durable memory, workspace indexing, filesystem reads, filesystem writes, DB access, provider/cloud calls, semantic/vector backend, LLM live calls, migration runners, browser/CDP automation, or runtime actions.

## Fixture Strategy

`WorkspaceContextAuthorityFreshnessGuard.CreateFixtureCatalog()` provides 20 adversarial and positive in-memory fixtures:

- evidence-linked current context;
- human-reviewed current context;
- fixture-only context;
- stale context;
- missing freshness;
- unknown authority;
- low-authority source;
- contradictory context;
- memory candidate without evidence;
- memory candidate with stale evidence;
- selected context with unknown authority;
- locked stale context;
- selected excluded context;
- sensitive context without clearance;
- raw payload context;
- provider/cloud-derived context while provider/cloud is disabled;
- semantic/vector-derived context while semantic/vector backend is disabled;
- legacy context without provenance;
- safe-next-step relying on stale context;
- decision memory missing human review.

## No-Side-Effect Proof

All guard fixtures carry `WorkspaceContextNoSideEffectProof.FixtureReadOnly()` and tests assert:

- no workspace filesystem read;
- no filesystem write;
- no DB touch;
- no durable persistence;
- no durable memory;
- no vector/semantic backend;
- no LLM/provider;
- no provider/cloud;
- no migration runner/execution;
- no runtime/live;
- no browser/CDP, WCU, or OCR live;
- no product action or product service registration.

## Future Unlock Requirements

Any future context or memory implementation must first add separate, explicit milestones for:

- real workspace source policy;
- durable memory design;
- provider/cloud and semantic/vector threat model;
- human-review workflow;
- installed/manual QA;
- no-runtime and no-side-effect audit.

Until then, context and memory remain fixture-safe and read-only.
