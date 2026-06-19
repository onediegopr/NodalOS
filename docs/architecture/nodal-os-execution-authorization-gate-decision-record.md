# NODAL OS Execution Authorization Gate Decision Record

Decision: `NODAL_OS_EXECUTION_AUTHORIZATION_GATE_REQUIRED_BEFORE_RUNTIME`

## Context

AUDIT-A found that current NODAL OS foundations correctly preserve no-runtime semantics with `CanAuthorizeExecution=false` and `RuntimeExecutionAllowed=false`.

The risk before UI real work is accidental coupling from a future UI or Agent Operations layer directly into real runtime hosts such as `OneBrain.BrowserExecutor.Cdp`.

## Decision

No real execution is allowed until a positive execution authorization gate exists.

Until that gate is implemented and independently audited:

- `CanAuthorizeExecution=false` remains mandatory for approval cards, approval previews, handoff packs and observability reports;
- approval UX preview does not authorize execution;
- approval decision alone does not authorize execution;
- UI cannot call runtime directly;
- AgentOperations cannot call `BrowserExecutor.Cdp` directly;
- Browser/automation runtime remains runtime-gated;
- LLM/BYOK cannot authorize execution;
- cloud/licensing cannot authorize execution.

## Required Future Bridge

Future execution can only happen through an explicit bridge that composes all required inputs:

- Execution Registry state;
- Policy Gate decision;
- Approval Decision;
- Evidence requirements;
- Verification plan;
- rollback/restore plan when applicable;
- redaction check;
- jail/path boundary;
- risk classifier hardening when recipe or automation is involved;
- human handoff state when applicable.

The bridge must fail closed when any required input is absent, stale, unredacted, unverified or ambiguous.

## Runtime Boundary

`BrowserExecutor.Cdp` may remain a temporary host, but it is not an Agent Operations dependency and not a UI dependency.

Any future bridge to browser or automation runtime requires a dedicated security audit before implementation.

## Non-Goals

- No positive gate implementation in M477-M479.
- No browser runtime move.
- No UI implementation.
- No scheduler, worker, recorder, replay, queue, DSL parser or recipe/step execution.

## Acceptance Criteria

- ADR exists before UI real work.
- Tests assert no runtime is authorized without the positive gate.
- Tests assert UI cannot call runtime directly.
- Tests assert AgentOperations cannot call `BrowserExecutor.Cdp` directly.
- Tests assert the future bridge requires policy, approval, verification, evidence, rollback/redaction/jail and classifier hardening where applicable.
