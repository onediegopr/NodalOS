# PaddleOCR Internal OCR Evidence Integration M327

## Scope

M325-M327 integrates the low-risk OCR observation envelope into an internal evidence adapter and runtime policy gate as auxiliary-only evidence.

## Result

- Accepted OCR evidence maps to auxiliary ledger evidence only.
- Rejected and uncertain OCR evidence map to diagnostic-only entries.
- OCR evidence cannot authorize actions.
- OCR evidence cannot approve click, submit, send, delete, pay, or sign.
- Region verification and confidence gate remain mandatory for accepted OCR evidence.

## Integration Note

The repository does not expose a single generic FSM evidence slot for OCR. This block adds the schema and policy adapter needed for ledger/runtime integration without widening action authority.

## Readiness

`M325+M326+M327 CERRADO / READY_FOR_OCR_EVIDENCE_LEDGER_EXPANSION`
