# QA Report - Global Safety Claim Reconciliation And Product Ledger Writer Concurrency Hardening

Date: 2026-07-05

Decision: `GO_WITH_FINDINGS_GLOBAL_SAFETY_CLAIM_RECONCILIATION_AND_PRODUCT_LEDGER_WRITER_CONCURRENCY_HARDENING_READY`

## Scope

This block fixes MA-01 P1 and MA-02 P2 before productization. It does not productize Pilot, ChromeLab, CDP or Product Ledger. It does not enable public deploy, provider/cloud/network, DB/migration, KMS/WORM/external trust, productive Browser/CDP/WCU/OCR/Recipes live automation, destructive action, unbounded export/write or release/commercial readiness.

## MA-01 Safety Claim Reconciliation

- `/run` is blocked by default.
- Explicit opt-in requires `NODAL_OS_ENABLE_PILOT_RECIPE_EXECUTION=1`.
- Pilot safety summary is relabeled as separate lab/dev runtime footprint default-blocked.
- Product Ledger local-only readiness is line-scoped, not repo-wide.
- Pilot, ChromeLab and CDP are not Product Ledger local-only authority and not release/commercial.

## MA-02 Writer Concurrency

- `ProductLedgerPathLocalOnlyActiveWriter.Append` now uses a per-canonical-ledger-file lock.
- The lock covers existing ledger read, checkpoint verification, sequence calculation, previous-hash calculation, append and checkpoint write.
- Tests prove concurrent appends produce unique sequential entries and preserve the hash chain.
- Tests prove blocked interleaved appends do not corrupt the successful chain.

## MA-03 Carry Forward

`MA-03_REAL_MINIMAL_REDACTION_RETENTION_BEHAVIORAL_GATES` remains pending. Current Product Ledger activation evidence booleans are caller-attested evidence, not deep behavioral redaction or retention enforcement.

## Validations

| Command | Result |
| --- | --- |
| `dotnet build src/OneBrain.Core/OneBrain.Core.csproj --no-restore` | PASS, 0 warnings, 0 errors |
| `dotnet test tests/OneBrain.Safety.Tests/OneBrain.Safety.Tests.csproj --no-restore --filter "FullyQualifiedName~ProductLedgerPathLocalOnlyActiveWriterTests"` | PASS, 11/11 |
| `dotnet test tests/OneBrain.Recipes.Tests/OneBrain.Recipes.Tests.csproj --no-restore --filter "FullyQualifiedName~PilotShellTests|FullyQualifiedName~PilotGuiUxHardeningTests|FullyQualifiedName~ProductLedgerPathLocalOnlyActiveWriterTests"` | PASS, 20/20 |
| `dotnet build OneBrain.slnx --no-restore` | PASS, 0 warnings, 0 errors |
| `dotnet test tests/OneBrain.Safety.Tests/OneBrain.Safety.Tests.csproj --no-build --filter "FullyQualifiedName~ProductLedger"` | PASS, 144/144 |
| `dotnet test tests/OneBrain.Recipes.Tests/OneBrain.Recipes.Tests.csproj --no-build --filter "FullyQualifiedName~ProductLedger|FullyQualifiedName~PilotShellTests|FullyQualifiedName~PilotGuiUxHardeningTests"` | PASS, 61/61 |
| `dotnet test tests/OneBrain.Recipes.Tests/OneBrain.Recipes.Tests.csproj --no-build` | PASS, 1551/1551 |
| `dotnet test tests/OneBrain.Safety.Tests/OneBrain.Safety.Tests.csproj --no-build` | TIMEOUT at 300s; not used as release gate |
| JSON validation for this report | PASS |
| `git diff --check` | PASS |
| static forbidden positive-claim scan on changed files | PASS, hits classified as blocked reasons, non-goals, negative assertions, test names or historical traceability |

## Findings

| Severity | Count | Notes |
| --- | ---: | --- |
| P0 | 0 | No product/live/cloud/release enablement introduced. |
| P1 | 0 | MA-01 fixed by default-off Pilot gate and claim relabel. |
| P2 | 0 | MA-02 fixed by per-path writer lock and concurrency tests. |
| P3 | 2 | MA-03 behavioral redaction/retention gates remain; lab/dev runtime footprints require continued claim isolation. |
| P4 | 2 | Older historical labels remain traceability only; local evidence is not WORM/KMS/compliance custody. |

TRUE_RISK: 0.

Static scan false positives: blocked boundary wording, enum/property names, negative tests and historical decision-log traceability. No `ZeroReadOnly` claim remains in changed source/tests/docs.

Full Safety.Tests was attempted after focused packs and timed out at 300s. The timed-out `dotnet`/`vstest` processes were stopped and `dotnet build-server shutdown` completed.

## Readiness

| Area | Before | After |
| --- | --- | --- |
| Product Ledger local-only core | `80-88%` | `88-92%` |
| Approval/Human Review | `local-only evidence` | `unchanged, line-scoped` |
| Evidence/Timeline/Audit Trail | `local-only evidence` | `concurrency-hardened local-only writer` |
| Runtime/Command/Execution | `lab/dev footprint ambiguous in global claims` | `Pilot /run default-blocked, explicit opt-in only` |
| UI/Operator Surface | `15-25%` | `15-25%` |
| Local-only internal product | `45-55%` | `48-57%` |
| Usable end-to-end local product | `20-30%` | `20-30%` |
| External/cloud | `0%` | `0%` |
| Release/commercial | `0%` | `0%` |

## Next Recommended Macro-Block

`D) MB3 Real minimal redaction+retention behavioral gates`
