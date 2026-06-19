# Agent Operations Browser Adapter Project Skeleton M430

Project: NODAL OS

Milestone: M428-M430

Base commit: c051e85

## What Was Created

M428-M430 created `src/OneBrain.AgentOperations.Adapters.Browser` as the physical project boundary for future browser-specific Agent Operations adapters.

Created:

- `OneBrain.AgentOperations.Adapters.Browser.csproj`
- `NodalOsBrowserAgentOperationsAdapterBoundary`
- dependency-direction tests
- audit/report/artifact
- roadmap entry

## What Was Not Moved

- `ChromeCdpBrowserExecutor`
- `BrowserRuntimeSmoke`
- `BrowserPersistentAuditLedger`
- browser profile/session/frame/download/upload services
- CDP runtime
- OCR runtime
- recipe execution
- skill execution
- step execution
- orchestration execution

## Dependency Direction

The adapter skeleton references:

- `OneBrain.AgentOperations.Contracts`
- `OneBrain.AgentOperations.Core`

It does not reference:

- `OneBrain.BrowserExecutor.Cdp`
- Chrome/CDP packages
- browser runtime packages

`OneBrain.AgentOperations.Contracts` and `OneBrain.AgentOperations.Core` remain Browser/CDP-free.

## Why Cdp Is Not Referenced Yet

The project is a skeleton boundary only. Referencing `OneBrain.BrowserExecutor.Cdp` now would blur the dependency direction before actual adapter classes are selected and moved. `OneBrain.BrowserExecutor.Cdp` remains the temporary adapter host until a future extraction phase.

## Future Adapter Moves

Future candidates include browser FSM adapter, human handoff companion adapter, recipe replay, recorder prototype, document workflow sandbox, external read-only, safe download, and safe upload services.

Each future move must prove no runtime behavior change and must avoid pulling Chrome/CDP dependencies into AgentOperations.Core or Contracts.

## No Runtime Behavior Change

The marker type exposes boundary metadata only:

- `AdapterProjectSkeleton=true`
- `RuntimeBehaviorImplemented=false`
- `BrowserRuntimeMoved=false`
- `OrchestrationApiImplemented=false`
- `ExecutionImplemented=false`

No runtime behavior, UI, scheduler, API, worker runtime, browser startup, or execution was implemented.

## Next Steps

Recommended next milestone: `M431-M433 Scheduled Read-Only Runs Decision Record or M431-M433 Browser Adapter Extraction Phase 1`.
