# M777 Evidence Envelope Projection

M777 defines evidence envelope projection for every simulated positive dry-run case.

Each envelope includes dry-run identity, request identity, capability name, decision, simulation-only flag, policy gate decision, approval decision, redacted affected resources, redaction proof reference, ledger event references, forbidden-field exclusion flags, and no-execution proof flags.

## Ledger Binding

Each positive case projects:

- `SIMULATED_DRY_RUN_REQUESTED`
- `SIMULATED_POLICY_GATE_EVALUATED`
- `SIMULATED_ACTION_ALLOWED_FOR_DRY_RUN`
- `SIMULATED_EVIDENCE_ENVELOPE_CREATED`
- `SIMULATED_REDACTION_PROOF_CREATED`
- `SIMULATED_NO_EXECUTION_PROOF_CREATED`

The manual approval case also projects `SIMULATED_MANUAL_APPROVAL_EVALUATED`.
