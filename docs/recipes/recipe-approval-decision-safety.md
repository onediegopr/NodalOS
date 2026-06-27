# Recipe Approval Decision Safety

Phase: 4/9 - Human Intervention + Approval Narrative 2.0.

`RecipeApprovalDecision` is a reference-only decision record. It can be projected to timeline and handoff summaries, but it cannot execute a recipe step.

## Rules

- Approval does not unlock live runtime.
- Approval does not grant action authority.
- Approval does not mutate external systems.
- Missing evidence results in `RequestMoreEvidence` or `KeepBlocked`, not approval.
- Dry-run and fixture approvals remain non-mutating.
- Browser live action and desktop live action remain blocked regardless of approval.
- CAPTCHA, 2FA, and challenge states cannot be automated by approval.

## Timeline/Handoff

Approval required, approval recorded, rejected approval, request-more-evidence, manual resolution, and keep-blocked decisions can be projected to timeline events by reference only.

Handoff summaries include operator-visible narrative summaries and omit raw screenshots, DOM, accessibility trees, HAR/network logs, raw payloads, and secret values.
