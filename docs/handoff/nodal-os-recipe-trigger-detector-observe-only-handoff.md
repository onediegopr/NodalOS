# NODAL OS Recipe Trigger / Detector Observe-Only Handoff

Block: `NODAL_RECIPE_RUNTIME_006_TRIGGER_DETECTOR_OBSERVE_ONLY`

Decision: `GO_RECIPE_TRIGGER_DETECTOR_OBSERVE_ONLY_READY`

## Current State

- Total phases: 9.
- Closed phases: 1-6.
- Current phase completion: 95%.
- Overall Recipe Runtime line completion: 75%.
- Next phase: Phase 7/9 - Recipe Lab + Locator Repair Studio.

## Added

- `RecipeTriggerDetectorContracts.cs`.
- Trigger definitions and detector definitions.
- Observe-only trigger policy/readiness.
- Trigger evidence refs and trigger timeline projection.
- Recipe/workitem association without autorun.
- Fixture-safe tests for future-gated detectors and no execution.

## Guardrails

Phase 6 remains contract/fixture-safe only.

- No scheduler/background worker.
- No file watcher.
- No OS hook or hotkey listener.
- No browser/desktop listener.
- No network/webhook listener.
- No connector execution.
- No vault or secret values.
- No recorder/replay.
- No automatic recipe run.
- No automatic workitem processing.
- No live runtime.

## Carry-Forward

- Phase 7 must remain a lab/repair surface with fixture-safe metadata only unless a separate policy gate is created.
- Trigger observations can support evidence and timeline refs, but cannot start execution.
- Claude audit can run now for Phases 1-6, or after Phase 7 if no P0/P1 appears.
