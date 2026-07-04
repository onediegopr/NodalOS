# Handoff - Product Ledger Path Persisted Candidate Local-Only No-Write

Date: 2026-07-04

Decision: `GO_WITH_FINDINGS_PRODUCT_LEDGER_PATH_PERSISTED_CANDIDATE_LOCAL_ONLY_NO_WRITE_READY`

## Summary

Added a local-only in-memory registry for policy accepted product ledger path candidates. It requires a passing active policy result and passing canonicalization result.

The registry stores candidate records in process memory only. It does not write files, create directories, append ledger events or activate a product ledger path.

## Files

- `src/OneBrain.Core/Approval/ProductLedgerPathPersistedCandidateRegistry.cs`
- `tests/OneBrain.Safety.Tests/ProductLedgerPathPersistedCandidateRegistryTests.cs`
- `tests/OneBrain.Recipes.Tests/ProductLedgerPathPersistedCandidateRegistryTests.cs`
- `docs/adr/product-ledger-path-persisted-candidate-local-only-no-write.md`
- `docs/qa/nodal-os-product-ledger-path-persisted-candidate-local-only-no-write/report.md`
- `docs/qa/nodal-os-product-ledger-path-persisted-candidate-local-only-no-write/report.json`

## Not Enabled

- no active product ledger path;
- no writer;
- no append-only ledger;
- no filesystem ledger persistence;
- no product DI/service registration;
- no command handlers;
- no UI product actions;
- no DB/migration/provider/cloud/network;
- no KMS/WORM/external trust;
- no Browser/CDP/WCU/OCR/Recipes live execution;
- no runtime/product enablement;
- no release/commercial readiness.

## Next Safe Block

Run `NODAL_OS_PRODUCT_LEDGER_PATH_PERSISTED_CANDIDATE_EXTERNAL_AUDIT_READ_ONLY`.
