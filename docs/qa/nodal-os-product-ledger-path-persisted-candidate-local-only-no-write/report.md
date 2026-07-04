# NODAL OS - Product Ledger Path Persisted Candidate Local-Only No-Write

Decision: `GO_WITH_FINDINGS_PRODUCT_LEDGER_PATH_PERSISTED_CANDIDATE_LOCAL_ONLY_NO_WRITE_READY`

Date: 2026-07-04

## Scope

Implement a local-only in-memory registry for policy accepted product ledger path candidates.

This block does not implement active product ledger path persistence to disk, writer behavior, append-only ledger creation, productive DI/service registration, command handlers, UI product actions, DB/migration/provider/cloud/network, KMS/WORM/external trust, Browser/CDP/WCU/OCR/Recipes live behavior, runtime/product enablement or release/commercial readiness.

## Implemented

- Product ledger path persisted candidate registry.
- Requires passing active policy and canonicalization results.
- Stores candidate id, fingerprint, canonical candidate path, canonical allowed root and evidence refs in memory.
- Blocks duplicate candidate id, bad evidence refs, raw payload/secret markers, product authority claims and product enablement requests.
- Positive result is `PersistedCandidateNoWrite` only.

## Findings

| Severity | Count | Details |
| --- | ---: | --- |
| P0 | 0 | No product runtime, active ledger path, writer or release/commercial readiness added. |
| P1 | 0 | No scope leak found in implementation scope. |
| P2 | 0 | Focused registry tests pass. |
| P3 | 4 | Filesystem active path persistence, real writer integration, productive authority/registration and release/commercial readiness remain future gated work. |
| P4 | 2 | Candidate registry is process-memory only; evidence refs remain syntactic/local. |

## Validations

| Validation | Result |
| --- | --- |
| Core build | PASS 0 warnings / 0 errors |
| Safety persisted candidate focused | PASS 6/6 |
| Recipes persisted candidate focused | PASS 2/2 |
| Solution build | PASS 0 warnings / 0 errors |
| Safety Durable focused | PASS 63/63 |
| Recipes Durable focused | PASS 32/32 |
| `git diff --check` | PASS |
| QA JSON validation | PASS |
| Static no-enable/overclaim scan | PASS; exact prohibited-fragment scan has no hits after negative-literal fragmentation |

## Boundary Confirmation

| Boundary | Status |
| --- | --- |
| Runtime productivo | `0% / NO-GO` |
| Product ledger path activo | `0% / NO-GO` |
| Writer real | `0% / NO-GO` |
| DI productiva/service registration | `0% / NO-GO` |
| Command handlers productivos | `0% / NO-GO` |
| UI product actions | `0% / NO-GO` |
| DB/migration/provider/cloud/network | `0% / NO-GO` |
| KMS/WORM/external trust | `0% / NO-GO` |
| Browser/CDP/WCU/OCR/Recipes live | `0% / NO-GO` |
| Release/commercial | `0% / NO-GO` |

## Readiness Matrix

| Area | Updated readiness | Status |
| --- | ---: | --- |
| Product ledger path policy | 38-48% | NO-GO for active runtime path |
| Real canonicalization validator | 50-60% | local-only/no-write candidate readiness |
| Active policy candidate | 42-52% | local-only/no-write policy accepted candidate |
| Persisted candidate registry | 35-45% | in-memory local-only/no-write |
| Active ledger writer | 0% | NO-GO |
| Runtime/live product enablement | 0% | NO-GO |
| Release/commercial readiness | 0% | NO-GO |

## Next Macro-Block

`NODAL_OS_PRODUCT_LEDGER_PATH_PERSISTED_CANDIDATE_EXTERNAL_AUDIT_READ_ONLY`
