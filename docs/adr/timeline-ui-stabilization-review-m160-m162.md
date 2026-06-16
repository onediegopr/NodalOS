# ADR: Timeline UI Stabilization Review M160-M162

## Decision

`TimelineStableForInternalPreview`

The vertical timeline / stepper UI is stable for internal local private preview. It remains presentation-only and does not authorize actions.

## What Was Reviewed

- Side panel timeline renderer.
- Task structuring timeline.
- Recipe preview timeline.
- Recipe execution summary timeline.
- Evidence/log summary timeline.
- Blocker explanation timeline.
- Needs-human / human intervention display.
- Operator summary timeline.
- Private preview run timeline.
- Issue triage timeline.

## Where Timeline Applies

- Operator-facing task decomposition.
- Local/private preview recipes.
- Execution state summaries.
- Redacted evidence references.
- Blocker explanations.
- Human intervention prompts.
- Local issue triage.

## Where Timeline Does Not Apply

- Production/SaaS public readiness.
- Public API real enablement.
- Billing/email real enablement.
- Real credentials.
- Sensitive sites.
- Submit/pay/sign/delete.
- Productive recorder/replay.
- External CDP general-ready.
- New external targets.

## Issues Found

- `tl-readability-001`: Info, TimelineReadability. Accepted for internal-only monitoring. Does not block.

## Stabilization Criteria

- No critical/high security issues: passed.
- No scope inflation: passed.
- Blockers visible: passed.
- Evidence redacted: passed.
- Needs-human clear: passed.
- Core authority visible: passed.
- UI does not authorize actions: passed.

## Active Blockers

- Production/SaaS public blocked.
- Public API real blocked.
- Billing/email real blocked.
- Real credentials blocked.
- Sensitive sites blocked.
- Submit/pay/sign/delete blocked.
- Productive recorder/replay blocked.
- External CDP general-ready blocked.
- New external targets blocked without dedicated evidence.
- Embedded runtime not enabled.
- Chromium fork not planned.

## No Scope Expansion

M160-M162 does not expand scope. The timeline only presents structure, status, evidence, blockers and safe next action. Core remains authoritative.

## Next Step

Continue internal local private preview with timeline stable. Monitor spacing/readability as recipes grow, but do not add broad features or expand scope from this review.
