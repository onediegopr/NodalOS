# NODAL OS Scheduled Read-Only Runs Boundary Discovery M431

## Scope

M431 reviewed the current NODAL OS orchestration, Agent Operations, browser adapter, worker boundary, registry, and reporting surfaces to decide whether scheduled read-only runs can be designed safely without implementing a scheduler.

This report is discovery-only. It does not create timers, background workers, cron, orchestration endpoints, browser actions, worker runtime, recipe execution, skill execution, step execution, UI, or persistence.

## Concepts Found

- `NodalOsOrchestrationCommandKind` already models conceptual commands such as `PrepareRun`, `GetRunStatus`, `PauseRun`, `ResumeRun`, `CancelRun`, `AttachEvidence`, `GetRunReport`, `GetProgressReport`, and `EvaluateVerificationBeforeDone`.
- `NodalOsOrchestrationInProcessFacade` exists as an in-process facade with no execution. It always returns `Executed=false` and keeps runtime execution deferred.
- Package and skill manifests already carry read-only capability metadata and runtime-deferred flags.
- Internal Skill Registry V1 can query catalog entries but cannot grant runtime permission.
- Worker Boundary V1 can describe worker capabilities and health but cannot authorize actions.
- Agent Operations reports already include RunReport, ProgressReport, verification, evidence refs, failure kinds, and common redaction integration.
- `OneBrain.AgentOperations.Adapters.Browser` exists as marker-only skeleton; real browser runtime still lives in `OneBrain.BrowserExecutor.Cdp`.

## What Already Exists

- Contract-only orchestration commands.
- Contract-only in-process facade.
- Policy/evidence/approval validation points.
- EvidenceRef bridge and no-authority evidence semantics.
- Common redaction service.
- RunReport and ProgressReport structures.
- Browser adapter project skeleton with no runtime behavior.

## What Is Missing

- Schedule contracts.
- Schedule preview model.
- Read-only run classification contracts.
- Rate/frequency constraints.
- Manual dry-run preview flow.
- Security audit for any actual scheduled execution.
- Scheduler runtime, intentionally not created in this milestone.

## Scheduled Read-Only vs Orchestration Command

An orchestration command is a single internal contract envelope, validated and handled by the facade without execution. A scheduled read-only run is a future policy-governed plan that may later emit one or more orchestration commands on a schedule.

Scheduled read-only runs must not be represented as direct runtime permission. They must require future explicit read-only classification, policy evaluation, evidence/redaction gates, and reporting-only outputs.

## Scheduled Read-Only vs Scheduler Real

This milestone defines the boundary only. A real scheduler would require timers, persistence, background workers, retry logic, concurrency limits, cancellation, audit retention, and operator controls. None of those are implemented here.

## Scheduled Read-Only vs Worker Runtime

Worker Health, Worker Boundary, Registry Visible, Skill Approved, and Recipe Approved remain descriptive states. They cannot grant permission to run scheduled work. Future scheduled read-only runs may prepare worker requests conceptually, but actual worker runtime remains deferred.

## Prohibited Now

- Scheduler implementation.
- Timers, cron, or background services.
- HTTP/gRPC API.
- UI.
- Worker runtime.
- Recipe, skill, or step execution.
- Browser or desktop actions.
- Persistence DB.
- Notifications or external integrations.

## Principal Risks

- Treating schedule presence as permission to execute.
- Treating read-only as allowing low-risk clicks or downloads.
- Treating browser navigation as safe without a future explicit read/navigation boundary.
- Storing raw secrets in schedule metadata, reports, or evidence.
- Letting Worker Healthy or Registry Visible bypass policy.
- Allowing future frequency settings to become denial-of-service or stealth automation.

## Decision

Proceed with a Scheduled Read-Only Runs ADR only. Implementation remains deferred. The next safe implementation step is schedule contracts or dry-run preview contracts, not a scheduler.
