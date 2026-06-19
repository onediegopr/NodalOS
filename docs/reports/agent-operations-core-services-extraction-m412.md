# Agent Operations Core Services Extraction M412

Project: NODAL OS

## Problem Resolved

Agent Operations contracts were extracted in M407-M409, but the core services remained physically hosted by `OneBrain.BrowserExecutor.Cdp`. That kept validators, builders, serializers, redaction, evidence bridge, recipe, registry, and worker boundary logic inside a browser/CDP project even though they do not depend on browser runtime.

M410-M412 executes Phase 2: core services only.

## What Moved

Moved to `src/OneBrain.AgentOperations.Core`:

- `NodalOsAgentWorkboardServices.cs`
- `NodalOsRunReportingServices.cs`
- `NodalOsRecipeManifestServices.cs`
- `NodalOsVerificationBeforeDoneGate.cs`
- `NodalOsAgentProgressReportingServices.cs`
- `NodalOsStepLibraryServices.cs`
- `NodalOsPackageSkillManifestServices.cs`
- `NodalOsInternalSkillRegistryServices.cs`
- `NodalOsWorkerBoundaryServices.cs`
- `NodalOsEvidenceRefBridgeServices.cs`
- `NodalOsRedactionServices.cs`

## New Project

Created:

- `src/OneBrain.AgentOperations.Core/OneBrain.AgentOperations.Core.csproj`

The project:

- targets `net11.0`;
- references `OneBrain.AgentOperations.Contracts`;
- does not reference `OneBrain.BrowserExecutor.Cdp`;
- does not reference browser/CDP runtime implementation.

## Namespace Strategy

The physical project boundary changed, but namespaces remain unchanged:

- current namespace: `OneBrain.BrowserExecutor.Cdp`
- new assembly/project: `OneBrain.AgentOperations.Core`

This is the Phase 2 compatibility shim. It preserves source compatibility and avoids a broad namespace rename.

## Project References

Updated:

- `OneBrain.slnx` now includes `OneBrain.AgentOperations.Core`.
- `OneBrain.BrowserExecutor.Cdp` references `OneBrain.AgentOperations.Core`.
- `OneBrain.Safety.Tests` references `OneBrain.AgentOperations.Core`.

Dependency direction:

```text
OneBrain.AgentOperations.Contracts
  ^ no project references

OneBrain.AgentOperations.Core
  -> OneBrain.AgentOperations.Contracts

OneBrain.BrowserExecutor.Cdp
  -> OneBrain.AgentOperations.Core
  -> OneBrain.AgentOperations.Contracts
  -> OneBrain.Core
  -> OneBrain.BrowserExecutor.Contracts
```

## Shims Used

Compatibility shim used: yes.

The shim is namespace preservation, not duplicate adapters. No duplicate validators/builders/serializers were introduced.

## What Was Not Moved

- `BrowserRuntimeSmoke.cs`
- `ChromeCdpBrowserExecutor.cs`
- Browser/CDP launch/session/cleanup/runtime files
- Browser-specific audit/runtime code
- OCR runtime
- UI
- orchestration
- execution

## Cycle Check

No project reference cycle was introduced.

`OneBrain.AgentOperations.Core` depends on contracts only. `OneBrain.BrowserExecutor.Cdp` depends on core, not the reverse.

## Tests

Added `NodalOsAgentOperationsCoreServicesExtractionM410M412Tests` to verify:

- Core project exists;
- Core references contracts;
- Core does not reference BrowserExecutor.Cdp or Chrome/CDP;
- BrowserExecutor.Cdp references Core;
- Safety.Tests references Core;
- moved services compile and are usable;
- browser runtime/adapters remain in BrowserExecutor.Cdp;
- no runtime/UI/orchestration artifact flags remain true.

## Next Recommended Milestone

`M413-M415 Agent Operations Browser Adapter Boundary or M413-M415 Orchestration API Decision Record`

The pragmatic next step is to define the browser adapter boundary before introducing orchestration. That will make it explicit which runtime-facing pieces remain browser-specific and which Agent Operations APIs can be consumed by future orchestration safely.

