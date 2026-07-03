# NODAL OS - Redaction Before Persistence Service Design Only

Decision: `GO_WITH_FINDINGS_REDACTION_BEFORE_PERSISTENCE_SERVICE_DESIGN_ONLY_READY`

Date: 2026-07-03

## Scope

This macro-block creates a docs-only design for a future redaction-before-persistence service. It does not implement Stage 2, enable runtime/live/product behavior, register a service, add command handlers, add UI product actions, create product ledger paths, add DB/migration/provider/cloud/network behavior, connect Browser/CDP/WCU/OCR/Recipes live paths, or claim release/commercial readiness.

## Repo Guard

| Field | Value |
| --- | --- |
| Repo | `C:/DESARROLLO/NodalOS/Codigo-m12-audit` |
| Branch | `chrome-lab-001-extension-local-ai-bridge` |
| Initial HEAD | `9f57da54f9a0975c7fdaf6fdfdccbf0e2ad2d3f7` |
| Expected HEAD | `9f57da54f9a0975c7fdaf6fdfdccbf0e2ad2d3f7` |
| Worktree initial | `clean` |
| Origin sync initial | `0 0` |
| Stash | listed only, not touched: `stash@{0}: On chrome-lab-001-extension-local-ai-bridge: pre-m11-legacy-state` |

## Evidence Audited

| Source | Result |
| --- | --- |
| Post Stage 2 global audit | Selected this macro-block as primary next scope after `GO_WITH_FINDINGS_POST_STAGE2_GLOBAL_AUDIT_NEXT_SCOPE_READY`. |
| Durable Stage 2 closeout | Confirms Stage 2 test-only gates are ready for local/temp scope only; product redaction service remains `0% / NO-GO`. |
| Durable Stage 2 planning gate | Confirms redaction-before-persistence was a required blocker before broader implementation. |
| `src/OneBrain.Core/Approval/DurableAuditTrailAppendOnlyMinimal.cs` | Current code has caller-attested `DurableAuditTrailAppendOnlyMinimalRedactionProof`, fail-closed Stage 2 test-only gate and pre-persistence sensitive-data rejection. |
| Safety/Recipes tests | Current tests prove test-only rejection for missing proof, product-like flags, product ledger path fragments, secret-like, email-like and path-like data. |
| Decision-log canon | Runtime/live/product enablement, redaction runtime, product ledger paths and release/commercial readiness remain NO-GO. |

## Design Outcome

The ADR defines a future conceptual `NodalOsRedactionBeforePersistenceService` with:

- explicit inputs and outputs;
- reject-versus-redact policy;
- metadata/reference/nested-value handling;
- required ordering before append;
- failure modes;
- future integration boundaries;
- anti-capabilities;
- required future test matrix.

## Boundary Confirmation

| Boundary | Status |
| --- | --- |
| Stage 2 implementation | NOT_CHANGED |
| Runtime/live/product | NO-GO_PRESERVED |
| Service registration | ABSENT |
| Command handlers / command bus | ABSENT |
| UI product actions | ABSENT |
| Product ledger path | PROHIBITED |
| DB/migration/provider/cloud/network | ABSENT |
| Browser/CDP/WCU/OCR/Recipes live | ABSENT |
| Product redaction service | DESIGN_ONLY_0_PERCENT_IMPLEMENTED |
| Release/commercial | NO-GO |

## Findings

| Severity | Count | Details |
| --- | ---: | --- |
| P0 | 0 | No runtime/product/live authority or scope leak. |
| P1 | 0 | No implementation, service registration, product ledger path, release/commercial claim or product enablement. |
| P2 | 0 | No blocker for the authorized docs-only design scope. Product/runtime implementation remains blocked by required external audit and manual GO. |
| P3 | 3 | (1) Service is design-only; current Stage 2 still relies on caller-attested test-only proof. (2) Future classifier/redactor corpus must be expanded before implementation. (3) Ordering, evidence leakage and error-message leakage remain future implementation risks. |
| P4 | 1 | Historical redaction/privacy docs remain traceability records under current decision-log canon. |

## Corrections Applied

Docs-only:

- added `docs/adr/redaction-before-persistence-service-design-only.md`;
- added this QA report;
- added `report.json`;
- added handoff;
- updated `docs/decision-log.md`.

No source code, tests, runtime files, service registration, product ledger path, DB/migration, provider/cloud/network, Browser/CDP/WCU/OCR/Recipes live path or stash state was changed.

## Validation Plan And Result

| Validation | Result |
| --- | --- |
| Repo guard | PASS |
| `git diff --check` | PASS |
| JSON validation | PASS |
| Static scan of changed files | PASS; all sensitive/product/runtime hits are design-only, prohibited-boundary or NO-GO wording |
| Build/tests | NOT_RUN_BY_DESIGN; docs-only macro-block |
| Final git status | CLEAN_AFTER_COMMIT_EXPECTED |

## Percentages

| Track | Conservative status |
| --- | --- |
| Redaction-before-persistence service design | 72-78% design-only |
| Product redaction service implementation | 0% / NO-GO |
| Durable Stage 2 test-only implementation | 88-92% |
| Runtime/live product enablement | 0% / NO-GO |
| Release/commercial readiness | 0% / NO-GO |
| Proyecto usable end-to-end | 24-34% |

## Next Macro-Block

Recommended next design/read-only block:

`NODAL_OS_REDACTION_BEFORE_PERSISTENCE_SERVICE_DESIGN_EXTERNAL_AUDIT_READ_ONLY`

It may audit this ADR, QA report, JSON, handoff and decision-log entry without implementation. The next implementation step still requires explicit manual GO and must remain paused until authorized.
