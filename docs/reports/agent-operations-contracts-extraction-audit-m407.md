# Agent Operations Contracts Extraction Audit M407

Project: NODAL OS

## Executive Summary

M407 audited whether Agent Operations contracts can be extracted from `OneBrain.BrowserExecutor.Contracts` into a dedicated contracts project without creating project reference cycles or changing runtime behavior.

Decision: feasible. The selected strategy is to create `OneBrain.AgentOperations.Contracts`, move contract-only Agent Operations files into that project, and preserve the existing `OneBrain.BrowserExecutor.Contracts` namespace as a compatibility shim. Services and browser adapters remain in place for later phases.

## Existing Projects

| Project | Role | M407 relevance |
| --- | --- | --- |
| `src/OneBrain.BrowserExecutor.Contracts` | Historical browser executor contracts project | Previously hosted Agent Operations contracts. Remains for non-Agent Operations contracts. |
| `src/OneBrain.BrowserExecutor.Cdp` | Browser/CDP services, validators, builders, adapters | Continues hosting Agent Operations services until Phase 2. References the new contracts project. |
| `tests/OneBrain.Safety.Tests` | Safety and architecture tests | References the new contracts project directly to protect the boundary. |
| `src/OneBrain.AgentOperations.Contracts` | New Agent Operations contracts boundary | New Phase 1 extraction target. |

## References Before Extraction

`OneBrain.BrowserExecutor.Cdp` referenced:

- `OneBrain.Core`
- `OneBrain.BrowserExecutor.Contracts`

`OneBrain.Safety.Tests` referenced:

- `OneBrain.BrowserExecutor.Cdp`
- `OneBrain.BrowserExecutor.Contracts`
- other existing application/test projects

## Contracts To Move

| Contract file | Classification | Dependency notes |
| --- | --- | --- |
| `NodalOsAgentWorkboardContracts.cs` | Contracts candidate | Defines mission/task/blocker/verification/evidence compatibility symbols. |
| `NodalOsFailureTaxonomyContracts.cs` | Contracts candidate | Defines failure taxonomy enums/records. |
| `NodalOsRunReportContracts.cs` | Contracts candidate | Depends on workboard evidence refs and failure taxonomy. |
| `NodalOsRecipeManifestContracts.cs` | Contracts candidate | Pure recipe manifest records/enums. |
| `NodalOsVerificationBeforeDoneContracts.cs` | Contracts candidate | Depends on mission/task/run report contracts. |
| `NodalOsAgentProgressReportingContracts.cs` | Contracts candidate | Depends on verification and blocker/evidence contracts. |
| `NodalOsStepLibraryContracts.cs` | Contracts candidate | Depends on recipe/failure taxonomy concepts. |
| `NodalOsRedactionContracts.cs` | Contracts candidate | Pure redaction models. |
| `NodalOsEvidenceRefBridgeContracts.cs` | Contracts candidate | Depends on evidence ref and redaction-state concepts. |
| `NodalOsPackageSkillManifestContracts.cs` | Contracts candidate | Pure package/skill manifest contracts. |
| `NodalOsInternalSkillRegistryContracts.cs` | Contracts candidate | Depends on package/skill capability and risk enums. |
| `NodalOsWorkerBoundaryContracts.cs` | Contracts candidate | Depends on evidence bridge refs and failure taxonomy. |

## Dependency Findings

- No candidate contract depends on `OneBrain.BrowserExecutor.Cdp`.
- No candidate contract depends on Chrome/CDP implementation types.
- Several contracts depend on other Agent Operations contracts; moving them together avoids partial-boundary coupling.
- Some compatibility symbols still use `Nexa*`; they remain compatibility symbols and are not renamed in this phase.
- The contracts retain the historical namespace `OneBrain.BrowserExecutor.Contracts` to avoid a broad using/namespace churn in Phase 1.

## Cycle Risk

No project reference cycle is required for Phase 1:

- `OneBrain.AgentOperations.Contracts` has no project references.
- `OneBrain.BrowserExecutor.Cdp` can reference `OneBrain.AgentOperations.Contracts`.
- `OneBrain.Safety.Tests` can reference `OneBrain.AgentOperations.Contracts`.
- `OneBrain.BrowserExecutor.Contracts` does not reference `OneBrain.AgentOperations.Contracts`.

## Strategy Chosen

1. Create `src/OneBrain.AgentOperations.Contracts`.
2. Move all 12 Agent Operations contract files into that project.
3. Preserve namespace `OneBrain.BrowserExecutor.Contracts` as a compatibility shim.
4. Update `OneBrain.slnx`.
5. Add one-way project references from CDP services and safety tests to the new contracts project.
6. Do not move services, builders, validators, browser adapters, runtime code, or UI.

## Compatibility Shims Needed

Compatibility is preserved by namespace strategy rather than duplicate facade types:

- Physical assembly boundary changes to `OneBrain.AgentOperations.Contracts`.
- Type namespace remains `OneBrain.BrowserExecutor.Contracts`.
- Existing source `using OneBrain.BrowserExecutor.Contracts;` remains valid.
- No duplicate types are introduced.

This is intentionally conservative because duplicate records/enums across namespaces would create serialization and API ambiguity.

## What Does Not Move

- `NodalOsAgentWorkboardServices.cs`
- `NodalOsRunReportingServices.cs`
- `NodalOsRecipeManifestServices.cs`
- `NodalOsVerificationBeforeDoneGate.cs`
- `NodalOsAgentProgressReportingServices.cs`
- `NodalOsStepLibraryServices.cs`
- `NodalOsRedactionServices.cs`
- `NodalOsEvidenceRefBridgeServices.cs`
- `NodalOsPackageSkillManifestServices.cs`
- `NodalOsInternalSkillRegistryServices.cs`
- `NodalOsWorkerBoundaryServices.cs`
- Browser runtime smoke/cleanup/CDP executor code
- Browser persistent audit ledger code

## Risks

| Risk | Severity | Mitigation |
| --- | --- | --- |
| Namespace still says `BrowserExecutor.Contracts` despite new project | Medium | Document as Phase 1 shim. Defer namespace cleanup to compatibility phase. |
| Consumers may need explicit project reference to new contracts assembly | Medium | Add references to CDP and Safety.Tests now. Future consumers must reference the new assembly. |
| Services remain in BrowserExecutor.Cdp | Medium | Phase 2 will extract core services. |
| `Nexa*` compatibility symbols remain | Low | Preserved intentionally; naming debt handled by later phase. |

## Feasibility Decision

`M407` feasibility decision: proceed with contracts extraction. No project reference cycle was found.

