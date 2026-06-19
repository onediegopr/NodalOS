# Agent Operations Namespace / Naming ADR

## Status

Accepted for M389-M391 with migration deferred.

## Context

NODAL OS built an Agent Operations layer between M344 and M388. The layer includes Mission/Task, blockers, verification before done, failure taxonomy, run reports, recipe manifests, step library, redaction, and evidence reference bridge contracts.

Most of this layer currently lives in:

- `OneBrain.BrowserExecutor.Contracts`
- `OneBrain.BrowserExecutor.Cdp`

Some public compatibility types still use `Nexa*`, while newer types use `NodalOs*`.

The current product name is NODAL OS. NEXA is historical/compatibility naming only.

## Problem

Agent Operations is conceptually core/platform-layer functionality. It should not depend conceptually on Browser/CDP.

Current placement and naming create future risks:

- UI or orchestration may depend on BrowserExecutor namespaces;
- package/skill registry work may inherit browser-specific coupling;
- `Nexa*` may be mistaken for the current naming standard;
- a future namespace move may become harder once orchestration/UI/persistence are built.

## Decision

Do not move namespaces or broadly rename symbols in M389-M391.

Document the current placement as temporary compatibility debt and define the future extraction boundary.

## Current State

Current Agent Operations contracts live in `OneBrain.BrowserExecutor.Contracts`.

Current Agent Operations services live in `OneBrain.BrowserExecutor.Cdp`.

Existing `Nexa*` types remain compatibility symbols. Examples include `NexaMission`, `NexaAgentTask`, `NexaEvidenceRef`, `NexaFailureKind`, and `NexaRunReport`.

Existing `OneBrain.*` namespaces remain historical implementation namespaces.

## Naming Rules Forward

New Agent Operations types must use `NodalOs*`.

No new `Nexa*` Agent Operations types should be introduced.

Exceptions are allowed only for explicit compatibility shims or adapters, and those must be documented as compatibility.

Docs should use NODAL OS as the current project/product name. NEXA may appear only as historical or compatibility context.

## Compatibility Rules

Existing `Nexa*` symbols are retained for compatibility.

Existing `OneBrain.*` namespaces are retained for compatibility.

Compatibility shims must preserve behavior and serialization until a dedicated extraction milestone moves contracts and services.

Broad rename is deferred because it would create unnecessary churn before package/skill, orchestration, UI, and persistence boundaries are final.

## Future Module Boundaries

Preferred future boundary:

- `OneBrain.AgentOperations.Contracts`
- `OneBrain.AgentOperations.Core`
- `OneBrain.AgentOperations.Adapters.Browser`

Alternative if the solution later supports product-aligned namespaces:

- `NodalOs.AgentOperations.Contracts`
- `NodalOs.AgentOperations.Core`
- `NodalOs.AgentOperations.Adapters.Browser`

The browser adapter layer should own browser/CDP integration only. Mission/Task, Verification, Progress Reporting, Run Report, Recipe Manifest, Step Library, Redaction, and EvidenceRef bridge contracts should not be browser-bound long term.

## Migration Phases

Phase 1: ADR and forward naming discipline.

- No new `Nexa*` types.
- New types use `NodalOs*`.
- Current BrowserExecutor location is documented as temporary.

Phase 2: Extraction prep.

- Add compatibility adapters/shims.
- Identify internal/public surfaces.
- Add no-divergence serialization tests.

Phase 3: Move contracts/services.

- Move Agent Operations contracts to the selected AgentOperations contracts project/namespace.
- Move validators/builders/core services to AgentOperations core.
- Keep BrowserExecutor-specific adapters in a browser adapter namespace.

Phase 4: Deprecate compatibility aliases.

- Mark old `Nexa*` and BrowserExecutor-hosted Agent Operations entry points as compatibility aliases where safe.
- Preserve JSON compatibility where needed.

Phase 5: Cleanup.

- Remove obsolete compatibility paths only after tests, docs, artifacts, and consumers have migrated.

## Preconditions Before Moving

Do not move Agent Operations until these are true:

- EvidenceRef bridge exists.
- Common redaction service exists.
- Completion semantics are canonicalized.
- Core legacy reference graph exists.
- Package/Skill boundary is defined or immediately next.
- Orchestration/UI consumers are not yet depending on BrowserExecutor placement.

## Non-Goals

- No namespace move now.
- No broad rename now.
- No breaking public/internal contracts now.
- No UI implementation.
- No orchestration API.
- No recipe execution.
- No step execution.
- No package registry.
- No persistence DB.
- No legacy delete.

## Consequences

Short term:

- Existing tests and consumers remain stable.
- Naming debt remains visible but governed.

Long term:

- Agent Operations has a documented extraction path.
- Future work can avoid adding more BrowserExecutor coupling.
- New types converge on NODAL OS naming.

## Acceptance Criteria

- ADR documents `Nexa*` as compatibility debt.
- ADR documents `OneBrain.*` as historical namespace debt.
- ADR defines `NodalOs*` as the forward naming rule.
- ADR documents current BrowserExecutor placement as temporary.
- ADR defines future extraction boundaries.
- ADR explicitly states no namespace move or broad rename is done in this milestone.
