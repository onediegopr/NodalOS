# NODAL OS — Simplification / Pruning Plan (Design-Only)

Baseline HEAD `9e8a1f35`. All blocks below are proposals. Only Blocks A/B/E are safe to
execute soon (docs/tests). Blocks C/D/F/G require a separate GO because they touch source
packaging. No block may change ledger/redaction/guard **behavior**, activate precedence /
latest pointer / product authority, or open any blocked frontier.

## BLOCK A — Docs compaction only
- Objective: create `docs/architecture/nodal-os-current-local-internal-architecture.md` as the single current-state source of truth; archive per-micro-block ADR/QA/handoff under `docs/archive/`; replace decision-log body with a short head + archive index.
- Risk: Low (docs only).
- Files touched: docs only (`docs/architecture/*`, `docs/archive/*`, `decision-log.md`).
- No-go: do not delete history (move/index), do not alter any claim's meaning.
- Tests required: none (markdown only); `git diff --check`.
- Rollback: revert docs commit.
- Benefit: DOC_NOISE 85→~45; onboarding clarity 20→~50.

## BLOCK B — Naming consolidation (design-only proposal)
- Objective: map verbose status-suffix type names → domain noun + `status` enum. Deliver a rename table (old→new) as a doc; NO renames executed yet.
- Risk: Low (proposal doc).
- Files touched: one design doc.
- No-go: no source rename in this block (renames = Block D/F under GO).
- Tests required: none.
- Rollback: revert doc.
- Benefit: unblocks contract merge; onboarding clarity.

## BLOCK C — Route consolidation (design-only)
- Objective: design one local operator route + one `OperatorSurfaceReadModel`; specify which preview/surface classes fold in.
- Risk: Low (design doc). Real change deferred to Block G.
- No-go: no runtime route change, no product/public route, keep dev-gating.
- Tests required: none (design).
- Benefit: UI clarity path.

## BLOCK D — Model/contract merge (design-only)
- Objective: design `LocalOnlyResult<T>`, shared `BoundaryClaims`, shared `Blocker` taxonomy, and `WriterMode`; show before/after LOC estimate.
- Risk: Medium (touches every node when executed later).
- No-go: design-only; no source edits.
- Tests required: define the required smoke suite that must stay green post-merge.
- Benefit: −several thousand LOC when Block F executes.

## BLOCK E — Test suite pruning / tiering
- Objective: split tests into `required-smoke` (writer round-trip, tamper fail-closed, path-confinement, one static no-enable scan, one concurrency test) vs `extended`. De-duplicate mirrored Safety/Recipes tests.
- Risk: Medium (must not drop real coverage).
- Files touched: test project categorization/traits + a small doc.
- No-go: do not delete a test that is the only cover for a guard; move, don't delete.
- Tests required: full Safety build must stay green.
- Benefit: TEST_NOISE 68→~40; faster feedback.

## BLOCK F — Source refactor implementation (ONLY on future GO)
- Objective: execute Blocks B/D (renames + contract merge + writer→WriterMode) behavior-preserving.
- Risk: High. Requires GO.
- No-go: no behavior change, no new capability, no frontier activation; add the missing writer lock as part of this block.
- Tests required: required-smoke gate + full Safety + focused Recipes green before/after.
- Rollback: branch + revert; behavior-preserving so diff is mechanical.
- Benefit: maintainability 32→~65.

## BLOCK G — Product surface simplification (ONLY on future GO)
- Objective: implement Block C — one dev-gated operator route rendering the merged read-model; hide DesignOnly/AntiCapability machinery behind "Advanced diagnostics".
- Risk: Medium. Requires GO.
- No-go: no public/product exposure, no Production route, dev-gated only.
- Tests required: route smoke test.
- Benefit: product clarity 35→~60.

## BLOCK H — Final post-prune audit
- Objective: re-run this editorial audit; confirm bloat/doc/test scores improved and behavior unchanged.
- Risk: Low (read-only).
- Benefit: verifies pruning didn't break security or claims.

## Recommended order
A → B → E → (GO) → D(design) → F → C(design) → G → H.
Start with A+B+E (safe). Everything touching source waits for an explicit GO.
