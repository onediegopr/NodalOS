# Selective Absorption Final Closure M358

## Initial State

The selective absorption pause started after the OCR line was closed, audited and cleaned. The OCR path is local ONNX .NET with regional capture, evidence envelope, audit ledger consumer, FSM read-only observation and read-only assisted verification with non-OCR corroboration.

The immediate goal was to absorb useful ideas from BotBoard, Axiom and Robomotion without copying, depending on, or implementing those products. The pause was explicitly limited to governed architecture, domain contracts, reporting contracts and validation surfaces.

## Closed Milestones

### M344-M346

Decision: `SELECTIVE_ABSORPTION_DECISIONS_READY`.

Delivered:

- Agent Workboard decision record.
- Axiom browser automation benchmark decision.
- Robomotion package, skill and worker roadmap note.
- Selective absorption summary artifact and tests.

### M347-M349

Decision: `MISSION_TASK_DOMAIN_READY`.

Delivered:

- Mission model.
- Agent task model.
- Progress notes.
- Blocker reports.
- Verification checks.
- Evidence refs.
- Verification-before-done validation.

### M350-M352

Decision: `RUN_REPORT_AND_FAILURE_TAXONOMY_READY`.

Delivered:

- Failure taxonomy.
- Troubleshooting recommendation mapper.
- Run Report V1.
- Policy decision, approval and failure reports.
- Run report sanitizer.

### M353-M355

Decision: `RECIPE_MANIFEST_V1_READY_WITH_EXECUTION_DEFERRED`.

Delivered:

- Recipe Manifest / Automation JSON V1 contracts.
- JSON serializer/deserializer.
- Policy validator.
- Read-only, supervised, blocked and unsafe fixtures.
- Manifest validation tests.

## What Was Taken From BotBoard

- Agent-native workboard concept.
- Task ownership.
- Explicit blockers.
- Progress notes.
- Handoff bundles as a future concept.
- Verification before completion.
- Evidence-linked task closure.

## What Was Taken From Axiom

- Run Report V1.
- Automation JSON / Recipe Manifest V1.
- Failure taxonomy.
- Troubleshooting recommendations.
- Future Step Library direction.
- Future run orchestration API direction.
- Future scheduled read-only run direction.

## What Was Taken From Robomotion

- Package / Skill Manifest direction.
- Internal Skill Registry direction.
- Worker Boundary Contract direction.
- Worker and connector lifecycle as future governed surfaces.
- Skill provenance and risk metadata direction.

## What Was Not Implemented

- Full workboard UI.
- Sidepanel replacement.
- Timeline replacement.
- Orchestration API.
- Scheduled runs.
- Package registry.
- Cloud runtime.
- Multi-worker runtime.
- Captcha solving.
- Bot bypassing.
- Recipe execution.
- Complete Step Library.
- Browser actions.
- Desktop actions.
- OCR changes.
- FSM/action policy changes.

## Why These Surfaces Were Deferred

The absorbed concepts introduce useful operational structure, but runtime and UI surfaces would expand authority. NODAL OS keeps authority in core policy and evidence gates. Workboard UI, orchestration, scheduling, package registry and worker runtime require additional policy surfaces before implementation.

Recipe execution was deferred because Recipe Manifest V1 is a portable definition and validation layer. Execution must remain separate and governed by policy, approval, evidence and runtime state.

## Final Agent Operations Platform Layer State

The immediate platform layer now has:

- Decision records for selective absorption.
- Mission / Task / Blocker / Verification / Evidence domain.
- Failure taxonomy.
- Troubleshooting mapper.
- Run Report V1.
- Recipe Manifest / Automation JSON V1.
- Robomotion roadmap note for future package, skill and worker boundaries.

It does not yet have:

- Workboard UI.
- Progress reporting contract as a dedicated surface.
- Verification-before-done gate integrated into future run lifecycle.
- Step Library V1.
- Run Orchestration API.
- Scheduled runs.
- Package / Skill Manifest implementation.
- Worker Boundary Contract implementation.

## Pending Risks

- Browser runtime flakes can still obscure full-suite confidence if they recur.
- Existing roadmap documents are long-lived and partially historical.
- Agent Operations has strong contracts but no UI or orchestration binding yet.
- Recipe Manifest V1 is intentionally non-executing.
- Package, skill and worker concepts remain roadmap-only.

## Possible Next Paths

### Path A: Agent Operations Continuation

Continue directly with Blocker + Progress Reporting Contract, Verification Before Done Gate, Step Library V1 and Run Orchestration API V1.

### Path B: Core Roadmap Return

Return to browser runtime flake hardening, core legacy reference graph, desktop identity/liveness, unified evidence model and broader core audit work.

### Path C: Hybrid Priority Roadmap

Use a mixed sequence that first reduces browser-runtime noise, then strengthens Agent Operations gates, then resumes core cleanup and desktop identity work.

## Recommended Continuity

Recommendation: Path C, hybrid priority roadmap.

Reason: the immediate Agent Operations surface is now coherent enough to pause. The next highest leverage is to reduce browser-runtime flake noise, then add verification/progress gates before implementing larger orchestration or Step Library surfaces.

Final decision: `M356+M357+M358 CERRADO / SELECTIVE_ABSORPTION_CLOSED_READY_FOR_HYBRID_PRIORITY_ROADMAP`.
