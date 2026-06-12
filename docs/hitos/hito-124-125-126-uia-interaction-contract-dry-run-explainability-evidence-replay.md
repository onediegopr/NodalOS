# HITO-124+125+126 - UIA Interaction Contract, Dry-Run Explainability, Evidence Replay

## Scope

This hito adds explainability and replay around the existing benign executor harness. It does not add new real actions.

- HITO-124: formal UIA interaction contract for the current benign harness.
- HITO-125: action dry-run explainability before executor use.
- HITO-126: read-only executor evidence replay from local runtime artifacts.

## UIA Interaction Contract

The contract captures:

- harness id,
- app profile,
- window constraints,
- target constraints,
- resolved target,
- action kind,
- approval state,
- safety matrix decision,
- pre-action state,
- post-action expectation,
- logical evidence path.

The only supported target remains the local benign Pilot harness target:

- `onebrain-pilot-benign-click-harness-v1`,
- `onebrain-pilot-local`,
- `ONE BRAIN Pilot`,
- `name:Objetivo benigno del harness`,
- `benign_harness_click`.

No user-configurable target is accepted.

## Dry-Run Explainability

`/executor-harness` now shows a read-only dry-run section that explains:

- what element would be touched,
- why it was selected,
- what safety rules apply,
- what conditions would block execution,
- whether the action is currently fail-closed.

The dry-run does not call UIA, does not click, and does not write runtime evidence.

## Evidence Replay

`/executor-harness/replay` reads the latest local artifact under:

`artifacts/executor-harness/`

It shows:

- target resolution,
- approval ids,
- safety decision,
- command/action kind,
- post-state verification,
- evidence metadata.

Replay is read-only. It does not auto-open artifacts and does not execute actions.

## Safety

Still prohibited:

- no OpenAI real call,
- no API keys,
- no free playback,
- no new real actions,
- no clicks outside the explicitly supervised benign harness,
- no cookies accepted,
- no login,
- no cart,
- no purchase,
- no payment,
- no MercadoLibre click,
- no auto-open HTML/artifacts.

Tests use fake executors only.
