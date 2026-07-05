# QA Report - HTTP In-Process Route Response Test Infrastructure Local-Only

Date: 2026-07-05

Decision: `GO_WITH_FINDINGS_HTTP_IN_PROCESS_ROUTE_RESPONSE_TEST_INFRASTRUCTURE_LOCAL_ONLY_READY`

## Summary

This block adds local-only HTTP route response evidence for the Product Ledger operator surface. The test host is ephemeral loopback only, Development-only, test-only and preview/read-only. It does not add public deploy, external network/provider/cloud, DB/migration, KMS/WORM/external trust, Browser/CDP/WCU/OCR/Recipes live execution, Product Ledger writes, exports, product command execution or release/commercial readiness.

## Route Response Coverage

- Route path: `/internal/product-ledger/operator-surface`.
- Host: local ephemeral Kestrel on `127.0.0.1:0`.
- Development environment: returns HTTP 200 with `text/html; charset=utf-8`.
- Production environment: route is not mapped and returns HTTP 404.
- Route mapper: `ProductLedgerLocalDevRouteEndpointMapper`.
- Evidence mode: `LOCAL_ONLY_DEVELOPMENT_ONLY_HTTP_RESPONSE_PREVIEW_NO_EXECUTION`.

## DOM / Response Assertions

- `local-dev-route-preview`
- `canonical-surface-model`
- `product-ledger-approval-preview`
- route path attribute
- read-only marker
- preview-only marker
- no product command execution marker
- no write/export marker
- no release/commercial marker
- no executable controls
- no forms/scripts/callback/formaction

## Validations

- `dotnet build src/OneBrain.Pilot/OneBrain.Pilot.csproj --no-restore -v:minimal`: PASS, 0 warnings, 0 errors.
- `dotnet test tests/OneBrain.Recipes.Tests/OneBrain.Recipes.Tests.csproj --no-restore --filter "FullyQualifiedName~ProductLedgerHttpInProcessRouteResponseTests"`: PASS, 2/2.
- `dotnet test tests/OneBrain.Safety.Tests/OneBrain.Safety.Tests.csproj --no-restore --filter "FullyQualifiedName~ProductLedgerHttpInProcessRouteResponseSafetyTests"`: PASS, 2/2.
- `dotnet test tests/OneBrain.Safety.Tests/OneBrain.Safety.Tests.csproj --no-restore --filter "FullyQualifiedName~ProductLedger"`: PASS, 169/169.
- `dotnet test tests/OneBrain.Recipes.Tests/OneBrain.Recipes.Tests.csproj --no-restore --no-build --filter "FullyQualifiedName~ProductLedger"`: PASS, 48/48.
- `dotnet build OneBrain.slnx --no-restore -v:minimal`: PASS, 0 warnings, 0 errors.

## Findings

P0: 0

P1: 0

P2: 0

P3:

- Browser pixel/screenshot verification remains separate.
- Route remains Development-only and fixture-safe, not arbitrary live local ledger path scanning.
- Real approval execution and public UI actions remain future scopes.

P4:

- HTTP evidence uses local loopback Kestrel rather than `WebApplicationFactory` or ASP.NET `TestServer`.
- `HttpClient` is present only in Recipes test-only loopback verification.

TRUE_RISK: 0.

## Percentages

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
