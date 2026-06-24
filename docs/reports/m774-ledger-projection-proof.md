# M774 Ledger Projection Proof

M774 proves simulated ledger projection for every M773 negative flow case.

Each case projects the required simulated events:

- `SIMULATED_DRY_RUN_REQUESTED`
- `SIMULATED_POLICY_GATE_EVALUATED`
- `SIMULATED_ACTION_DENIED`
- `SIMULATED_EVIDENCE_ENVELOPE_CREATED`
- `SIMULATED_NO_EXECUTION_PROOF_CREATED`

High-risk or mutable cases additionally project `SIMULATED_MANUAL_APPROVAL_EVALUATED`.

Every projected ledger event is simulation-only and preserves false no-execution flags for actual execution, live calls, filesystem writes, browser automation, capability unlock, public release, Store submission, and signed public ZIP creation.
