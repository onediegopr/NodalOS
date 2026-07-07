# NODAL OS Simplification Backlog

Date: 2026-07-07

This backlog follows the full-system bloat audit and Block A documentation compaction. Source changes require separate explicit GO.

## BLOCK B - Naming Consolidation Design-Only

- Objective: map verbose status-suffix names to domain nouns plus status/claim fields.
- Status: completed as design-only in `docs/architecture/nodal-os-naming-consolidation-design.md` and `docs/architecture/nodal-os-naming-consolidation-map.csv`.
- Expected files: design doc and rename table.
- Do not touch: source names, runtime routes, behavior.
- Tests required: docs-only diff guard, `git diff --check`; build/test evidence may be carried by the block run.
- Risk: low.
- Benefit: reduces onboarding friction before source refactor.
- Follow-up: no source rename until Blocks C-D define test tiering/static scans and contract merge design.

## BLOCK C - Test Tiering / Static Scan Consolidation

- Objective: split required-smoke vs extended suites and centralize negative static scans.
- Status: completed as design-only in `docs/architecture/nodal-os-test-tiering-and-static-scan-consolidation-design.md` and `docs/architecture/nodal-os-test-tiering-map.csv`.
- Expected files: test governance doc, category matrix, scanner design.
- Do not touch: source behavior, unique load-bearing tests.
- Tests required: Safety build, Product Ledger Safety, Product Ledger Recipes.
- Risk: medium; coverage loss if done carelessly.
- Benefit: lower `TEST_NOISE`, faster feedback, less duplicated Safety/Recipes assertions.
- Follow-up: no test deletion, assertion rewrite or static scan implementation change until a separate GO proves coverage equivalence.

## BLOCK D - Model / Contract Merge Design-Only

- Objective: design shared `LocalOnlyResult<T>`, `BoundaryClaims`, shared blockers and unified request/options/validation patterns.
- Status: completed as design-only in `docs/architecture/nodal-os-model-contract-merge-design.md` and `docs/architecture/nodal-os-model-contract-merge-map.csv`.
- Expected files: ADR/design doc and merge map.
- Do not touch: source contracts yet.
- Tests required: design guard only.
- Risk: medium.
- Benefit: prepares removal of contract explosion.
- Follow-up: no common contract implementation, class rename or contract deletion until a separate GO runs Tier 1 plus focused Product Ledger Safety/Recipes.

## BLOCK E - Source Refactor Implementation (Future GO Only)

- Objective: behavior-preserving source merge after Blocks B-D.
- Expected files: source under Core/Approval and tests.
- Do not touch: runtime/product enablement, public/product routes, latest pointer, active read precedence unless separately authorized.
- Tests required: full Product Ledger Safety/Recipes, core/pilot/solution build, static scans.
- Risk: high.
- Benefit: major maintainability improvement.

## BLOCK F - Product Surface Simplification

- Objective: one dev-gated operator route/read model; hide diagnostics under Advanced diagnostics.
- Expected files: route/surface docs first, source only after GO.
- Do not touch: public/product exposure, Production route.
- Tests required: route smoke, DOM contract, Product Ledger Recipes.
- Risk: medium.
- Benefit: higher product clarity.

## BLOCK G - Final External Audit

- Objective: re-run editorial/bloat and claim-coherence audit after compaction/refactor.
- Expected files: audit report and scorecard.
- Do not touch: source.
- Tests required: read-only checks plus prior suite evidence.
- Risk: low.
- Benefit: verifies bloat reduction and claim correctness.

## Explicit Future Merge Targets

- Minimal/Candidate durable audit trail concepts into one evidence model.
- Snapshot/manifest/reader/auxiliary evidence into `LatestStateEvidence` with `role`.
- Writer variants into `WriterMode`.
- Per-node request/result/options/validation into a unified local-only result model.
- Safety/Recipes mirror tests into tiered required/extended suites.
- Negative scanners into one central static scanner.
