# M642 Future Runtime Enablement Plan

Decision: `M642 CERRADO / FUTURE_RUNTIME_ENABLEMENT_PLAN_READY`

## Scope

M642 defines a safe future runtime enablement plan. It does not enable runtime, provider/cloud, filesystem, shell, browser automation, connector actions, or public release.

## Current Runtime Status

Runtime evidence is clean for the installed extension release evidence gate.

Runtime productive execution remains `NO-GO`.

Clean Runtime evidence means the extension and bridge state are healthy; it does not authorize execution.

## Required Gates Before Runtime Enablement

1. Runtime scope definition.
2. Capability inventory and explicit disabled-by-default policy.
3. Approval gate for every sensitive action.
4. Evidence ledger for every proposed and executed action.
5. Redaction policy for context, prompts, logs and evidence.
6. Provider/BYOK consent gate before any provider-backed execution.
7. Filesystem boundary and path jail before any file operation.
8. Rollback/kill-switch plan.
9. No shell-free execution policy.
10. Browser automation gate before any browser action.
11. Connector action gate before external integration.
12. Release candidate audit.

## Runtime Enablement Stages

### Stage R0: Planning Only

Current stage.

- No runtime execution.
- No provider.
- No filesystem.
- No browser automation.
- No connector action.

### Stage R1: Preview Contracts

Allowed only after a dedicated milestone:

- Runtime action preview.
- Approval preview.
- Evidence projection.
- No execution.

### Stage R2: Local Dry Run

Allowed only after R1 audit:

- Deterministic no-op runtime simulations.
- No provider call.
- No file mutation.
- No external side effects.

### Stage R3: Gated Local Runtime

Future-only:

- Explicit user approval.
- Evidence required.
- Rollback required.
- No shell-free execution.
- No provider unless provider gate is separately closed.

### Stage R4: Provider/Workspace Integration

Future-only and separate from R3:

- BYOK consent.
- Redaction.
- Source grounding.
- Filesystem boundaries.
- Evidence and rollback.

## Explicit Runtime No-Go

M642 does not allow:

- Productive runtime execution.
- Shell/subprocess.
- Filesystem writes.
- Browser automation.
- Connector actions.
- Provider calls.
- Cloud execution.
- Capability unlock.
- Public release.

## Approval Model

Runtime must require human approval before sensitive action. Approval must be explicit, scoped, logged, revocable, and non-transferable across materially different actions.

Approval preview is not execution approval.

## Evidence Model

Every future runtime action needs:

- Action intent.
- Inputs summary.
- Risk class.
- Approval ref.
- Evidence refs.
- Redaction status.
- Result state.
- Rollback availability.

## Redaction Model

Secrets, credentials, tokens, private keys, auth headers, cookies and raw personal data must not be written to artifacts, prompts, logs or public reports.

## Provider/BYOK Consent

Provider execution requires a dedicated provider release gate. BYOK consent does not unlock runtime by itself.

## Filesystem Boundaries

Filesystem access requires path jail, read-only proof, explicit consent and mutation blocking before any write-capable path is considered.

## Rollback Plan

Before runtime enablement:

- Kill switch.
- Per-capability disablement.
- Evidence rollback report.
- Clear support path.
- No silent retry loops for sensitive actions.

## Recommended Outcome

Keep runtime `NO-GO`. Use this plan as the basis for future runtime preview and dry-run milestones only.
