# Agent Operations Browser Adapter Boundary M415

Project: NODAL OS

## Boundary Result

M413-M415 defines and protects the browser adapter boundary after Agent Operations contracts and core services extraction.

No runtime code moved. No adapter project was created in this hito. `OneBrain.BrowserExecutor.Cdp` remains the temporary browser adapter host while `OneBrain.AgentOperations.Core` and `OneBrain.AgentOperations.Contracts` remain Browser/CDP-free at project-reference level.

## Current Boundary

```text
OneBrain.AgentOperations.Contracts
  - contracts/enums/records only
  - no BrowserExecutor.Cdp reference

OneBrain.AgentOperations.Core
  - validators/builders/serializers/core services
  - references AgentOperations.Contracts
  - no BrowserExecutor.Cdp reference

OneBrain.BrowserExecutor.Cdp
  - browser/CDP runtime
  - browser-specific audit ledger
  - OCR runtime
  - temporary browser adapter host
  - may consume AgentOperations.Core
```

## What Stays In BrowserExecutor.Cdp

- `ChromeCdpBrowserExecutor.cs`
- `BrowserRuntimeSmoke.cs`
- `BrowserPersistentAuditLedger.cs`
- browser launch/session/cleanup/runtime code
- safe download/upload browser services
- recorder/replay browser prototypes
- external read-only browser proof services
- OCR runtime and OCR evidence integration

## Future Browser Adapter Boundary

Future target:

- `OneBrain.AgentOperations.Adapters.Browser`

Possible future contents:

- browser-to-Agent-Operations translation adapters;
- browser runtime event to run/progress report mappers;
- browser evidence to EvidenceRef bridge adapters;
- browser policy decision to Agent Operations report adapters.

Not included now:

- Chrome/CDP executor;
- BrowserRuntimeSmoke;
- BrowserPersistentAuditLedger;
- OCR runtime;
- execution/orchestration runtime.

## Dependency Direction

Allowed:

```text
BrowserExecutor.Cdp -> AgentOperations.Core -> AgentOperations.Contracts
```

Prohibited:

```text
AgentOperations.Core -> BrowserExecutor.Cdp
AgentOperations.Contracts -> BrowserExecutor.Cdp
```

## BrowserPersistentAuditLedger Classification

`BrowserPersistentAuditLedger` remains classified as browser-specific audit infrastructure. It is not moved because it is tied to browser audit event shapes, browser profile/session IDs, vault consent decisions, and browser proof persistence.

## Tests

M415 adds guard tests for:

- audit and boundary report existence;
- artifact existence and safety flags;
- `AgentOperations.Core` does not reference `BrowserExecutor.Cdp`;
- `AgentOperations.Contracts` does not reference `BrowserExecutor.Cdp`;
- `BrowserExecutor.Cdp` references `AgentOperations.Core`;
- `ChromeCdpBrowserExecutor.cs` remains in BrowserExecutor.Cdp;
- `BrowserRuntimeSmoke.cs` remains in BrowserExecutor.Cdp;
- roadmap update;
- NODAL OS project naming in reports.

## What Was Not Implemented

- No adapter project.
- No runtime behavior change.
- No UI.
- No orchestration API.
- No worker runtime.
- No registry persistence.
- No recipe, skill, or step execution.
- No browser actions.
- No OCR changes.
- No broad rename.
- No legacy delete.

## Next Recommended Milestone

`M416-M418 Orchestration API Decision Record or M416-M418 Agent Operations Extraction Phase 3 Adapter Project`

Recommended order: do the Orchestration API Decision Record before moving adapter implementation if the next decision is about orchestration semantics; do adapter project extraction first if the next work is strictly modular cleanup.

