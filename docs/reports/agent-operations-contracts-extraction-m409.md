# Agent Operations Contracts Extraction M409

Project: NODAL OS

## Problem Resolved

Agent Operations contracts had grown inside `OneBrain.BrowserExecutor.Contracts`, which made a browser-oriented project look like the long-term owner of mission/task, recipe, run reporting, step library, redaction, evidence bridge, package/skill, registry, and worker boundary contracts.

M407-M409 executes Phase 1 of the extraction plan: contracts only.

## What Moved

The following files were moved to `src/OneBrain.AgentOperations.Contracts`:

- `NodalOsAgentWorkboardContracts.cs`
- `NodalOsFailureTaxonomyContracts.cs`
- `NodalOsRunReportContracts.cs`
- `NodalOsRecipeManifestContracts.cs`
- `NodalOsVerificationBeforeDoneContracts.cs`
- `NodalOsAgentProgressReportingContracts.cs`
- `NodalOsStepLibraryContracts.cs`
- `NodalOsRedactionContracts.cs`
- `NodalOsEvidenceRefBridgeContracts.cs`
- `NodalOsPackageSkillManifestContracts.cs`
- `NodalOsInternalSkillRegistryContracts.cs`
- `NodalOsWorkerBoundaryContracts.cs`

No core services were moved. No browser adapters were moved.

## New Project

Created:

- `src/OneBrain.AgentOperations.Contracts/OneBrain.AgentOperations.Contracts.csproj`

The project is contract-only:

- target framework: `net11.0`
- nullable enabled
- implicit usings enabled
- no project references
- no Browser/CDP dependency

## Namespace Strategy

The physical project boundary changed, but namespaces remain unchanged:

- current namespace: `OneBrain.BrowserExecutor.Contracts`
- new assembly/project: `OneBrain.AgentOperations.Contracts`

This is a deliberate compatibility shim. It avoids a broad namespace rename and keeps existing source imports meaningful while the project boundary is extracted.

## Project References

Updated:

- `OneBrain.slnx` now includes `OneBrain.AgentOperations.Contracts`.
- `OneBrain.BrowserExecutor.Cdp` references `OneBrain.AgentOperations.Contracts`.
- `OneBrain.Safety.Tests` references `OneBrain.AgentOperations.Contracts`.

`OneBrain.AgentOperations.Contracts` does not reference:

- `OneBrain.BrowserExecutor.Cdp`
- Chrome/CDP implementation code
- browser runtime adapters

## Cycle Check

No project reference cycle was introduced.

Dependency direction after Phase 1:

```text
OneBrain.AgentOperations.Contracts
  ^ no project references

OneBrain.BrowserExecutor.Cdp
  -> OneBrain.AgentOperations.Contracts
  -> OneBrain.BrowserExecutor.Contracts
  -> OneBrain.Core

OneBrain.Safety.Tests
  -> OneBrain.AgentOperations.Contracts
  -> existing test dependencies
```

## Shims Used

Compatibility shim used: yes.

The shim is namespace preservation, not duplicate facade records. This prevents duplicated public contract types and avoids ambiguity in JSON serialization, equality, and test expectations.

## What Was Not Implemented

- No core service extraction.
- No browser adapter extraction.
- No namespace move.
- No broad rename of `Nexa*`.
- No runtime behavior change.
- No UI.
- No orchestration API.
- No worker runtime.
- No registry persistence.
- No recipe, skill, or step execution.

## Tests

Added `NodalOsAgentOperationsContractsExtractionM407M409Tests` to verify:

- new contracts project exists;
- new contracts project does not reference Browser/CDP;
- CDP services reference the new contracts project;
- safety tests reference the new contracts project;
- moved contract files compile and are usable;
- no runtime/UI/orchestration flags remain true in the artifact.

## Next Recommended Milestone

`M410-M412 Agent Operations Extraction Phase 2 Core Services`

Phase 2 should move pure Agent Operations services/builders/validators into `OneBrain.AgentOperations.Core`, keeping browser-specific adapters in `OneBrain.BrowserExecutor.Cdp` or a later browser adapter project.

