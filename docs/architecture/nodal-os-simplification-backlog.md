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

## BLOCK E - Source Refactor Readiness Audit Design-Only

- Objective: decide whether the first implementation block should be static guard centralization, common contracts in parallel or a low-risk source adapter.
- Status: completed as design-only in `docs/architecture/nodal-os-source-refactor-readiness-audit.md`.
- Decision: `GO_WITH_FINDINGS_SOURCE_REFACTOR_READINESS_DESIGN_READY`.
- Recommendation: run `NODAL_OS_BLOCK_C1_STATIC_GUARD_CATALOG_TEST_ONLY_IMPLEMENTATION` before any source contract refactor.
- Do not touch: `src/`, tests, scanner behavior, class/file names, runtime/product features.
- Tests required: docs-only diff guard plus requested build/test evidence.
- Risk: low for this audit; medium for future C1 false positives; high for source refactor if C1 is skipped.
- Benefit: chooses the first safe implementation block and defines the pre-refactor gate.

## BLOCK C1 - Static Guard Catalog Test-Only Implementation (Future GO Only)

- Objective: add central `NodalOsStaticGuardCatalog` test-only helper and migrate 1-2 duplicated scans as proof.
- Status: completed as partial test-only implementation; the catalog exists under Safety tests and only 1-2 low-risk public/product plus Production route scans were delegated.
- Expected files: tests/test helper and selected static scan tests only.
- Do not touch: production source behavior, runtime/product enablement, public/product routes, Production routes, active read precedence, latest pointer, product authority.
- Tests required: Tier 1 static guards, Product Ledger Safety, Product Ledger Recipes/focused fallback, Core/Pilot/Solution builds and `git diff --check`.
- Risk: medium due false positives or accidental assertion weakening.
- Benefit: creates the safety net required before source contract work.

## BLOCK C2 - Static Guard Catalog Equivalence Expansion (Future GO Only)

- Objective: migrate additional duplicated static scans only after C1 evidence stays green.
- Status: completed as partial test-only equivalence expansion; five additional source-scan checks now delegate to `NodalOsStaticGuardCatalog` while retaining their old local assertions.
- Expected files: selected tests only; no production source behavior.
- Do not touch: assertions that are the only coverage for a boundary, suite membership, runtime/product behavior or scanner hard-fail semantics.
- Tests required: catalog-focused tests, Product Ledger Safety/Recipes and static no-enable scans.
- Risk: medium; category mismatch could hide forbidden tokens.
- Benefit: reduces duplicated forbidden-token arrays without coverage loss.
- Follow-up: keep future migrations small and require old assertion samples, allowed negative wording and source/docs scope evidence for every migrated family.

## BLOCK C3 - Test Tier Labels / Gate Documentation (Future GO Only)

- Objective: document or tag Tier 1/Tier 2/Tier 3 without moving tests to extended yet.
- Status: completed as docs/design-only in `docs/architecture/nodal-os-test-tier-labels-and-gate-policy.md`.
- Expected files: docs and possibly test trait metadata under a dedicated GO.
- Do not touch: CI behavior or assertion strength without separate authorization.
- Tests required: docs/static diff guard and any affected focused tests.
- Risk: low/medium; labels can be confused with actual CI behavior.
- Benefit: makes source-refactor gates easier to run consistently.

## BLOCK C4 - Test Labels Metadata Implementation (Future GO Only)

- Objective: add additive labels/traits metadata to a tiny subset as proof.
- Status: completed as partial test-only metadata implementation. Initial labels cover the static guard catalog class/tests and two Product Ledger public/product plus Production route blocker methods only.
- Expected files: selected tests and docs/log updates only.
- Do not touch: CI filters, test movement, test deletion, skip behavior, assertion strength, scanner behavior or source behavior.
- Tests required: metadata discovery/listing, focused labeled tests, Product Ledger Safety/Recipes, Core/Pilot/Solution builds and `git diff --check`.
- Risk: medium; metadata can be mistaken for permission to move or skip tests.
- Benefit: makes the Tier 1 hard gate discoverable while preserving current coverage.

## BLOCK C5 - Pre-Refactor Gate Commands / Discovery Design (Future GO Only)

- Objective: document exact pre-refactor gate commands, label discovery commands and expected outputs without changing CI.
- Status: completed as docs/design-only in `docs/architecture/nodal-os-pre-refactor-gate-commands-and-discovery.md`.
- Expected files: docs-only command policy and possibly a future dry-run evidence note.
- Do not touch: CI behavior, source behavior, test movement, test deletion, skip behavior, assertions or scanner behavior.
- Tests required: docs/static diff guard; optional command dry-run evidence if requested.
- Risk: low/medium; documented commands can be misread as enforced CI.
- Benefit: makes Tier 1 reproducible before D1/D2 source work.

## BLOCK C6 - Tier 1 Label Expansion Test-Only (Future GO Only)

- Objective: expand additive labels to a few more load-bearing hard-block tests.
- Status: completed as controlled test-only/metadata-only expansion. C6 labels 15 additional existing hard-block methods plus one reflection evidence method, without assertion, scanner, CI or source changes.
- Expected files: selected tests and docs/log updates only.
- Do not touch: CI behavior, source behavior, test movement, test deletion, skip behavior, assertions or scanner behavior.
- Tests required: labeled discovery, Product Ledger Safety/Recipes, static guard focused tests, Core/Pilot/Solution builds and `git diff --check`.
- Risk: medium; expanded labels can be mistaken for a complete Tier 1 suite.
- Benefit: improves pre-refactor gate discoverability without changing execution behavior.
- Follow-up: do not treat the C6 labeled set as a full replacement for Product Ledger Safety/Recipes. Use it as a pre-refactor smoke plus full focused Product Ledger gates until D1/D2 equivalence is proven.

## BLOCK D1 - Common Contracts Parallel Implementation (Future GO Only)

- Objective: add shared contracts such as `LocalOnlyResult<T>`, `BoundaryClaims`, blockers, `WriterMode`, `EvidenceRole` and `GuardEvaluationResult` without runtime use.
- Status: completed as design/test-only and parallel-only. The D1 candidate contracts live under Safety tests as `NodalOsCommonContractsDesignOnlyCandidate` and are not in `src/`, not wired, not registered and not product authority.
- Expected files: new shared contracts plus invariant tests.
- Do not touch: existing behavior, redaction service, path validators, hash/checkpoint kernel, route behavior or product/public gates.
- Tests required: Tier 1 plus contract invariants and central static guard evidence.
- Risk: medium/high if common contracts create double truth.
- Benefit: prepares later D2 low-risk adapter migration.
- Follow-up: D2 should add mapping adapters/equivalence tests in parallel only. Do not replace old contracts, move tests, weaken assertions or wire runtime/product behavior.

## BLOCK D2 - Mapping Adapters Equivalence Expansion Test-Only (Future GO Only)

- Objective: prove selected existing hard-block concepts map into D1 common-contract candidates without creating production authority or double-truth.
- Status: completed as test-only/design-only/parallel-only. The mapper lives under Safety tests as `NodalOsCommonBoundaryMappingDesignOnlyAdapter`.
- Expected files: test-only mapper, equivalence tests and docs/log updates.
- Do not touch: `src/`, existing contracts, runtime behavior, route behavior, DI, command handlers, CI, public/product gates, assertions, test movement or test deletion.
- Tests required: Tier 1, CommonContracts, DesignOnly, MappingAdapters, static guard focused tests, Product Ledger Safety/Recipes and `git diff --check`.
- Risk: medium/high if a future block treats mapper output as source of truth instead of translation evidence.
- Benefit: proves D1 candidate can represent current hard-block semantics and unknown states fail closed before any source-facing refactor.
- Follow-up: D3 should be source-refactor plan/audit only, or a minimal parallel source candidate behind a separate no-runtime/no-wiring guard. No real source adapter use without explicit GO.

## BLOCK D3 - Source Refactor Plan Audit Only

- Objective: choose the safest first source-facing simplification move using D1/D2 evidence before touching `src/`.
- Status: completed as docs/audit-only in `docs/architecture/nodal-os-d3-source-refactor-plan-audit.md`.
- Decision: recommend `AUTHORIZE_NODAL_OS_BLOCK_D4_MINIMAL_SOURCE_CANDIDATE_NO_RUNTIME_WIRING`.
- Selected D4 candidate: one isolated source-side common boundary-claims candidate, non-wired, no consumers, no runtime/product authority.
- Expected files: docs/log updates only in D3.
- Do not touch: `src/`, tests, CI, runtime behavior, existing contracts, route behavior, DI, command handlers or public/product gates.
- Tests required: C5/D2 gate evidence; docs-only `git diff --check`.
- Risk: low for D3; P3 for D4 if the source candidate is mistaken for authority.
- Benefit: prevents the first source-facing move from jumping straight into route/latest-state/handoff/writer refactors.
- Follow-up: D4 requires explicit Diego authorization. D4 must be one-file source-minimal and protected by no-runtime/no-reference tests.

## BLOCK D4 - Minimal Source Candidate No Runtime Wiring (Future GO Only)

- Objective: add one isolated source-side common boundary-claims candidate with no runtime/product wiring.
- Status: future GO only.
- Expected files: one new Core/Approval source candidate plus focused Safety tests and docs/log updates.
- Do not touch: existing Product Ledger behavior files, Pilot routes, DI, command handlers, CI, public/product gates, latest pointer/read precedence/product authority, command execution or release/commercial claims.
- Tests required: Core/Pilot/Solution build, Product Ledger Safety/Recipes, Tier 1, CommonContracts, MappingAdapters, static guard, public/product and Production route filters, no-reference source scans and `git diff --check`.
- Risk: medium; source-side names can imply authority if not explicitly marked candidate/non-wired.
- Benefit: gives future source migrations a controlled target without changing behavior.

## BLOCK F - Source Refactor Implementation (Future GO Only)

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
