# NODAL OS - Agent Operations Namespace Migration M445

## Summary

M443-M445 closes the remaining pre-runtime namespace debt for Agent Operations assemblies. Files physically located under `OneBrain.AgentOperations.Contracts` and `OneBrain.AgentOperations.Core` now declare logical Agent Operations namespaces.

## Migrated Namespaces

Contracts project:

- From `OneBrain.BrowserExecutor.Contracts`
- To `OneBrain.AgentOperations.Contracts`

Core project:

- From `OneBrain.BrowserExecutor.Cdp`
- To `OneBrain.AgentOperations.Core`

Browser adapter project remains canonical:

- `OneBrain.AgentOperations.Adapters.Browser`

## Compatibility Strategy

No duplicate type shims were created. The shim review concluded that duplicating sealed records and enums in legacy namespaces would create distinct CLR types and increase compatibility risk.

Internal source-level compatibility is maintained by adding canonical global usings to `OneBrain.Safety.Tests`. Browser runtime code remains in `BrowserExecutor.*` and may continue to use the browser namespaces for browser/OCR surfaces.

## Obsolete Shim Status

Compatibility shim files are not required for this scoped internal migration. The test suite verifies that any future file placed in a `Compatibility` folder must be marked `[Obsolete]`.

## Consumers Updated

- `tests/OneBrain.Safety.Tests/NodalOsAgentOperationsNamespaceGlobalUsings.cs` provides canonical Agent Operations namespaces to existing safety tests.

## Dependency Direction

- `AgentOperations.Contracts` remains browser/CDP-free.
- `AgentOperations.Core` references `AgentOperations.Contracts`.
- `AgentOperations.Core` remains browser/CDP-free.
- `AgentOperations.Adapters.Browser` remains skeleton-only and does not reference `BrowserExecutor.Cdp`.
- `BrowserExecutor.Cdp` remains the temporary browser adapter host.

## Runtime Behavior

No runtime behavior changed. No scheduler, timer, background worker, API, UI, worker runtime, recipe execution, skill execution, step execution, browser action, or desktop action was introduced.

## Deferred

- External compatibility package/type-forwarding design, if ever needed.
- Browser adapter extraction phase 1.
- Broad naming cleanup for legacy `Nexa*` symbols.
- Broad `OneBrain.*` project naming migration.

## Next Step

Recommended next milestone: `M446-M448 Browser Adapter Extraction Phase 1`.
