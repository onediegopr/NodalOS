# Agent Operations Browser Adapter Boundary Audit M413

Project: NODAL OS

## Executive Summary

M413 audited the current boundary after Agent Operations contracts and core services were extracted into:

- `OneBrain.AgentOperations.Contracts`
- `OneBrain.AgentOperations.Core`

`OneBrain.BrowserExecutor.Cdp` remains the temporary host for browser/CDP runtime and browser adapter code. The selected M413-M415 strategy is docs plus dependency guard tests. A dedicated `OneBrain.AgentOperations.Adapters.Browser` project is deferred until there is a concrete move of browser adapter classes.

## Current Projects

| Project | Current role | Boundary decision |
| --- | --- | --- |
| `OneBrain.AgentOperations.Contracts` | Agent Operations contracts/enums/records | Must stay Browser/CDP-free at project-reference level. |
| `OneBrain.AgentOperations.Core` | Agent Operations core validators/builders/serializers/services | Must stay Browser/CDP-free at project-reference level. |
| `OneBrain.BrowserExecutor.Cdp` | Browser/CDP runtime, OCR runtime, historical browser services, temporary browser adapter host | May depend on Agent Operations Core. Must keep browser-specific implementation out of Core. |

## Current References

`OneBrain.AgentOperations.Contracts`:

- no project references

`OneBrain.AgentOperations.Core`:

- references `OneBrain.AgentOperations.Contracts`

`OneBrain.BrowserExecutor.Cdp`:

- references `OneBrain.Core`
- references `OneBrain.AgentOperations.Contracts`
- references `OneBrain.AgentOperations.Core`
- references `OneBrain.BrowserExecutor.Contracts`

## BrowserRuntimeStay

These classes are browser/runtime-specific and must stay in `OneBrain.BrowserExecutor.Cdp`:

- `ChromeCdpBrowserExecutor.cs`
- `BrowserRuntimeSmoke.cs`
- `BrowserProfileSessionManager.cs`
- `BrowserTargetFrameManager.cs`
- `BrowserRuntimePhaseServices.cs`
- `BrowserEmbeddedRuntimeServices.cs`
- `ChromeCdpExternalProofServices.cs`
- browser safe download/upload services
- browser authenticated sandbox/document workflow services
- browser recorder/replay prototype services

## BrowserAdapterCandidate

These are adapter candidates for a future `OneBrain.AgentOperations.Adapters.Browser` project, but are not moved in this hito:

- `BrowserExecutorFsmAdapter.cs`
- `BrowserHumanHandoffCompanionAdapter.cs`
- `BrowserRecipeReplayServices.cs`
- `BrowserRecorderPrototypeServices.cs`
- `BrowserDocumentWorkflowSandboxServices.cs`
- `BrowserExternalReadOnlyServices.cs`
- `BrowserSafeDownloadServices.cs`
- `BrowserSafeUploadServices.cs`

They are candidates because they connect browser-facing concepts to higher-level operational workflows, but several still depend on browser audit, credentials, safe download/upload, runtime gates, or fixture-specific browser policies.

## AgentOperationsCoreConsumer

`OneBrain.BrowserExecutor.Cdp` is allowed to consume Agent Operations Core. That direction is valid:

```text
BrowserExecutor.Cdp -> AgentOperations.Core -> AgentOperations.Contracts
```

The reverse is prohibited:

```text
AgentOperations.Core -> BrowserExecutor.Cdp
AgentOperations.Contracts -> BrowserExecutor.Cdp
```

## BrowserPersistentAuditLedger Classification

`BrowserPersistentAuditLedger.cs` is classified as `AuditLedgerBrowserSpecific`.

Reason:

- it uses browser audit policy and browser audit event contracts;
- it persists `browser-audit-ledger.jsonl`;
- it creates browser/vault/session/profile audit events;
- it is used by browser external proof and OCR evidence integration paths.

Decision: do not move it in M413-M415. A future shared evidence ledger bridge can integrate with it, but this class remains browser-specific for now.

## AgentOperations.Core Dependencies

Agent Operations Core is intentionally limited to Agent Operations contracts and BCL dependencies. It has no project reference to BrowserExecutor.Cdp.

The namespace is still `OneBrain.BrowserExecutor.Cdp` as a compatibility shim from Phase 2. This is naming debt, not a runtime dependency. It should be handled in a later compatibility cleanup phase, not in the browser adapter boundary hito.

## Dependency Direction Rules

Allowed:

- `AgentOperations.Core -> AgentOperations.Contracts`
- `BrowserExecutor.Cdp -> AgentOperations.Core`
- `BrowserExecutor.Cdp -> AgentOperations.Contracts`

Prohibited:

- `AgentOperations.Contracts -> BrowserExecutor.Cdp`
- `AgentOperations.Core -> BrowserExecutor.Cdp`
- browser runtime code inside `AgentOperations.Core`
- Chrome/CDP launch/session/page/frame/download/upload code inside `AgentOperations.Core`

## Risks

| Risk | Severity | Mitigation |
| --- | --- | --- |
| Browser adapter candidates remain mixed with broader browser runtime | Medium | Document and protect direction now; extract adapter project only when moving real adapter classes. |
| Namespace shim still says `BrowserExecutor.Cdp` in Core services | Medium | Defer namespace cleanup; avoid broad rename in this hito. |
| `BrowserPersistentAuditLedger` may look like shared evidence infrastructure | Medium | Classify as browser-specific and do not move until evidence architecture phase. |
| String-based tests could become brittle because legitimate contracts mention `BrowserRuntime` source kinds | Low | Guard project references and physical file ownership instead of banning all strings. |

## Adapter Project Decision

Adapter project deferred.

Rationale:

- no behavior should move in this hito;
- an empty project adds little value;
- moving Chrome/CDP runtime is explicitly out of scope;
- adapter candidates need a later extraction with behavior-preserving tests.

## What Does Not Move

- `ChromeCdpBrowserExecutor.cs`
- `BrowserRuntimeSmoke.cs`
- `BrowserPersistentAuditLedger.cs`
- browser session/launch/cleanup/runtime files
- OCR runtime
- UI
- orchestration
- execution

