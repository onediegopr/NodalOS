# ADR: First Real Capability Candidate Scope Proposal Read-Only

## Status

Accepted as read-only scope proposal if validation passes.

## Context

NODAL OS remains paused in `PAUSED_READ_ONLY_NO_RUNTIME_NO_EXECUTION_NO_MUTATION_NO_PHYSICAL_EXPORT_NO_REDACTION_RUNTIME`.
The implementation planning gate was hardened with explicit Browser/CDP, WCU/OCR and Recipes negative requirements. Runtime/live, execution, mutation, export, redaction runtime, retention/deletion runtime and release/commercial readiness remain unavailable.

## Decision

Create a deterministic read-only scope proposal packet that evaluates future real capability candidates and selects exactly one candidate for external audit preparation only:

`DURABLE_AUDIT_TRAIL_APPEND_ONLY_MINIMAL_SCOPE_PROPOSAL_READ_ONLY`

The maximum allowed decision is:

`SAFE_TO_PREPARE_EXTERNAL_AUDIT_FOR_SELECTED_CANDIDATE`

The selected candidate remains blocked. It is not approved for implementation, not runtime ready and not release/commercial ready.

## Candidate Matrix Result

- Durable audit trail append-only minimal scope proposal: selected for read-only scope proposal and external audit preparation.
- Approval execution minimal bridge: not first candidate because it is too close to product actions.
- Physical export controlled path: not first candidate because it depends on redaction, retention/deletion and destination gates.
- Redaction runtime minimal path: not first candidate because it requires privacy scanner policy and external audit.
- Retention/deletion runtime: high-risk no-go as first candidate because lifecycle changes can be irreversible.
- Browser/CDP safe runtime: high-risk no-go as first candidate.
- WCU/OCR safe runtime: high-risk no-go as first candidate.
- Recipes execution safe runtime: high-risk no-go as first candidate.
- Mutation store minimal path: future candidate blocked until audit trail accountability exists.

## Selected Scope

Future hito name:

`NODAL_OS_DURABLE_AUDIT_TRAIL_APPEND_ONLY_IMPLEMENTATION_CANDIDATE_BLOCKED_PENDING_EXTERNAL_AUDIT_AND_USER_GO`

Future-only scope:

- append-only audit event contract;
- approved event schema;
- fail-closed append eligibility;
- redaction status reference requirement;
- deterministic denied-append tests.

## Mandatory Gates

- Explicit user GO for selected candidate implementation.
- Repo guard clean.
- Dedicated branch or explicit branch confirmation.
- External audit before implementation.
- Negative tests before implementation.
- Scope isolation.
- Overclaim scan before and after.
- No service registration unless explicitly scoped.
- No command handler unless explicitly scoped.
- No product UI action unless explicitly scoped.
- No broad IO.
- No secrets, PII or real data.
- Fail-closed behavior.
- Rollback and no-side-effect proof.
- QA report MD/JSON.
- Final audit before enablement.
- Release/commercial remains `NO-GO`.

## Non-Goals

This ADR does not implement durable audit trail real, runtime/live, execution, mutation, physical export, redaction runtime, secret/PII scan, retention/deletion runtime, mutation store real, writer/policy productive integration, service registration, command handlers, product actions, filesystem product IO, DB/migration, provider/cloud/network, LLM/browser/CDP/WCU/OCR live, recipes execution real or release/commercial readiness.

## Consequences

- A single first-candidate scope can be audited.
- Implementation remains blocked.
- All counts remain `0`.
- External audit is required before any implementation prompt can be considered.
- A second audit is required after any future implementation and before enablement.
