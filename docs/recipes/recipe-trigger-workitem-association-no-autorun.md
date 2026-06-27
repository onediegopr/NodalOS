# Recipe Trigger Workitem Association Without Autorun

Phase: 6/9 - Trigger / Detector Layer observe-only.

Triggers can reference recipes, workitem queues, workitems, human intervention refs, approval refs, evidence refs and timeline refs. These references do not start or advance execution.

## Allowed

- Observe a due workitem by ref.
- Suggest a recipe run draft by ref.
- Create observation-only metadata.
- Create manual acknowledgement metadata.
- Project timeline events.

## Forbidden

- Starting `RecipeRun`.
- Advancing `RecipeRunStep`.
- Processing workitems.
- Enqueueing real work automatically.
- Running scheduler/timer/cron.
- Opening network/webhook listeners.
- Creating file watchers, OS hooks, browser listeners or desktop listeners.

Approval decisions remain narrative-bound and cannot unlock autorun.
