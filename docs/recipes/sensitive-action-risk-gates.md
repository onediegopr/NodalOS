# Sensitive Action Risk Gates

Phase: 2/9 - Limits / Validation / Risk / Deterministic Policy.

`RecipeRiskProfile` and `RecipeRiskGate` classify recipe definitions before runtime.

## Risk Levels

- `Low`
- `Medium`
- `High`
- `Critical`
- `Blocked`

High, critical, and blocked recipes require approval or human intervention gates.

## Sensitive Categories

Sensitive action categories include:

- login,
- credential use,
- two-factor authentication,
- CAPTCHA or challenge,
- payment,
- fiscal or legal submission,
- email or message send,
- public posting,
- data deletion,
- data mutation,
- file write,
- external system mutation,
- marketplace listing change,
- price or stock change,
- personal data handling,
- secret handling,
- browser live action,
- desktop live action,
- unknown sensitive action.

## Gate Behavior

Payment, fiscal/legal submission, public posting, and email/message send require approval.

CAPTCHA, 2FA, and challenge categories block into human intervention. They are never auto-bypassed.

Browser live action and desktop live action remain blocked.

Secret handling is by reference only. Secret values in recipe definitions produce blocking readiness issues.
