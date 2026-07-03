# NODAL OS - Redaction Before Persistence Service Test-Only External Audit And Fixes

Decision: `GO_WITH_FINDINGS_REDACTION_BEFORE_PERSISTENCE_TEST_ONLY_EXTERNAL_AUDIT_READY`

Date: 2026-07-03

## Scope

External-audit style review and controlled test-only fixes for the redaction-before-persistence service. This block does not enable runtime product behavior, productive DI/service registration, command handlers, UI product actions, product ledger paths, DB/migration/provider/cloud/network behavior, Browser/CDP/WCU/OCR/Recipes live paths or release/commercial readiness.

## Repo Guard

| Field | Value |
| --- | --- |
| Repo | `C:/DESARROLLO/NodalOS/Codigo-m12-audit` |
| Branch | `chrome-lab-001-extension-local-ai-bridge` |
| Initial HEAD | `ce27d6775dad77a1bd93a47bcb76ec6dc8b64b3f` |
| Worktree initial | `clean` |
| Origin sync initial | `0 0` |
| Stash | listed only, not touched: `stash@{0}: On chrome-lab-001-extension-local-ai-bridge: pre-m11-legacy-state` |

## Audit Findings And Fixes

| Finding | Severity before fix | Fix |
| --- | --- | --- |
| Manually constructed `RedactionBeforePersistenceResult` could be internally incoherent while carrying a matching candidate hash. | P2 for test-only hardening | Stage 2 now validates non-null reasons/evidence, evidence decision, policy id/version, non-blank hash and safe-request hash equality. |
| Secret detector missed obvious whitespace assignment variants. | P3 | Added regex for `password/token/secret/api key` with `:` or `=` and optional whitespace. |
| UNC path detector missed leading whitespace before `\\`. | P3 | Trim leading whitespace for UNC detection. |
| Tests lacked tampered safe-request/null evidence coverage. | P3 | Added Safety coverage for tampered safe request, missing hash, null reasons and null evidence. |
| Corpus lacked uppercase email and leading-space UNC variants. | P4 | Added variants to Safety corpus. |

## Files Changed

| File | Purpose |
| --- | --- |
| `src/OneBrain.Core/Approval/RedactionBeforePersistenceService.cs` | Harden secret/path detection. |
| `src/OneBrain.Core/Approval/DurableAuditTrailAppendOnlyMinimal.cs` | Harden Stage 2 redaction evidence validation. |
| `tests/OneBrain.Safety.Tests/RedactionBeforePersistenceServiceSafetyTests.cs` | Add casing/whitespace/UNC corpus variants. |
| `tests/OneBrain.Safety.Tests/DurableAuditTrailAppendOnlyMinimalSafetyTests.cs` | Add malformed/tampered redaction result tests. |
| Docs/QA/handoff/decision-log | Audit/fix closeout packet. |

## Validations

| Validation | Result |
| --- | --- |
| Repo guard | PASS |
| Core build | PASS, 0 warnings, 0 errors |
| Safety focused tests | PASS, 33/33 |
| Recipes focused tests | PASS, 6/6 |
| Full solution build | PASS, 0 warnings, 0 errors |
| `git diff --check` | PASS |
| JSON validation | PASS |
| Static scan changed files | PASS; no TRUE_RISK |

Safety/Recipes test project builds still emit pre-existing unrelated warnings in broader legacy files when those projects compile; changed Core files compile warning-free.

## Findings

| Severity | Count | Details |
| --- | ---: | --- |
| P0 | 0 | No runtime/product/live authority or scope leak. |
| P1 | 0 | No product enablement, DI registration, command handler, UI product action, product ledger path or release/commercial claim. |
| P2 | 0 | Evidence-coherence gap closed for test-only scope. |
| P3 | 3 | (1) Corpus remains focused and should continue expanding. (2) Nested metadata remains future work because current durable request metadata is flat. (3) Product/runtime adoption remains blocked by external audit and explicit manual GO. |
| P4 | 1 | Historical design/implementation docs remain traceability records. |

## Percentages

| Track | Conservative status |
| --- | --- |
| Redaction-before-persistence test-only service | 86-91% |
| Redaction-before-persistence product service | 0% / NO-GO |
| Durable Stage 2 test-only implementation | 91-95% |
| Runtime/live product enablement | 0% / NO-GO |
| Release/commercial readiness | 0% / NO-GO |
| Proyecto usable end-to-end | 24-34% |

## Next Macro-Block

`NODAL_OS_REDACTION_BEFORE_PERSISTENCE_SERVICE_PROPERTY_CORPUS_EXPANSION_TEST_ONLY`

Allowed next: test-only corpus/property expansion and docs-only audit. Runtime/product wiring requires a new manual GO.
