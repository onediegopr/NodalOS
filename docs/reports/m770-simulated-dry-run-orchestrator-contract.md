# M770 Simulated Dry-Run Orchestrator Contract

M770 defines the simulated dry-run orchestrator contract for NODAL OS.

The orchestrator is `SIMULATED_FAKE_ONLY`. It processes simulated requests only, evaluates simulated policy and manual approval gates, rejects prohibited actions, and returns evidence and ledger projection references without executing anything real.

## Request Contract

- `requestedMode`: `SIMULATED_DRY_RUN`.
- Policy gate required: true.
- Manual approval required when high-risk.
- Evidence envelope required: true.
- Ledger projection required: true.

## Response Contract

Responses may return only `ALLOW_SIMULATED_DRY_RUN`, `DENY`, or `REQUIRE_MANUAL_APPROVAL`.

All no-execution flags remain false for real execution, live calls, filesystem writes, browser automation, capability unlock, public release, Store submission, and signed public ZIP creation.

## Boundary

The contract does not create adapters, run providers, write files, automate browsers, unlock capabilities, publish releases, submit to Store, modify product files, or modify Bridge/CSP.
