# QA Report - Local Route Live Ledger Read-Model Test-Safe

Date: 2026-07-05

Decision: `GO_WITH_FINDINGS_LOCAL_ROUTE_LIVE_LEDGER_READ_MODEL_TEST_SAFE_READY`

## Summary

This block adds a test-safe live Product Ledger read-model path to the local/dev operator route. The route remains Development-only, local-only, read-only and preview-only. It does not accept arbitrary paths, does not scan the filesystem, does not append/write/export and does not execute commands.

## Live Read-Model Route

- Default mode: `FixtureSafeReadModel`.
- Test-safe mode: `TestSafeLiveLedgerReadModel`.
- Route path: `/internal/product-ledger/operator-surface`.
- Source: explicit in-memory `ProductLedgerOperatorSurfaceReadModelSource`.
- Live read: `ProductLedgerPathLocalOnlyActiveWriter.ReadVerified`.
- Rendered live evidence: entry count, checkpoint/head status, head hash prefix, ledger hash prefix, redaction/retention status, concurrency status and bounded-export no-call status.
- Path output: `LOCAL_ONLY_BOUNDARY_PATH_REDACTED_NO_ARBITRARY_PATH_INPUT`.
- Production behavior: HTTP 404.

## Read-Only Guarantees

- No route append/write/export.
- No product command execution.
- No Pilot `/run` enablement.
- No user-controlled filesystem path input.
- No directory scanning.
- No external network/provider/cloud.
- No DB/migration.
- No KMS/WORM/external trust.
- No public deploy or release/commercial claim.

## Validations

- `dotnet build src/OneBrain.Core/OneBrain.Core.csproj --no-restore -v:minimal`: PASS, 0 warnings, 0 errors.
- `dotnet build src/OneBrain.Pilot/OneBrain.Pilot.csproj --no-restore -v:minimal`: PASS, 0 warnings, 0 errors.
- `dotnet test tests/OneBrain.Recipes.Tests/OneBrain.Recipes.Tests.csproj --no-restore --filter "FullyQualifiedName~ProductLedgerHttpInProcessRouteResponseTests"`: PASS, 3/3.
- `dotnet test tests/OneBrain.Safety.Tests/OneBrain.Safety.Tests.csproj --no-restore --filter "FullyQualifiedName~ProductLedgerHttpInProcessRouteResponseSafetyTests"`: PASS, 3/3.
- `dotnet test tests/OneBrain.Safety.Tests/OneBrain.Safety.Tests.csproj --no-restore --no-build --filter "FullyQualifiedName~ProductLedgerRenderedRouteInteractionTests"`: PASS, 4/4.
- `dotnet test tests/OneBrain.Recipes.Tests/OneBrain.Recipes.Tests.csproj --no-restore --no-build --filter "FullyQualifiedName~ProductLedgerRenderedRouteInteractionTests"`: PASS, 1/1.
- `dotnet test tests/OneBrain.Safety.Tests/OneBrain.Safety.Tests.csproj --no-restore --no-build --filter "FullyQualifiedName~ProductLedgerLocalApprovalPreviewLoopTests"`: PASS, 6/6.
- `dotnet test tests/OneBrain.Recipes.Tests/OneBrain.Recipes.Tests.csproj --no-restore --no-build --filter "FullyQualifiedName~ProductLedgerLocalApprovalPreviewLoopTests"`: PASS, 1/1.
- `dotnet test tests/OneBrain.Safety.Tests/OneBrain.Safety.Tests.csproj --no-restore --filter "FullyQualifiedName~ProductLedger"`: PASS, 170/170.
- `dotnet test tests/OneBrain.Recipes.Tests/OneBrain.Recipes.Tests.csproj --no-restore --no-build --filter "FullyQualifiedName~ProductLedger"`: PASS, 49/49.
- `dotnet test tests/OneBrain.Safety.Tests/OneBrain.Safety.Tests.csproj --no-restore --no-build --filter "FullyQualifiedName~ProductLedgerInternalCommandPreviewRouterTests|FullyQualifiedName~ProductLedgerInternalCommandHandlerTests|FullyQualifiedName~ProductLedgerPublicUiActionSurfaceTests"`: PASS, 24/24.
- `dotnet test tests/OneBrain.Safety.Tests/OneBrain.Safety.Tests.csproj --no-restore --no-build --filter "FullyQualifiedName~PilotRecipeExecutionGate|FullyQualifiedName~Pilot"`: PASS, 24/24.
- `dotnet test tests/OneBrain.Safety.Tests/OneBrain.Safety.Tests.csproj --no-restore --no-build --filter "FullyQualifiedName~ProductLedgerIntegrationPropertyTestPackTests"`: PASS, 4/4.
- `dotnet build OneBrain.slnx --no-restore -v:minimal`: PASS, 0 warnings, 0 errors.

## Findings

P0: 0

P1: 0

P2: 0

P3:

- Live read-model is test-safe/injected only, not a user-facing product ledger selector.
- Local approval execution remains a future design-only boundary.
- Browser pixel evidence remains separate.

P4:

- Hashes are rendered as prefixes only.
- `HttpClient` appears only in Recipes loopback test-only code.

TRUE_RISK: 0.

## Percentages

- Product Ledger local-only core: 94-96%.
- Approval/Human Review: 90-94%.
- Evidence/Timeline/Audit Trail: 84-90%.
- Runtime/Command/Execution: 46-55%.
- UI/Operator Surface: 55-65%.
- Local-only internal product: 65-73%.
- Usable end-to-end local product: 40-50%.
- External/cloud: 0%.
- Release/commercial: 0%.

## Next Safe Block

Recommended next macro-block: `NODAL_OS_LOCAL_APPROVAL_EXECUTION_DESIGN_ONLY_BOUNDARY`.
