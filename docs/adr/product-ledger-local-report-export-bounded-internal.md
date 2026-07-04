# Product Ledger Local Report Export Bounded Internal

Date: 2026-07-04

Decision: `GO_WITH_FINDINGS_PRODUCT_LEDGER_LOCAL_REPORT_EXPORT_BOUNDED_INTERNAL_READY`

## Context

The Product Ledger chain already has a local-only active writer, runtime local-only internal gate, operator diagnostics surface, internal operator UI preview, internal command preview router and internal command handler. This block adds a bounded local physical export for diagnostic reports only.

## Decision

Implement `ProductLedgerLocalReportExportService` in Core and expose it through the existing internal command handler only for `LocalReportPhysicalExportBoundedInternal`.

The export is:

- local-only;
- internal-only;
- bounded to a canonical local root;
- diagnostic report export only;
- fail-closed;
- non-destructive;
- redacted/safe-content only;
- hash verified after write.

## Boundary

The service blocks null/empty paths, relative paths, traversal, UNC/network paths, environment expansion, reserved Windows devices, drive-relative paths, long path ambiguity, trailing dot/space, ADS/suspicious colon, canonical paths outside boundary, missing allowed root, missing reparse/TOCTOU evidence, unresolved symlink/junction/reparse evidence, unsafe report file names, non-report extensions and overwrite without explicit safe policy.

The service blocks raw payload, secret markers, unsafe metadata, external trust claims, WORM/KMS/cloud claims, public-ready/product-ready/release/commercial wording, public UI action, destructive action, public/product command handler exposure, productive DI/service registration, provider/cloud/network, DB/migration, Browser/CDP/WCU/OCR/Recipes live, external telemetry/sync, billing/licensing cloud and release/commercial readiness.

## Non-Goals

- No public UI action.
- No destructive user-facing action.
- No public/product command handler exposure.
- No unbounded physical export/write.
- No external/cloud export.
- No productive DI/service registration.
- No provider/cloud/network.
- No DB/migration.
- No KMS/WORM/external trust.
- No Browser/CDP/WCU/OCR/Recipes live.
- No release/commercial readiness.

## Findings

P0: 0.

P1: 0.

P2: 0 in focused Safety/Recipes coverage.

P3: public UI, public/product command exposure, external/cloud export, provider/cloud/network, DB/migration and KMS/WORM/external trust remain future gated.

P4: local report export evidence is same-machine local evidence and not WORM/compliance-grade evidence.

## Status

This ADR authorizes only the bounded local diagnostic report export implemented here. It does not authorize public UI, public command exposure, external/cloud export, DB/provider integration, KMS/WORM/external trust, live automation or release/commercial readiness.
