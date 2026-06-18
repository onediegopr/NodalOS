# PaddleOCR OCR Evidence Ledger Consumer M330

## Scope

M328-M330 wires OCR evidence into a concrete audit-ledger consumer and expands confidence and diff metrics without changing any action authority.

## Result

- Accepted OCR evidence writes an auxiliary OCR audit event.
- Rejected OCR evidence writes a diagnostic rejected OCR audit event.
- Uncertain OCR evidence writes a diagnostic uncertain OCR audit event.
- Policy violations write a policy rejection OCR audit event and never become accepted or auxiliary.
- Confidence and diff metrics are preserved in the OCR ledger entry.

## Readiness

`M328+M329+M330 CERRADO / READY_FOR_OCR_EVIDENCE_TO_FSM_OBSERVATION_WIRING`
