# ADR: PaddleOCR OCR FSM Observation Policy M331-M333

## Decision

OCR evidence is now consumable by a read-only FSM observation layer.

- `AcceptedAuxiliary` may enter observation context.
- `RecordedDiagnosticRejected` and `RecordedDiagnosticUncertain` may enter diagnostics only.
- `RejectedPolicyViolation` is excluded from observation context.

## Ranking

Observation ranking prioritizes:

1. Accepted auxiliary + high confidence
2. Accepted auxiliary + medium confidence
3. Accepted auxiliary + low confidence
4. Diagnostic uncertain
5. Diagnostic rejected
6. Policy violation excluded

## Boundary

This wiring cannot:

- produce action plans
- produce safe actions
- approve clicks
- approve submit/send/delete/pay/sign
- convert OCR into authority
