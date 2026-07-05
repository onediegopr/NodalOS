# QA Report - Product Ledger Single Real Local Operator Route And Surface Consolidation

Date: 2026-07-05

Decision: `GO_WITH_FINDINGS_PRODUCT_LEDGER_SINGLE_REAL_LOCAL_OPERATOR_ROUTE_AND_SURFACE_CONSOLIDATION_READY`

## Summary

The Product Ledger operator route now exposes a canonical local/dev/read-only surface model at `/internal/product-ledger/operator-surface`. Existing snapshot/preview artifacts remain compatibility wrappers, but the route result and DOM now carry the canonical authority, statuses, evidence refs and blocked frontiers.

## Files Changed

- `src/OneBrain.Core/Approval/ProductLedgerOperatorSurfaceModel.cs`
- `src/OneBrain.Core/Approval/ProductLedgerLocalDevRoutePreview.cs`
- `tests/OneBrain.Safety.Tests/ProductLedgerLocalDevRoutePreviewTests.cs`
- `tests/OneBrain.Recipes.Tests/ProductLedgerLocalDevRoutePreviewTests.cs`
- `docs/adr/product-ledger-single-real-local-operator-route-and-surface-consolidation.md`
- `docs/qa/nodal-os-product-ledger-single-real-local-operator-route-and-surface-consolidation/report.md`
- `docs/qa/nodal-os-product-ledger-single-real-local-operator-route-and-surface-consolidation/report.json`
- `docs/handoff/nodal-os-product-ledger-single-real-local-operator-route-and-surface-consolidation-handoff.md`
- `docs/decision-log.md`

## Route / Surface

- Canonical route path: `/internal/product-ledger/operator-surface`.
- Legacy route string retained: `/__internal/local-dev/product-ledger/operator-snapshot`.
- Host gate: `OneBrain.Pilot` maps the route only inside `app.Environment.IsDevelopment()`.
- Mode: local-only, development-only, internal-only, read-only, non-destructive, fail-closed.
- Read model: fixture-safe canonical read model; no arbitrary Product Ledger file path read is claimed.
- Displays: ledger authority, verification, checkpoint, redaction/retention, concurrency, bounded export, operator acceptance, public local-only action contract, visual evidence, screenshot evidence and blocked frontiers.
- Cannot do: command execution, append/write/export, public UI action, external network/cloud, DB/migration, KMS/WORM/external trust, live automation, release/commercial.

## Validations

- `dotnet build src/OneBrain.Core/OneBrain.Core.csproj --no-restore`: PASS, 0 warnings, 0 errors.
- `dotnet test tests/OneBrain.Safety.Tests/OneBrain.Safety.Tests.csproj --no-restore --filter "FullyQualifiedName~ProductLedgerLocalDevRoutePreviewTests"`: PASS, 6/6.
- `dotnet test tests/OneBrain.Recipes.Tests/OneBrain.Recipes.Tests.csproj --no-restore --filter "FullyQualifiedName~ProductLedgerLocalDevRoutePreviewTests"`: PASS, 2/2.

- `dotnet build OneBrain.slnx --no-restore`: PASS, 0 warnings, 0 errors.
- `dotnet test tests/OneBrain.Safety.Tests/OneBrain.Safety.Tests.csproj --no-restore --filter "FullyQualifiedName~ProductLedger"`: PASS, 157/157.
- `dotnet test tests/OneBrain.Recipes.Tests/OneBrain.Recipes.Tests.csproj --no-restore --filter "FullyQualifiedName~ProductLedger"`: PASS, 44/44.
- `dotnet test tests/OneBrain.Safety.Tests/OneBrain.Safety.Tests.csproj --no-restore --filter "FullyQualifiedName~PilotRecipeExecutionGate|FullyQualifiedName~Pilot"`: PASS, 20/20.
- `git diff --check`: PASS.
- QA JSON validation: PASS.
- Changed-file broad static scan: PASS, hits classified as negative assertions, blocked-frontier wording, docs limitations or tests.
- Changed Core source API scan: PASS, no `HttpClient`, `WebSocket`, `Process.Start`, DB/migration API, file write/append, route mapping, DI registration or export invocation in changed Core source.

## Findings

P0: 0

P1: 0

P2: 0

P3:

- In-process HTTP response testing remains a future local-only test pack.
- Existing route reads fixture-safe canonical status, not a live Product Ledger file path.
- Old surface classes remain as compatibility wrappers.

P4:

- Legacy route string retained for traceability.
- Screenshot/visual evidence is not compliance custody or live telemetry.

TRUE_RISK: 0.

## Percentages

- Product Ledger local-only core: 94-96%.
- Approval/Human Review: 86-91%.
- Evidence/Timeline/Audit Trail: 78-84%.
- Runtime/Command/Execution: 42-50%.
- UI/Operator Surface: 35-45%.
- Local-only internal product: 56-64%.
- Usable end-to-end local product: 28-38%.
- External/cloud: 0%.
- Release/commercial: 0%.
