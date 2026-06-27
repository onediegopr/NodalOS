# Recipe Timeline Projection Contract

Phase: 3/9 - Evidence Pack + Timeline Projection.

`RecipeTimelineProjection` maps recipe run state into redacted, fixture-safe timeline events. It does not replace existing global timeline systems; it provides recipe-specific refs that can be adapted later.

## Event Kinds

Timeline event kinds include:

- recipe run created,
- readiness evaluated,
- fixture run started,
- step planned,
- step validated,
- step blocked,
- step succeeded,
- step failed,
- workitem queued/processing/succeeded/retry/failed,
- human intervention requested,
- approval required,
- approval recorded by ref,
- evidence captured by ref,
- evidence missing,
- redaction applied,
- risk gate blocked,
- action resolution blocked,
- run completed/failed/cancelled,
- handoff created.

## Projection Behavior

The projector supports contract-only mappings:

- blocking readiness issue to timeline event,
- validation failure to timeline event,
- human intervention to timeline event,
- missing evidence to timeline event,
- redaction applied to timeline event,
- live blocked to timeline event.

Events carry refs for evidence, validation, approval, risk gate, and redaction. Summaries must be redacted.

## Boundary

Timeline projection does not execute actions, inspect live UI, capture evidence, read files, call CDP, call network APIs, or subscribe to runtime events.
