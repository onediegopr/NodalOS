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
- `MappingAdapters`
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
dotnet test tests/OneBrain.Safety.Tests/OneBrain.Safety.Tests.csproj --no-build --list-tests --filter "TestCategory=MappingAdapters"
```

Fallback if `--list-tests --filter` is unavailable or fragile in a local SDK/runner:

```powershell
rg -n 'TestCategory\("(NodalOsTier1Safety|StaticGuard|ProductLedger|PublicProductBlock|ProductionRouteBlock|RunClaimCoherence|LatestPointerBlock|ReadPrecedenceBlock|ProductAuthorityBlock|CommandExecutionBlock|ReleaseCommercialBlock|CommonContracts|MappingAdapters|DesignOnly|NoRuntimeWiring)"\)' tests/OneBrain.Safety.Tests
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

D2 mapping adapter equivalence proof:

```powershell
dotnet test tests/OneBrain.Safety.Tests/OneBrain.Safety.Tests.csproj --no-build --filter "TestCategory=MappingAdapters" -v:minimal
dotnet test tests/OneBrain.Safety.Tests/OneBrain.Safety.Tests.csproj --no-build --filter "FullyQualifiedName~NodalOsCommonBoundaryMappingDesignOnlyAdapter" -v:minimal
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

## 13. D2 Mapping Adapter Equivalence Note

D2 adds a Safety-test-only mapper and equivalence tests under `NodalOsCommonBoundaryMappingDesignOnlyAdapterTests`.

Expected after D2:

- `TestCategory=MappingAdapters`: 14 tests.
- `FullyQualifiedName~NodalOsCommonBoundaryMappingDesignOnlyAdapter`: 14 tests.

D2 keeps unknown/unsupported/non-authoritative concepts fail-closed. It does not replace existing contracts, does not touch `src/`, does not create production adapters and does not authorize runtime/product behavior.

## 14. D3 Source Refactor Plan Audit Note

D3 selects the first safe source-facing move but does not implement it.

Canonical D3 plan:

- `docs/architecture/nodal-os-d3-source-refactor-plan-audit.md`

Selected future D4 candidate:

- `AUTHORIZE_NODAL_OS_BLOCK_D4_MINIMAL_SOURCE_CANDIDATE_NO_RUNTIME_WIRING`

D4 expected extra no-reference checks:

```powershell
rg -n "NodalOsCommonBoundaryClaimsCandidate" src/OneBrain.Pilot src/OneBrain.Cli .github azure-pipelines.yml
rg -n "NodalOsCommonBoundaryClaimsCandidate" src/OneBrain.Core/Approval -g "*.cs"
```

The first command should return no matches. The second command should return only the new candidate file and any D4 test-approved self-reference if a future D4 is authorized.

## 15. D4 Minimal Source Candidate Note

D4 adds exactly one source candidate:

- `src/OneBrain.Core/Approval/NodalOsCommonBoundaryClaimsCandidate.cs`

The candidate is parallel-only, non-authoritative and not wired into runtime/product. It does not replace existing Product Ledger contracts, D1/D2 remain design/test-only, and existing hard-block authorities remain authoritative.

Expected after D4:

- `TestCategory=SourceCandidate`: 19 tests.
- `FullyQualifiedName~NodalOsCommonBoundaryClaimsCandidate`: 19 tests.
- `TestCategory=CommonContracts`: increases by 19.
- `TestCategory=DesignOnly`: increases by 19.
- `TestCategory=NoRuntimeWiring`: increases by 19.

D4 no-reference checks:

```powershell
rg -n "NodalOsCommonBoundaryClaimsCandidate" src/OneBrain.Pilot src/OneBrain.Cli .github azure-pipelines.yml
rg -n "NodalOsCommonBoundaryClaimsCandidate" src/OneBrain.Core/Approval -g "*.cs"
```

The first command must return no matches. The second command should return only `src/OneBrain.Core/Approval/NodalOsCommonBoundaryClaimsCandidate.cs`.

CI enforcement remains 0%. Runtime/product enablement remains 0%. Release/commercial remains 0% / NO-GO.

## 16. D5 Isolation And Equivalence Hardening Note

D5 adds Safety tests only. It changes no `src/`, adds no source candidate and does not modify `NodalOsCommonBoundaryClaimsCandidate`.

New D5-focused command:

```powershell
dotnet test tests/OneBrain.Safety.Tests/OneBrain.Safety.Tests.csproj --no-build --filter "FullyQualifiedName~NodalOsCommonBoundaryClaimsCandidateIsolationHardening" -v:minimal
```

Additional manual/discovery category commands:

```powershell
dotnet test tests/OneBrain.Safety.Tests/OneBrain.Safety.Tests.csproj --no-build --filter "TestCategory=SourceCandidate" -v:minimal
dotnet test tests/OneBrain.Safety.Tests/OneBrain.Safety.Tests.csproj --no-build --filter "TestCategory=NoRuntimeWiring" -v:minimal
dotnet test tests/OneBrain.Safety.Tests/OneBrain.Safety.Tests.csproj --no-build --filter "TestCategory=NoAuthority" -v:minimal
dotnet test tests/OneBrain.Safety.Tests/OneBrain.Safety.Tests.csproj --no-build --filter "TestCategory=NoDoubleTruth" -v:minimal
dotnet test tests/OneBrain.Safety.Tests/OneBrain.Safety.Tests.csproj --no-build --filter "TestCategory=CommonContracts" -v:minimal
dotnet test tests/OneBrain.Safety.Tests/OneBrain.Safety.Tests.csproj --no-build --filter "TestCategory=MappingAdapters" -v:minimal
dotnet test tests/OneBrain.Safety.Tests/OneBrain.Safety.Tests.csproj --no-build --filter "TestCategory=NodalOsTier1Safety" -v:minimal
```

Expected after D5:

- `FullyQualifiedName~NodalOsCommonBoundaryClaimsCandidateIsolationHardening`: 14 tests.
- `TestCategory=NoAuthority`: 14 tests.
- `TestCategory=NoDoubleTruth`: 14 tests.

These are manual/discovery commands, not CI enforcement. CI enforcement remains 0%. Runtime/product enablement remains 0%. Release/commercial remains 0% / NO-GO.

## 17. Future Implementation Options

Safe next blocks:

- `NODAL_OS_BLOCK_C7_PRE_REFACTOR_GATE_SCRIPT_TEST_ONLY_DISABLED`: add a disabled/local-only helper script for the documented gate, with no CI wiring.
- `NODAL_OS_BLOCK_D6_MINIMAL_REPLACEMENT_PLAN_AUDIT_ONLY`: plan one future replacement path, docs/audit-only, before touching existing source behavior.
- `STOP_FOR_AUDIT`: pause for external/read-only audit if more confidence is needed before a replacement plan.

Do not proceed to source refactor, contract use, public/product exposure or release/commercial readiness from this document alone.

## 18. D6 Minimal Replacement Plan Audit Note

D6 completed as docs/audit/plan-only in `docs/architecture/nodal-os-d6-minimal-replacement-plan-audit.md`.

Selected future D7:

`AUTHORIZE_NODAL_OS_BLOCK_D7_MINIMAL_REPLACEMENT_IMPLEMENTATION_NO_RUNTIME_CHANGE`

Selected D7 target:

`src/OneBrain.Core/Approval/ReentryDecisionPacketReadOnly.cs`

Required future D7 focused commands:

```powershell
dotnet test tests/OneBrain.Safety.Tests/OneBrain.Safety.Tests.csproj --no-build --filter "FullyQualifiedName~ReentryDecisionPacketReadOnly" -v:minimal
dotnet test tests/OneBrain.Recipes.Tests/OneBrain.Recipes.Tests.csproj --no-build --filter "FullyQualifiedName~ReentryDecisionPacketReadOnly" -v:minimal
dotnet test tests/OneBrain.Safety.Tests/OneBrain.Safety.Tests.csproj --no-build --filter "FullyQualifiedName~NodalOsCommonBoundaryClaimsCandidate" -v:minimal
rg -n "NodalOsCommonBoundaryClaimsCandidate" src/OneBrain.Pilot src/OneBrain.Cli .github azure-pipelines.yml
rg -n "NodalOsCommonBoundaryClaimsCandidate" src -g "*.cs"
```

After a future D7, the second `rg` command may only show:

- `src/OneBrain.Core/Approval/NodalOsCommonBoundaryClaimsCandidate.cs`
- `src/OneBrain.Core/Approval/ReentryDecisionPacketReadOnly.cs`

D6 itself did not change `src/`, tests, CI, runtime/product behavior, source bloat or release/commercial status.

## 19. D7 Minimal Replacement Implementation Note

D7 completed as source-minimal/no-runtime-behavior-change in `NODAL_OS_BLOCK_D7_MINIMAL_REPLACEMENT_IMPLEMENTATION_NO_RUNTIME_CHANGE`.

Actual source target:

`src/OneBrain.Core/Approval/ReentryDecisionPacketReadOnly.cs`

Actual focused D7 command:

```powershell
dotnet test tests/OneBrain.Safety.Tests/OneBrain.Safety.Tests.csproj --no-build --filter "FullyQualifiedName~ReentryDecisionPacketReadOnlyCommonBoundaryD7" -v:minimal
```

Expected D7 focused count: 12 tests.

D7 reference rule:

```powershell
rg -n "NodalOsCommonBoundaryClaimsCandidate" src -g "*.cs"
```

After D7, this command may show only:

- `src/OneBrain.Core/Approval/NodalOsCommonBoundaryClaimsCandidate.cs`
- `src/OneBrain.Core/Approval/ReentryDecisionPacketReadOnly.cs`

D7 does not change CI. Tier 1 remains manual/discovery-only. Runtime/product enablement remains `0%`. Release/commercial remains `0% / NO-GO`.

## 20. D8 Post-Replacement Isolation/Equivalence Audit Note

D8 completed as test/audit/docs-only in `NODAL_OS_BLOCK_D8_POST_REPLACEMENT_ISOLATION_EQUIVALENCE_AUDIT`.

Actual focused D8 command:

```powershell
dotnet test tests/OneBrain.Safety.Tests/OneBrain.Safety.Tests.csproj --no-build --filter "FullyQualifiedName~ReentryDecisionPacketReadOnlyPostReplacementD8" -v:minimal
```

Expected D8 focused count: 10 tests.

New manual/discovery category command:

```powershell
dotnet test tests/OneBrain.Safety.Tests/OneBrain.Safety.Tests.csproj --no-build --filter "TestCategory=PostReplacementAudit" -v:minimal
```

D8 reference rule:

```powershell
rg -n "NodalOsCommonBoundaryClaimsCandidate" src -g "*.cs"
```

After D8, this command may still show only:

- `src/OneBrain.Core/Approval/NodalOsCommonBoundaryClaimsCandidate.cs`
- `src/OneBrain.Core/Approval/ReentryDecisionPacketReadOnly.cs`

D8 changes no `src/`, no CI and no runtime/product behavior. It does not implement a second replacement. The D7 command guard exception remains exact to `ReentryDecisionPacketReadOnly.cs`. Tier 1 remains manual/discovery-only. Runtime/product enablement remains `0%`. CI enforcement remains `0%`. Release/commercial remains `0% / NO-GO`.

## 21. D9 Second Minimal Replacement Plan Audit Note

D9 completed as docs/audit/plan-only in `NODAL_OS_BLOCK_D9_SECOND_MINIMAL_REPLACEMENT_PLAN_AUDIT_ONLY`.

Canonical D9 plan:

`docs/architecture/nodal-os-d9-second-minimal-replacement-plan-audit.md`

Selected future D10:

`AUTHORIZE_NODAL_OS_BLOCK_D10_SECOND_MINIMAL_REPLACEMENT_IMPLEMENTATION_NO_RUNTIME_CHANGE`

Selected future D10 target:

`src/OneBrain.Core/Approval/ApprovalExecutionDesignOnlyProtected.cs`

Required future D10 focused commands:

```powershell
dotnet test tests/OneBrain.Safety.Tests/OneBrain.Safety.Tests.csproj --no-build --filter "FullyQualifiedName~ApprovalExecutionDesignOnlyProtected" -v:minimal
dotnet test tests/OneBrain.Recipes.Tests/OneBrain.Recipes.Tests.csproj --no-build --filter "FullyQualifiedName~ApprovalExecutionDesignOnlyProtected" -v:minimal
dotnet test tests/OneBrain.Safety.Tests/OneBrain.Safety.Tests.csproj --no-build --filter "FullyQualifiedName~NodalOsCommonBoundaryClaimsCandidate" -v:minimal
dotnet test tests/OneBrain.Safety.Tests/OneBrain.Safety.Tests.csproj --no-build --filter "FullyQualifiedName~ReentryDecisionPacketReadOnly" -v:minimal
dotnet test tests/OneBrain.Safety.Tests/OneBrain.Safety.Tests.csproj --no-build --filter "TestCategory=NoAuthority" -v:minimal
dotnet test tests/OneBrain.Safety.Tests/OneBrain.Safety.Tests.csproj --no-build --filter "TestCategory=NoDoubleTruth" -v:minimal
dotnet test tests/OneBrain.Safety.Tests/OneBrain.Safety.Tests.csproj --no-build --filter "TestCategory=PostReplacementAudit" -v:minimal
rg -n "NodalOsCommonBoundaryClaimsCandidate" src/OneBrain.Pilot src/OneBrain.Cli .github azure-pipelines.yml
rg -n "NodalOsCommonBoundaryClaimsCandidate" src -g "*.cs"
rg -n "IServiceCollection|AddSingleton|AddScoped|AddTransient|MapGet|MapPost|ICommandHandler|CommandHandler|Process\\.Start|File\\.Write|Directory\\.CreateDirectory|HttpClient|DbContext|MigrationBuilder" src/OneBrain.Core/Approval/ApprovalExecutionDesignOnlyProtected.cs
```

After a future D10, the candidate source reference command may show only:

- `src/OneBrain.Core/Approval/NodalOsCommonBoundaryClaimsCandidate.cs`
- `src/OneBrain.Core/Approval/ReentryDecisionPacketReadOnly.cs`
- `src/OneBrain.Core/Approval/ApprovalExecutionDesignOnlyProtected.cs`

D9 itself changed no `src/`, no tests, no CI and no runtime/product behavior. It did not implement the second replacement. D10 remained unauthorized until explicit Diego GO and was later executed as the next section records. Tier 1 remains manual/discovery-only. Runtime/product enablement remains `0%`. CI enforcement remains `0%`. Release/commercial remains `0% / NO-GO`.

## 22. D10 Second Minimal Replacement Implementation Note

D10 completed as source-minimal/proof-only/no-runtime-behavior-change in `NODAL_OS_BLOCK_D10_SECOND_MINIMAL_REPLACEMENT_IMPLEMENTATION_NO_RUNTIME_CHANGE`.

Actual source target:

`src/OneBrain.Core/Approval/ApprovalExecutionDesignOnlyProtected.cs`

Actual focused D10 command:

```powershell
dotnet test tests/OneBrain.Safety.Tests/OneBrain.Safety.Tests.csproj --no-build --filter "FullyQualifiedName~ApprovalExecutionDesignOnlyProtectedCommonBoundaryD10" -v:minimal
```

Expected D10 focused count: 15 tests.

New manual/discovery category command:

```powershell
dotnet test tests/OneBrain.Safety.Tests/OneBrain.Safety.Tests.csproj --no-build --filter "TestCategory=ApprovalExecution" -v:minimal
```

D10 reference rule:

```powershell
rg -n "NodalOsCommonBoundaryClaimsCandidate" src -g "*.cs"
```

After D10, this command may show only:

- `src/OneBrain.Core/Approval/NodalOsCommonBoundaryClaimsCandidate.cs`
- `src/OneBrain.Core/Approval/ReentryDecisionPacketReadOnly.cs`
- `src/OneBrain.Core/Approval/ApprovalExecutionDesignOnlyProtected.cs`

D10 does not change CI and does not enable runtime/product behavior. The D10 command/execution wording exception is exact to `ApprovalExecutionDesignOnlyProtected.cs`; it does not permit command handlers, shell/subprocess, runtime command execution, route/DI/service registration or similar future files. Tier 1 remains manual/discovery-only. Runtime/product enablement remains `0%`. CI enforcement remains `0%`. Release/commercial remains `0% / NO-GO`.

## 23. D11 Post-Second-Replacement Isolation/Equivalence Audit Note

D11 completed as test/audit/docs-only in `NODAL_OS_BLOCK_D11_POST_SECOND_REPLACEMENT_ISOLATION_EQUIVALENCE_AUDIT`.

Actual focused D11 command:

```powershell
dotnet test tests/OneBrain.Safety.Tests/OneBrain.Safety.Tests.csproj --no-build --filter "FullyQualifiedName~ApprovalExecutionPostSecondReplacementD11" -v:minimal
```

Expected D11 focused count: 12 tests.

Additional required D11 category commands:

```powershell
dotnet test tests/OneBrain.Safety.Tests/OneBrain.Safety.Tests.csproj --no-build --filter "TestCategory=PostReplacementAudit" -v:minimal
dotnet test tests/OneBrain.Safety.Tests/OneBrain.Safety.Tests.csproj --no-build --filter "TestCategory=NoAuthority" -v:minimal
dotnet test tests/OneBrain.Safety.Tests/OneBrain.Safety.Tests.csproj --no-build --filter "TestCategory=NoDoubleTruth" -v:minimal
dotnet test tests/OneBrain.Safety.Tests/OneBrain.Safety.Tests.csproj --no-build --filter "TestCategory=ApprovalExecution" -v:minimal
```

D11 reference rule:

```powershell
rg -n "NodalOsCommonBoundaryClaimsCandidate" src -g "*.cs"
```

After D11, this command may still show only:

- `src/OneBrain.Core/Approval/NodalOsCommonBoundaryClaimsCandidate.cs`
- `src/OneBrain.Core/Approval/ReentryDecisionPacketReadOnly.cs`
- `src/OneBrain.Core/Approval/ApprovalExecutionDesignOnlyProtected.cs`

D11 changes no `src/`, no CI and no runtime/product behavior. It does not implement a third replacement. The D10 command/execution wording exception remains exact to `ApprovalExecutionDesignOnlyProtected.cs`, and the D7/D10 exceptions remain independent file-exact exceptions rather than a broad command/execution allowlist.

Source bloat reduction remains `0%`; cumulative D7+D10 source impact remains net `+140` lines. Tier 1 remains manual/discovery-only. Runtime/product enablement remains `0%`. CI enforcement remains `0%`. Release/commercial remains `0% / NO-GO`.

## 24. D12 Source Reduction Plan/Audit Note

D12 completed as docs/audit/plan-only in `NODAL_OS_BLOCK_D12_SOURCE_REDUCTION_PLAN_AUDIT_ONLY`.

Canonical D12 plan:

```powershell
docs/architecture/nodal-os-d12-source-reduction-plan-audit.md
```

Selected future D13:

`AUTHORIZE_NODAL_OS_BLOCK_D13_MINIMAL_SOURCE_REDUCTION_IMPLEMENTATION_NO_RUNTIME_CHANGE`

Selected future D13 source target:

- `src/OneBrain.Core/Approval/ApprovalExecutionDesignOnlyProtected.cs`

Required future D13 focused commands:

```powershell
dotnet build src/OneBrain.Core/OneBrain.Core.csproj
dotnet build src/OneBrain.Pilot/OneBrain.Pilot.csproj
dotnet build OneBrain.slnx
dotnet test tests/OneBrain.Safety.Tests/OneBrain.Safety.Tests.csproj --no-build --filter "ProductLedger" -v:minimal
dotnet test tests/OneBrain.Recipes.Tests/OneBrain.Recipes.Tests.csproj --no-build --filter "ProductLedger" -v:minimal
dotnet test tests/OneBrain.Safety.Tests/OneBrain.Safety.Tests.csproj --no-build --filter "TestCategory=NodalOsTier1Safety" -v:minimal
dotnet test tests/OneBrain.Safety.Tests/OneBrain.Safety.Tests.csproj --no-build --filter "TestCategory=CommonContracts" -v:minimal
dotnet test tests/OneBrain.Safety.Tests/OneBrain.Safety.Tests.csproj --no-build --filter "TestCategory=SourceCandidate" -v:minimal
dotnet test tests/OneBrain.Safety.Tests/OneBrain.Safety.Tests.csproj --no-build --filter "TestCategory=NoRuntimeWiring" -v:minimal
dotnet test tests/OneBrain.Safety.Tests/OneBrain.Safety.Tests.csproj --no-build --filter "TestCategory=NoAuthority" -v:minimal
dotnet test tests/OneBrain.Safety.Tests/OneBrain.Safety.Tests.csproj --no-build --filter "TestCategory=NoDoubleTruth" -v:minimal
dotnet test tests/OneBrain.Safety.Tests/OneBrain.Safety.Tests.csproj --no-build --filter "TestCategory=PostReplacementAudit" -v:minimal
dotnet test tests/OneBrain.Safety.Tests/OneBrain.Safety.Tests.csproj --no-build --filter "TestCategory=ApprovalExecution" -v:minimal
dotnet test tests/OneBrain.Safety.Tests/OneBrain.Safety.Tests.csproj --no-build --filter "FullyQualifiedName~ApprovalExecutionDesignOnlyProtectedCommonBoundaryD10" -v:minimal
dotnet test tests/OneBrain.Safety.Tests/OneBrain.Safety.Tests.csproj --no-build --filter "FullyQualifiedName~ApprovalExecutionPostSecondReplacementD11" -v:minimal
```

D13 reference rule:

```powershell
rg -n "NodalOsCommonBoundaryClaimsCandidate" src -g "*.cs"
```

After D13, this command may still show only:

- `src/OneBrain.Core/Approval/NodalOsCommonBoundaryClaimsCandidate.cs`
- `src/OneBrain.Core/Approval/ReentryDecisionPacketReadOnly.cs`
- `src/OneBrain.Core/Approval/ApprovalExecutionDesignOnlyProtected.cs`

Required future D13 command/runtime scan:

```powershell
rg -n "IServiceCollection|AddSingleton|AddScoped|AddTransient|MapGet|MapPost|ICommandHandler|CommandHandler|Process\\.Start|File\\.Write|Directory\\.CreateDirectory|HttpClient|DbContext|MigrationBuilder" src/OneBrain.Core/Approval/ApprovalExecutionDesignOnlyProtected.cs src/OneBrain.Core/Approval/ReentryDecisionPacketReadOnly.cs
```

D12 changes no `src/`, no tests, no CI and no runtime/product behavior. It selects D13 as a one-file private helper compaction in the D10 target only. D13 must not touch the D4 candidate source, D7 source target, Product Ledger source behavior, routes, DI, service registration, command handlers or CI unless a future explicit scope says otherwise.

Source bloat reduction remains `0%` after D12; D7+D10 cumulative source impact remains net `+140` lines until a future D13 removes lines and passes gates. Tier 1 remains manual/discovery-only. Runtime/product enablement remains `0%`. CI enforcement remains `0%`. Release/commercial remains `0% / NO-GO`.

## 25. D13 Minimal Source Reduction Implementation Note

D13 completed as source-minimal/reduction-only/no-runtime-behavior-change in `NODAL_OS_BLOCK_D13_MINIMAL_SOURCE_REDUCTION_IMPLEMENTATION_NO_RUNTIME_CHANGE`.

Canonical D13 doc:

```powershell
docs/architecture/nodal-os-d13-minimal-source-reduction-implementation.md
```

Actual D13 source target:

- `src/OneBrain.Core/Approval/ApprovalExecutionDesignOnlyProtected.cs`

Actual D13 reduction:

- before D13: 368 lines;
- after D13: 338 lines;
- diff: `+22 / -52`, net `-30`.

Required future D14 focused commands:

```powershell
dotnet test tests/OneBrain.Safety.Tests/OneBrain.Safety.Tests.csproj --no-build --filter "FullyQualifiedName~ApprovalExecutionDesignOnlyProtected" -v:minimal
dotnet test tests/OneBrain.Recipes.Tests/OneBrain.Recipes.Tests.csproj --no-build --filter "FullyQualifiedName~ApprovalExecutionDesignOnlyProtected" -v:minimal
dotnet test tests/OneBrain.Safety.Tests/OneBrain.Safety.Tests.csproj --no-build --filter "FullyQualifiedName~ApprovalExecutionDesignOnlyProtectedCommonBoundaryD10" -v:minimal
dotnet test tests/OneBrain.Safety.Tests/OneBrain.Safety.Tests.csproj --no-build --filter "FullyQualifiedName~ApprovalExecutionPostSecondReplacementD11" -v:minimal
dotnet test tests/OneBrain.Safety.Tests/OneBrain.Safety.Tests.csproj --no-build --filter "TestCategory=PostReplacementAudit" -v:minimal
dotnet test tests/OneBrain.Safety.Tests/OneBrain.Safety.Tests.csproj --no-build --filter "TestCategory=NoAuthority" -v:minimal
dotnet test tests/OneBrain.Safety.Tests/OneBrain.Safety.Tests.csproj --no-build --filter "TestCategory=NoDoubleTruth" -v:minimal
dotnet test tests/OneBrain.Safety.Tests/OneBrain.Safety.Tests.csproj --no-build --filter "TestCategory=ApprovalExecution" -v:minimal
rg -n "NodalOsCommonBoundaryClaimsCandidate" src -g "*.cs"
rg -n "IServiceCollection|AddSingleton|AddScoped|AddTransient|MapGet|MapPost|ICommandHandler|CommandHandler|Process\\.Start|File\\.Write|Directory\\.CreateDirectory|HttpClient|DbContext|MigrationBuilder" src/OneBrain.Core/Approval/ApprovalExecutionDesignOnlyProtected.cs src/OneBrain.Core/Approval/ReentryDecisionPacketReadOnly.cs
```

D13 reference rule remains unchanged:

- `src/OneBrain.Core/Approval/NodalOsCommonBoundaryClaimsCandidate.cs`
- `src/OneBrain.Core/Approval/ReentryDecisionPacketReadOnly.cs`
- `src/OneBrain.Core/Approval/ApprovalExecutionDesignOnlyProtected.cs`

D13 changes no tests, no CI and no runtime/product behavior. D4 candidate source remains unchanged, D7 source target remains unchanged, D1/D2 remain design/test-only and existing hard-block tests remain authoritative.

Cumulative source impact after D13: D7 `+70`, D10 `+70`, D13 `-30`, net `+110`. Tier 1 remains manual/discovery-only. Runtime/product enablement remains `0%`. CI enforcement remains `0%`. Release/commercial remains `0% / NO-GO`.

## 26. D14 D-Series Value Checkpoint Note

D14 completed as docs/audit/checkpoint-only in `NODAL_OS_BLOCK_D14_D_SERIES_VALUE_CHECKPOINT_AND_POST_REDUCTION_AUDIT`.

Canonical D14 doc:

```powershell
docs/architecture/nodal-os-d14-d-series-value-checkpoint.md
```

D14 changes no commands, tests, CI, source, scanner behavior or runtime/product behavior. It adds no new test label requirement and does not modify the pre-refactor command gate.

D14 selected `CLOSE_D_SERIES_RETURN_TO_MAIN_ROADMAP` because D13 proved a real local reduction while the remaining obvious D-series compaction target is low-value. Future source simplification should return to the main roadmap rather than continue D-series proof-target work by default.

D-series source trajectory after D14 remains: D7 `+70`, D10 `+70`, D13 `-30`, net `+110`. Tier 1 remains manual/discovery-only. Runtime/product enablement remains `0%`. CI enforcement remains `0%`. Release/commercial remains `0% / NO-GO`.

## 27. E1 Main Roadmap Rebaseline Note

E1 completed as docs-only/roadmap-only/rebaseline-only in `NODAL_OS_BLOCK_E1_MAIN_ROADMAP_REBASELINE_AFTER_D_SERIES_DOCS_ONLY`.

Canonical E1 doc:

```powershell
docs/architecture/nodal-os-e1-main-roadmap-rebaseline-after-d-series.md
```

E1 changes no commands, tests, CI, source, scanner behavior or runtime/product behavior. It adds no new test label requirement and does not modify the pre-refactor command gate.

E1 selected `NODAL_OS_BLOCK_E2_PRODUCT_LEDGER_LOCAL_DEV_SAFETY_BACKLOG_RECONCILIATION_DOCS_TEST_ONLY` because the Product Ledger local/dev backlog is now the largest safe main-roadmap ordering problem after D-series closure.

Tier 1 remains manual/discovery-only. Runtime/product enablement remains `0%`. CI enforcement remains `0%`. Release/commercial remains `0% / NO-GO`. E1 recommends E2 but does not authorize E2.

## 28. E2 Product Ledger Local/Dev Backlog Canon Note

E2 completed as docs-only/backlog-reconciliation-only in `NODAL_OS_BLOCK_E2_PRODUCT_LEDGER_LOCAL_DEV_SAFETY_BACKLOG_RECONCILIATION_DOCS_TEST_ONLY`.

Canonical E2 doc:

```powershell
docs/architecture/nodal-os-product-ledger-local-dev-safety-backlog-canon.md
```

Product Ledger local/dev gate commands remain manual/discovery-only:

```powershell
dotnet test tests/OneBrain.Safety.Tests/OneBrain.Safety.Tests.csproj --no-build --filter "ProductLedger" -v:minimal
dotnet test tests/OneBrain.Recipes.Tests/OneBrain.Recipes.Tests.csproj --no-build --filter "ProductLedger" -v:minimal
dotnet test tests/OneBrain.Safety.Tests/OneBrain.Safety.Tests.csproj --no-build --filter "TestCategory=NodalOsTier1Safety" -v:minimal
dotnet test tests/OneBrain.Safety.Tests/OneBrain.Safety.Tests.csproj --no-build --filter "TestCategory=PublicProductBlock" -v:minimal
dotnet test tests/OneBrain.Safety.Tests/OneBrain.Safety.Tests.csproj --no-build --filter "TestCategory=ProductionRouteBlock" -v:minimal
dotnet test tests/OneBrain.Safety.Tests/OneBrain.Safety.Tests.csproj --no-build --filter "TestCategory=NoRuntimeWiring" -v:minimal
dotnet test tests/OneBrain.Safety.Tests/OneBrain.Safety.Tests.csproj --no-build --filter "TestCategory=NoAuthority" -v:minimal
dotnet test tests/OneBrain.Safety.Tests/OneBrain.Safety.Tests.csproj --no-build --filter "TestCategory=NoDoubleTruth" -v:minimal
dotnet test tests/OneBrain.Safety.Tests/OneBrain.Safety.Tests.csproj --no-build --filter "FullyQualifiedName~NodalOsStaticGuardCatalogTests" -v:minimal
```

E2 adds no CI enforcement, no test labels and no new Product Ledger source behavior. Passing these commands is safety evidence only; it is not public/product readiness. Runtime/product enablement remains `0%`. CI enforcement remains `0%`. Release/commercial remains `0% / NO-GO`.

## 29. E5 Product Ledger Local/Dev Canon Guard

E5 adds one focused Safety test class:

`tests/OneBrain.Safety.Tests/ProductLedgerLocalDevCanonGuardTests.cs`

Manual/discovery-only focused command:

```powershell
dotnet test tests/OneBrain.Safety.Tests/OneBrain.Safety.Tests.csproj --no-build --filter "FullyQualifiedName~ProductLedgerLocalDevCanonGuardTests" -v:minimal
```

Related manual/discovery category commands:

```powershell
dotnet test tests/OneBrain.Safety.Tests/OneBrain.Safety.Tests.csproj --no-build --filter "TestCategory=ProductLedger" -v:minimal
dotnet test tests/OneBrain.Safety.Tests/OneBrain.Safety.Tests.csproj --no-build --filter "TestCategory=NodalOsTier1Safety" -v:minimal
dotnet test tests/OneBrain.Safety.Tests/OneBrain.Safety.Tests.csproj --no-build --filter "TestCategory=NoRuntimeWiring" -v:minimal
dotnet test tests/OneBrain.Safety.Tests/OneBrain.Safety.Tests.csproj --no-build --filter "TestCategory=NoAuthority" -v:minimal
dotnet test tests/OneBrain.Safety.Tests/OneBrain.Safety.Tests.csproj --no-build --filter "TestCategory=NoDoubleTruth" -v:minimal
dotnet test tests/OneBrain.Safety.Tests/OneBrain.Safety.Tests.csproj --no-build --filter "TestCategory=ReleaseCommercialBlock" -v:minimal
```

These commands are not CI enforcement. Passing the E5 canon guard does not mean Product Ledger product readiness, public/product readiness, Production route readiness, latest pointer readiness, read precedence readiness, product authority or release/commercial readiness. Runtime/product enablement remains `0%`; CI enforcement remains `0%`; Tier 1 remains manual/discovery-only.

## 30. E6 Product Ledger Local/Dev External Audit Packet

E6 adds the read-only audit packet:

`docs/audit/product-ledger-local-dev/README.md`

The packet references the exact manual validation commands in:

`docs/audit/product-ledger-local-dev/validation-commands.md`

These commands remain manual/discovery-only. They include Product Ledger Safety, Product Ledger Recipes, `ProductLedgerLocalDevCanonGuardTests`, `TestCategory=NodalOsTier1Safety`, `TestCategory=ProductLedger`, `TestCategory=NoRuntimeWiring`, `TestCategory=NoAuthority`, `TestCategory=NoDoubleTruth`, `TestCategory=ReleaseCommercialBlock`, `NodalOsStaticGuardCatalogTests`, `TestCategory=PublicProductBlock`, `TestCategory=ProductionRouteBlock` and MSTest discovery commands.

E6 does not submit an external audit, does not add CI enforcement and does not imply Product Ledger product readiness. Runtime/product enablement remains `0%`; release/commercial remains `0% / NO-GO`.
