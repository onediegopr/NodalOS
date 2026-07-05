# Product Ledger HTTP In-Process Route Response Test Infrastructure Local-Only

Date: 2026-07-05

Decision: `GO_WITH_FINDINGS_HTTP_IN_PROCESS_ROUTE_RESPONSE_TEST_INFRASTRUCTURE_LOCAL_ONLY_READY`

## Context

The Product Ledger operator surface previously had render-function DOM evidence, but no HTTP route response evidence. The local/dev route exists only for `/internal/product-ledger/operator-surface` and must remain local-only, Development-only, read-only and preview-only.

## Decision

Centralize Product Ledger route mapping in `ProductLedgerLocalDevRouteEndpointMapper` and add local-only HTTP route response tests.

The mapper preserves the same behavior as the prior inline `Program.cs` route:

- maps only when `environment.IsDevelopment()` is true;
- uses `MapGet` only;
- renders `ProductLedgerLocalDevRoutePreview.CreateDefaultLocalDevRequest()`;
- returns `Results.Content(result.HtmlSnapshot, result.ContentType)` for the safe preview result;
- returns `Results.NotFound()` if the preview fails closed.

Recipes tests now start a local ephemeral Kestrel host on `127.0.0.1:0`, request the Product Ledger route through `HttpClient`, verify canonical HTML and verify Production does not map the route.

## Scope

Implemented:

- local-only route mapper;
- Development-only route guard retained;
- HTTP loopback route response tests;
- Production route not-found test;
- Safety static mapper guards;
- QA, handoff, roadmap/readiness and decision-log evidence.

Not implemented:

- public deploy;
- public internet exposure;
- external network/provider/cloud;
- product command execution;
- append/write/export from route;
- destructive user-facing action;
- DB/migration;
- KMS/WORM/external trust;
- Browser/CDP/WCU/OCR/Recipes live execution;
- Pilot `/run` enablement;
- release/commercial readiness.

## Boundary Confirmation

The only HTTP activity added is test-only loopback to an ephemeral local Kestrel host. It does not expose a public route, does not add a package or cloud dependency, does not enable runtime product execution and does not write Product Ledger data.

## Findings

P0: 0

P1: 0

P2: 0

P3:

- Browser pixel/screenshot verification remains separate from HTTP response testing.
- Route remains Development-only and fixture-safe, not arbitrary live local ledger path scanning.
- Real approval execution and public UI actions remain outside scope.

P4:

- HTTP evidence uses local loopback Kestrel, not `WebApplicationFactory` or ASP.NET `TestServer`.
- `HttpClient` appears only in Recipes test-only code for loopback verification.

TRUE_RISK: 0

## Readiness

- Product Ledger local-only core: 94-96%.
- Approval/Human Review: 90-94%.
- Evidence/Timeline/Audit Trail: 82-88%.
- Runtime/Command/Execution: 46-54%.
- UI/Operator Surface: 50-60%.
- Local-only internal product: 62-70%.
- Usable end-to-end local product: 36-46%.
- External/cloud: 0%.
- Release/commercial: 0%.

## Next Safe Block

Recommended next macro-block: `NODAL_OS_LOCAL_ROUTE_LIVE_LEDGER_READ_MODEL_TEST_SAFE`.

It must remain local-only/test-only/read-only/fail-closed and must not scan arbitrary paths, write, export, execute commands, enable public UI actions, introduce external network/provider/cloud, DB/migration, KMS/WORM/external trust, Browser/CDP/WCU/OCR/Recipes live execution or release/commercial readiness.
