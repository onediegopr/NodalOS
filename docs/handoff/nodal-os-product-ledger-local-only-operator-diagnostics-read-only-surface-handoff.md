# Handoff - Product Ledger Local-Only Operator Diagnostics Read-Only Surface

Date: 2026-07-04

Decision: `GO_WITH_FINDINGS_PRODUCT_LEDGER_LOCAL_ONLY_OPERATOR_DIAGNOSTICS_READ_ONLY_SURFACE_FINAL_PACKET_READY`

## Summary

Added a Core-only internal operator diagnostics presenter for Product Ledger local-only runtime state. The surface is read-only and fail-closed. It renders runtime flag, active path policy, bounded writer, checkpoint/head and evidence-gate status, plus disabled action previews.

## Files

- `src/OneBrain.Core/Approval/ProductLedgerLocalOnlyOperatorDiagnosticsSurface.cs`
- `tests/OneBrain.Safety.Tests/ProductLedgerLocalOnlyOperatorDiagnosticsSurfaceTests.cs`
- `tests/OneBrain.Recipes.Tests/ProductLedgerLocalOnlyOperatorDiagnosticsSurfaceTests.cs`
- `docs/adr/product-ledger-local-only-operator-diagnostics-read-only-surface.md`
- `docs/adr/product-ledger-local-only-operator-diagnostics-read-only-surface-external-audit.md`
- `docs/qa/nodal-os-product-ledger-local-only-operator-diagnostics-read-only-surface/report.md`
- `docs/qa/nodal-os-product-ledger-local-only-operator-diagnostics-read-only-surface/report.json`

## Not Enabled

- no public UI action;
- no destructive user-facing action;
- no productive command handler;
- no productive DI registration;
- no provider/cloud/network;
- no DB/migration;
- no KMS/WORM/external trust;
- no Browser/CDP/WCU/OCR/Recipes live execution;
- no release/commercial readiness.

## Frontier

Stop before public UI action, external provider/cloud/network, DB/migration, KMS/WORM/external trust, Browser/CDP/WCU/OCR/Recipes live execution, release/commercial readiness or destructive user-facing action.
