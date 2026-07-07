# NODAL OS Pre-Refactor Gate Commands And Discovery

Date: 2026-07-07

Mode: docs-only / design-only / command-documentation-only / discovery-documentation-only. This document does not change CI, tests, test movement, test deletion, assertions, source behavior, scanner behavior, runtime/product behavior, public/product exposure, Production route, active read precedence, latest pointer, product authority, cloud/network/DB, KMS/WORM, release or commercial readiness.

Baseline: C3 test tier labels and gate policy, C4 initial additive `TestCategory` metadata and C6 controlled Tier 1 label expansion.

## 1. Executive Verdict

Decision: `GO_WITH_FINDINGS_PRE_REFACTOR_GATE_COMMANDS_DISCOVERY_DESIGN_ONLY_READY`.

C5 documents the exact manual commands for label discovery, the current partial Tier 1 label run, static guard checks, Product Ledger Safety/Recipes and pre-source-refactor gating. It intentionally does not enforce these commands in CI. The C6 label set is stronger than the C4 proof, but it is still not a complete Tier 1 suite.

Findings: P0=0, P1=0, P2=0 new. P3 remains that documented commands may be mistaken for enforced CI. P4 remains that Tier 1 labels are partial and solution builds can still surface inherited non-C5 warnings.

## 2. Current C6 Metadata Status

Current metadata exists in MSTest `TestCategory` attributes.

Implemented labels:

- `NodalOsTier1Safety`
- `StaticGuard`
- `ProductLedger`
- `PublicProductBlock`
- `ProductionRouteBlock`
- `RunClaimCoherence`
- `LatestPointerBlock`
- `ReadPrecedenceBlock`
- `ProductAuthorityBlock`
- `CommandExecutionBlock`
- `ReleaseCommercialBlock`
- `CommonContracts`
- `DesignOnly`
- `NoRuntimeWiring`

Current labeled subset:

- `NodalOsStaticGuardCatalogTests` class and selected static guard methods.
- Two `ProductLedgerBroaderWorkspaceOrPublicProductBoundaryTests` methods.
- Selected Product Ledger hard-block methods for active read precedence, durable latest-state auxiliary evidence, public/product workspace authorization, user-workspace/public-product boundaries, first real local action readiness and public UI action surface.
- Reflection evidence: `StaticGuardCatalog_C4MetadataLabelsAreAdditiveAndDiscoverable`.
- Reflection evidence: `StaticGuardCatalog_C6ExpandedTier1LabelsAreDiscoverable`.

Important limit: current `NodalOsTier1Safety` is not the complete Tier 1 gate. It is a controlled metadata expansion only. Product Ledger Safety and Recipes remain mandatory for Product Ledger-adjacent source/refactor confidence.

## 3. Discovery Commands

Run from repo root:

```powershell
cd C:\DESARROLLO\NodalOS\Codigo-m12-audit
```

List all discovered Safety tests:

```powershell
dotnet test tests/OneBrain.Safety.Tests/OneBrain.Safety.Tests.csproj --no-build --list-tests
```

List the current Tier 1 labeled subset:

```powershell
dotnet test tests/OneBrain.Safety.Tests/OneBrain.Safety.Tests.csproj --no-build --list-tests --filter "TestCategory=NodalOsTier1Safety"
```

List static guard tests:

```powershell
dotnet test tests/OneBrain.Safety.Tests/OneBrain.Safety.Tests.csproj --no-build --list-tests --filter "TestCategory=StaticGuard"
```

List Product Ledger public/product blockers:

```powershell
dotnet test tests/OneBrain.Safety.Tests/OneBrain.Safety.Tests.csproj --no-build --list-tests --filter "TestCategory=ProductLedger&TestCategory=PublicProductBlock"
```

List Product Ledger Production route blockers:

```powershell
dotnet test tests/OneBrain.Safety.Tests/OneBrain.Safety.Tests.csproj --no-build --list-tests --filter "TestCategory=ProductLedger&TestCategory=ProductionRouteBlock"
```

List D1 common-contract candidate tests:

```powershell
dotnet test tests/OneBrain.Safety.Tests/OneBrain.Safety.Tests.csproj --no-build --list-tests --filter "TestCategory=CommonContracts"
dotnet test tests/OneBrain.Safety.Tests/OneBrain.Safety.Tests.csproj --no-build --list-tests --filter "TestCategory=DesignOnly"
```

Fallback if `--list-tests --filter` is unavailable or fragile in a local SDK/runner:

```powershell
rg -n 'TestCategory\("(NodalOsTier1Safety|StaticGuard|ProductLedger|PublicProductBlock|ProductionRouteBlock|RunClaimCoherence|LatestPointerBlock|ReadPrecedenceBlock|ProductAuthorityBlock|CommandExecutionBlock|ReleaseCommercialBlock|CommonContracts|DesignOnly|NoRuntimeWiring)"\)' tests/OneBrain.Safety.Tests
```

## 4. Tier 1 Commands

Current partial labeled Tier 1 proof after C6:

```powershell
dotnet test tests/OneBrain.Safety.Tests/OneBrain.Safety.Tests.csproj --no-build --filter "TestCategory=NodalOsTier1Safety" -v:minimal
```

Static guard label proof:

```powershell
dotnet test tests/OneBrain.Safety.Tests/OneBrain.Safety.Tests.csproj --no-build --filter "TestCategory=StaticGuard" -v:minimal
```

Product Ledger public/product blocker proof:

```powershell
dotnet test tests/OneBrain.Safety.Tests/OneBrain.Safety.Tests.csproj --no-build --filter "TestCategory=ProductLedger&TestCategory=PublicProductBlock" -v:minimal
```

Product Ledger Production route blocker proof:

```powershell
dotnet test tests/OneBrain.Safety.Tests/OneBrain.Safety.Tests.csproj --no-build --filter "TestCategory=ProductLedger&TestCategory=ProductionRouteBlock" -v:minimal
```

D1 common-contract candidate proof:

```powershell
dotnet test tests/OneBrain.Safety.Tests/OneBrain.Safety.Tests.csproj --no-build --filter "TestCategory=CommonContracts" -v:minimal
dotnet test tests/OneBrain.Safety.Tests/OneBrain.Safety.Tests.csproj --no-build --filter "TestCategory=DesignOnly" -v:minimal
dotnet test tests/OneBrain.Safety.Tests/OneBrain.Safety.Tests.csproj --no-build --filter "FullyQualifiedName~NodalOsCommonContractsDesignOnlyCandidate" -v:minimal
```

These commands are discovery/gate previews only. They are not a substitute for full Product Ledger Safety and Recipes, and they are not CI enforcement.

## 5. Static Guard Commands

Focused static guard catalog:

```powershell
dotnet test tests/OneBrain.Safety.Tests/OneBrain.Safety.Tests.csproj --no-build --filter "FullyQualifiedName~NodalOsStaticGuardCatalog" -v:minimal
```

Run claim-coherence label proof:

```powershell
dotnet test tests/OneBrain.Safety.Tests/OneBrain.Safety.Tests.csproj --no-build --filter "TestCategory=RunClaimCoherence" -v:minimal
```

Public/product plus Production route catalog proof:

```powershell
dotnet test tests/OneBrain.Safety.Tests/OneBrain.Safety.Tests.csproj --no-build --filter "TestCategory=PublicProductBlock|TestCategory=ProductionRouteBlock" -v:minimal
```

Use the fully qualified name command as the stable fallback because it is tied to the current catalog class name.

## 6. Product Ledger Safety / Recipes Commands

Product Ledger Safety:

```powershell
dotnet test tests/OneBrain.Safety.Tests/OneBrain.Safety.Tests.csproj --no-build --filter "FullyQualifiedName~ProductLedger" -v:minimal
```

Product Ledger Recipes:

```powershell
dotnet test tests/OneBrain.Recipes.Tests/OneBrain.Recipes.Tests.csproj --no-build --filter "FullyQualifiedName~ProductLedger" -v:minimal
```

These remain required for Product Ledger source refactor confidence. Do not replace them with C4 label-only filters.

## 7. Pre-Source-Refactor Gate

Recommended manual sequence before any future source refactor, contract implementation, class/file rename or adapter migration:

```powershell
dotnet build src/OneBrain.Core/OneBrain.Core.csproj --no-restore -v:minimal
dotnet build src/OneBrain.Pilot/OneBrain.Pilot.csproj --no-restore -v:minimal
dotnet build OneBrain.slnx --no-restore -v:minimal
dotnet test tests/OneBrain.Safety.Tests/OneBrain.Safety.Tests.csproj --no-build --filter "TestCategory=NodalOsTier1Safety" -v:minimal
dotnet test tests/OneBrain.Safety.Tests/OneBrain.Safety.Tests.csproj --no-build --filter "FullyQualifiedName~NodalOsStaticGuardCatalog" -v:minimal
dotnet test tests/OneBrain.Safety.Tests/OneBrain.Safety.Tests.csproj --no-build --filter "FullyQualifiedName~ProductLedger" -v:minimal
dotnet test tests/OneBrain.Recipes.Tests/OneBrain.Recipes.Tests.csproj --no-build --filter "FullyQualifiedName~ProductLedger" -v:minimal
git diff --check
git status --short
git rev-list --left-right --count HEAD...'@{u}'
```

Pass criteria:

- all commands complete successfully;
- `git diff --check` has no whitespace errors;
- `git status --short` contains only intended changes before commit and is clean after commit;
- origin is `0 0` before final handoff;
- no public/product, Production route, release/commercial or authority-frontier claim is introduced by the change.

## 8. Gate Matrix By Scenario

| Scenario | Required commands | Optional commands | Timeout fallback | Pass criteria | Blocks GO if fails? |
| --- | --- | --- | --- | --- | --- |
| A. Pre-docs-only change | `git diff --check`; docs-only diff review; final `git status --short` | Core/Pilot/Solution build if requested by block | Report timeout honestly; do not claim build PASS | No source/test/CI diff unless authorized | Yes for diff/check mismatch |
| B. Pre-test-only change | Core/Pilot/Solution build; affected focused tests; `git diff --check` | Product Ledger Safety/Recipes if Product Ledger-adjacent | Run narrower affected tests only if broad suite times out; report broad timeout | Changed tests and affected focus pass | Yes |
| C. Pre-source-refactor | Full pre-source-refactor sequence in section 7 | `--list-tests` discovery snapshots | If Product Ledger Recipes times out, run focused affected Recipes and report timeout | Core/Pilot/Solution, Tier label proof, static guard, Product Ledger Safety and Product Ledger Recipes pass or timeout is explicitly scoped | Yes |
| D. Pre-contract-merge | Scenario C plus old/new equivalence tests and selected Tier 2 integration | Contract invariant focused tests | If Tier 2 times out, run affected Tier 2 subset and report timeout | No double truth, default-deny invariants pass | Yes |
| E. Pre-public/product future | Scenario D plus all relevant Tier 2 route/DOM, Tier 3 corpus/static scans and external audit packet | Selected Tier 4 runtime/lab coverage | No fallback may convert timeout to PASS; manual review required | P0/P1/P2=0 and manual/business signoff exists | Yes |
| F. Pre-release/commercial future | All tiers plus external security/product/compliance/release review | Release artifact/install validation | Current status is NO-GO; timeout or skipped external review blocks | Release/commercial remains `0% / NO-GO` until future explicit approval | Yes |

## 9. Timeout Policy

Timeouts are not passes.

If a command times out:

1. Record the exact command and timeout duration.
2. State that full PASS was not obtained.
3. Run the smallest relevant focused fallback only if it is useful.
4. Report the fallback as fallback evidence, not as a replacement for the timed-out suite.
5. Do not commit/push a source refactor if a required safety test timed out and no scoped policy allows fallback.

For docs-only/design-only blocks, a timed-out optional build/test can be reported honestly if all mandatory docs-only guards pass and no source/test/CI files changed.

## 10. What This Does NOT Do

C5 does not:

- change CI;
- add scripts;
- move tests;
- delete tests;
- skip tests;
- change assertions;
- change scanner behavior;
- touch `src/`;
- activate runtime/product behavior;
- expose public/product routes;
- enable Production routes;
- authorize active read precedence, latest pointer or product authority;
- authorize command execution, shell/subprocess, provider/cloud/network, DB/migration, KMS/WORM or release/commercial readiness.

## 11. C6 Label Expansion Note

C6 added additive MSTest metadata to 15 existing hard-block methods and one reflection evidence method. The expected `NodalOsTier1Safety` discovery count after C6 is 26 tests if the local build artifacts are current.

The newly labeled groups cover:

- active durable read precedence/latest pointer/product authority blockers;
- durable latest-state auxiliary evidence blockers;
- public/product and Production route blockers;
- command execution blockers;
- release/commercial blockers;
- public UI action fail-closed and dangerous-action rejection blockers.

C6 did not change test assertions, scanner behavior, source behavior, CI or runtime/product behavior.

## 12. D1 Common Contracts Parallel Candidate Note

D1 adds five Safety tests under `NodalOsCommonContractsDesignOnlyCandidateTests` and one test-only candidate contract file under `tests/OneBrain.Safety.Tests`.

Expected after D1:

- `TestCategory=CommonContracts`: 5 tests.
- `TestCategory=DesignOnly`: 5 tests.
- `FullyQualifiedName~NodalOsCommonContractsDesignOnlyCandidate`: 5 tests.

D1 remains manual/discovery-only. It does not create CI enforcement, production source contracts, route registration, service registration, command handlers or runtime/product behavior.

## 13. Future Implementation Options

Safe next blocks:

- `NODAL_OS_BLOCK_C7_PRE_REFACTOR_GATE_SCRIPT_TEST_ONLY_DISABLED`: add a disabled/local-only helper script for the documented gate, with no CI wiring.
- `NODAL_OS_BLOCK_D2_MAPPING_ADAPTERS_EQUIVALENCE_EXPANSION_TEST_ONLY`: add mapping adapters/equivalence tests in parallel only, with no replacement of existing contracts.

Do not proceed to source refactor, contract use, public/product exposure or release/commercial readiness from this document alone.
