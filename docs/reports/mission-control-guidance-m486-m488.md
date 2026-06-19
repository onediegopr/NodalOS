# M486-M488 - Mission Control Guidance Empty States

## Executive summary

M486-M488 adds Mission Control guidance surfaces for NODAL OS: safe empty states, contextual onboarding and guardrail explainers. The block improves user clarity without introducing runtime execution, browser automation, cloud, LLM provider calls, productive persistence, telemetry or policy bypass.

Decision: `MISSION_CONTROL_GUIDANCE_EMPTY_STATES_READY`.

## Initial git state

- Worktree: `C:\DESARROLLO\NodalOS\Codigo-m12-audit`
- Branch: `chrome-lab-001-extension-local-ai-bridge`
- Base commit: `367de7a801692bfc888a58b2a5e05d3e225e1cb9`
- Remote: `https://github.com/onediegopr/NodalOS.git`
- Forbidden path not used: `C:\Users\diego\OneDrive\PERSONAL\ONE Brain\Codigo`

## Objective

Make the read-only Mission Control shell easier to understand through:

- safe empty states;
- contextual onboarding;
- guardrail explainers;
- blocked/attention state explanations;
- Mission Control-first microcopy;
- no execution authority.

## M486 - Mission Control Empty States

Added contract and service coverage for empty states:

- no mission selected;
- no active mission;
- no timeline events;
- no approvals pending;
- no evidence available;
- no observability report yet;
- no workspace selected;
- no UI interaction history;
- no approval draft;
- no selected evidence ref;
- no guardrail warnings;
- runtime unavailable by design;
- LLM not configured by design;
- cloud sync disabled by design;
- browser automation deferred by design.

Every empty state includes title, short description, user-facing explanation, recommended next safe step, optional disabled action label/reason, guardrail refs, severity, attention flag, `CanExecuteAction=false` and `IsReadOnly=true`.

## M487 - Contextual Onboarding

Added onboarding steps for:

- what a mission is;
- what Timeline shows;
- what Approval Display means;
- why approval does not execute;
- evidence ref-only behavior;
- Observability / LOG preview;
- runtime blocked;
- LLM/BYOK future;
- cloud disabled;
- what must exist before real execution.

Dismiss/reopen state is mock-safe and no-op. No telemetry, analytics, cloud, productive persistence or model calls were added.

## M488 - Guardrail Explainers

Added guardrail explainers for:

- read-only mode;
- no runtime execution;
- no browser automation;
- no cloud sync;
- no LLM provider calls;
- no filesystem mutation;
- no shell/subprocess;
- approval no-authority;
- evidence ref-only;
- redaction applied;
- positive execution gate missing;
- recipe-risk hardening required before runtime;
- BrowserExecutor.Cdp disconnected;
- legacy sensitive subsystem quarantine;
- human handoff;
- blocked action / disabled button explanation.

Explainers cannot unlock execution, change policy, mutate registry or create exceptions.

## Files created

- `src/OneBrain.AgentOperations.Contracts/NodalOsMissionControlGuidanceContracts.cs`
- `src/OneBrain.AgentOperations.Core/NodalOsMissionControlGuidanceServices.cs`
- `tests/OneBrain.Safety.Tests/NodalOsMissionControlGuidanceM486M488Tests.cs`
- `docs/reports/mission-control-guidance-m486-m488.md`
- `artifacts/agent-operations/m488/mission-control-guidance-summary.json`

## Files modified

- `docs/roadmap/nodal-os-roadmap-vnext.md`
- `docs/roadmap/nodal-os-unified-roadmap-post-pause.md`

## Tests

New tests cover:

- empty states and read-only/no-execute invariants;
- onboarding generation and mock-safe dismiss/reopen;
- guardrail explainers and no-unlock/no-policy/no-registry semantics;
- redacted serialization;
- boundary guards against BrowserExecutor.Cdp and runtime primitives;
- continuity with M480-M485 shell and no-op interactions.

## Guardrails confirmed

- No runtime execution.
- No positive execution gate implementation.
- No BrowserExecutor.Cdp reference.
- No browser automation.
- No recorder/replay.
- No queue/scheduler/worker.
- No LLM provider call.
- No cloud call.
- No productive persistence.
- No telemetry or analytics.
- No shell/subprocess.
- No filesystem mutation.
- No operational NEXA/NODRIX/HOTEP naming.

## What was not implemented

- Runtime.
- UI action execution.
- Real onboarding persistence.
- Telemetry/analytics.
- Cloud sync.
- LLM/BYOK.
- Browser automation.
- Scheduler/worker/queue.
- DSL parser.
- Productive filesystem or DB persistence.

## Risks and pending work

- Positive execution gate remains future-only.
- Recipe Risk Classifier hardening remains runtime-gated.
- Legacy sensitive subsystem quarantine remains a blocker before cloud/licensing/BYOK.
- Browser automation remains disconnected and deferred.

## Updated percentages

- NODAL OS global: `97.1%`.
- Agent Operations / Automation Layer: `97.5%`.
- Core Runtime: `75%`.
- Evidence/Timeline foundation: `82%`.
- Approval foundation: `80%`.
- Redaction/Safety foundation: `82%`.
- Productization foundation: `51%`.
- Mission Control UX: `54%`.

## Next recommended block

`M489+M490+M491 - Mission Control Visual Polish + Responsive Desktop Layout + Static UX Acceptance Pack`.

## Final decision

`M486+M487+M488 CERRADO / MISSION_CONTROL_GUIDANCE_EMPTY_STATES_READY`

