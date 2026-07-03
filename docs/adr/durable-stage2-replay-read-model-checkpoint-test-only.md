# Durable Stage 2 Replay Read Model Checkpoint Test-Only

Status: `TEST_ONLY_HARDENED / REPLAY_READ_MODEL_CHECKPOINT_EVIDENCE / RUNTIME_PRODUCT_STILL_NO_GO`

Baseline HEAD: `57547075fe5e167b76f6e75eae8a5444616e5980`

Decision: expand Durable Stage 2 test-only evidence for read-only replay/read-model behavior and local checkpoint/truncation claim boundaries. No product/runtime/live behavior is enabled.

## Scope

Implemented as tests only:

- repeated `VerifyFile` reads do not mutate ledger text, length, entry count or last hash;
- local hash-chain verification does not overclaim tail-deletion evidence without an external checkpoint.

The second test deliberately proves a limitation: deleting a valid tail event can leave a shorter internally valid ledger. Without an external checkpoint or trusted expected head, the local hash chain must not claim tail-deletion detection.

## Validation

- `dotnet build OneBrain.slnx --no-restore /p:UseSharedCompilation=false /maxcpucount:1`: PASS, 0 warnings, 0 errors.
- `dotnet test tests/OneBrain.Safety.Tests/OneBrain.Safety.Tests.csproj --filter DurableAuditTrailAppendOnlyMinimal --no-restore /p:UseSharedCompilation=false /maxcpucount:1`: PASS, 25/25.
- `dotnet test tests/OneBrain.Recipes.Tests/OneBrain.Recipes.Tests.csproj --filter DurableAuditTrailAppendOnlyMinimal --no-restore /p:UseSharedCompilation=false /maxcpucount:1`: PASS, 6/6.
- `git diff --check`: PASS.
- Static scan changed files: PASS; positive hits are guard-string fixtures inside tests only.

## Findings

| Severity | Finding |
| --- | --- |
| P0 | None. No runtime/product/live authority introduced. |
| P1 | None. No product enablement or release/commercial claim. |
| P2 | None for authorized Stage 2 test-only scope. |
| P3 | External checkpoint, WORM, KMS, cloud or compliance-grade trust remains unimplemented and prohibited. |
| P4 | Historical docs remain traceability records under latest decision-log canon. |

## Decision

`GO_WITH_FINDINGS_DURABLE_STAGE2_REPLAY_READ_MODEL_CHECKPOINT_READY`

The next safe macro-block is `NODAL_OS_DURABLE_STAGE2_REDACTION_HARDENING_TEST_ONLY`.
