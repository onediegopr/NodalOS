# Product Ledger Local Dev Route Internal Endpoint Preview

Date: 2026-07-04

Decision: `GO_WITH_FINDINGS_PRODUCT_LEDGER_LOCAL_DEV_ROUTE_INTERNAL_ENDPOINT_PREVIEW_READY`

## Context

The Product Ledger operator surface now has a deterministic render model and HTML snapshot fixture. This block adds a local-dev/internal-only route preview contract for that snapshot without adding a public deploy, server route mapping, controller, external network, telemetry, provider/cloud, DB/migration, KMS/WORM/external trust, live automation, destructive action, unbounded export/write or release/commercial readiness.

The repo contains a local web host in `OneBrain.Pilot`, which binds to `127.0.0.1` by default. This block maps the Product Ledger preview route only inside `app.Environment.IsDevelopment()`. Outside Development the route is not registered. CI/CD workflows and public hosting surfaces are not modified.

## Decision

Add `ProductLedgerLocalDevRoutePreview` as a Core local-dev/internal-only endpoint preview adapter and map it through `OneBrain.Pilot` only in Development mode. It exposes:

- a route template preview string for local operator documentation;
- a `text/html` content type;
- the existing renderable operator snapshot HTML;
- a local-dev/internal-only banner;
- fail-closed blockers for non-local, non-dev, production, release, commercial, public deploy, external host/origin, telemetry/sync, provider/cloud/network, DB/migration, KMS/WORM/external trust, live automation, destructive action, unbounded export/write, external/cloud export and raw payload/secret claims.

## Scope

Allowed:

- local-dev/internal-only preview contract;
- read-only/non-destructive render of the existing snapshot fixture;
- no public deploy and no mapping outside Development;
- no external network;
- no telemetry;
- no provider/cloud;
- no DB;
- no KMS/WORM/external trust;
- no live Browser/CDP/WCU/OCR/Recipes execution;
- no release/commercial.

Not implemented:

- public route;
- deployed product UI;
- server endpoint mapping outside Development;
- controller inheritance or route attributes;
- external telemetry/sync;
- provider/cloud/network;
- DB/migration;
- KMS/WORM/external trust;
- destructive action execution;
- unbounded physical export/write;
- release/commercial readiness.

## Readiness

- Local dev route/internal endpoint preview: 100%.
- Renderable operator snapshot: 100%.
- DOM contract: 100%.
- Product Ledger public local-only actions: 76%.
- Operator acceptance: 82%.
- Bounded local report export: 100%.
- External/cloud readiness: 0%.
- Provider/cloud/network: 0%.
- DB/migration: 0%.
- KMS/WORM/external trust: 0%.
- Browser/CDP/WCU/OCR/Recipes live: 0%.
- Release/commercial: 0%.

## Findings

P0: 0

P1: 0

P2: 0

P3:

- A future local UI host can wire this contract to an actual local-only development route after a separate route-host safety review.
- Visual screenshot evidence remains future work once a local UI host exists.
- Static scan helper extraction remains a future maintainability improvement.

P4:

- The active server mapping exists only when `OneBrain.Pilot` runs in Development.
- The snapshot is deterministic HTML string evidence, not public deployed UI telemetry.
- Local same-machine evidence is not WORM/compliance-grade custody.
