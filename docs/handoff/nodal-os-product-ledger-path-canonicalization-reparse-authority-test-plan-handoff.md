# NODAL OS - Product Ledger Path Canonicalization Reparse Authority Test Plan Handoff

Date: 2026-07-04

Decision: `GO_WITH_FINDINGS_PRODUCT_LEDGER_PATH_CANONICALIZATION_REPARSE_AND_AUTHORITY_TEST_PLAN_ONLY_READY`

## Summary

Created a test-plan-only/design-only package for future product ledger path canonicalization, reparse/symlink/junction threat modeling and product authority wiring.

No product ledger path, writer, canonicalization service, authority service, DI registration, command handler, UI product action, DB/provider/cloud/KMS/WORM/external trust or live automation was implemented.

## Key Deliverables

- Canonicalization test plan for null, relative, traversal, UNC, drive-relative, long prefix, casing, Unicode, reserved device, local-temp and resolved-outside cases.
- Windows/local filesystem threat model for symlink, junction, reparse point, TOCTOU, swaps, hardlink, ADS, partial writes, truncation and append interleaving.
- Product authority test plan for stale, replayed, tampered, wrong-scope and over-scoped approvals.
- Acceptance criteria for a future product ledger path candidate.
- Future disabled implementation scaffold map and external audit checklist.

## Remaining Blockers

- Real canonicalization and jail enforcement.
- Real symlink/junction/reparse handling.
- Product authority implementation.
- Rollback/non-rollback policy.
- Disabled implementation scaffold.

## Next Recommended Safe Block

`NODAL_OS_PRODUCT_LEDGER_PATH_CANONICALIZATION_REPARSE_AUTHORITY_TEST_PLAN_EXTERNAL_AUDIT_READ_ONLY`
