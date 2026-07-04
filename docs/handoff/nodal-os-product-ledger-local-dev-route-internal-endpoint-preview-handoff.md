# NODAL OS Product Ledger Local Dev Route Internal Endpoint Preview Handoff

Date: 2026-07-04

Decision: `GO_WITH_FINDINGS_PRODUCT_LEDGER_LOCAL_DEV_ROUTE_INTERNAL_ENDPOINT_PREVIEW_FINAL_PACKET_READY`

## Safe Blocks Chained

- `NODAL_OS_PRODUCT_LEDGER_LOCAL_DEV_ROUTE_INTERNAL_ENDPOINT_PREVIEW_WINDOW`
- `NODAL_OS_PRODUCT_LEDGER_LOCAL_DEV_ROUTE_INTERNAL_ENDPOINT_PREVIEW_EXTERNAL_AUDIT`
- `NODAL_OS_PRODUCT_LEDGER_LOCAL_DEV_ROUTE_INTERNAL_ENDPOINT_PREVIEW_STATIC_SCAN_HARDENING`

## Scope Completed

Implemented:

- Core local-dev/internal-only route preview contract.
- Development-only `OneBrain.Pilot` route mapping.
- Fail-closed local/dev guard.
- Render of the existing Product Ledger operator HTML snapshot.
- Local-dev/internal-only banner with no public deploy, no telemetry, no external network, no release/commercial, no external trust and no WORM/KMS/cloud notices.
- Safety and Recipes tests.
- External audit read-only packet, QA report/JSON, ADR, roadmap and decision-log updates.

No public server route or controller was added; the route is mapped only when `OneBrain.Pilot` runs in Development.

## Files

- `src/OneBrain.Core/Approval/ProductLedgerLocalDevRoutePreview.cs`
- `src/OneBrain.Pilot/Program.cs`
- `tests/OneBrain.Safety.Tests/ProductLedgerLocalDevRoutePreviewTests.cs`
- `tests/OneBrain.Recipes.Tests/ProductLedgerLocalDevRoutePreviewTests.cs`
- `docs/adr/product-ledger-local-dev-route-internal-endpoint-preview.md`
- `docs/adr/product-ledger-local-dev-route-internal-endpoint-preview-external-audit-read-only.md`
- `docs/qa/nodal-os-product-ledger-local-dev-route-internal-endpoint-preview/report.md`
- `docs/qa/nodal-os-product-ledger-local-dev-route-internal-endpoint-preview/report.json`

## Boundary Confirmation

Not enabled:

- public deploy;
- public internet exposure;
- server route mapping outside Development;
- controller;
- external network/provider/cloud;
- telemetry/sync/billing cloud;
- DB/migration;
- KMS/WORM/external trust;
- Browser/CDP/WCU/OCR/Recipes live;
- destructive user-facing action;
- unbounded physical export/write;
- external/cloud export;
- release/commercial.

## Findings

P0: 0

P1: 0

P2: 0

P3:

- Future non-Development or public route mapping needs a host-layer safety review.
- Visual screenshot evidence remains future work once a local UI host exists.

P4:

- Route preview is Development-only and not publicly deployed.
- Snapshot evidence is not live telemetry and not WORM/compliance-grade custody.

## Real Stop Frontier

Public deploy, external network/provider/cloud, telemetry/sync/billing cloud, DB/migration, KMS/WORM/external trust, live Browser/CDP/WCU/OCR/Recipes, destructive user-facing action, unbounded physical export/write or release/commercial readiness require a new explicit GO.
