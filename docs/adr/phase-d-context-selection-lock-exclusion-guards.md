# Phase D Context Selection Lock Exclusion Guards

Decision target: `GO_PHASE_D_CONTEXT_SELECTION_LOCK_EXCLUSION_GUARDS_READY`

## Decision

Phase D workspace context and memory previews now include deterministic read-only guards for selection, lock, and exclusion state. These guards run only on in-memory fixtures and are fail-closed.

Precedence rules:

- excluded wins over selected;
- locked wins over automatic influence;
- human review wins over decision use;
- stale, missing, unknown, contradictory, raw, sensitive, provider-derived, semantic-derived, and legacy-unprovenanced context cannot feed decisions;
- memory candidates, safe next steps, claim/action previews, graph refs, export candidates, and dashboard candidates cannot reference excluded context.

## Scope

This ADR covers guard hardening only. It does not add workspace reads, filesystem writes, durable memory, DB usage, provider/cloud calls, semantic/vector backend, LLM live calls, migration runners, browser/CDP automation, WCU/OCR live behavior, runtime actions, or UI actions.

## Fixture Strategy

`WorkspaceContextSelectionLockExclusionGuard.CreateFixtureCatalog()` covers 22 deterministic fixtures:

- selected evidence-linked fresh context;
- selected plus excluded;
- selected plus locked-by-safety;
- selected stale context;
- selected unknown authority;
- selected missing freshness;
- selected contradictory context;
- locked stale context;
- locked missing evidence;
- locked memory promotion attempt;
- excluded context referenced by memory;
- excluded context referenced by safe next step;
- excluded context referenced by claim/action preview;
- excluded context referenced by graph refs;
- selected raw/sensitive unsafe context;
- selected provider-derived context while provider/cloud is disabled;
- selected semantic-derived context while semantic/vector is disabled;
- selected legacy context without provenance;
- duplicate selected context with conflicting lock states;
- empty selected context with dependent safe next step;
- locked context requiring human review with review missing;
- excluded context appearing in export/dashboard candidate list.

## No-Side-Effect Proof

All fixtures carry `WorkspaceContextNoSideEffectProof.FixtureReadOnly()` and tests assert no workspace filesystem reads, no filesystem writes, no database touch, no durable persistence, no durable memory, no vector/semantic backend, no LLM/provider, no provider/cloud, no migration runner/execution, no runtime/live, no browser/CDP, no WCU/OCR, no product action, and no product service registration.

## Future Unlock Requirements

Future work that wants real selection, lock, exclusion, memory, or workspace source behavior must add explicit milestones for source policy, human review workflow, durable memory design, provider/semantic threat model, UI proof if visible, and no-runtime audit.
