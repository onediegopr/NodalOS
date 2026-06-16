# M118-M120 - Local Private Preview Final Review

## Decision

`ReadyWithRestrictions` is confirmed for internal local private preview.

This is not production readiness.

## Allowed Scope

- Internal local private preview.
- Local Product/Admin shell.
- Private local API in-process.
- Readiness dashboard.
- Local diagnostics/evidence review.
- Local issue triage.
- Operator blocker explanations.

## Preconditions

- Canonical worktree only.
- Build OK.
- Full suite OK.
- M51 and M65 evidence remains scoped and frozen.
- Skipped tests audit remains synchronized.
- Dangerous surfaces remain blocked.
- Claude external audit prompt is reviewed before broadening the preview.

## Must Not Open

- SaaS public.
- Public API real.
- Billing/email real.
- Real credentials.
- Sensitive sites.
- Submit/pay/sign/delete.
- Productive recorder/replay.
- External CDP general-ready.
- New external targets without dedicated evidence.

## Claude Review

Claude should review:

- whether `ReadyWithRestrictions` is valid;
- whether M51/M65 are being inflated;
- whether Product/Admin private preview local is safe to start;
- whether evidence/log summaries leak data;
- what should block internal tests;
- whether HITO-162 should be rewritten next or delayed for more hardening.

## Next Steps

If Claude finds no blockers, start controlled internal local private preview.

If Claude finds blockers, fix them before starting internal tests.
