# Handoff - Product Ledger Path Real Canonicalization Validator Local-Only No-Write

Date: 2026-07-04

Decision: `GO_WITH_FINDINGS_PRODUCT_LEDGER_PATH_REAL_CANONICALIZATION_VALIDATOR_LOCAL_ONLY_NO_WRITE_READY`

## Summary

Added a real local-only/no-write canonicalization validator for product ledger path candidate readiness. It validates an allowed root and candidate path with post-canonicalization boundary checks and local filesystem reparse-risk inspection where possible.

## Files

- `src/OneBrain.Core/Approval/ProductLedgerPathCanonicalizationValidator.cs`
- `tests/OneBrain.Safety.Tests/ProductLedgerPathCanonicalizationValidatorTests.cs`
- `tests/OneBrain.Recipes.Tests/ProductLedgerPathCanonicalizationValidatorTests.cs`
- `docs/adr/product-ledger-path-real-canonicalization-validator-local-only-no-write.md`
- `docs/qa/nodal-os-product-ledger-path-real-canonicalization-validator-local-only-no-write/report.md`
- `docs/qa/nodal-os-product-ledger-path-real-canonicalization-validator-local-only-no-write/report.json`

## Implementation Notes

- Candidate readiness is allowed only when blocker count is zero.
- Product capability flags are always false.
- Status text includes `CANDIDATE_READINESS_ONLY`, `REAL_LOCAL_CANONICALIZATION_VALIDATOR`, `NO_PRODUCT_LEDGER_WRITE`, `NO_PRODUCT_RUNTIME_ENABLEMENT`, `NO_ACTIVE_PRODUCT_LEDGER_PATH`, `NO_RELEASE_COMMERCIAL`, `NO_WORM_KMS_CLOUD` and `NO_EXTERNAL_TRUST`.
- Reparse/symlink/junction evidence is fail-closed: missing evidence blocks, and unreadable filesystem attributes are treated as unresolved reparse risk.

## Not Implemented

- no active product ledger path;
- no writer;
- no append-only ledger;
- no product DI/service registration;
- no command handlers;
- no UI product actions;
- no DB/migration/provider/cloud/network;
- no KMS/WORM/external trust;
- no Browser/CDP/WCU/OCR/Recipes live execution;
- no runtime/product enablement;
- no release/commercial readiness.

## Next Safe Block

Run `NODAL_OS_PRODUCT_LEDGER_PATH_REAL_CANONICALIZATION_VALIDATOR_EXTERNAL_AUDIT_READ_ONLY` before any further product-ledger implementation step.
