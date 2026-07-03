# Durable Stage 2 Test-Only External Audit And Fixes

Status: `READ_ONLY_EXTERNAL_AUDIT_WITH_TARGETED_TEST_ONLY_FIX / RUNTIME_PRODUCT_STILL_NO_GO`

Baseline HEAD: `c3506479f91dfb611a83b110d974dcc30d77e673`

Decision: accept the Durable Stage 2 test-only implementation with a targeted P3 fix. Stage 2 remains test-only/local-temp. Runtime/product/live enablement, product ledger paths, service registration, command handlers, UI product actions, DB/migration, provider/cloud/network, Browser/CDP live automation, WCU/OCR live action, Recipes live execution and release/commercial readiness remain prohibited.

## Audit Result

The audit found no P0/P1/P2 issues. One P3 issue was corrected:

- with a valid Stage 2 gate and empty `StorageRoot`, the Stage 2 pre-gate could classify the request as `ProductLedgerPathRejected` before the base validator returned `EmptyStorageRoot`;
- the fix limits product-ledger path checks to non-empty roots and adds Safety coverage proving empty storage roots preserve the base rejection reason.

## Validation

- `dotnet build OneBrain.slnx /p:UseSharedCompilation=false /maxcpucount:1`: PASS, 0 warnings, 0 errors.
- `dotnet test tests/OneBrain.Safety.Tests/OneBrain.Safety.Tests.csproj --filter DurableAuditTrailAppendOnlyMinimal --no-restore /p:UseSharedCompilation=false /maxcpucount:1`: PASS, 21/21.
- `dotnet test tests/OneBrain.Recipes.Tests/OneBrain.Recipes.Tests.csproj --filter DurableAuditTrailAppendOnlyMinimal --no-restore /p:UseSharedCompilation=false /maxcpucount:1`: PASS, 6/6.
- `git diff --check`: PASS.
- Static scan changed code/tests: PASS; positive hits are guard-string fixtures inside tests only.

## Findings

| Severity | Finding |
| --- | --- |
| P0 | None. No runtime/product/live authority introduced. |
| P1 | None. No product enablement or release/commercial claim. |
| P2 | None for authorized Stage 2 test-only scope. |
| P3 | Fixed: empty storage root now preserves `EmptyStorageRoot` instead of product-ledger classification. |
| P3 | Remaining: redaction proof is caller-attested test-only evidence, not a product redaction service. |
| P3 | Remaining: property/concurrency expansion, replay/read-model and checkpoint/truncation hardening remain future safe macro-blocks. |
| P4 | Historical docs remain traceability records under latest decision-log canon. |

## Decision

`GO_WITH_FINDINGS_DURABLE_STAGE2_TEST_ONLY_EXTERNAL_AUDIT_READY`

The next safe macro-block is `NODAL_OS_DURABLE_STAGE2_PROPERTY_CONCURRENCY_EXPANSION_TEST_ONLY`.
