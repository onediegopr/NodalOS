# ADR: NODAL OS Approval Execution Design-Only Protected

Decision target: `GO_NODAL_OS_APPROVAL_EXECUTION_DESIGN_ONLY_PROTECTED_READY`

## Status

Accepted as protected design-only work if validation passes.

## Context

NODAL OS has a closed read-only Approval/Human Review line with packet surfaces, risk/decision guards, evidence/context link guards and in-memory export preview. Runtime/live, approval execution, state mutation and release/commercial readiness remain unavailable.

The next safe step is to model what future approval execution would require without implementing execution, mutation, writer/policy integration, product service registration, runtime/live, product IO or physical export.

## Decision

Add a deterministic read-only design contract:

- `ApprovalExecutionDesignSpec`
- `ApprovalExecutionReadiness`
- `ApprovalExecutionGate`
- `ApprovalExecutionBlockedReason`
- `ApprovalExecutionPreview`
- `ApprovalExecutionAntiCapabilityProof`

The design spec is produced by `ApprovalExecutionDesignOnlyProtectedPresenter.CreateFixture()`. It is a fixture-only DTO model and does not expose executors, command handlers, product services, product IO, runtime/live or export behavior.

## Gates

The design keeps future execution blocked behind explicit gates:

- execution authorization;
- state mutation authorization;
- writer/policy boundary;
- runtime/live boundary;
- physical export boundary;
- clipboard/download boundary;
- filesystem product IO boundary;
- database boundary;
- provider/cloud boundary;
- LLM live boundary;
- service registration boundary;
- release/commercial boundary.

Each gate is blocked and allows zero real execution, zero state mutation, zero runtime/live, zero physical export and zero product action.

## Non-Goals

- No real approval execution.
- No approval state mutation.
- No productive writer/policy integration.
- No command handler.
- No product service registration.
- No runtime/live.
- No physical export, clipboard or download.
- No filesystem product IO.
- No DB or migration runner.
- No provider/cloud/network.
- No semantic/vector backend.
- No LLM live.
- No durable memory.
- No browser/CDP live, WCU live or OCR live.
- No recipe execution.
- No release/commercial readiness claim.

## Future Requirements

Any later move from design to implementation requires a separate protected hito, explicit operator authority, durable mutation/audit model, writer/policy boundary review, physical export policy, provider/cloud/DB/LLM policy if applicable, and external audit before runtime or release readiness can be reconsidered.
