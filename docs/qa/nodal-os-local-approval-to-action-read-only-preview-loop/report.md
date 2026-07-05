# QA Report - Local Approval-To-Action Read-Only Preview Loop

Date: 2026-07-05

Decision: `GO_WITH_FINDINGS_LOCAL_APPROVAL_TO_ACTION_READ_ONLY_PREVIEW_LOOP_READY`

## Summary

This block adds a local-only/read-only/preview-only approval-to-action loop to the Product Ledger operator surface. It connects approval preview, candidate action preview, policy/gate result, no-op execution result, evidence refs and safe next step without executing product commands or triggering append/write/export.

## Loop Coverage

- Loop id: `product-ledger.local-approval-action-preview-loop.v1`.
- Scope: `ProductLedgerLocalOnlyLineScoped`.
- Candidate action: `ViewLedgerReadiness`.
- Policy decision: `NeedsHumanReviewPreview`.
- No-op result: `NO_OP_PREVIEW_ONLY_BLOCKED_BEFORE_HANDLER`.
- Safe next step: `LOCAL_ROUTE_LIVE_LEDGER_READ_MODEL_TEST_SAFE_OR_HTTP_IN_PROCESS_ROUTE_TEST_INFRA`.

## DOM Anchors Covered

- `product-ledger-approval-preview`
- `product-ledger-candidate-action-preview`
- `product-ledger-policy-gate-preview`
- `product-ledger-noop-execution-preview`
- `product-ledger-preview-evidence-refs`
- `product-ledger-approval-safe-next-step`

Every section states read-only, preview-only, no product command execution, no write/export and no release/commercial.

## Interaction / Non-Execution Coverage

- Preview control is disabled.
- Preview control carries `data-executable="false"`.
- Handler and callback attributes are empty.
- Rendered HTML has no forms, scripts, `onclick`, `formaction` or executable controls.
- No-op execution preview records handler, callback, append, write, export and Pilot run as not invoked.
- Changed source scan confirms no process start, file append/write/delete APIs, network APIs, DB/migration APIs, MapPost execution endpoint, export invocation, command handler invocation or Pilot execution gate call in the approval loop path.

## Evidence Packet

- Model evidence: `ProductLedgerLocalApprovalPreviewLoop`.
- Surface evidence: `ProductLedgerOperatorSurfaceModel.ApprovalPreviewLoop`.
- Route evidence: Product Ledger local/dev HTML section.
- Test evidence: Safety and Recipes approval preview loop tests.
- Readiness evidence refs: active writer, operator acceptance and visual QA references already present on the canonical operator surface.

## Validations

- `dotnet build src/OneBrain.Core/OneBrain.Core.csproj --no-restore -v:minimal`: PASS, 0 warnings, 0 errors after build-server shutdown/rerun.
- `dotnet build OneBrain.slnx --no-restore -v:minimal`: PASS, 0 warnings, 0 errors.
- `dotnet test tests/OneBrain.Safety.Tests/OneBrain.Safety.Tests.csproj --no-restore --filter "FullyQualifiedName~ProductLedgerLocalApprovalPreviewLoopTests"`: PASS, 6/6.
- `dotnet test tests/OneBrain.Recipes.Tests/OneBrain.Recipes.Tests.csproj --no-restore --filter "FullyQualifiedName~ProductLedgerLocalApprovalPreviewLoopTests"`: PASS, 1/1.
- `dotnet test tests/OneBrain.Safety.Tests/OneBrain.Safety.Tests.csproj --no-restore --no-build --filter "FullyQualifiedName~ProductLedgerRenderedRouteInteractionTests"`: PASS, 4/4.
- `dotnet test tests/OneBrain.Recipes.Tests/OneBrain.Recipes.Tests.csproj --no-restore --no-build --filter "FullyQualifiedName~ProductLedgerRenderedRouteInteractionTests"`: PASS, 1/1.
- `dotnet test tests/OneBrain.Safety.Tests/OneBrain.Safety.Tests.csproj --no-restore --no-build --filter "FullyQualifiedName~ProductLedger"`: PASS, 167/167.
- `dotnet test tests/OneBrain.Recipes.Tests/OneBrain.Recipes.Tests.csproj --no-restore --no-build --filter "FullyQualifiedName~ProductLedger"`: PASS, 46/46.
- `dotnet test tests/OneBrain.Safety.Tests/OneBrain.Safety.Tests.csproj --no-restore --no-build --filter "FullyQualifiedName~ProductLedgerInternalCommandPreviewRouterTests|FullyQualifiedName~ProductLedgerInternalCommandHandlerTests|FullyQualifiedName~ProductLedgerPublicUiActionSurfaceTests"`: PASS, 24/24.
- `dotnet test tests/OneBrain.Safety.Tests/OneBrain.Safety.Tests.csproj --no-restore --no-build --filter "FullyQualifiedName~PilotRecipeExecutionGate|FullyQualifiedName~Pilot"`: PASS, 22/22.
- `dotnet test tests/OneBrain.Safety.Tests/OneBrain.Safety.Tests.csproj --no-restore --no-build --filter "FullyQualifiedName~ProductLedgerIntegrationPropertyTestPackTests"`: PASS, 4/4.

## Findings

P0: 0

P1: 0

P2: 0

P3:

- HTTP in-process route response test infrastructure remains future.
- The route still uses fixture-safe canonical read model state, not live arbitrary Product Ledger path scanning.
- Real approval execution remains outside this block.

P4:

- The preview loop is intentionally blocked with `NeedsHumanReviewPreview`.
- Evidence refs are local readiness links, not compliance custody evidence.

TRUE_RISK: 0.

## Percentages

- Product Ledger local-only core: 94-96%.
- Approval/Human Review: 90-94%.
- Evidence/Timeline/Audit Trail: 80-86%.
- Runtime/Command/Execution: 45-53%.
- UI/Operator Surface: 48-58%.
- Local-only internal product: 61-69%.
- Usable end-to-end local product: 34-44%.
- External/cloud: 0%.
- Release/commercial: 0%.

## Next Safe Block

Recommended next macro-block: `NODAL_OS_HTTP_IN_PROCESS_ROUTE_RESPONSE_TEST_INFRASTRUCTURE_LOCAL_ONLY`.
