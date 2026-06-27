# Recipe Trigger Evidence + Timeline

Phase: 6/9 - Trigger / Detector Layer observe-only.

Trigger evidence and timeline projection are reference-only. They explain that a fixture/manual/workitem signal was observed and why no run started.

## Evidence

`RecipeTriggerEvidence` stores:

- trigger id,
- observation id,
- source kind,
- evidence ref id,
- redacted summary,
- redaction status.

It does not embed raw payloads, secret values, screenshots, DOM, accessibility trees, HAR/network logs, files, or live captures.

## Timeline

`RecipeTriggerTimelineProjection` can project:

- `TriggerReadinessEvaluated`,
- `TriggerObservationCreated`,
- `TriggerFutureGated`,
- `WorkitemDueObserved`,
- `ManualCheckpointObserved`,
- `TriggerRunNotStartedByPolicy`,
- `UnknownTriggerBlocked`.

If a future design might have started a run, Phase 6 projects `TriggerRunNotStartedByPolicy` and remains blocked.
