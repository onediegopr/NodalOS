# Agent Operations Core Services Extraction Audit M410

Project: NODAL OS

## Executive Summary

M410 audited whether Agent Operations core services can be extracted from `OneBrain.BrowserExecutor.Cdp` into `OneBrain.AgentOperations.Core` without moving browser adapters, Chrome/CDP runtime, OCR runtime, UI, orchestration, or execution behavior.

Decision: feasible. The 11 candidate service files are pure Agent Operations logic and depend only on Agent Operations contracts plus BCL utilities such as JSON serialization and regular expressions. They do not depend on Chrome, CDP sessions, page/session launch code, WebSocket runtime, browser cleanup, OCR runtime, or UI.

## Existing Projects

| Project | Role | M410 relevance |
| --- | --- | --- |
| `OneBrain.AgentOperations.Contracts` | Extracted Phase 1 contracts boundary | Required dependency for Core. |
| `OneBrain.BrowserExecutor.Cdp` | Historical host for Agent Operations services and browser/CDP runtime | Source project for core service extraction. Remains owner of browser-specific implementation. |
| `OneBrain.BrowserExecutor.Contracts` | Historical browser executor contracts | Remains for non-Agent Operations browser executor contracts. |
| `OneBrain.Safety.Tests` | Architecture and safety tests | Must reference the new Core project directly. |

## References Before Extraction

`OneBrain.BrowserExecutor.Cdp` referenced:

- `OneBrain.Core`
- `OneBrain.AgentOperations.Contracts`
- `OneBrain.BrowserExecutor.Contracts`

`OneBrain.AgentOperations.Contracts` referenced no projects.

## Service Candidate Classification

| Service file | Classification | Dependency notes |
| --- | --- | --- |
| `NodalOsAgentWorkboardServices.cs` | SafeToMove | Uses Agent Operations contracts only. |
| `NodalOsRunReportingServices.cs` | SafeToMove | Uses run report/failure contracts only. Browser word appears in diagnostic text, not runtime dependency. |
| `NodalOsRecipeManifestServices.cs` | SafeToMove | Uses JSON serializer and Agent Operations contracts only. |
| `NodalOsVerificationBeforeDoneGate.cs` | SafeToMove | Uses mission/task/run report contracts only. |
| `NodalOsAgentProgressReportingServices.cs` | SafeToMove | Uses progress, verification, blocker, evidence contracts only. |
| `NodalOsStepLibraryServices.cs` | SafeToMove | Uses step, recipe, failure contracts only. |
| `NodalOsPackageSkillManifestServices.cs` | SafeToMove | Uses JSON serializer, redaction, and package/skill contracts only. |
| `NodalOsInternalSkillRegistryServices.cs` | SafeToMove | Uses JSON serializer, redaction, package/skill, and registry contracts only. |
| `NodalOsWorkerBoundaryServices.cs` | SafeToMove | Uses JSON serializer, redaction, evidence bridge, failure taxonomy, and worker contracts only. |
| `NodalOsEvidenceRefBridgeServices.cs` | SafeToMove | Uses evidence bridge contracts and common redaction service only. |
| `NodalOsRedactionServices.cs` | SafeToMove | Uses Regex and redaction contracts only. |

## Browser/CDP Coupling Audit

The candidate files were scanned for:

- `Chrome`
- `Cdp`
- `Browser`
- `BrowserRuntime`
- `PageSession`
- `WebSocket`
- `HttpClient`
- process/runtime execution concepts

Findings:

- No candidate uses Chrome/CDP implementation types.
- No candidate opens browser sessions.
- No candidate starts processes.
- No candidate performs runtime execution.
- Browser-related words only appear in text labels or the historical namespace.

## Strategy Chosen

1. Create `src/OneBrain.AgentOperations.Core`.
2. Reference `OneBrain.AgentOperations.Contracts`.
3. Move all 11 SafeToMove services into the Core project.
4. Preserve namespace `OneBrain.BrowserExecutor.Cdp` as a compatibility shim.
5. Add one-way project reference from `OneBrain.BrowserExecutor.Cdp` to `OneBrain.AgentOperations.Core`.
6. Add direct Safety.Tests reference to `OneBrain.AgentOperations.Core`.
7. Do not move browser runtime/adapters.

## Compatibility Shims Needed

Compatibility is preserved by namespace strategy:

- Physical assembly/project boundary changes to `OneBrain.AgentOperations.Core`.
- Type namespace remains `OneBrain.BrowserExecutor.Cdp`.
- Existing source imports remain valid.
- No duplicate service types are introduced.

This is intentionally conservative because a namespace move would be a broad rename and should be deferred to the compatibility cleanup phase.

## What Does Not Move

- `BrowserRuntimeSmoke.cs`
- `ChromeCdpBrowserExecutor.cs`
- CDP launch/session/page/frame/download/upload/browser-specific services
- `BrowserPersistentAuditLedger` if browser-specific
- OCR runtime
- UI
- orchestration
- execution

## Cycle Risk

No cycle is required:

- `OneBrain.AgentOperations.Core` references `OneBrain.AgentOperations.Contracts`.
- `OneBrain.AgentOperations.Core` does not reference `OneBrain.BrowserExecutor.Cdp`.
- `OneBrain.BrowserExecutor.Cdp` references `OneBrain.AgentOperations.Core`.

## Feasibility Decision

`M410` feasibility decision: proceed with core service extraction. No project reference cycle and no browser adapter coupling were found for the 11 candidate service files.

