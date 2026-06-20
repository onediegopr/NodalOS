# NODAL OS Real Scan Readiness After Synthetic Dry Run ADR

Status: `REAL_SCAN_NOT_READY_SYNTHETIC_BASELINE_READY`

## Context

NODAL OS has completed governed preconditions, consent and scope contracts, policy previews, dry-run contracts, dry-run review, implementation boundary, fixture matrix, synthetic simulator, fixture result review, and scan boundary audit.

The synthetic layer is useful as a governance and prototype baseline. It does not prove operational workspace safety and it does not enable operational scan behavior.

## Decision

NODAL OS is not ready for operational scan behavior.

Synthetic baseline is ready as a governance and prototype baseline.

The next phase may implement a disabled-by-default path jail prototype only if it remains fixture-gated, audited, consent-aware, and non-mutating.

Operational filesystem access remains blocked until a separate milestone implements:

- Real path jail enablement gate.
- Explicit consent enforcement.
- No-mutation guarantee.
- Cancellation semantics.
- Evidence and timeline emission.
- Redaction and sensitive-data enforcement.
- Exclusion enforcement.
- Audit before enablement.

Operational folder enumeration, content access, content fingerprinting, indexing, representation build, and LLM context remain blocked.

Cloud, provider, and runtime behavior remain blocked.

Future operational scan behavior must remain read-only, local-only, consent-gated, path-jail-gated, redaction-first, evidence-first, and audit-logged.

Synthetic fixture coverage is necessary but not sufficient.

## Consequences

- Progress remains slower but safer.
- The future implementation path is explicit and auditable.
- Sensitive data exposure risk remains reduced.
- Any future operational prototype needs a separate enablement gate.

## Accepted Alternatives

- Synthetic baseline closeout before operational access.
- Disabled-by-default prototype path for future work.
- Separate audit gate before each operational capability.

## Rejected Alternatives

- Direct operational scan behavior.
- Direct content access.
- Broad workspace crawler.
- Cloud-based scan.
- Provider-assisted context build.
- Implicit consent.
- Index-first or representation-first project understanding.

## Required Next Milestones

- Disabled-by-default path jail prototype gate.
- Consent enforcement gate.
- No-mutation and cancellation proof.
- Evidence and timeline emission audit.
- Sensitive-data and exclusion enforcement audit.
- Operational scan readiness re-review.

## Explicit Non-Goals

- No operational scan behavior in this milestone.
- No operational workspace access in this milestone.
- No LLM context build in this milestone.
- No prompt construction in this milestone.
- No provider, cloud, or runtime behavior in this milestone.

## Audit Triggers

- Any marker for operational workspace access.
- Any marker for content context build.
- Any provider, cloud, or runtime marker.
- Any productive persistence marker.
- Any missing consent or path jail gate.

## Rollback And Disable Strategy

- Future prototype must default to disabled.
- Audit failure blocks enablement.
- Consent withdrawal blocks operational behavior.
- Policy failure blocks operational behavior.
- Redaction failure blocks operational behavior.

## Decision Record

Decision: `REAL_SCAN_NOT_READY_SYNTHETIC_BASELINE_READY`

This closes the synthetic baseline phase without enabling operational scan behavior.

