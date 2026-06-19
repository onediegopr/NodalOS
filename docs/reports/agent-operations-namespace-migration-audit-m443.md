# NODAL OS - Agent Operations Namespace Migration Audit M443

## Scope

M443 audits only the physical Agent Operations assemblies:

- `src/OneBrain.AgentOperations.Contracts`
- `src/OneBrain.AgentOperations.Core`
- `src/OneBrain.AgentOperations.Adapters.Browser`

No Browser/CDP runtime, scheduler, API, UI, worker runtime, execution engine, OCR surface, broad project rename, or legacy deletion is in scope.

## Files With Historical Contract Namespace

Before migration, these files lived in `OneBrain.AgentOperations.Contracts` but declared `OneBrain.BrowserExecutor.Contracts`:

- `NodalOsAgentProgressReportingContracts.cs`
- `NodalOsAgentWorkboardContracts.cs`
- `NodalOsEvidenceRefBridgeContracts.cs`
- `NodalOsFailureTaxonomyContracts.cs`
- `NodalOsInternalSkillRegistryContracts.cs`
- `NodalOsOrchestrationCommandContracts.cs`
- `NodalOsPackageSkillManifestContracts.cs`
- `NodalOsRecipeManifestContracts.cs`
- `NodalOsRedactionContracts.cs`
- `NodalOsRunReportContracts.cs`
- `NodalOsScheduledReadOnlyRunContracts.cs`
- `NodalOsStepLibraryContracts.cs`
- `NodalOsVerificationBeforeDoneContracts.cs`
- `NodalOsWorkerBoundaryContracts.cs`

## Files With Historical Core Namespace

Before migration, these files lived in `OneBrain.AgentOperations.Core` but declared `OneBrain.BrowserExecutor.Cdp`:

- `NodalOsAgentProgressReportingServices.cs`
- `NodalOsAgentWorkboardServices.cs`
- `NodalOsEvidenceRefBridgeServices.cs`
- `NodalOsInternalSkillRegistryServices.cs`
- `NodalOsOrchestrationCommandServices.cs`
- `NodalOsOrchestrationInProcessFacade.cs`
- `NodalOsPackageSkillManifestServices.cs`
- `NodalOsRecipeManifestServices.cs`
- `NodalOsRedactionServices.cs`
- `NodalOsRunReportingServices.cs`
- `NodalOsScheduledReadOnlyRunServices.cs`
- `NodalOsStepLibraryServices.cs`
- `NodalOsVerificationBeforeDoneGate.cs`
- `NodalOsWorkerBoundaryServices.cs`

## Internal Consumers

The main internal consumer set is `tests/OneBrain.Safety.Tests`, which historically imported `OneBrain.BrowserExecutor.Contracts` and `OneBrain.BrowserExecutor.Cdp` for both browser runtime and Agent Operations types.

`OneBrain.BrowserExecutor.Cdp` remains a valid namespace for browser-specific code and temporary adapter hosting. It is not broadly renamed in this milestone.

## Canonical Namespace Plan

- Contracts: `OneBrain.AgentOperations.Contracts`
- Core: `OneBrain.AgentOperations.Core`
- Browser adapter skeleton: `OneBrain.AgentOperations.Adapters.Browser`

The migration updates only files physically located in Agent Operations projects.

## Shim Plan

Type-level compatibility shims were reviewed and deliberately not created in this pass. Most Agent Operations contracts are enums or sealed records. Duplicating them in legacy namespaces would create divergent runtime types and could break validators, serializers, and cross-layer invariants.

Instead, internal source compatibility is preserved by updating consumers to canonical namespaces through a test project global using file. If external source compatibility becomes required later, it should be handled with a dedicated compatibility package or explicit type-forwarding design, not duplicated models.

## Risks

- Updating namespaces can break consumers that still rely on the historical namespace.
- Creating fake aliases for enums/records can be worse than a clean source migration because it creates distinct types.
- Browser-specific code must remain under `BrowserExecutor.*` until browser adapter extraction.

## What Is Not Migrated

- `src/OneBrain.BrowserExecutor.Contracts`
- `src/OneBrain.BrowserExecutor.Cdp`
- Browser runtime classes
- Chrome/CDP classes
- OCR contracts/services
- Legacy `Nexa*` symbols
- Any runtime execution path

## Decision

Proceed with scoped canonical namespace migration for Agent Operations Contracts/Core and use source-level internal consumer updates instead of duplicate type shims.
