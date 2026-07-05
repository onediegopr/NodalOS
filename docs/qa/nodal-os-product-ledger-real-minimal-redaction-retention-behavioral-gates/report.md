# QA Report - Product Ledger Real Minimal Redaction Retention Behavioral Gates

Date: 2026-07-05

Decision: `GO_WITH_FINDINGS_PRODUCT_LEDGER_REAL_MINIMAL_REDACTION_RETENTION_BEHAVIORAL_GATES_READY`

## Scope

This block fixes `MA-03_REAL_MINIMAL_REDACTION_RETENTION_BEHAVIORAL_GATES` by adding minimal local-only behavioral redaction and retention/bounded metadata guards before Product Ledger append persistence.

No productization, release/commercial, cloud/provider/network, DB/migration, KMS/WORM/external trust, live Browser/CDP/WCU/OCR/Recipes automation, public deploy, destructive action, unbounded export/write, compliance custody or raw payload persistence was introduced.

## Implemented

- `ProductLedgerPathLocalOnlyMetadataGuard`.
- Writer integration before append persistence.
- Redaction of sensitive metadata into `redaction.fieldNN=redacted-sensitive`.
- Safe markers: `redaction.applied`, `retention.mode`, `retention.max-fields`, `retention.max-value-chars`.
- Fail-closed handling for raw payload/content keys, paths, oversized metadata, too many fields, unbounded retention and compliance/custody overclaims.
- Ledger entry/byte ceilings for bounded local retention policy.
- Safety and Recipes coverage for redaction, retention, hash-chain verification, concurrency and failure no-corruption behavior.

## MA-03 Status

No longer caller-attested only:

- Product Ledger metadata redaction before persistence.
- Product Ledger metadata retention/bounded persistence.

Still caller-attested:

- authority evidence;
- failure/replay/rollback evidence;
- activation evidence references.

Pending:

- full deletion lifecycle is not implemented;
- compliance-grade retention/custody is not implemented;
- broader property/integration packs can still increase confidence.

## Validations

| Command | Result |
| --- | --- |
| `dotnet build src/OneBrain.Core/OneBrain.Core.csproj --no-restore` | PASS, 0 warnings, 0 errors |
| `dotnet test tests/OneBrain.Safety.Tests/OneBrain.Safety.Tests.csproj --no-restore --filter "FullyQualifiedName~ProductLedgerPathLocalOnlyActiveWriterTests"` | PASS, 13/13 |
| `dotnet test tests/OneBrain.Recipes.Tests/OneBrain.Recipes.Tests.csproj --no-restore --filter "FullyQualifiedName~ProductLedgerPathLocalOnlyActiveWriterTests"` | PASS, 3/3 |
| `dotnet build OneBrain.slnx --no-restore` | PASS, 0 warnings, 0 errors |
| `dotnet test tests/OneBrain.Safety.Tests/OneBrain.Safety.Tests.csproj --no-build --filter "FullyQualifiedName~ProductLedger"` | PASS, 146/146 |
| `dotnet test tests/OneBrain.Recipes.Tests/OneBrain.Recipes.Tests.csproj --no-build --filter "FullyQualifiedName~ProductLedger"` | PASS, 44/44 |
| `dotnet test tests/OneBrain.Recipes.Tests/OneBrain.Recipes.Tests.csproj --no-build` | PASS, 1552/1552 |
| `dotnet test tests/OneBrain.Safety.Tests/OneBrain.Safety.Tests.csproj --no-build` | TIMEOUT at 300s; processes stopped and build servers shut down |
| QA JSON validation | PASS |
| `git diff --check` | PASS |
| static scan changed files | PASS; hits classified as detector markers, synthetic test inputs/negative assertions, blocked reasons, docs limitations or historical traceability |

No raw payload or raw secret persistence artifact was introduced.

## Findings

| Severity | Count | Notes |
| --- | ---: | --- |
| P0 | 0 | No raw payload persistence, external boundary or destructive behavior introduced. |
| P1 | 0 | No productization/release/commercial claim introduced. |
| P2 | 0 | MA-03 addressed for Product Ledger persisted metadata. |
| P3 | 2 | Deletion lifecycle not implemented; broader property/integration pack remains useful. |
| P4 | 2 | Minimal guard is not DLP/compliance-grade; local evidence is not WORM/KMS custody. |

TRUE_RISK: 0.

Full Safety.Tests timed out at 300s and was not used as a release/product gate. Focused Safety ProductLedger coverage passed 146/146 and focused active writer coverage passed 13/13.

## Readiness

| Area | Before | After |
| --- | --- | --- |
| Product Ledger local-only core | `88-92%` | `92-95%` |
| Approval/Human Review | `line-scoped local-only evidence` | `unchanged, line-scoped` |
| Evidence/Timeline/Audit Trail | `local-only writer with caller-attested redaction/retention` | `local-only writer with behavioral metadata guard` |
| Runtime/Command/Execution | `Pilot default-blocked, explicit local lab/dev opt-in only` | `unchanged` |
| UI/Operator Surface | `15-25%` | `15-25%` |
| Local-only internal product | `48-57%` | `50-59%` |
| Usable end-to-end local product | `20-30%` | `20-32%` |
| External/cloud | `0%` | `0%` |
| Release/commercial | `0%` | `0%` |

## Next Recommended Macro-Block

`B) MB6 Integration + property test pack`
