# M771 Ledger Event Projection

M771 defines projected ledger event records for the simulated dry-run orchestrator.

The projection is simulation-only. It binds orchestrator requests and responses to evidence envelope references, redaction proof references, and no-execution proof flags.

## Event Types

- `SIMULATED_DRY_RUN_REQUESTED`
- `SIMULATED_POLICY_GATE_EVALUATED`
- `SIMULATED_MANUAL_APPROVAL_EVALUATED`
- `SIMULATED_ACTION_DENIED`
- `SIMULATED_ACTION_ALLOWED_FOR_DRY_RUN`
- `SIMULATED_EVIDENCE_ENVELOPE_CREATED`
- `SIMULATED_REDACTION_PROOF_CREATED`
- `SIMULATED_NO_EXECUTION_PROOF_CREATED`

## No-Execution Proof

Projected events include `simulationOnly=true` and false flags for actual execution, live calls, filesystem writes, browser automation, capability unlock, public release, Store submission, and signed public ZIP creation.
