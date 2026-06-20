# ADR: Project Understanding Implementation Boundary

Date: 2026-06-20

Decision: `NODAL_OS_PROJECT_UNDERSTANDING_IMPLEMENTATION_BOUNDARY_DEFINED`

## Context

NODAL OS already has governed Project Understanding preconditions, a scan audit gate, consent and scope contracts, sensitive-data policy preview, exclusion policy preview, dry-run contract, dry-run UI preview, consent review cards, and an evidence plan.

There is still no operational scan behavior and no direct content access. The next safe step is a prototype contract plus a synthetic fixture matrix before any implementation that touches real workspace content.

## Decision

Project Understanding implementation will proceed only after these prerequisites exist and pass audit:

- Path Jail Prototype Contract.
- Scan Fixture Matrix.
- Synthetic-only tests.
- Dry-run simulator contract.
- Audit checkpoint.
- Explicit user consent.
- No-mutation guarantee.
- Cancellation semantics.
- Evidence and timeline plan.
- Redaction, sensitive-data, and exclusion policies.

The first future implementation step must be read-only and synthetic fixture first. This ADR does not enable folder enumeration, content access, content fingerprinting, indexing, vectorization, LLM context, prompt construction, provider activity, cloud, runtime, or execution.

## Consequences

- Implementation moves more slowly, but safety and auditability increase.
- Sensitive-data exposure risk is reduced.
- Workspace boundary mistakes are less likely because path policy is separated from operational behavior.
- Contract, prototype, and real implementation remain separate phases.

## Accepted Alternatives

- Contract-first with synthetic fixtures.
- Prototype-only with symbolic paths.
- Explicit future gate before operational scan behavior.

## Rejected Alternatives

- Direct operational scan.
- Direct content access.
- Source-control operations as an understanding shortcut.
- Direct LLM context construction.
- Vectorization or indexing first.
- Cloud scan.
- Broad crawler.
- Implicit consent.

## Required Next Milestones

- Synthetic path jail prototype.
- Fixture-based dry-run simulator.
- Sensitive-data and exclusion fixture validation.
- Cancellation and no-mutation semantics.
- Audit before operational filesystem access.

## Explicit Non-Goals

- No operational scan behavior.
- No operational filesystem access.
- No LLM use.
- No prompt construction.
- No cloud.
- No runtime.
- No execution.
