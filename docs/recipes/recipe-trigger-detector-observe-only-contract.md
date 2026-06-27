# Recipe Trigger / Detector Observe-Only Contract

Phase: 6/9 - Trigger / Detector Layer observe-only.

This contract lets recipes describe future event associations without creating live listeners or automatic execution.

## Contract Scope

- `RecipeTriggerDefinition` describes a trigger and its optional recipe/workitem refs.
- `RecipeDetectorDefinition` describes the passive detector metadata.
- `RecipeTriggerObservation` records a redacted observation.
- `RecipeDetectorObservation` records detector output by reference only.
- `RecipeTriggerPolicyEvaluator` evaluates static readiness for observation-only use.

## Supported Kinds

Safe/current kinds:

- `Manual`
- `WorkitemDue`
- `ManualCheckpointResolved`

Future-gated kinds:

- File created/changed/download completed.
- Browser URL/element/DOM matched.
- Desktop window/element appeared.
- Hotkey/clipboard/email/connector/schedule/webhook.

Unknown trigger or detector kinds are blocked.

## Safety Boundary

- No scheduler or background worker.
- No real file watcher.
- No OS hook or hotkey listener.
- No browser or desktop listener.
- No network or webhook listener.
- No connector execution.
- No automatic recipe run.
- No automatic workitem processing.
- No secret values.
- No live runtime.

OpenRPA/OpenCore detector patterns are inspiration only. No dependency, code copy, or XAML import is used.
