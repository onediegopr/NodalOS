# Handoff - Product Ledger Internal Command Handler Local-Only Non-Destructive

Date: 2026-07-04

Decision: `GO_WITH_FINDINGS_PRODUCT_LEDGER_INTERNAL_COMMAND_HANDLER_LOCAL_ONLY_NON_DESTRUCTIVE_FINAL_PACKET_READY`

## Summary

Added a Core-only internal command handler for Product Ledger diagnostics/readiness. The handler consumes router previews and returns in-memory/read-only results only. It blocks executable callbacks, productive command ids, public/product command exposure, destructive actions, physical exports and file writes.

## Files

- `src/OneBrain.Core/Approval/ProductLedgerInternalCommandHandler.cs`
- `src/OneBrain.Core/Approval/ProductLedgerInternalCommandPreviewRouter.cs`
- `tests/OneBrain.Safety.Tests/ProductLedgerInternalCommandHandlerTests.cs`
- `tests/OneBrain.Recipes.Tests/ProductLedgerInternalCommandHandlerTests.cs`
- `docs/adr/product-ledger-internal-command-handler-local-only-non-destructive.md`
- `docs/adr/product-ledger-internal-command-handler-local-only-non-destructive-external-audit.md`
- `docs/qa/nodal-os-product-ledger-internal-command-handler-local-only-non-destructive/report.md`
- `docs/qa/nodal-os-product-ledger-internal-command-handler-local-only-non-destructive/report.json`

## Not Enabled

- no public UI action;
- no destructive user-facing action;
- no public/product command handler exposure;
- no executable callback externo;
- no physical export/write file;
- no productive DI/service registration;
- no provider/cloud/network;
- no DB/migration;
- no KMS/WORM/external trust;
- no Browser/CDP/WCU/OCR/Recipes live;
- no release/commercial.

## Frontier

Stop before public UI action, public/product command handler exposure, executable callback, physical export/write file, destructive action, provider/cloud/network, DB/migration, KMS/WORM/external trust, live Browser/CDP/WCU/OCR/Recipes or release/commercial.
