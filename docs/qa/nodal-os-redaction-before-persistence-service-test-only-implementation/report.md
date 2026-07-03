# NODAL OS - Redaction Before Persistence Service Test-Only Implementation

Decision: `GO_WITH_FINDINGS_REDACTION_BEFORE_PERSISTENCE_TEST_ONLY_SERVICE_READY`

Date: 2026-07-03

## Scope

Manual GO authorized implementation and tests for Redaction-before-Persistence Service in test-only/local-safe mode. This block does not enable runtime product behavior, productive service registration, command handlers, UI product actions, product ledger paths, DB/migration/provider/cloud/network behavior, Browser/CDP/WCU/OCR/Recipes live paths or release/commercial readiness.

## Repo Guard

| Field | Value |
| --- | --- |
| Repo | `C:/DESARROLLO/NodalOS/Codigo-m12-audit` |
| Branch | `chrome-lab-001-extension-local-ai-bridge` |
| Initial HEAD | `7ba1e9fe3cf3d7e4c4819f5d02e994dbd052f639` |
| Worktree initial | `clean` |
| Origin sync initial | `0 0` |
| Stash | listed only, not touched: `stash@{0}: On chrome-lab-001-extension-local-ai-bridge: pre-m11-legacy-state` |

## Files Changed

| File | Purpose |
| --- | --- |
| `src/OneBrain.Core/Approval/RedactionBeforePersistenceService.cs` | New isolated deterministic test-only service and result/evidence model. |
| `src/OneBrain.Core/Approval/DurableAuditTrailAppendOnlyMinimal.cs` | Stage 2 test-only gate now requires successful redaction result and candidate hash binding. |
| `tests/OneBrain.Safety.Tests/RedactionBeforePersistenceServiceSafetyTests.cs` | New Safety coverage for corpus rejection, safe evidence and static no-enable scan. |
| `tests/OneBrain.Safety.Tests/DurableAuditTrailAppendOnlyMinimalSafetyTests.cs` | Stage 2 tests updated for service-produced redaction evidence and missing/failed/mismatched evidence rejection. |
| `tests/OneBrain.Recipes.Tests/DurableAuditTrailAppendOnlyMinimalTests.cs` | Recipes Stage 2 positive path updated to use service evidence. |
| Docs/QA/handoff/decision-log | Implementation closeout packet. |

## Implementation Summary

`RedactionBeforePersistenceService` evaluates a durable audit candidate before append and returns:

- `Allowed` with a safe request and evidence-safe summary; or
- `Rejected` with safe reason codes and no raw candidate payload.

The service fail-closes on missing/unknown policy, missing candidate, malformed references, malformed metadata, raw payloads, secret-like strings, PII-like strings and path-like strings.

`AppendStage2TestOnly` rejects if redaction evidence is missing, failed, unsafe, not completed before persistence or mismatched to the exact candidate hash. When allowed, it appends only the service-produced safe request.

## Boundary Confirmation

| Boundary | Status |
| --- | --- |
| Test-only service | IMPLEMENTED |
| Runtime/live/product | NO-GO_PRESERVED |
| Product service registration | ABSENT |
| Command handlers / command bus | ABSENT |
| UI product actions | ABSENT |
| Product ledger path | PROHIBITED |
| DB/migration/provider/cloud/network | ABSENT |
| Browser/CDP/WCU/OCR/Recipes live | ABSENT |
| Raw sensitive persistence/logging | BLOCKED_BY_TESTS_FOR_COVERED_CORPUS |
| Release/commercial | NO-GO |

## Validations

| Validation | Result |
| --- | --- |
| Repo guard | PASS |
| Full solution build (`dotnet build OneBrain.slnx`) | PASS, 0 warnings, 0 errors on final successful run |
| Core build (`dotnet build src/OneBrain.Core/OneBrain.Core.csproj`) | PASS, 0 warnings, 0 errors |
| Safety focused tests | PASS, 32/32 |
| Recipes focused tests | PASS, 6/6 |
| `git diff --check` | PASS |
| JSON validation | PASS |
| Static scan changed files | PASS; no TRUE_RISK |

Note: the first full solution build attempt timed out before returning final output. It was rerun with a longer timeout and passed. Safety/Recipes test project builds emit pre-existing unrelated warnings outside the changed Core files.

## Findings

| Severity | Count | Details |
| --- | ---: | --- |
| P0 | 0 | No runtime/product/live authority or scope leak. |
| P1 | 0 | No product enablement, DI registration, command handler, UI product action, product ledger path or release/commercial claim. |
| P2 | 0 | No blocker for authorized test-only implementation. |
| P3 | 3 | (1) Corpus is intentionally focused and should expand before broader claims. (2) Nested metadata is future work because current durable request metadata is flat. (3) Product/runtime adoption remains blocked by external audit and explicit manual GO. |
| P4 | 1 | Historical design/read-only docs remain traceability records. |

## Percentages

| Track | Conservative status |
| --- | --- |
| Redaction-before-persistence test-only service | 82-88% |
| Redaction-before-persistence product service | 0% / NO-GO |
| Durable Stage 2 test-only implementation | 90-94% |
| Runtime/live product enablement | 0% / NO-GO |
| Release/commercial readiness | 0% / NO-GO |
| Proyecto usable end-to-end | 24-34% |

## Next Macro-Block

`NODAL_OS_REDACTION_BEFORE_PERSISTENCE_SERVICE_TEST_ONLY_EXTERNAL_AUDIT_AND_FIXES`

Allowed next: read-only external audit and test-only/docs-only fixes. Runtime/product wiring requires a new manual GO.
