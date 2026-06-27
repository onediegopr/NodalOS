# Reliable Recipe / Recorder / Eval / Sandbox Foundation v1

## Decision

NODAL OS implements its own Reliable Recipe + Recorder Draft + Eval Harness + Sandbox Readiness foundation as a clean-room contract layer.

Stagehand, OpenAdapt, CUA and Skyvern are conceptual references only. NODAL OS does not import their code, dependencies, licenses, schemas or runtime behavior in this block.

## Placement

This foundation sits above existing NODAL OS contracts and product surfaces:

- Mission Control UI and read-only Recipe Product Surface.
- Recipe Lab and template catalog.
- Assignment Engine and Policy Gate.
- Approval Center and Human Intervention.
- Execution Registry.
- Evidence Engine and Timeline.
- BrowserPerception / future PerceptionStack.
- Future Cloak/CDP browser adapter.
- Future desktop/computer-use adapter.

The M1 implementation is contract-only and fixture-safe inside `OneBrain.Core.Recipes`.

## Principles

- Deterministic-first target resolution.
- Evidence-first timeline and handoff.
- Validation-first completion.
- Human handoff for login, CAPTCHA, 2FA, credentials, ambiguity or high risk.
- No opaque autonomy.
- Evolution path: draft -> dry-run -> assisted-run -> supervised-run -> limited autonomy.
- Limited autonomy remains blocked by default.

## Clean-Room Inspirations

### Stagehand

Conceptual ideas:

- observe / act / extract separation.
- deterministic workflow after exploration.
- cache, repair and replay patterns.
- action preview.

Not included:

- Stagehand dependency.
- Stagehand code.
- live browser execution.

### OpenAdapt

Conceptual ideas:

- recorded trajectory.
- demonstration-to-draft.
- eval trajectories.
- action/observation episodes.

Not included:

- real recorder.
- replay.
- OS hooks.
- screenshots or credential capture.

### CUA

Conceptual ideas:

- sandbox readiness.
- computer-use isolation.
- benchmark/eval concepts.

Not included:

- VM creation.
- Docker/sandbox runtime.
- remote desktop.
- OS automation.

### Skyvern

Conceptual ideas:

- workflow/block builder.
- validation blocks.
- artifacts/evidence.
- human interaction block.
- SOP-to-workflow draft.

Not included:

- Skyvern runtime.
- AGPL code.
- provider calls.
- live browser automation.

## Explicit Exclusions

This block does not add:

- browser live execution.
- Playwright/CDP live calls.
- Cloak mutation.
- desktop hooks.
- OS screenshots.
- recorder live.
- sandbox live.
- secrets, tokens, cookies or credential storage.
- CAPTCHA/2FA bypass.
- provider/LLM calls.
- network/API/connector execution.
- payment, publish, send, delete or fiscal mutation.

## Roadmap

| Milestone | Scope |
| --- | --- |
| M1 | Audit + contracts for reliable recipes, recorder draft, eval harness, sandbox readiness and perception stack. |
| M2 | Recipe preflight + quality score hardening. |
| M3 | Recipe Lab UI read-only integration. |
| M4 | Recorder-to-Recipe fixture draft expansion. |
| M5 | Eval Harness fixture scenarios. |
| M6 | Sandbox readiness report expansion. |
| M7 | PerceptionStack integration with BrowserPerception. |
| M8 | Cloak/CDP dry-run adapter under strict feature flag and audit gate. |
| M9 | Assisted-run only after policy, approval, evidence and validation gates. |
| M10 | Full audit before any supervised runtime. |

## Product Claim Boundary

Allowed claim:

NODAL OS has a fixture-safe foundation for reliable recipe design, recorder-to-draft modeling, eval scenarios, sandbox readiness and perception-stack contracts.

Forbidden claim:

NODAL OS can execute or live-automate reliable recipes from this M1 foundation.
