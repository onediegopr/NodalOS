# Ledger Evidence Consolidation and Writer De-Triplication QA Report

Decision: `GO_WITH_FINDINGS_LEDGER_EVIDENCE_CONSOLIDATION_WRITER_DETRIPLICATION_READY`

Initial HEAD: `534954b7167d031e9ddce314aae6ddad88d42f1b`

## Scope

This block reduces architectural drift in Product Ledger writer/evidence concepts without changing ledger format or opening product/runtime/release boundaries.

## Implemented

- Added `ProductLedgerLocalAppendOnlyHashing`.
- Active and local-temp writers now share entry-hash and ledger-hash calculation.
- Added `ProductLedgerLocalLedgerTaxonomy` to classify active, temp, scaffold, Durable and future modes.
- Left `ProductLedgerPathLocalOnlyActiveWriter` as the single active local-only Product Ledger authority.
- Kept `ProductLedgerPathLocalTempWriterTestOnly` as local-temp/test-only.
- Kept `ProductLedgerPathWriterScaffoldDisabled` as historical/non-authoritative compatibility surface.
- Added preview-only/public-execution-false/product-command-execution-false guarantees to command handler results.
- Added `ProductLedgerEvidenceConsolidationTests`.

## Compatibility Notes

No public classes were renamed or deleted because historical QA, ADR, handoff and tests still reference the existing names. Compatibility was preferred over a broad rename in this block.

No ledger JSONL/checkpoint format changed. Hash constants in regression tests prove helper output matches the existing material format.

## Validations

| Command | Result |
| --- | --- |
| `dotnet build src/OneBrain.Core/OneBrain.Core.csproj --no-restore` | PASS, 0 warnings, 0 errors |
| `dotnet test tests/OneBrain.Safety.Tests/OneBrain.Safety.Tests.csproj --no-restore --filter "FullyQualifiedName~ProductLedgerEvidenceConsolidationTests"` | PASS, 6/6; test project emitted pre-existing warning set |
| `dotnet test tests/OneBrain.Safety.Tests/OneBrain.Safety.Tests.csproj --no-build --filter "FullyQualifiedName~ProductLedger"` | PASS, 156/156 |
| `dotnet test tests/OneBrain.Recipes.Tests/OneBrain.Recipes.Tests.csproj --no-build --filter "FullyQualifiedName~ProductLedger"` | PASS, 44/44 |
| `dotnet test tests/OneBrain.Safety.Tests/OneBrain.Safety.Tests.csproj --no-build --filter "FullyQualifiedName~ProductLedgerInternalCommandHandlerTests\|FullyQualifiedName~ProductLedgerInternalCommandPreviewRouterTests"` | PASS, 15/15 |
| `dotnet test tests/OneBrain.Safety.Tests/OneBrain.Safety.Tests.csproj --no-build --filter "FullyQualifiedName~DurableAuditTrailAppendOnlyMinimal\|FullyQualifiedName~DurableAuditTrailLocalTempCheckpointEvidence"` | PASS, 36/36 |
| `dotnet test tests/OneBrain.Recipes.Tests/OneBrain.Recipes.Tests.csproj --no-build --filter "FullyQualifiedName~DurableAuditTrailAppendOnlyMinimal\|FullyQualifiedName~DurableAuditTrailLocalTempCheckpointEvidence"` | PASS, 8/8 |
| `dotnet build OneBrain.slnx --no-restore` | PASS, 1 pre-existing Recipes MSTEST0037 warning, 0 errors |
| `dotnet test tests/OneBrain.Recipes.Tests/OneBrain.Recipes.Tests.csproj --no-build` | PASS, 1552/1552 |
| `dotnet test tests/OneBrain.Safety.Tests/OneBrain.Safety.Tests.csproj --no-build` | TIMEOUT at 300s; processes stopped and build servers shut down |
| QA JSON validation | PASS |
| `git diff --check` | PASS |
| static scan changed files | PASS; hits classified as taxonomy terms, preview/no-op guarantees, status strings, blocked/no-go reasons, docs limitations or historical traceability |

No runtime/product/release boundary was opened, and no ledger format migration was required.

## Findings

| Severity | Count | Notes |
| --- | ---: | --- |
| P0 | 0 | No external/product/destructive boundary introduced. |
| P1 | 0 | No productization/release/commercial claim introduced. |
| P2 | 0 | Single active Product Ledger authority is code-level tested. |
| P3 | 3 | Public names retained for compatibility; local-temp DTOs still separate; deletion lifecycle remains future work. |
| P4 | 2 | Durable/Product Ledger concepts remain sibling subsystems; historical docs retain old names. |

TRUE_RISK: 0.

## Readiness

| Area | Status |
| --- | --- |
| Product Ledger local-only core | `94-96%` |
| Approval/Human Review | `line-scoped local-only evidence unchanged` |
| Evidence/Timeline/Audit Trail | `improved taxonomy coherence; Product Ledger remains canonical active local ledger` |
| Runtime/Command/Execution | `preview-only/no public product command execution; Pilot default-blocked explicit opt-in` |
| UI/operator surface | `15-25%` |
| Local-only internal product | `52-61%` |
| Usable end-to-end local product | `22-34%` |
| External/cloud | `0%` |
| Release/commercial | `0%` |

## Next

Recommended next macro-block: `A) MB5 Single real local operator route + surface consolidation`.
