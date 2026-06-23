# M759 Runtime Promotion Criteria

Project: NODAL OS

M759 defines future promotion criteria for dry-run eligibility. It does not enable productive execution.

## Allowed Future Transitions

- PLANNING_ONLY -> DRY_RUN_ELIGIBLE_FUTURE.
- DISABLED -> DRY_RUN_ELIGIBLE_FUTURE.
- DRY_RUN_ELIGIBLE_FUTURE -> DRY_RUN_READY_FUTURE.

## Prohibited Transition

DRY_RUN_READY_FUTURE -> PRODUCTIVE_ENABLED is prohibited in this milestone and requires a future explicit owner-approved runtime unlock gate.

## Promotion Requirements

Promotion to dry-run eligibility requires policy gate, manual approval gate for high-risk capabilities, evidence envelope, redaction rules, forbidden fields, dry-run disallowed actions, mutable rollback/compensation boundary where relevant, browser human handoff policy where relevant, provider/cloud secrets boundary where relevant, and proof of no real execution.

Promotion is blocked by productive execution requests, live call requests, real filesystem writes, browser actions, public release requests, Store submission requests, product file changes, or Bridge/CSP changes.
