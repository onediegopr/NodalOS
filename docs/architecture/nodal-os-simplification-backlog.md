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
- Status: completed as source-minimal/parallel-only/no-runtime-wiring in `NODAL_OS_BLOCK_D4_MINIMAL_SOURCE_CANDIDATE_NO_RUNTIME_WIRING`.
- Actual source artifact: `src/OneBrain.Core/Approval/NodalOsCommonBoundaryClaimsCandidate.cs`.
- Actual tests: `tests/OneBrain.Safety.Tests/NodalOsCommonBoundaryClaimsCandidateTests.cs`.
- Expected files: one new Core/Approval source candidate plus focused Safety tests and docs/log updates.
- Do not touch: existing Product Ledger behavior files, Pilot routes, DI, command handlers, CI, public/product gates, latest pointer/read precedence/product authority, command execution or release/commercial claims.
- Tests required: Core/Pilot/Solution build, Product Ledger Safety/Recipes, Tier 1, CommonContracts, MappingAdapters, static guard, public/product and Production route filters, no-reference source scans and `git diff --check`.
- Risk: medium; source-side names can imply authority if not explicitly marked candidate/non-wired.
- Benefit: gives future source migrations a controlled target without changing behavior.
- Follow-up: D5 should be equivalence hardening/no-runtime reference audit or minimal replacement plan/audit only. Do not start broad source refactor from D4 alone.

## BLOCK D5 - Equivalence Hardening No Runtime Reference Audit

- Objective: harden D1/D2/D4 equivalence, no-runtime-reference scans and non-authority/no-double-truth guarantees before any replacement plan.
- Status: completed as test/audit/docs-only in `NODAL_OS_BLOCK_D5_EQUIVALENCE_HARDENING_NO_RUNTIME_REFERENCE_AUDIT`.
- Actual tests: `tests/OneBrain.Safety.Tests/NodalOsCommonBoundaryClaimsCandidateIsolationHardeningTests.cs`.
- Do not touch: `src/`, D4 candidate source, new source candidates, existing Product Ledger behavior files, Pilot routes, DI, command handlers, CI, public/product gates, latest pointer/read precedence/product authority, command execution or release/commercial claims.
- Tests required: Core/Pilot/Solution build, Product Ledger Safety/Recipes, Tier 1, CommonContracts, DesignOnly, MappingAdapters, SourceCandidate, NoRuntimeWiring, NoAuthority, NoDoubleTruth, static guard, public/product and Production route filters, no-reference source scans and `git diff --check`.
- Risk: low/medium; no-authority labels can be mistaken for source replacement permission unless docs remain explicit.
- Benefit: raises confidence that D4 can remain safely parallel before any future replacement plan.
- Follow-up: D6 minimal replacement plan/audit only or STOP_FOR_AUDIT. No broad refactor from D5 alone.

## BLOCK D6 - Minimal Replacement Plan Audit Only

- Objective: inventory possible first replacement targets related to the D4 candidate and select exactly one future D7 path or stop.
- Status: completed as docs/audit/plan-only in `docs/architecture/nodal-os-d6-minimal-replacement-plan-audit.md`.
- Selected future D7: `AUTHORIZE_NODAL_OS_BLOCK_D7_MINIMAL_REPLACEMENT_IMPLEMENTATION_NO_RUNTIME_CHANGE`.
- Selected D7 target: `src/OneBrain.Core/Approval/ReentryDecisionPacketReadOnly.cs`.
- Expected D7 scope: one tiny read-only proof replacement around duplicated common-boundary no-go evidence; no route, DI, command, Product Ledger writer, public/product, DB/provider/cloud/KMS/WORM or release/commercial behavior.
- Do not touch in D7: Product Ledger latest-state/handoff/writer/path/route/UI source behavior, Pilot routes, service registration, command handlers, CI, release/commercial claims or existing hard-block authorities.
- Tests required for D7: Core/Pilot/Solution build, Reentry Safety/Recipes focused tests, D4/D5 focused tests, Tier 1/CommonContracts/DesignOnly/SourceCandidate/NoRuntimeWiring/NoAuthority/NoDoubleTruth, Product Ledger Safety/Recipes and no-reference scans.
- Risk: medium; a single allowed source reference can become broad adoption if the D5 guard is weakened instead of narrowed.
- Benefit: first source-facing replacement path is outside Product Ledger runtime and can be rolled back cleanly.
- Follow-up: D7 requires explicit Diego authorization.

## BLOCK D7 - Minimal Replacement Implementation No Runtime Change

- Objective: implement the one D6-selected replacement target without runtime/product behavior change.
- Status: completed as source-minimal/no-runtime-behavior-change in `NODAL_OS_BLOCK_D7_MINIMAL_REPLACEMENT_IMPLEMENTATION_NO_RUNTIME_CHANGE`.
- Actual source target: `src/OneBrain.Core/Approval/ReentryDecisionPacketReadOnly.cs`.
- Actual tests: `tests/OneBrain.Safety.Tests/ReentryDecisionPacketReadOnlyCommonBoundaryD7Tests.cs` and a narrow D5 allowed-reference guard update.
- Candidate usage: `NodalOsCommonBoundaryClaimsCandidate.DefaultBlocked()` is used only as a private read-only fail-closed proof inside the selected target.
- Do not treat as authority: the D4 candidate remains non-authoritative, D1/D2 remain test/design-only and existing hard-block tests remain authoritative.
- Non-goals preserved: no route/DI/service registration, command handlers, Product Ledger runtime/latest-state/handoff/writer, public/product exposure, Production route, latest pointer/read precedence/product authority, CI change or release/commercial readiness.
- Bloat impact: effectively `0%` net source reduction; additive proof-only.
- Follow-up: D8 post-replacement isolation/equivalence audit or STOP_FOR_AUDIT. No broad refactor from D7 alone.

## BLOCK D8 - Post-Replacement Isolation/Equivalence Audit

- Objective: audit the D7 minimal replacement after the fact and harden isolation/equivalence evidence.
- Status: completed as test/audit/docs-only in `NODAL_OS_BLOCK_D8_POST_REPLACEMENT_ISOLATION_EQUIVALENCE_AUDIT`.
- Actual tests: `tests/OneBrain.Safety.Tests/ReentryDecisionPacketReadOnlyPostReplacementD8Tests.cs`.
- Do not touch: `src/`, D4 candidate source, Reentry source target, new source candidates, Product Ledger behavior files, Pilot routes, DI, command handlers, CI, public/product gates, latest pointer/read precedence/product authority, command execution or release/commercial claims.
- Verified: D7 command guard exception is exact to `ReentryDecisionPacketReadOnly.cs`, does not allow runtime/product command execution, shell/subprocess, route/DI/service registration or similar future files.
- Verified: candidate references remain limited to candidate source, D7 source target, Safety tests and docs/logs.
- Risk: low/medium; post-replacement confidence is higher, but source bloat reduction remains 0% and broad refactor is still not authorized.
- Benefit: makes D7 auditable before any future D9 replacement planning.
- Follow-up: `STOP_FOR_AUDIT` or D9 second minimal replacement plan/audit only. No broad refactor from D8 alone.

## BLOCK D9 - Second Minimal Replacement Plan Audit Only

- Objective: inventory possible second replacement targets after D7/D8 and select exactly one safe future D10 path or stop.
- Status: completed as docs/audit/plan-only in `docs/architecture/nodal-os-d9-second-minimal-replacement-plan-audit.md`.
- Decision: `GO_WITH_FINDINGS_SECOND_MINIMAL_REPLACEMENT_PLAN_AUDIT_ONLY_READY`.
- Selected future D10: `AUTHORIZE_NODAL_OS_BLOCK_D10_SECOND_MINIMAL_REPLACEMENT_IMPLEMENTATION_NO_RUNTIME_CHANGE`.
- Selected D10 target: `src/OneBrain.Core/Approval/ApprovalExecutionDesignOnlyProtected.cs`.
- Rationale: read-only/design-only/preview-only Approval fixture, non-route, non-DI, non-service-registered, non-writer, not Product Ledger runtime/latest-state/handoff-facing and already covered by focused Safety/Recipes tests.
- Expected D10 scope: one tiny proof-only common-boundary replacement in the selected source target; exact source-reference, no-authority, no-double-truth and command-guard exception tests; no route/DI/command/product/runtime behavior.
- Do not touch in D10: D4 candidate source, D7 Reentry source target, Product Ledger source behavior, Pilot routes, service registration, command handlers, CI, DB/provider/cloud/network/KMS/WORM, public/product gates or release/commercial claims.
- Tests required for D10: Core/Pilot/Solution build, Approval execution Safety/Recipes focused tests, D4/D5/D7/D8 focused tests, Tier 1/CommonContracts/SourceCandidate/NoRuntimeWiring/NoAuthority/NoDoubleTruth/PostReplacementAudit, Product Ledger Safety/Recipes and exact no-reference scans.
- Risk: medium; D10 command/execution vocabulary must not broaden scanner exceptions or make the D4 candidate authority.
- Benefit: proves whether the D7 pattern can be repeated once in a second narrow non-Product-Ledger target before any broad source refactor.
- Follow-up: D10 requires explicit Diego authorization.

## BLOCK D10 - Second Minimal Replacement Implementation No Runtime Change

- Objective: implement the D9-selected Approval execution proof-only replacement without runtime/product behavior change.
- Status: completed as source-minimal/proof-only/no-runtime-behavior-change in `NODAL_OS_BLOCK_D10_SECOND_MINIMAL_REPLACEMENT_IMPLEMENTATION_NO_RUNTIME_CHANGE`.
- Decision: `GO_WITH_FINDINGS_SECOND_MINIMAL_REPLACEMENT_NO_RUNTIME_CHANGE_READY`.
- Actual source target: `src/OneBrain.Core/Approval/ApprovalExecutionDesignOnlyProtected.cs`.
- Actual tests: `tests/OneBrain.Safety.Tests/ApprovalExecutionDesignOnlyProtectedCommonBoundaryD10Tests.cs` and exact D4/D5/D7/D8 guard updates.
- Candidate usage: `NodalOsCommonBoundaryClaimsCandidate.DefaultBlocked()` is used only as a private read-only fail-closed proof inside `ApprovalExecutionAntiCapabilityProof.Passes`.
- Do not treat as authority: D4 remains non-authoritative, D1/D2 remain test/design-only and existing hard-block tests remain authoritative.
- Non-goals preserved: no route/DI/service registration, command handlers, Product Ledger runtime/latest-state/handoff/writer, public/product exposure, Production route, latest pointer/read precedence/product authority, CI change, Browser/CDP/WCU/OCR/Recipes live behavior or release/commercial readiness.
- Bloat impact: source bloat reduction remains `0%`; D10 adds net `+70` source lines and cumulative D7+D10 source impact is net `+140` lines.
- Follow-up: D11 post-second-replacement isolation/equivalence audit or STOP_FOR_AUDIT. No broad refactor from D10 alone.

## BLOCK D11 - Post-Second-Replacement Isolation/Equivalence Audit

- Objective: audit the D10 second minimal replacement after the fact and harden isolation/equivalence evidence before any third replacement or broad refactor.
- Status: completed as test/audit/docs-only in `NODAL_OS_BLOCK_D11_POST_SECOND_REPLACEMENT_ISOLATION_EQUIVALENCE_AUDIT`.
- Decision: `GO_WITH_FINDINGS_POST_SECOND_REPLACEMENT_ISOLATION_EQUIVALENCE_AUDIT_READY`.
- Actual tests: `tests/OneBrain.Safety.Tests/ApprovalExecutionPostSecondReplacementD11Tests.cs`.
- Actual audit doc: `docs/architecture/nodal-os-d11-post-second-replacement-isolation-audit.md`.
- Do not touch: `src/`, D4 candidate source, D7 target, D10 target, new source candidates, Product Ledger behavior files, Pilot routes, DI, command handlers, CI, public/product gates, latest pointer/read precedence/product authority, command execution or release/commercial claims.
- Verified: D10 command/execution exception is exact to `ApprovalExecutionDesignOnlyProtected.cs`, D7 and D10 exceptions are independent, and similar future files are not automatically allowed.
- Verified: candidate references remain limited to candidate source, D7 source target, D10 source target, Safety tests and docs/logs.
- Verified: D7+D10 together do not create common authority by accumulation; D1/D2 remain test/design-only and existing hard-block tests remain authoritative.
- Bloat impact: source bloat reduction remains `0%`; cumulative D7+D10 source impact remains net `+140` lines. The D-series has so far proven equivalence/isolation, not reduced source bloat.
- Follow-up: `D12 source-reduction plan/audit only` or STOP_FOR_AUDIT. Do not default to a third proof-only replacement.

## BLOCK D12 - Source Reduction Plan Audit Only

- Objective: decide whether future D13 can safely reduce actual source bloat after D7/D10/D11 without implementing source reduction in D12.
- Status: completed as docs/audit/plan-only in `NODAL_OS_BLOCK_D12_SOURCE_REDUCTION_PLAN_AUDIT_ONLY`.
- Decision: `GO_WITH_FINDINGS_SOURCE_REDUCTION_PLAN_AUDIT_ONLY_READY`.
- Actual audit doc: `docs/architecture/nodal-os-d12-source-reduction-plan-audit.md`.
- Selected future D13 recommendation: `AUTHORIZE_NODAL_OS_BLOCK_D13_MINIMAL_SOURCE_REDUCTION_IMPLEMENTATION_NO_RUNTIME_CHANGE`.
- Selected future D13 target: `src/OneBrain.Core/Approval/ApprovalExecutionDesignOnlyProtected.cs`.
- Expected D13 scope: one-file private helper compaction inside the D10 target; preserve claim/state equivalence, D4 non-authority, D7 isolation and all D10/D11 focused guard meaning.
- Do not touch in D13 unless separately justified by failed guards: D4 candidate source, D7 target source, Product Ledger source behavior, Pilot routes, DI, command handlers, CI, tests or release/commercial claims.
- Tests required for D13: Core/Pilot/Solution build, Product Ledger Safety/Recipes, Tier 1/CommonContracts/SourceCandidate/NoRuntimeWiring/NoAuthority/NoDoubleTruth/PostReplacementAudit/ApprovalExecution, D10/D11 focused tests and exact no-reference scans.
- Risk: medium if D13 broadens source references or turns the D4 candidate into reusable authority; low/medium if it stays one-file and private.
- Benefit: first actual source bloat reduction in the D-series, after equivalence/isolation evidence has been established.
- Follow-up: D13 requires explicit Diego authorization. Do not choose a third proof-only replacement before attempting this one-file reduction.

## BLOCK D13 - Minimal Source Reduction Implementation No Runtime Change

- Objective: implement the D12-selected one-file source reduction while preserving runtime/product behavior.
- Status: completed as source-minimal/reduction-only/no-runtime-behavior-change in `NODAL_OS_BLOCK_D13_MINIMAL_SOURCE_REDUCTION_IMPLEMENTATION_NO_RUNTIME_CHANGE`.
- Decision: `GO_WITH_FINDINGS_MINIMAL_SOURCE_REDUCTION_NO_RUNTIME_CHANGE_READY`.
- Actual source target: `src/OneBrain.Core/Approval/ApprovalExecutionDesignOnlyProtected.cs`.
- Actual audit doc: `docs/architecture/nodal-os-d13-minimal-source-reduction-implementation.md`.
- Source reduction: compacted duplicated D10 private claim/state proof checks into a private expected-claims table and `All(...)` loop.
- Do not treat as broad simplification: D4 candidate source unchanged, D7 target unchanged, Product Ledger source behavior unchanged, tests unchanged and CI unchanged.
- Bloat impact: D13 net `-30` source lines in the D10 target; cumulative D7+D10+D13 source impact remains net `+110` lines.
- Tests required for D14: D10/D11 focused tests, NoRuntimeWiring/NoAuthority/NoDoubleTruth/PostReplacementAudit/ApprovalExecution, Product Ledger Safety/Recipes, exact source-reference scans and D7/D10 command-runtime scans.
- Risk: low/medium after green validation; future reductions should not proceed until D14 audits that this compaction preserved D10/D11 guard meaning.
- Follow-up: D14 post-source-reduction isolation/equivalence audit or STOP_FOR_AUDIT.

## BLOCK D14 - D-Series Value Checkpoint and Post-Reduction Audit

- Objective: audit D13 after implementation and decide whether another D-series reduction target has enough value.
- Status: completed as docs/audit/checkpoint-only in `NODAL_OS_BLOCK_D14_D_SERIES_VALUE_CHECKPOINT_AND_POST_REDUCTION_AUDIT`.
- Decision: `GO_WITH_FINDINGS_D_SERIES_VALUE_CHECKPOINT_READY`.
- Actual checkpoint doc: `docs/architecture/nodal-os-d14-d-series-value-checkpoint.md`.
- Source changed: none.
- Tests changed: none.
- CI changed: none.
- Verified: D13 stayed one-file, D4 remains non-authoritative, D7 remains unchanged, D1/D2 remain test/design-only and existing hard-block tests remain authoritative.
- Bloat impact: D7 `+70`, D10 `+70`, D13 `-30`; cumulative D-series source impact remains net `+110`.
- Deferred: D7 private proof-chain compaction is possible but low-value; broad Product Ledger/model-contract simplification belongs to the main roadmap.
- Follow-up: `CLOSE_D_SERIES_RETURN_TO_MAIN_ROADMAP`.

## BLOCK D15 - Close D-Series And Return To Main Roadmap

- Objective: formally close D-series and choose the next safe main-roadmap lane without implementation.
- Status: completed as docs-only/roadmap-only/decision-log-only in `NODAL_OS_BLOCK_D15_CLOSE_D_SERIES_RETURN_TO_MAIN_ROADMAP_DOCS_ONLY`.
- Decision: `GO_WITH_FINDINGS_D_SERIES_CLOSED_RETURN_TO_MAIN_ROADMAP_READY`.
- Actual closure doc: `docs/architecture/nodal-os-d15-d-series-closure-and-main-roadmap-return.md`.
- Source changed: none.
- Tests changed: none.
- CI changed: none.
- Closure: D-series is closed for now; no further D-series implementation is recommended immediately.
- Achieved: useful safety/refactor confidence, D4 non-authoritative source candidate, D7/D10 narrow proof replacements, D13 local reduction and D14 value checkpoint.
- Not achieved: broad source bloat reduction, broad Product Ledger/model-contract simplification, CI enforcement, runtime/product enablement or release/commercial readiness.
- Bloat impact remains: D7 `+70`, D10 `+70`, D13 `-30`, cumulative `+110`.
- Follow-up: `NODAL_OS_BLOCK_E1_MAIN_ROADMAP_REBASELINE_AFTER_D_SERIES_DOCS_ONLY`. D15 recommends but does not authorize E1.

## BLOCK E1 - Main Roadmap Rebaseline After D-Series

- Objective: rebaseline the main NODAL OS roadmap after D-series closure.
- Status: completed as docs-only/roadmap-only/rebaseline-only in `NODAL_OS_BLOCK_E1_MAIN_ROADMAP_REBASELINE_AFTER_D_SERIES_DOCS_ONLY`.
- Decision: `GO_WITH_FINDINGS_MAIN_ROADMAP_REBASELINE_AFTER_D_SERIES_READY`.
- Actual rebaseline doc: `docs/architecture/nodal-os-e1-main-roadmap-rebaseline-after-d-series.md`.
- Source changed: none.
- Tests changed: none.
- CI changed: none.
- Rebaseline: D-series is closed; Product Ledger local/dev safety backlog is now the highest-value safe next reconciliation lane.
- Current posture: Tier 1 manual/discovery-only, CI enforcement `0%`, runtime/product enablement `0%`, release/commercial `0% / NO-GO`.
- Not authorized: broad source refactor, runtime/product enablement, public/product route, Production route, latest pointer, read precedence, product authority, CI enforcement or release/commercial readiness.
- Follow-up: `NODAL_OS_BLOCK_E2_PRODUCT_LEDGER_LOCAL_DEV_SAFETY_BACKLOG_RECONCILIATION_DOCS_TEST_ONLY`. E1 recommends but does not authorize E2.

## BLOCK E2 - Product Ledger Local/Dev Safety Backlog Reconciliation

- Objective: reconcile distributed Product Ledger local/dev backlog artifacts into one current canon before any new capability, runtime/product or source lane.
- Status: completed as docs-only/backlog-reconciliation-only in `docs/architecture/nodal-os-product-ledger-local-dev-safety-backlog-canon.md`.
- Decision: `GO_WITH_FINDINGS_PRODUCT_LEDGER_LOCAL_DEV_BACKLOG_RECONCILIATION_READY`.
- Source changed: none.
- Tests changed: none; `TEST_CHANGE_NOT_NEEDED` because existing Product Ledger Safety/Recipes, Tier 1, no-runtime, no-authority, no-double-truth and static guards already protect the blocked frontiers documented by E2.
- CI changed: none.
- Reconciled: path/canonicalization, local approval/execution, handoff draft boundaries, latest-state evidence, operator surface/local-dev UI, runtime local-only/internal evidence, public/product blocker maps, redaction/retention, integration/property and broad user-workspace boundary docs.
- Current posture: Product Ledger local/dev is evidence only; public/product, Production route, latest pointer, read precedence, product authority, runtime/product enablement, CI enforcement and release/commercial remain blocked or `0%`.
- Follow-up: `NODAL_OS_BLOCK_E3_PRODUCT_LEDGER_LOCAL_DEV_NEXT_ACTION_PLAN_DOCS_ONLY`.

## BLOCK E3 - Product Ledger Local/Dev Next Action Plan

- Objective: choose the next safe Product Ledger local/dev action after E2 canonization without implementing runtime/product behavior.
- Status: completed as docs-only/plan-only/roadmap-only in `docs/architecture/nodal-os-product-ledger-local-dev-next-action-plan.md`.
- Decision: `GO_WITH_FINDINGS_PRODUCT_LEDGER_LOCAL_DEV_NEXT_ACTION_PLAN_READY`.
- Source changed: none.
- Tests changed: none.
- CI changed: none.
- Selected next block: `NODAL_OS_BLOCK_E4_PRODUCT_LEDGER_LOCAL_DEV_STALE_DOC_CROSSLINK_CLEANUP_DOCS_ONLY`.
- Rationale: stale Product Ledger QA/handoff/roadmap entrypoints remain the biggest practical drift risk after E2; cross-link cleanup should happen before canon guard tests or external audit packets.
- Current posture: Product Ledger local/dev remains evidence only; runtime/product enablement, CI enforcement and release/commercial remain `0%`; public/product, Production route, latest pointer, read precedence and product authority remain blocked.
- Follow-up: E3 recommends but does not authorize E4.

## BLOCK E4 - Product Ledger Local/Dev Stale Doc Cross-Link Cleanup

- Objective: add concise canon/current-status notices to stale or high-risk Product Ledger local/dev docs without deleting historical evidence.
- Status: completed as docs-only/crosslink-only/no-runtime-change in `docs/architecture/nodal-os-product-ledger-local-dev-stale-entrypoint-crosslink-index.md`.
- Decision: `GO_WITH_FINDINGS_PRODUCT_LEDGER_LOCAL_DEV_STALE_DOC_CROSSLINK_READY`.
- Source changed: none.
- Tests changed: none.
- CI changed: none.
- Cross-linked: selected high-risk QA, handoff and roadmap entrypoints for active writer, runtime local-only internal enablement, public UI/actions, public surface readiness, public local-only operator acceptance, latest pointer/read precedence/product exposure decision matrices and the historical path threat model.
- Current authority: E2 canon plus E3 plan; old docs remain preserved as block-specific evidence and should not be read as current product readiness.
- Current posture: Product Ledger local/dev remains evidence only; runtime/product enablement, CI enforcement and release/commercial remain `0%`; public/product, Production route, latest pointer, read precedence and product authority remain blocked.
- Follow-up: `NODAL_OS_BLOCK_E5_PRODUCT_LEDGER_LOCAL_DEV_CANON_GUARD_TEST_ONLY`. E4 recommends but does not authorize E5.

## BLOCK E5 - Product Ledger Local/Dev Canon Guard

- Objective: add a focused test-only guard that protects the E2 Product Ledger local/dev canon, E3 plan and E4 stale-entrypoint cross-link index from future overclaim drift.
- Status: completed as test-only/docs-only/no-runtime-change in `tests/OneBrain.Safety.Tests/ProductLedgerLocalDevCanonGuardTests.cs`.
- Decision: `GO_WITH_FINDINGS_PRODUCT_LEDGER_LOCAL_DEV_CANON_GUARD_READY`.
- Source changed: none under `src/`.
- Tests changed: one new Safety test class; no tests moved, deleted or weakened.
- CI changed: none; Tier 1 remains manual/discovery-only and CI enforcement remains `0%`.
- Guarded posture: Product Ledger local/dev remains evidence-only; runtime/product enablement, public/product, Production route, latest pointer, read precedence, product authority, DB/provider/cloud/network/KMS/WORM and release/commercial remain blocked or `0% / NO-GO`.
- Current authority: E2 canon plus E3 plan, with E4 high-risk historical entrypoint links.
- Next recommended macro-block: `NODAL_OS_BLOCK_E6_EXTERNAL_AUDIT_PACKET_PRODUCT_LEDGER_LOCAL_DEV_READ_ONLY`.
- Authorization note: E5 recommends E6 but does not authorize starting E6.

## BLOCK E6 - Product Ledger Local/Dev External Audit Packet

- Objective: package Product Ledger local/dev current authority, evidence, blocked states, questions and risks for read-only audit review.
- Status: completed as docs-only/read-only/audit-packet-only in `docs/audit/product-ledger-local-dev/README.md`.
- Decision: `GO_WITH_FINDINGS_PRODUCT_LEDGER_LOCAL_DEV_EXTERNAL_AUDIT_PACKET_READY`.
- Source changed: none.
- Tests changed: none.
- CI changed: none; Tier 1 remains manual/discovery-only and CI enforcement remains `0%`.
- External submission: none; E6 creates the packet only.
- Current posture: Product Ledger local/dev remains evidence-only; runtime/product enablement, public/product, Production route, latest pointer, read precedence, product authority, DB/provider/cloud/network/KMS/WORM and release/commercial remain blocked or `0% / NO-GO`.
- Next recommended macro-block: `NODAL_OS_BLOCK_E7_EXTERNAL_AUDIT_PACKET_REVIEW_READ_ONLY`.
- Authorization note: E6 recommends E7 but does not authorize starting E7.

## BLOCK E7 - Product Ledger Local/Dev External Audit Packet Review

- Objective: review the E6 Product Ledger local/dev audit packet internally/read-only as if an external auditor.
- Status: completed as docs-only/read-only/audit-review in `docs/audit/product-ledger-local-dev/audit-review-result.md`.
- Decision: `GO_WITH_FINDINGS_PRODUCT_LEDGER_LOCAL_DEV_AUDIT_PACKET_REVIEW_READY`.
- Source changed: none.
- Tests changed: none.
- CI changed: none; Tier 1 remains manual/discovery-only and CI enforcement remains `0%`.
- External submission: none; E7 reviewed the packet only.
- Findings: P0=0, P1=0, P2=0; P3 manual gates remain operator-run and lower-risk stale docs may still need future cross-links; P4 packet next-step wording needed E7 result linkage and was updated.
- Current posture: Product Ledger local/dev remains evidence-only; runtime/product enablement, public/product, Production route, latest pointer, read precedence, product authority, DB/provider/cloud/network/KMS/WORM and release/commercial remain blocked or `0% / NO-GO`.
- Next recommended macro-block: `NODAL_OS_BLOCK_E8_EXTERNAL_AUDIT_PACKET_OPERATOR_REVIEW_HANDOFF_READ_ONLY`.
- Authorization note: E7 recommends E8 but does not authorize starting E8.

## BLOCK E8 - Product Ledger Local/Dev Operator Review Handoff

- Objective: prepare a clean Diego/operator review handoff for the Product Ledger local/dev audit packet after E7 review.
- Status: completed as docs-only/read-only/operator-handoff-only in `docs/audit/product-ledger-local-dev/operator-review-handoff.md`.
- Decision: `GO_WITH_FINDINGS_PRODUCT_LEDGER_OPERATOR_REVIEW_HANDOFF_READY`.
- Source changed: none.
- Tests changed: none.
- CI changed: none; Tier 1 remains manual/discovery-only and CI enforcement remains `0%`.
- External submission: none; E8 prepares handoff only.
- Operator review execution: none; Diego/operator review is the next step.
- Findings: P0=0, P1=0, P2=0; P3 manual gates remain operator-run and lower-risk stale docs may still need future cross-links if operator review finds ambiguity; P4 historical percentages/noisy anti-capability wording remain by design.
- Current posture: Product Ledger local/dev remains evidence-only; runtime/product enablement, public/product, Production route, latest pointer, read precedence, product authority, DB/provider/cloud/network/KMS/WORM and release/commercial remain blocked or `0% / NO-GO`.
- Next recommended macro-block: `STOP_FOR_OPERATOR_REVIEW`.
- Authorization note: E8 stops for Diego/operator review and does not authorize E9 or runtime/product work.

## BLOCK E9 - Product Ledger External Review Approval Record

- Objective: record Diego/operator decision `APPROVE_PACKET_FOR_EXTERNAL_REVIEW` and prepare external/manual review handoff.
- Status: completed as docs-only/read-only/external-manual-review-handoff-only in `docs/audit/product-ledger-local-dev/external-review-handoff.md`.
- Decision: `GO_WITH_FINDINGS_PRODUCT_LEDGER_EXTERNAL_REVIEW_APPROVAL_RECORDED_READY`.
- Source changed: none.
- Tests changed: none.
- CI changed: none; Tier 1 remains manual/discovery-only and CI enforcement remains `0%`.
- External submission: none by Codex; packet is prepared for Diego/operator manual submission only.
- Findings: P0=0, P1=0, P2=0; P3 external review approval can be misread as runtime/product authorization if detached from E9 wording and manual gates remain operator-run; P4 historical percentages/noisy anti-capability wording remain by design.
- Current posture: Product Ledger local/dev remains evidence-only; runtime/product enablement, public/product, Production route, latest pointer, read precedence, product authority, DB/provider/cloud/network/KMS/WORM and release/commercial remain blocked or `0% / NO-GO`.
- Next recommended macro-block: `STOP_FOR_EXTERNAL_REVIEW_SUBMISSION_BY_OPERATOR`.
- Authorization note: E9 does not authorize E10, runtime/product or release/commercial work.

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
