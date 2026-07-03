# Durable Stage 2 Redaction Hardening Test-Only

Status: `TEST_ONLY_HARDENED / REDACTION_SENSITIVE_DATA / RUNTIME_PRODUCT_STILL_NO_GO`

Baseline HEAD: `cb6fc9ca326bdf9a93ee8e08c75e7525370a5668`

Decision: harden Durable Stage 2 test-only redaction/sensitive-data gates so email-like PII and Windows absolute paths reject before persistence. No product/runtime/live behavior is enabled.

## Implemented

- Stage 2-only sensitive-data validation before delegating to `Append`.
- Email-like PII rejection before persistence.
- Windows absolute path and UNC-like path rejection before persistence.
- Safety tests proving no append/write and no ledger creation for those inputs.

Stage 1 behavior remains unchanged. Stage 2 remains local/temp and test-only.

## Validation

- `dotnet build OneBrain.slnx --no-restore /p:UseSharedCompilation=false /maxcpucount:1`: PASS, 0 warnings, 0 errors.
- `dotnet build src/OneBrain.Core/OneBrain.Core.csproj --no-restore /p:UseSharedCompilation=false /maxcpucount:1`: PASS, 0 warnings, 0 errors.
- `dotnet test tests/OneBrain.Safety.Tests/OneBrain.Safety.Tests.csproj --filter DurableAuditTrailAppendOnlyMinimal --no-restore /p:UseSharedCompilation=false /maxcpucount:1`: PASS, 27/27.
- `dotnet test tests/OneBrain.Recipes.Tests/OneBrain.Recipes.Tests.csproj --filter DurableAuditTrailAppendOnlyMinimal --no-restore /p:UseSharedCompilation=false /maxcpucount:1`: PASS, 6/6.
- `git diff --check`: PASS.
- Static scan changed files: PASS; positive hits are guard-string fixtures inside tests only.

## Findings

| Severity | Finding |
| --- | --- |
| P0 | None. No runtime/product/live authority introduced. |
| P1 | None. No product enablement or release/commercial claim. |
| P2 | None for authorized Stage 2 test-only scope. |
| P3 | Redaction remains deterministic rejection, not a product redaction service. |
| P4 | Historical docs remain traceability records under latest decision-log canon. |

## Decision

`GO_WITH_FINDINGS_DURABLE_STAGE2_REDACTION_HARDENING_READY`

The next safe macro-block is `NODAL_OS_DURABLE_STAGE2_TEST_ONLY_CLOSEOUT_EXTERNAL_AUDIT_READ_ONLY`.
