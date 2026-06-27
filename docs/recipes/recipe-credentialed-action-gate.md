# Recipe Credentialed Action Gate

Phase: 5/9 - Tool Trust Registry + Secrets by Reference.

`RecipeCredentialedActionGate` checks whether a recipe action has the references needed for fixture-safe credentialed review.

It verifies:

- tool trust ref exists,
- tool is trusted for fixture-safe use,
- required secret refs exist,
- raw secret values are absent,
- secret scopes match,
- required approval narrative exists,
- connector eligibility stays preview/fixture/manual-assist only.

## Outcomes

Credentialed action readiness can be preview/fixture-ready or blocked for missing tool trust, missing secret refs, raw secret detection, scope mismatch, missing approval, untrusted tool, live runtime, connector execution, or policy.

No decision grants action authority. No decision enables live runtime or connector execution.

Payment, fiscal, marketplace, message, external mutation, delete, and credentialed categories remain approval/human-gated and live-blocked.
