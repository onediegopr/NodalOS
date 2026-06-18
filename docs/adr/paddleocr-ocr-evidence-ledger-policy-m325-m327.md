# ADR: PaddleOCR OCR Evidence Ledger Policy M325-M327

## Decision

OCR observation evidence is integrated as:

- `AuxiliaryOnly` when acceptance state is `AcceptedEvidence` and both `RegionVerified=true` and `ConfidenceGatePassed=true`.
- `DiagnosticOnly` when acceptance state is rejected or uncertain.
- `RejectedPolicyViolation` when OCR attempts to carry authority or action rights.

## Constraints

- `NoAuthority=true`
- `EvidenceOnly=true`
- `ActionAllowed=false`
- `SoftmaxReapplied=false`
- `OfficialSpacePolicy=true`

## Consequences

- OCR can enrich internal diagnostics and evidence trails.
- OCR cannot widen runtime permissions.
- Wiring into broader ledger consumers can expand later without reopening the authority boundary.
