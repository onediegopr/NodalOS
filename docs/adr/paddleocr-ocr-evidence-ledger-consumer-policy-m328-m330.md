# ADR: PaddleOCR OCR Evidence Ledger Consumer Policy M328-M330

## Decision

OCR evidence now has a concrete audit-ledger consumer:

- `AcceptedAuxiliary` -> `OcrEvidenceAuxiliaryRecorded`
- `RecordedDiagnosticRejected` -> `OcrEvidenceDiagnosticRejectedRecorded`
- `RecordedDiagnosticUncertain` -> `OcrEvidenceDiagnosticUncertainRecorded`
- `RejectedPolicyViolation` -> `OcrEvidencePolicyViolationRejected`

## Confidence / Diff

The OCR ledger entry now preserves:

- fingerprint hash match
- diff score
- dark pixel ratio
- mean RGB
- sample signature
- confidence band

These metrics help ranking and filtering evidence only. They do not authorize actions.

## Boundary

OCR remains:

- `NoAuthority=true`
- `EvidenceOnly=true`
- `ActionAllowed=false`

No OCR evidence can approve click, submit, send, delete, pay, or sign.
