# NODAL OS Operational Access Audit Before Filesystem Access ADR

Status: `OPERATIONAL_FILESYSTEM_ACCESS_NOT_READY_AUDIT_REQUIRED`

## Context

NODAL OS has a synthetic baseline, a disabled Path Jail prototype gate, synthetic canonicalization cases, and a no-mutation proof contract. These artifacts establish governance preconditions only.

## Decision

NODAL OS does not allow operational filesystem access yet.

Disabled Path Jail Prototype Gate is a precondition, not an authorization.

Any future operational access requires:

- Explicit future milestone.
- Disabled-by-default gate.
- User consent enforcement.
- Path jail implementation audit.
- Canonicalization implementation audit.
- Runtime-level no-mutation proof.
- Cancellation semantics.
- Local-only guarantee.
- Redaction and sensitive-data enforcement.
- Exclusion policy enforcement.
- Evidence and timeline emission.
- Kill switch, rollback, and disable strategy.
- Full suite and adversarial tests.

Kill switch, rollback, and disable strategy are required.

Real path canonicalization must not be introduced casually.

Folder enumeration and content access are separate capabilities and require separate gates.

Content fingerprinting is separate and requires a separate gate.

Indexing, representation build, and LLM context remain blocked.

Cloud, provider, and runtime remain blocked.

Synthetic policy regression pack is necessary but not sufficient.

Any future operational prototype must fail closed.

## Consequences

- More explicit gates before operational access.
- Safer failure behavior.
- Higher audit burden before any real implementation.
- Clear separation between synthetic regression and operational authorization.

## Accepted Alternatives

- Continue synthetic-only regression.
- Add disabled UI preview for review.
- Require future per-capability gates.

## Rejected Alternatives

- Implicit operational access.
- Broad crawler.
- One gate for all access modes.
- Cloud-assisted scan.
- Provider-assisted context build.
- Runtime execution from Project Understanding outputs.

## Required Next Milestones

- Disabled Path Jail UI preview closeout.
- Operational access audit gate.
- Synthetic policy regression pack.
- Future per-capability enablement ADRs.

## Explicit Non-Goals

- No operational filesystem access in this milestone.
- No operational scan behavior in this milestone.
- No LLM context build in this milestone.
- No provider, cloud, or runtime behavior in this milestone.

## Audit Triggers

- Any marker for operational filesystem access.
- Any marker for OS path resolution.
- Any marker for content access or fingerprinting.
- Any marker for index, representation, or LLM context build.
- Any provider, cloud, or runtime marker.

## Kill Switch / Rollback / Disable Strategy

- Future prototype must default to disabled.
- Audit failure blocks enablement.
- Consent withdrawal blocks operational behavior.
- Policy failure blocks operational behavior.
- Redaction failure blocks operational behavior.

## Decision Record

Decision: `OPERATIONAL_FILESYSTEM_ACCESS_NOT_READY_AUDIT_REQUIRED`

This closes the operational access audit ADR without enabling operational behavior.
