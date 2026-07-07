# Block A Compaction Scorecard

Date: 2026-07-07

Baseline audit: `docs/audit/nodal-os-full-system-cloud-editorial-bloat-architecture-audit/report.md`

## Important Limitation

Block A indexed and clarified. It did not delete, move, refactor or reduce source files. Scores therefore improve only modestly; the real bloat remains until archive, naming, test-tiering and contract-merge blocks run.

## Before / After Estimate

| Metric | Before | After Block A estimate | Note |
| --- | ---: | ---: | --- |
| BLOAT_SCORE | 78 | 74 | Canonical docs reduce navigation bloat, not source bloat. |
| SIMPLIFICATION_URGENCY | 80 | 78 | Still urgent; now better scoped. |
| PRODUCT_CORE_HEALTH | 62 | 62 | No product code changed. |
| SECURITY_CORE_HEALTH | 82 | 83 | `/run` claim scope is clearer. |
| DOC_NOISE | 85 | 72 | Indexes/logs reduce current-state ambiguity; no archive yet. |
| TEST_NOISE | 68 | 68 | No test tiering yet. |
| Maintainability | 32 | 36 | Slight improvement from canonical architecture/backlog docs. |
| Product readiness | 30 | 30 | No feature change. |
| Onboarding clarity | 20 | 35 | Current architecture, ADR index and logs give a path through the corpus. |
| Release readiness | 0 | 0 | No release/commercial change. |

## New Canonical Documents

- `docs/architecture/nodal-os-current-local-internal-architecture.md`
- `docs/architecture/nodal-os-documentation-inventory-and-compaction-map.md`
- `docs/architecture/nodal-os-documentation-governance.md`
- `docs/architecture/nodal-os-simplification-backlog.md`
- `docs/adr/ADR_CANONICAL_INDEX.md`
- `docs/qa/qa-log.md`
- `docs/handoff/handoff-log.md`
- `docs/audit/nodal-os-run-claim-coherence-reconciliation.md`

## Remaining Work

- Physical archive/index moves.
- Naming consolidation design.
- Test tiering/static scanner consolidation.
- Model/contract merge design.
- Source refactor only after explicit GO.
