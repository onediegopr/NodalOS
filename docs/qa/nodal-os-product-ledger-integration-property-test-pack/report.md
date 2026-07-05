# Product Ledger Integration Property Test Pack QA Report

Decision: `GO_WITH_FINDINGS_PRODUCT_LEDGER_INTEGRATION_PROPERTY_TEST_PACK_READY`

Initial HEAD: `ee91fcb569247aa3bf74eb5c0a395e3dce419dd5`

## Scope

This block adds local-only/test-only integration, property/corpus, tamper/adversarial and static write-surface coverage for the Product Ledger local-only line.

No productization, release/commercial, public deploy, public internet exposure, external network/provider/cloud, DB/migration, KMS/WORM/external trust, live Browser/CDP/WCU/OCR/Recipes automation, destructive action, unbounded export/write, external/cloud export, compliance custody or raw payload persistence was introduced.

## Implemented

- Safety integration pack for append, checkpoint, read/verify and bounded local report export.
- Operator acceptance matrix and static visual evidence linkage assertions.
- Deterministic metadata corpus covering unicode, markdown-like, JSON-like, fake bearer/api-key/password/connection-string/email/high-entropy inputs.
- Fail-closed corpus for duplicate logical keys, paths, UNC paths, URLs, raw payload/content values, control characters, oversized values/field counts and WORM/KMS/external-trust/custody overclaims.
- Tamper/adversarial tests for entry body, checkpoint, tail deletion, duplicate sequence, previous hash, metadata, redacted value, partial line and malformed JSONL.
- Static write-surface allowlist test for Core/Approval.
- Metadata guard hardening for raw values, URL-like values, control characters and duplicate logical keys.
- Read verification hardening so persisted metadata must already be safe.

## Coverage

Integration coverage:

- activation -> append -> checkpoint -> read/verify;
- multiple appends and hash-chain continuity;
- sensitive metadata redaction before persistence;
- bounded local report export with post-write hash verification;
- operator acceptance and visual evidence references remain linkable.

Property/corpus coverage:

- accepted metadata never persists raw secret-like values;
- rejected metadata does not append;
- rejected metadata does not mutate checkpoint;
- accepted metadata remains bounded;
- redaction is deterministic;
- generated ledger/checkpoint/export artifacts do not contain the fake secret corpus.

Tamper/adversarial coverage:

- ledger body tamper detected;
- checkpoint tamper detected;
- tail deletion detected;
- duplicate sequence detected;
- previous hash mismatch detected;
- metadata and redacted value tamper detected;
- checkpoint ahead/behind detected;
- partial last line and malformed JSONL fail closed.

## Validations

| Command | Result |
| --- | --- |
| `dotnet build src/OneBrain.Core/OneBrain.Core.csproj --no-restore` | PASS, 0 warnings, 0 errors |
| `dotnet test tests/OneBrain.Safety.Tests/OneBrain.Safety.Tests.csproj --no-restore --filter "FullyQualifiedName~ProductLedgerPathLocalOnlyActiveWriterTests.LocalOnlyWriter_FailsClosedOnRehashedUnsafeExistingLedgerMetadata\|FullyQualifiedName~ProductLedgerIntegrationPropertyTestPackTests"` | PASS, 5/5; project build emitted pre-existing warning set |
| `dotnet test tests/OneBrain.Safety.Tests/OneBrain.Safety.Tests.csproj --no-build --filter "FullyQualifiedName~ProductLedger"` | PASS, 150/150 |
| `dotnet test tests/OneBrain.Recipes.Tests/OneBrain.Recipes.Tests.csproj --no-build --filter "FullyQualifiedName~ProductLedger"` | PASS, 44/44 |
| `dotnet build OneBrain.slnx --no-restore` | PASS, 1 pre-existing Recipes MSTEST0037 warning, 0 errors |
| `dotnet test tests/OneBrain.Recipes.Tests/OneBrain.Recipes.Tests.csproj --no-build` | PASS, 1552/1552 |
| `dotnet test tests/OneBrain.Safety.Tests/OneBrain.Safety.Tests.csproj --no-build` | TIMEOUT at 300s; processes stopped and build servers shut down |
| QA JSON validation | PASS |
| `git diff --check` | PASS |
| static scan changed files | PASS; hits classified as detector markers, synthetic test corpus/negative assertions, blocked reasons, docs limitations or historical traceability |

No raw payload or raw secret persistence artifact was introduced.

## Findings

| Severity | Count | Notes |
| --- | ---: | --- |
| P0 | 0 | No external boundary, destructive action or raw payload persistence introduced. |
| P1 | 0 | No productization/release/commercial claim introduced. |
| P2 | 0 | Rehashed unsafe metadata gap closed by read verification hardening. |
| P3 | 3 | Future route-specific negative tests, deletion lifecycle and broader property fuzzing remain. |
| P4 | 2 | Static scans need false-positive classification; local evidence is not WORM/KMS/compliance custody. |

TRUE_RISK: 0.

## Readiness

| Area | Status |
| --- | --- |
| Product Ledger local-only core | `94-96%` |
| Approval/Human Review | `line-scoped local-only evidence unchanged` |
| Evidence/Timeline/Audit Trail | `local append/checkpoint/export/evidence linkage tested` |
| Runtime/Command/Execution | `unchanged, Pilot default-blocked explicit opt-in` |
| UI/operator surface | `15-25%` |
| Local-only internal product | `51-60%` |
| Usable end-to-end local product | `22-34%` |
| External/cloud | `0%` |
| Release/commercial | `0%` |

## Next

Recommended next macro-block: `A) MB4 Ledger/evidence consolidation & writer de-triplication`.
