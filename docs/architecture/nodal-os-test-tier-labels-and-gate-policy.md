# NODAL OS Test Tier Labels And Gate Policy

Date: 2026-07-07

Mode: design-only / docs-only / test-plan-only / guard-policy-only. This document does not change tests, test attributes, CI, source behavior, scanner behavior, runtime behavior, routes, product surfaces, active read precedence, latest pointer, product authority, public/product exposure, cloud/network/DB, KMS/WORM, release or commercial readiness.

Baseline: Block C test tiering/static scan design, Block C1 static guard catalog test-only implementation, Block C2 static guard catalog equivalence expansion and the source refactor readiness audit.

## 1. Executive Verdict

Decision: `GO_WITH_FINDINGS_TEST_TIER_LABELS_GATE_DOCUMENTATION_DESIGN_ONLY_READY`.

C3 defines the future labels, gates and do-not-move policy needed before any later metadata implementation. No test movement, deletion, assertion rewrite, CI behavior change or source change is authorized here.

C1 created the central test-only `NodalOsStaticGuardCatalog`. C2 expanded equivalence while keeping old local assertions intact. C3 turns that evidence into a gate policy: labels are names for future discovery and reporting only; they must not be interpreted as permission to skip, move or downgrade load-bearing tests.

Findings: P0=0, P1=0, P2=0 new. P3 remains around future label misuse causing hidden coverage loss. P4 remains around historical docs and mixed legacy/current vocabulary.

## 2. Tier Label Design

Future labels should be additive MSTest categories or equivalent metadata. They must not replace existing categories and must not change test discovery, skip behavior, CI filters or assertion strength without a later implementation GO.

| Tier | Future label | Purpose | Default use |
| --- | --- | --- | --- |
| Tier 1 | `NodalOsTier1Safety` | Required pre-refactor and pre-push safety gate. | Always run before source refactor, contract merge, path/writer changes and public/product readiness. |
| Tier 2 | `NodalOsTier2Integration` | Extended integration, route, DOM, Recipes and operator-surface confidence. | Run when route/surface/read-model/product-ledger flows change. |
| Tier 3 | `NodalOsTier3CorpusAudit` | Property, corpus, static audit and historical claim scans. | Run before public/product readiness, high-risk path/writer work and final external audits. |
| Tier 4 | `NodalOsTier4LegacyPeriodic` | Legacy, OCR/Paddle/ONNX, broader lab/runtime and long-running periodic suites. | Run when touched or before release/commercial decisions. |

Complementary labels:

- `StaticGuard`
- `ProductLedger`
- `Approval`
- `RunClaimCoherence`
- `PublicProductBlock`
- `ProductionRouteBlock`
- `LatestPointerBlock`
- `ReadPrecedenceBlock`
- `ProductAuthorityBlock`
- `CommandExecutionBlock`
- `CloudNetworkDbBlock`
- `KmsWormComplianceBlock`
- `ReleaseCommercialBlock`

Label rule: a test may carry more than one complementary label. A test with any hard-block label that is the only current coverage for that boundary remains Tier 1 until an explicit future GO proves equivalent coverage.

## 3. Gates By Scenario

### A. Pre-Source-Refactor Gate

Required before source refactor, class/file rename, common contract implementation, source adapter migration or route consolidation:

1. `dotnet build src/OneBrain.Core/OneBrain.Core.csproj --no-restore -v:minimal`
2. `dotnet build src/OneBrain.Pilot/OneBrain.Pilot.csproj --no-restore -v:minimal`
3. `dotnet build OneBrain.slnx --no-restore -v:minimal`
4. Tier 1 Safety gate.
5. Product Ledger Safety focused run.
6. Central static guard catalog tests.
7. `/run` claim-coherence guard.
8. No public/product exposure guard.
9. No Production route guard.
10. No latest pointer, active read precedence and product authority guards.
11. No command execution, shell/subprocess, provider/cloud/network, DB/migration, KMS/WORM/external trust and release/commercial guards.
12. `git diff --check`.
13. Docs/log update for changed risk posture.

### B. Pre-Contract-Merge Gate

Required before adding or using shared contracts such as `LocalOnlyResult<T>`, `BoundaryClaims`, `WriterMode`, `EvidenceRole` or `GuardEvaluationResult`:

1. Everything in the pre-source-refactor gate.
2. Selected Tier 2 integration for the affected capability.
3. Old/new equivalence tests for each migrated contract surface.
4. Default-deny/fail-closed invariant tests for common contracts.
5. Evidence that redaction, path canonicalization and hash/checkpoint behavior remain behavior-frozen.

### C. Pre-Public/Product Gate

Required before any public/product exposure decision:

1. Everything in the pre-contract-merge gate.
2. All relevant Tier 2 route/DOM/operator-surface tests.
3. Tier 3 overclaim, property, corpus and full static scan coverage.
4. Product Ledger Safety and Product Ledger Recipes focused runs.
5. External audit packet and manual/business signoff placeholder.
6. Explicit proof that public/product route, Production route, product authority, latest pointer and read precedence semantics are authorized and bounded.

### D. Pre-Release/Commercial Gate

Current state: `0% / NO-GO`.

Required before any future release/commercial readiness claim:

1. All previous gates.
2. Selected Tier 4 legacy/lab/runtime coverage.
3. External security/product audit.
4. Compliance review for retention, custody, KMS/WORM/external trust and deletion lifecycle claims.
5. Installer/deployment/release validation.
6. Production configuration review.
7. No stale evidence overclaim.
8. Explicit human release/commercial approval.

## 4. Do-Not-Move Tier 1 List

These tests/scans must remain Tier 1 until a future implementation block proves equivalent or stronger coverage and receives explicit GO. The list is conceptual; C3 does not modify test metadata.

- Path confinement and canonicalization.
- Redaction-before-persistence.
- Append-only, no-overwrite and no-truncation.
- Hash, checkpoint, tamper and corruption detection.
- Product Ledger authority and not-authority semantics.
- Approval decision fail-closed behavior.
- No public/product exposure.
- No Production route.
- No latest pointer and no latest pointer overwrite.
- No active read precedence.
- No product authority and no product read-model authority.
- No command execution and no product command handler exposure.
- No shell/subprocess activation.
- No provider/cloud/network and no DB/migration.
- No KMS/WORM/external trust and no compliance custody overclaim.
- `/run` claim coherence and Pilot/runtime isolation.
- Release/commercial block.
- Central static guard catalog focused tests and each migrated old-assertion equivalence test.

## 5. Move-Candidate List

These are candidates for future Tier 2/3/4 metadata, not for movement in C3:

- Duplicate Safety/Recipes mirror checks where one Tier 1 invariant and one Tier 2 story test can be proven equivalent.
- Repeated DOM label checks that do not carry unique authority, route, write or release/commercial semantics.
- Docs negative wording scans that can move into a central Tier 3 docs scan with scoped negative wording.
- Deep property/corpus traversal variants after one Tier 1 smoke remains.
- OCR/Paddle/ONNX warning-heavy suites, unless OCR is touched or release/commercial review is underway.
- Long-running Recipes variants when equivalent focused tests exist and remain green.
- Historical closeout docs scans superseded by current canon, after a scoped archive/index policy exists.

No candidate may be moved if it is the only assertion for a P0/P1/P2 boundary, a public/product blocker, a Production route blocker, a latest pointer/read precedence/product authority blocker or a redaction/path/hash/checkpoint invariant.

## 6. Static Guard Catalog Role

`NodalOsStaticGuardCatalog` is a test-only hard gate helper. It is not production source and is not a product/runtime feature.

Current catalog role after C1/C2:

- centralizes categories for public/product exposure, Production routes, runtime execution claims, latest pointer, read precedence, product authority, command execution, shell/subprocess, cloud/network/DB, KMS/WORM/compliance, release/commercial and `/run` claim coherence;
- provides explicit source/docs scan entrypoints;
- proves allowed negative no-go wording stays allowed;
- proves positive forbidden wording still hard-fails;
- keeps old local assertions as equivalence backstops in migrated tests.

Future hard-gate categories:

- `PublicProductBlock`
- `ProductionRouteBlock`
- `LatestPointerBlock`
- `ReadPrecedenceBlock`
- `ProductAuthorityBlock`
- `CommandExecutionBlock`
- `CloudNetworkDbBlock`
- `KmsWormComplianceBlock`
- `ReleaseCommercialBlock`
- `RunClaimCoherence`

Future scanner work must keep source scans and docs scans separate. It must not use file-wide allowlists when one scoped negative sentence is intended.

## 7. Future C4 Implementation Plan

Future block name: `NODAL_OS_BLOCK_C4_TEST_LABELS_METADATA_IMPLEMENTATION_TEST_ONLY`.

Allowed only in that future GO:

- Add additive labels/traits metadata to a small subset of tests.
- Start with central static guard catalog tests and a tiny number of obvious Product Ledger hard-block tests.
- Do not move tests.
- Do not delete tests.
- Do not skip tests.
- Do not change CI filters.
- Do not change assertions.
- Do not change scanner behavior.
- Validate discovery/listing of the new labels.
- Update docs, decision-log and handoff.

Recommended first C4 subset:

- `NodalOsStaticGuardCatalogTests` as `NodalOsTier1Safety` + `StaticGuard`.
- One public/product blocker as `NodalOsTier1Safety` + `ProductLedger` + `PublicProductBlock`.
- One Production route blocker as `NodalOsTier1Safety` + `ProductLedger` + `ProductionRouteBlock`.
- One `/run` claim-coherence guard as `NodalOsTier1Safety` + `RunClaimCoherence`.

Stop conditions for C4:

- any test movement or deletion;
- any assertion weakening;
- any CI behavior change;
- any source behavior change;
- any broader metadata sweep without evidence;
- any P0/P1/P2 or TRUE_RISK.

## 8. Risks

P3:

- Future labels can be misread as permission to move or skip tests.
- Static guard categories can become noisy if source/docs scopes are blurred.
- Duplicate Safety/Recipes tests can hide unique load-bearing assertions.
- C4 can create false confidence if discovery/listing is not validated.

P4:

- Historical docs retain older no-runtime/no-execution wording.
- OCR/Paddle/ONNX legacy tests keep inherited warning noise in solution builds.
- Some Product Ledger route/DOM label tests look redundant but still protect user-facing claims.

Mitigations:

- Labels are additive only.
- Old assertions stay until equivalence is proven.
- Tier 1 remains hard before source edits.
- Public/product and release/commercial remain blocked until explicit future gates are satisfied.

## 9. Exact Next GO

After C6 closes with required gates green, the safest next block is:

`AUTHORIZE_NODAL_OS_BLOCK_D1_COMMON_CONTRACTS_PARALLEL_IMPLEMENTATION_DESIGN_OR_TEST_ONLY`

Expected outcome: prepare or add shared contracts in parallel only, with no runtime/product use and no deletion of existing Product Ledger contracts until equivalence is proven.

## 10. C4 Initial Metadata Implementation

Implementation status: partially implemented in `NODAL_OS_BLOCK_C4_TEST_LABELS_METADATA_IMPLEMENTATION_TEST_ONLY`.

Applied labels:

| File | Test scope | Labels |
| --- | --- | --- |
| `tests/OneBrain.Safety.Tests/NodalOsStaticGuardCatalogTests.cs` | class | `NodalOsTier1Safety`, `StaticGuard` |
| `NodalOsStaticGuardCatalogTests.StaticGuardCatalog_DetectsForbiddenPositiveSamples` | method | `PublicProductBlock`, `ProductionRouteBlock`, `LatestPointerBlock`, `ReadPrecedenceBlock`, `ProductAuthorityBlock` |
| `NodalOsStaticGuardCatalogTests.StaticGuardCatalog_AllowsExpectedNegativeNoGoWording` | method | `RunClaimCoherence` |
| `NodalOsStaticGuardCatalogTests.StaticGuardCatalog_C2MirrorsRetainedOldSourceAssertions` | method | `LatestPointerBlock`, `ReadPrecedenceBlock`, `ProductAuthorityBlock` |
| `NodalOsStaticGuardCatalogTests.StaticGuardCatalog_C2KeepsAllowedNegativeWordingSeparateFromPositiveMatches` | method | `LatestPointerBlock` |
| `NodalOsStaticGuardCatalogTests.StaticGuardCatalog_C2SourceAndDocsScopesUseExplicitEntrypoints` | method | `ProductAuthorityBlock` |
| `NodalOsStaticGuardCatalogTests.StaticGuardCatalog_PublicProductAndProductionRouteAssertionsRemainHardFailing` | method | `PublicProductBlock`, `ProductionRouteBlock` |
| `ProductLedgerBroaderWorkspaceOrPublicProductBoundaryTests.BroaderWorkspaceOrPublicProductBoundary_LatestStateSnapshotImplementationIsAuthorizedButOtherFrontiersRemainClosed` | method | `NodalOsTier1Safety`, `ProductLedger`, `PublicProductBlock`, `ProductionRouteBlock` |
| `ProductLedgerBroaderWorkspaceOrPublicProductBoundaryTests.BroaderWorkspaceOrPublicProductBoundary_PublicProductMutationAndUnsafeFrontiersRemainClosed` | method | `NodalOsTier1Safety`, `ProductLedger`, `PublicProductBlock`, `ProductionRouteBlock` |

Discovery evidence:

- `NodalOsStaticGuardCatalogTests.StaticGuardCatalog_C4MetadataLabelsAreAdditiveAndDiscoverable` verifies the initial class and method labels via reflection.
- This is metadata-only. It does not change test discovery defaults, CI filters, skip behavior, test movement, assertions, scanner behavior or source behavior.

Current label-run preview:

- `dotnet test tests/OneBrain.Safety.Tests/OneBrain.Safety.Tests.csproj --no-build --filter "TestCategory=NodalOsTier1Safety" -v:minimal`

Do not promote this filter to CI until a later GO proves the labeled subset is representative and keeps current Product Ledger Safety/Recipes gates green.

## 11. C5 Gate Commands And Discovery

Implementation status: completed as docs/design-only in `NODAL_OS_BLOCK_C5_PRE_REFACTOR_GATE_COMMANDS_AND_DISCOVERY_DESIGN_ONLY`.

Canonical command policy:

- `docs/architecture/nodal-os-pre-refactor-gate-commands-and-discovery.md`

The C5 command policy documents label discovery, current partial Tier 1 execution, focused static guard runs, Product Ledger Safety/Recipes, the pre-source-refactor gate, scenario matrix and timeout policy. It does not change CI or test behavior.

## 12. C6 Tier 1 Label Expansion

Implementation status: completed as test-only/metadata-only in `NODAL_OS_BLOCK_C6_TIER1_LABEL_EXPANSION_TEST_ONLY`.

Applied scope:

| Group | Newly labeled methods | Why Tier 1 |
| --- | ---: | --- |
| Active durable read precedence decision matrix | 1 | Blocks read precedence, latest pointer, product authority, public/product, Production route, command execution and release/commercial overclaims. |
| Durable latest-state auxiliary evidence | 3 | Blocks authority, read precedence, latest pointer, public/product, command execution and forbidden source activation while preserving auxiliary-not-authority semantics. |
| Public/product or workspace action authorization | 2 | Blocks public/product, Production route, command execution and release/commercial claims around workspace-test-jail authorization. |
| User workspace or public/product authorization boundary | 1 | Keeps allowlisted user-workspace source bounded and public/product unsafe frontiers closed. |
| First real user-facing local action readiness | 2 | Keeps the local handoff draft candidate Development-only and blocks public/product, Production, command and release frontiers. |
| Public UI action surface | 6 | Proves fail-closed behavior, dangerous action rejection, unsafe claim rejection, disabled dangerous buttons and no network/DB/KMS/live/release source overclaim. |
| Reflection evidence | 1 | `StaticGuardCatalog_C6ExpandedTier1LabelsAreDiscoverable` verifies selected C6 labels remain discoverable. |

New complementary categories used in C6:

- `CommandExecutionBlock`
- `ReleaseCommercialBlock`

C6 deliberately does not label every Product Ledger test. Remaining future candidates include path canonicalization, redaction-before-persistence, append/hash/checkpoint, writer mode, route/DOM integration and property/corpus tests. Those remain covered by the full Product Ledger Safety and Recipes commands until a later C7 or D1/D2 gate proves a smaller Tier 1 is representative.

Current Tier 1 label coverage estimate after C6: approximately 15-25% of the intended conceptual Tier 1 safety surface. It is meaningfully better than the C4 proof, but still not a complete replacement for Product Ledger Safety/Recipes.

Non-goals preserved:

- no `src/` changes;
- no test movement, deletion, skip behavior or assertion change;
- no scanner behavior change;
- no CI/build script change;
- no Tier 1 CI enforcement;
- no runtime/product capability change;
- no public/product route, Production route, active read precedence, latest pointer or product authority;
- no provider/cloud/network, DB/migration, KMS/WORM/external trust or release/commercial readiness.
