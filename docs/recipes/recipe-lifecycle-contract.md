# Recipe Lifecycle Contract

## Lifecycle Stages

The recipe lifecycle is represented as metadata:

- `Preflight`
- `Prepare`
- `Perceive`
- `Plan`
- `Act`
- `Verify`
- `Recover`
- `Evidence`
- `Cleanup`
- `Handoff`

These stages do not execute in this block. They give future deterministic policy and runtime planning a stable vocabulary.

## Block Types

Contract-only block types include:

- `BrowserGoal`
- `BrowserAction`
- `DesktopActionDraft`
- `Extract`
- `Validate`
- `Wait`
- `Conditional`
- `Loop`
- `HumanIntervention`
- `Approval`
- `FileDownloadEvidence`
- `CaptureArtifact`
- `ConnectorDraft`
- `WorkitemPop`
- `WorkitemUpdate`
- `WorkitemCreateNextStage`
- `Cleanup`

`BrowserAction`, `DesktopActionDraft`, and `ConnectorDraft` are draft/contract terms only. They do not add browser, desktop, connector, provider, CDP, UIA, native messaging, file watcher, scheduler, or live execution behavior.

## Readiness

Readiness statuses include preview, dry-run, fixture-run, and blocked states for missing limits, validation, evidence policy, approval policy, tool trust, secret references, protected scope, or live runtime disablement.

The phase-1 evaluator is intentionally conservative. Missing limits or validation blocks readiness, and live modes remain blocked.
