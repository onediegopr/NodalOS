# Agent Operations Browser Adapter Project Skeleton Audit M428

Project: NODAL OS

Milestone: M428-M430

Base commit: c051e85

## Current State

Agent Operations contracts and core services have been physically extracted into:

- `src/OneBrain.AgentOperations.Contracts`
- `src/OneBrain.AgentOperations.Core`

`OneBrain.BrowserExecutor.Cdp` remains the temporary browser adapter host. Browser runtime classes, CDP classes, browser profile/session/frame/download/upload services, OCR runtime, and browser audit infrastructure remain there.

## Existing Projects

- `OneBrain.AgentOperations.Contracts`
- `OneBrain.AgentOperations.Core`
- `OneBrain.BrowserExecutor.Cdp`
- `OneBrain.BrowserExecutor.Contracts`
- `OneBrain.Core`
- existing application, safety, test, and tool projects

## Current References

- `OneBrain.AgentOperations.Core` references `OneBrain.AgentOperations.Contracts`.
- `OneBrain.BrowserExecutor.Cdp` references `OneBrain.AgentOperations.Contracts` and `OneBrain.AgentOperations.Core`.
- `OneBrain.AgentOperations.Contracts` remains Browser/CDP-free.
- `OneBrain.AgentOperations.Core` remains Browser/CDP-free.

## Future Browser Adapter Candidates

- `BrowserExecutorFsmAdapter`
- `BrowserHumanHandoffCompanionAdapter`
- `BrowserRecipeReplayServices`
- `BrowserRecorderPrototypeServices`
- `BrowserDocumentWorkflowSandboxServices`
- `BrowserExternalReadOnlyServices`
- `BrowserSafeDownloadServices`
- `BrowserSafeUploadServices`

These are candidates only. They are not moved in this milestone.

## Stay In BrowserExecutor.Cdp For Now

- `ChromeCdpBrowserExecutor`
- `BrowserRuntimeSmoke`
- `BrowserPersistentAuditLedger`
- Browser profile/session/frame/download/upload services
- CDP runtime
- OCR runtime
- browser-specific audit and evidence plumbing

## What Can Be Created Now

Create `OneBrain.AgentOperations.Adapters.Browser` as a skeleton project with:

- references to `OneBrain.AgentOperations.Contracts`
- references to `OneBrain.AgentOperations.Core`
- no reference to `OneBrain.BrowserExecutor.Cdp`
- no Chrome/CDP package references
- a marker boundary type only
- no behavior
- no browser startup
- no runtime move

## What Must Not Move Yet

No Chrome/CDP runtime, browser smoke, browser audit ledger, browser profile/session/frame/download/upload services, OCR runtime, recipe execution, skill execution, step execution, scheduler, API, UI, or orchestration execution moves in this block.

## Cycle Risk

The skeleton project can reference Contracts/Core without a cycle. It must not be referenced by Core or Contracts. It must not reference `OneBrain.BrowserExecutor.Cdp` until a future adapter extraction phase explicitly moves browser adapter implementation.

## Reference Decision

The skeleton references:

- `OneBrain.AgentOperations.Contracts`
- `OneBrain.AgentOperations.Core`

It intentionally does not reference:

- `OneBrain.BrowserExecutor.Cdp`
- Chrome/CDP packages
- browser runtime packages

## Decision

Create the adapter skeleton project now. Defer runtime moves and real adapter implementation to a future milestone.
