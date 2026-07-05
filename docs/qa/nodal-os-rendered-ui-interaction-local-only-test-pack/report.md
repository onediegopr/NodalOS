# QA Report - Product Ledger Rendered UI Interaction Local-Only Test Pack

Date: 2026-07-05

Decision: `GO_WITH_FINDINGS_PRODUCT_LEDGER_RENDERED_UI_INTERACTION_LOCAL_ONLY_TEST_PACK_READY`

## Summary

This block adds local-only/test-only rendered DOM and interaction coverage for the Product Ledger operator surface route. No external browser automation, public deploy, command execution, append/write/export, network/cloud/DB/KMS/WORM/live automation or release/commercial claim is introduced.

## Route / Rendered Coverage

- HTTP in-process route tested: no.
- Reason: current Safety/Recipes test projects do not include `WebApplicationFactory`, ASP.NET `TestServer` or equivalent local test-host infrastructure.
- Fallback used: direct `ProductLedgerLocalDevRoutePreview.Render(...)` invocation with the default local/dev request.
- Route path asserted: `/internal/product-ledger/operator-surface`.
- Content type asserted: `text/html; charset=utf-8`.

## DOM Anchors Covered

- `product-ledger-surface-root`
- `product-ledger-authority`
- `product-ledger-verification-status`
- `product-ledger-checkpoint-status`
- `product-ledger-redaction-retention-status`
- `product-ledger-concurrency-status`
- `product-ledger-bounded-export-status`
- `product-ledger-operator-acceptance-status`
- `product-ledger-public-local-only-action-contract-status`
- `product-ledger-visual-evidence-status`
- `product-ledger-screenshot-evidence-status`
- `product-ledger-blocked-frontiers`
- `product-ledger-safe-next-steps`

## Interaction / Non-Execution Coverage

- Canonical action preview buttons are disabled.
- Canonical action preview buttons carry `data-executable="false"`.
- Canonical controls have empty handler/callback attributes.
- Rendered HTML has no forms, script tags, `onclick`, `formaction`, external HTTP links or external HTTP scripts.
- Changed Core source scan confirms no command execution, append/write/export, Pilot `/run`, recipe opt-in flag, network, DB/migration or process boundary APIs.

## Evidence Packet

- Rendered DOM contract summary: this report plus tests.
- Disabled/no-op interaction summary: this report plus tests.
- Screenshot updated: no.
- Hash: not applicable.
- Limitation: evidence is render-function DOM evidence, not HTTP in-process response evidence and not browser pixel evidence.

## Validations

- `dotnet build src/OneBrain.Core/OneBrain.Core.csproj --no-restore`: PASS, 0 warnings, 0 errors after rerun.
- `dotnet test tests/OneBrain.Safety.Tests/OneBrain.Safety.Tests.csproj --no-restore --filter "FullyQualifiedName~ProductLedgerRenderedRouteInteractionTests"`: PASS, 4/4.
- `dotnet test tests/OneBrain.Recipes.Tests/OneBrain.Recipes.Tests.csproj --no-restore --filter "FullyQualifiedName~ProductLedgerRenderedRouteInteractionTests"`: PASS, 1/1.

- `dotnet build OneBrain.slnx --no-restore`: PASS, 0 warnings, 0 errors.
- `dotnet test tests/OneBrain.Safety.Tests/OneBrain.Safety.Tests.csproj --no-restore --filter "FullyQualifiedName~ProductLedger"`: PASS, 161/161.
- `dotnet test tests/OneBrain.Recipes.Tests/OneBrain.Recipes.Tests.csproj --no-restore --filter "FullyQualifiedName~ProductLedger"`: PASS, 45/45.
- `dotnet test tests/OneBrain.Safety.Tests/OneBrain.Safety.Tests.csproj --no-restore --filter "FullyQualifiedName~PilotRecipeExecutionGate|FullyQualifiedName~Pilot"`: PASS, 20/20.
- `dotnet test tests/OneBrain.Safety.Tests/OneBrain.Safety.Tests.csproj --no-restore --filter "FullyQualifiedName~ProductLedgerInternalCommandPreviewRouterTests|FullyQualifiedName~ProductLedgerInternalCommandHandlerTests|FullyQualifiedName~ProductLedgerPublicUiActionSurfaceTests"`: PASS, 24/24.
- `dotnet test tests/OneBrain.Safety.Tests/OneBrain.Safety.Tests.csproj --no-restore --filter "FullyQualifiedName~ProductLedgerIntegrationPropertyTestPackTests"`: PASS, 4/4.
- QA JSON validation: PASS.
- `git diff --check`: PASS.
- Changed-file broad static scan: PASS, hits classified as negative assertions, blocked-frontier wording, docs limitations or tests.
- Changed Core source API scan: PASS, no network, DB/migration, process, file write/delete, append/export, MapPost, Pilot `/run` or recipe opt-in flag APIs.

## Findings

P0: 0

P1: 0

P2: 0

P3:

- HTTP in-process response testing remains future.
- Evidence is DOM/render-function level, not live browser screenshot evidence.
- Route remains fixture-safe canonical read model.

P4:

- Forbidden terms appear in tests/docs as negative assertions and blocked-frontier labels.

TRUE_RISK: 0.

## Percentages

- Product Ledger local-only core: 94-96%.
- Approval/Human Review: 87-92%.
- Evidence/Timeline/Audit Trail: 79-85%.
- Runtime/Command/Execution: 42-50%.
- UI/Operator Surface: 42-52%.
- Local-only internal product: 58-66%.
- Usable end-to-end local product: 30-40%.
- External/cloud: 0%.
- Release/commercial: 0%.
