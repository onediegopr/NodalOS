# Productive Consent Storage Implementation ADR

Decision status: `PRODUCTIVE_CONSENT_STORAGE_NOT_IMPLEMENTED_ADR_READY`.

## Context

M570-M575 established the access checkpoint, productive consent design draft, design review, disabled storage contract, and consent audit acceptance. These outputs are governance artifacts only. They do not persist consent, enforce consent, authorize capabilities, enable operational access, or build LLM context.

The next consent step must define how future storage could be implemented without combining storage, enforcement, or capability enablement into one milestone.

## Decision

NODAL OS does not implement productive consent storage in this block.

Any future storage implementation must be:

- disabled-by-default;
- local-first;
- scope-bound;
- capability-bound;
- workspace-bound;
- mission-bound;
- redaction-first;
- audit-gated;
- rollback-ready.

Future storage records cannot contain sensitive material, content payloads, or unredacted broad path values.

Consent storage cannot imply operational access, LLM permission, cloud permission, provider permission, or runtime permission. Consent for one capability cannot imply another capability. Revoked, stale, or missing consent must fail closed.

Productive implementation requires a storage boundary ADR, migration/rollback/disable strategy, adversarial test matrix, redaction review, evidence/timeline emission, audit before enablement, full suite, guard checks, and no operational access as part of storage implementation.

Storage and enforcement remain separate milestones.

Storage and capability enablement remain separate milestones.

## Consequences

- Safer consent governance before implementation.
- Clear separation between storage, enforcement, and capability use.
- Higher auditability.
- Slower path to operational behavior.
- Lower risk of implicit permission expansion.

## Accepted Alternatives

- ADR-first storage boundary.
- Disabled-by-default future implementation.
- Local-first storage design.
- Adversarial matrix before implementation.

## Rejected Alternatives

- Direct productive storage.
- Storage that implies capability authorization.
- Storage and enforcement in one milestone.
- Storage and capability enablement in one milestone.
- Cloud-backed default storage.
- Implicit consent inheritance.
- Records containing sensitive material or content payloads.

## Required Next Milestones

- Storage boundary test pack.
- Disabled productive consent storage preview.
- Consent implementation audit plan.
- Redaction review.
- Migration, rollback, and disable strategy.

## Explicit Non-Goals

- No productive consent storage.
- No productive consent enforcement.
- No capability enablement.
- No operational access.
- No path jail activation.
- No content access.
- No content fingerprinting.
- No indexing.
- No representation build.
- No LLM context.
- No provider activity.
- No cloud.
- No runtime.

## Audit Triggers

Audit is required before any future change that introduces storage writes, storage reads, migration, enforcement, capability authorization, operational access, LLM context, cloud, provider activity, or runtime behavior.

## Disable And Rollback Strategy

Any future implementation must start disabled, support kill switch behavior, preserve a rollback path, and fail closed when storage state is missing, stale, revoked, ambiguous, or out of scope.
