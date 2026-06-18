# ADR: OCR Assisted Verification Fixtures Policy M337-M339

## Context

The assisted-verification policy exists and is read-only. The next step is proving that controlled fixtures behave correctly across passing and failing cases.

## Decision

- Execute a fixed fixture set with accepted OCR auxiliary evidence and controlled non-OCR corroboration.
- Keep non-OCR corroboration mandatory for any `VerifiedLowRisk` outcome.
- Keep OCR-only exact and high-confidence fixtures rejected.
- Keep fuzzy residuals such as `PVC WALI` vs `PVC WALL` below the pass line; the policy returns `NeedsMoreEvidence`.

## Consequences

- The current fixture surface is ready for audit and later expansion.
- Any future fuzzy-tolerance expansion must be explicit and separately audited.
