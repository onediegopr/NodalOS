# NODAL OS Recipe Runtime Phase 6 Prompt

Block: `NODAL_RECIPE_RUNTIME_006_TRIGGER_DETECTOR_LAYER_OBSERVE_ONLY`

Objective: add fixture-safe trigger and detector layer contracts for Recipe Runtime.

Scope:

- Observe-only trigger definitions.
- Detector contracts for incoming work, external state refs, manual signals, timeline refs, and policy refs.
- No scheduler.
- No file watcher.
- No OS hook.
- No network/API calls.
- No connector execution.
- No browser automation.
- No desktop automation.
- No live runtime.
- No secret value access.

Required guardrails:

- Phase 6 must remain contract/fixture-safe.
- Triggers and detectors cannot start runs automatically.
- Triggers and detectors cannot unlock live runtime.
- Secrets remain by reference only.
- Tool trust remains passive.
- Connector eligibility remains reference/fixture/manual-assist only.
- Approval decisions remain narrative-bound.

Expected phase:

- Total phases: 9.
- Current phase: 6/9.
- Phase name: Trigger / Detector Layer observe-only.

Claude audit recommendation: continue without Claude if Phase 5 closed cleanly and no P0/P1 appeared; run Claude after Phase 6 or Phase 7.
