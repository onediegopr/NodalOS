# Recipe Trigger Policy Readiness

Phase: 6/9 - Trigger / Detector Layer observe-only.

Trigger readiness is static contract evaluation only. It never creates watchers, hooks, listeners, timers, network endpoints, connector sessions, recipe runs, or workitem processors.

## Canonical Rules

- Default safety mode is observe-only or disabled.
- `Future*` trigger and detector kinds are future-gated.
- `Unknown` trigger and detector kinds are blocked.
- `FutureAutoRunBlocked` is blocked and projects `TriggerRunNotStartedByPolicy`.
- Tool trust refs are checked by id only.
- Secret refs are checked by id only.
- Raw secret detection blocks readiness.
- Approval cannot unlock trigger autorun.
- 2FA/CAPTCHA/challenge observations map to human/block, not bypass.

## Outputs

`RecipeTriggerReadiness` returns:

- readiness status,
- blocked reason,
- blocking issues,
- warnings,
- no live runtime,
- no action authority.

Allowed outcomes are limited to observation, draft suggestion, and manual acknowledgement metadata.
