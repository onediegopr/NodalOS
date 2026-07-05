# Product Ledger Single Real Local Operator Route And Surface Consolidation

Date: 2026-07-05

Decision: `GO_WITH_FINDINGS_PRODUCT_LEDGER_SINGLE_REAL_LOCAL_OPERATOR_ROUTE_AND_SURFACE_CONSOLIDATION_READY`

## Context

Product Ledger had several local-only operator artifacts: diagnostics, renderable snapshot, local-dev route preview, visual QA evidence, screenshot evidence, acceptance matrix and public local-only action contract. They were safe, but the operator story could read as several separate product surfaces.

This block consolidates them behind a canonical model and a single Development-only Pilot route:

- route: `/internal/product-ledger/operator-surface`;
- legacy compatibility route constant: `/__internal/local-dev/product-ledger/operator-snapshot`;
- canonical model: `ProductLedgerOperatorSurfaceModel`;
- route result field: `CanonicalSurface`;
- DOM contract anchor: `data-testid="canonical-surface-model"`.

## Decision

Add a canonical Product Ledger operator surface model and make the local-dev route preview render that model beside the legacy snapshot compatibility wrapper. The route remains mapped by `OneBrain.Pilot` only inside `app.Environment.IsDevelopment()`.

The canonical model is fixture-safe/read-model mode for this block. It does not read arbitrary local files, append ledger entries, call the exporter, execute commands, call network, register services, add DI, add DB/migrations or expose public UI actions.

## Scope

Allowed and implemented:

- local/dev route consolidation;
- read-only operator surface model;
- canonical status rows for ledger authority, verification, checkpoint, redaction/retention, concurrency, bounded export, operator acceptance, public local-only action contract, visual evidence and screenshot evidence;
- blocked-frontier DOM contract;
- compatibility for the existing renderable snapshot and legacy route string;
- Safety and Recipes DOM/model assertions.

Not enabled:

- public deploy or public internet route;
- product command execution;
- destructive user-facing action;
- unbounded export/write;
- external/cloud export;
- external network/provider/cloud;
- telemetry/sync/billing;
- DB/migration;
- KMS/WORM/external trust;
- live Browser/CDP/WCU/OCR/Recipes;
- release/commercial or compliance custody claim.

## Findings

P0: 0

P1: 0

P2: 0

P3:

- Route response tests currently validate the Core presenter and Pilot source mapping, not an in-process HTTP server fixture.
- The route uses fixture-safe read-model status rather than reading an existing Product Ledger file path.
- Legacy snapshot classes remain compatibility wrappers to avoid broad churn.

P4:

- The route path changed to the canonical `/internal/product-ledger/operator-surface`; the old route string remains as a legacy constant for traceability.
- Screenshot evidence remains fixture/local-only evidence, not live browser telemetry.

## Readiness

- Product Ledger local-only core: 94-96%.
- Approval/Human Review: 86-91%.
- Evidence/Timeline/Audit Trail: 78-84%.
- Runtime/Command/Execution: 42-50%.
- UI/Operator Surface: 35-45%.
- Local-only internal product: 56-64%.
- Usable end-to-end local product: 28-38%.
- External/cloud: 0%.
- Release/commercial: 0%.

## Next Safe Block

Recommended next macro-block: `NODAL_OS_RENDERED_UI_INTERACTION_LOCAL_ONLY_TEST_PACK`.

It should remain local-only/dev-only/read-only unless a separate GO explicitly opens a larger frontier.
