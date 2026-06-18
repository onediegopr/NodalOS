# ADR: OCR Assisted Verification Policy M334-M336

## Context

OCR evidence already reaches the read-only FSM observation surface with ranking, provenance, diff, and confidence gates. The next step is not action enablement; it is controlled low-risk assisted verification.

## Decision

- OCR may assist low-risk verification only as auxiliary evidence.
- A passing verification requires corroboration from at least one non-OCR signal.
- OCR-only requests are rejected.
- Rejected, uncertain, and policy-violation OCR evidence cannot support verification.
- Verified low-risk results remain read-only:
  - no action plan
  - no safe action
  - no approval
  - no click/submit/send/delete/pay/sign approval

## Consequences

- The policy surface is ready for controlled assisted-verification fixtures.
- Further expansion should focus on richer non-OCR signals and fixture coverage, not on granting OCR authority.
