# Handoff - Product Ledger Path Local-Only Active Writer

Date: 2026-07-04

Decision: `GO_WITH_FINDINGS_PRODUCT_LEDGER_PATH_LOCAL_ONLY_ACTIVE_WRITER_READY`

## Summary

Added a Core-only local Product Ledger Path activation and bounded writer. It accepts a persisted local-only candidate, requires local authority and safety evidence, keeps runtime flag default-off and writes hash-only JSONL entries under the activated local path.

The writer verifies existing ledger entries and a local head checkpoint before append/read. It revalidates safe payload hashes and metadata on existing entries, then fails closed on malformed/tampered state.

## Files

- `src/OneBrain.Core/Approval/ProductLedgerPathLocalOnlyActiveWriter.cs`
- `tests/OneBrain.Safety.Tests/ProductLedgerPathLocalOnlyActiveWriterTests.cs`
- `tests/OneBrain.Recipes.Tests/ProductLedgerPathLocalOnlyActiveWriterTests.cs`
- `docs/adr/product-ledger-path-local-only-active-writer.md`
- `docs/qa/nodal-os-product-ledger-path-local-only-active-writer/report.md`
- `docs/qa/nodal-os-product-ledger-path-local-only-active-writer/report.json`

## Not Enabled

- no provider/cloud/network;
- no KMS/WORM/external trust;
- no DB/migration;
- no Browser/CDP/WCU/OCR/Recipes live execution;
- no public UI product actions;
- no productive DI/service registration;
- no command handlers;
- no runtime product enablement;
- no release/commercial readiness.

## Next Safe Block

Run `NODAL_OS_PRODUCT_LEDGER_PATH_LOCAL_ONLY_ACTIVE_WRITER_EXTERNAL_AUDIT_READ_ONLY`.
