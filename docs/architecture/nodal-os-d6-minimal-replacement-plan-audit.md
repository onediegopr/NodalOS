# NODAL OS D6 Minimal Replacement Plan Audit

Date: 2026-07-07

Mode: audit-only / plan-only / docs-only.

D6 changed no `src/`, no tests and no CI. D6 did not implement replacement, did not reduce source bloat and did not wire `NodalOsCommonBoundaryClaimsCandidate` into runtime/product. The D4 candidate remains parallel, fail-closed and non-authoritative until a future explicitly authorized D7 narrows one source use.

## Decision

Decision: `GO_WITH_FINDINGS_MINIMAL_REPLACEMENT_PLAN_AUDIT_ONLY_READY`.

Selected next recommendation:

`AUTHORIZE_NODAL_OS_BLOCK_D7_MINIMAL_REPLACEMENT_IMPLEMENTATION_NO_RUNTIME_CHANGE`

D7 was not authorized by this document. It required a separate explicit Diego authorization before any source or test change.

## Read Evidence

| Evidence | Current state |
| --- | --- |
| D1 common contracts | Safety-test-only, descriptive and not production authority. |
| D2 mapping adapter | Safety-test-only, fail-closed and not production authority. |
| D4 source candidate | `src/OneBrain.Core/Approval/NodalOsCommonBoundaryClaimsCandidate.cs`, parallel-only and non-authoritative. |
| D5 hardening | Safety tests prove no runtime reference, no authority and no double truth. |
| Reentry packet | `ReentryDecisionPacketReadOnly` is read-only, fixture-safe, non-route and covered by Safety/Recipes tests. |
| Product Ledger latest-state candidates | Valuable future targets, but Product Ledger-facing and therefore not first D7 under this audit. |

## D7 Target Inventory

| Target | Source file/class | Duplicated concept | Runtime-facing | Product/public-facing | Product Ledger-facing | Approval/read-only only | Route behavior | Command execution | Release/commercial | Current tests | D1/D2/D4/D5 coverage | Risk | Expected bloat reduction | Verdict |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| Reentry read-only common boundary proof | `src/OneBrain.Core/Approval/ReentryDecisionPacketReadOnly.cs` / `ReentryDecisionPacketReadOnly` | runtime/product/service/command/provider/DB/release no-go proof overlaps D4 blocked claims | No | No | No | Yes | No | No | Blocks release/commercial | `ReentryDecisionPacketReadOnlySafetyTests`, `ReentryDecisionPacketReadOnlyTests` | D4 candidate covers public/product, Production route, command, provider/cloud, DB, external trust, release/commercial and runtime/product no-go; D5 guards must be narrowed for one allowed source reference | P3 | Tiny, 0.2-0.5% source noise only | Select for future D7. |
| Latest-state auxiliary evidence | `ProductLedgerLocalDurableLatestStateAuxiliaryEvidencePresenter` | auxiliary/not authority/not precedence/latest pointer | No direct external runtime, but Product Ledger source behavior | Internal product-line surface | Yes | Partially | Route-adjacent through operator surface | No | Blocks release/commercial | Tier 1/Product Ledger Safety/Recipes | Strong hard-block coverage | P2/P3 | Moderate | Defer; Product Ledger-facing. |
| Latest-state reader candidate | `ProductLedgerLocalDurableLatestStateReaderCandidateValidator` | reader candidate not authority/read precedence/latest pointer | No direct external runtime, but Product Ledger source behavior | Internal product-line surface | Yes | Partially | Route-adjacent through read model | No | Blocks release/commercial | Safety/Recipes latest-state tests | Strong hard-block coverage | P2/P3 | Moderate | Defer; Product Ledger-facing. |
| Internal operator UI preview | `ProductLedgerInternalOperatorUiPreview` | internal-only read-only preview boundary | Product surface adjacent | Internal UI/product preview | Yes | No | Route/render adjacent | No | Blocks release/commercial | UI/route/surface tests | Partial | P3 | Moderate | Defer; surface drift risk. |
| Renderable operator surface | `ProductLedgerRenderableOperatorSurface` | render model plus disabled action claims | Product surface adjacent | Internal render surface | Yes | No | Render/DOM adjacent | No | Blocks release/commercial | Recipes/render tests | Partial | P3 | Moderate | Defer; DOM/render drift risk. |
| Product Ledger local dev route preview | `ProductLedgerLocalDevRoutePreview` | local route preview and boundary claims | Yes, route-facing | Internal route/product surface | Yes | No | Yes | No | Blocks release/commercial | broad Safety/Recipes/HTTP tests | Partial | P2/P3 | Larger | Do not select first. |
| Path readiness scaffold | `ProductLedgerPathReadinessScaffold` | path readiness blockers and authority preview | Product Ledger path policy | Internal product-line path | Yes | No | No | No | Blocks external trust | path readiness/canonicalization tests | Partial | P3 | Moderate | Defer; path boundary is load-bearing. |
| Handoff draft writer variants | handoff draft executors | writer mode and path boundary | Writer/source IO | Local workspace action | Yes | No | Route-adjacent | Possible action semantics | Blocks release/commercial | handoff/path tests | Partial | P2/P3 | Larger | Do not select first. |
| Static guard source promotion | current `NodalOsStaticGuardCatalog` in tests | static hard-block categories | No | No | Indirect | Test-only today | No | No | Blocks release/commercial | static guard tests | Strong | P3 | Moderate | Defer; do not promote test helper yet. |
| `/run` claim coherence status | Pilot/Product Ledger separation docs/tests | runtime overclaim prevention | Pilot `/run` adjacent | No | Indirect | No | Runtime-adjacent | No | No release claim | claim coherence tests/docs | D2 maps PilotRunCoupling | P3 | Small | Defer; runtime wording risk. |

## Selected D7 Plan

Block name:

`AUTHORIZE_NODAL_OS_BLOCK_D7_MINIMAL_REPLACEMENT_IMPLEMENTATION_NO_RUNTIME_CHANGE`

Exact source target:

- `src/OneBrain.Core/Approval/ReentryDecisionPacketReadOnly.cs`

Allowed files to change in D7:

- `src/OneBrain.Core/Approval/ReentryDecisionPacketReadOnly.cs`
- `tests/OneBrain.Safety.Tests/ReentryDecisionPacketReadOnlySafetyTests.cs`
- `tests/OneBrain.Safety.Tests/NodalOsCommonBoundaryClaimsCandidateIsolationHardeningTests.cs`
- `tests/OneBrain.Recipes.Tests/ReentryDecisionPacketReadOnlyTests.cs` only if output invariants need an explicit unchanged assertion
- docs/logs for D7

Forbidden files and areas in D7:

- `src/OneBrain.Pilot/**`
- any route mapper, endpoint mapper or `Program.cs`
- Product Ledger writer, latest-state, path, route, UI or action source behavior
- service registration, DI, command handlers, shell/subprocess, CI workflows or build scripts
- DB/migration, provider/cloud/network, KMS/WORM/external trust
- public/product enablement, Production route activation, active read precedence, latest pointer, product authority or release/commercial readiness

Candidate usage rule:

- D7 may reference `NodalOsCommonBoundaryClaimsCandidate.DefaultBlocked()` only inside `ReentryDecisionPacketReadOnly.cs` as a local read-only proof input.
- The candidate must not decide runtime behavior, route availability, command execution, Product Ledger writer state or release readiness.
- Existing `ReentryDecisionPacketReadOnly` counters/statuses remain authoritative for the reentry packet.
- The candidate can only replace or centralize the duplicated common-boundary subset of the proof: service/command/product/provider/DB/release no-go signals that already remain false/blocked.
- If the replacement requires editing Product Ledger source, routes, DI, command handlers or writer behavior, D7 must stop.

Tests to add/update in D7:

- Focused Safety test that the reentry packet still passes `PassesSafetyProof`.
- Focused Safety test that the D4 candidate is referenced only by its own source file plus the single D7 target and approved tests/docs.
- Focused Safety test that the D7 target still has no route/DI/service/command/product/public/CI references.
- Focused Safety test that unsafe candidate authority flags cannot make the reentry packet pass a common-boundary proof.
- Existing Reentry Safety/Recipes tests must continue to pass with unchanged observable packet values.

Tests forbidden to modify in D7:

- Product Ledger Safety assertions unrelated to D7.
- Product Ledger Recipes assertions unrelated to D7.
- D1/D2 mapper semantics unless only adding an explicit comparison assertion.
- Static guard catalog behavior or scanner hard-fail token lists.
- Any test movement, skip behavior, suite removal or CI filter.

Required D7 commands:

```powershell
dotnet build src/OneBrain.Core/OneBrain.Core.csproj --no-restore -v:minimal
dotnet build src/OneBrain.Pilot/OneBrain.Pilot.csproj --no-restore -v:minimal
dotnet build OneBrain.slnx --no-restore -v:minimal
dotnet test tests/OneBrain.Safety.Tests/OneBrain.Safety.Tests.csproj --no-build --filter "FullyQualifiedName~ReentryDecisionPacketReadOnly" -v:minimal
dotnet test tests/OneBrain.Recipes.Tests/OneBrain.Recipes.Tests.csproj --no-build --filter "FullyQualifiedName~ReentryDecisionPacketReadOnly" -v:minimal
dotnet test tests/OneBrain.Safety.Tests/OneBrain.Safety.Tests.csproj --no-build --filter "FullyQualifiedName~NodalOsCommonBoundaryClaimsCandidate" -v:minimal
dotnet test tests/OneBrain.Safety.Tests/OneBrain.Safety.Tests.csproj --no-build --filter "TestCategory=NodalOsTier1Safety" -v:minimal
dotnet test tests/OneBrain.Safety.Tests/OneBrain.Safety.Tests.csproj --no-build --filter "TestCategory=CommonContracts" -v:minimal
dotnet test tests/OneBrain.Safety.Tests/OneBrain.Safety.Tests.csproj --no-build --filter "TestCategory=DesignOnly" -v:minimal
dotnet test tests/OneBrain.Safety.Tests/OneBrain.Safety.Tests.csproj --no-build --filter "TestCategory=SourceCandidate" -v:minimal
dotnet test tests/OneBrain.Safety.Tests/OneBrain.Safety.Tests.csproj --no-build --filter "TestCategory=NoRuntimeWiring" -v:minimal
dotnet test tests/OneBrain.Safety.Tests/OneBrain.Safety.Tests.csproj --no-build --filter "TestCategory=NoAuthority" -v:minimal
dotnet test tests/OneBrain.Safety.Tests/OneBrain.Safety.Tests.csproj --no-build --filter "TestCategory=NoDoubleTruth" -v:minimal
dotnet test tests/OneBrain.Safety.Tests/OneBrain.Safety.Tests.csproj --no-build --filter "FullyQualifiedName~ProductLedger" -v:minimal
dotnet test tests/OneBrain.Recipes.Tests/OneBrain.Recipes.Tests.csproj --no-build --filter "FullyQualifiedName~ProductLedger" -v:minimal
rg -n "NodalOsCommonBoundaryClaimsCandidate" src/OneBrain.Pilot src/OneBrain.Cli .github azure-pipelines.yml
rg -n "NodalOsCommonBoundaryClaimsCandidate" src -g "*.cs"
git diff --check
git status --short
git rev-list --left-right --count HEAD...'@{u}'
```

Expected `rg -n "NodalOsCommonBoundaryClaimsCandidate" src -g "*.cs"` after D7:

- `src/OneBrain.Core/Approval/NodalOsCommonBoundaryClaimsCandidate.cs`
- `src/OneBrain.Core/Approval/ReentryDecisionPacketReadOnly.cs`

Expected diff size target:

- one small source edit in the D7 target;
- focused test edit only where needed to preserve the D5 allowed-reference guard;
- docs/log updates;
- no route/DI/product/runtime files.

Expected bloat reduction target:

- tiny and intentionally conservative: 0.2-0.5% of source noise, or 0% if the safe implementation is additive proof-only.
- No deletion of old contracts in D7.

Rollback strategy:

- revert the D7 source edit in `ReentryDecisionPacketReadOnly.cs`;
- revert D7-focused test updates;
- keep D4 candidate source and D1/D2/D5 evidence intact;
- rerun the D7 command set.

GO criteria for D7:

- explicit Diego authorization for D7;
- source change limited to `ReentryDecisionPacketReadOnly.cs`;
- no observable packet output drift unless deliberately documented and tested;
- all required D7 commands pass;
- no runtime/product, route, DI, service registration, command handler, public/product, Product Ledger writer, DB/migration, provider/cloud/network, KMS/WORM/external trust or release/commercial drift;
- D4 candidate remains fail-closed and non-authoritative outside the one D7 local proof.

NO-GO criteria for D7:

- any need to touch Product Ledger source behavior;
- any route/DI/command-handler/CI reference;
- any failing Reentry, D4/D5, Product Ledger Safety or Product Ledger Recipes gate;
- any ambiguity about whether D4 becomes product/runtime authority;
- any source edit that broadens runtime, product, release or command semantics.

Revert criteria:

- `NodalOsCommonBoundaryClaimsCandidate` appears in Pilot, CLI, routes, DI, command handlers, Product Ledger runtime or CI;
- `ReentryDecisionPacketReadOnly` exposes product/runtime action, state mutation, export, DB, provider/cloud/network or release readiness;
- existing Reentry Safety/Recipes outputs drift without explicit expected-output tests;
- D1/D2/D4/D5 no-authority/no-double-truth tests fail.

## Proof Of Unchanged Boundaries

D6 only planned the replacement. No source, tests or CI changed.

For future D7, runtime/product behavior remains unchanged because:

- the target is a Core read-only fixture packet, not a route, service, command handler or writer;
- the candidate can only be used inside an existing boolean safety proof;
- existing counters and capability status fields remain the packet authority;
- no Pilot, CLI, Product Ledger writer, route, DB/provider/cloud/KMS/WORM or release path is in scope.

Existing hard-block authorities remain authoritative:

- D1 and D2 stay test/design-only;
- D4 remains fail-closed and non-authoritative;
- D5 no-authority/no-double-truth tests must be updated only to permit the single explicit D7 source reference;
- Product Ledger Safety/Recipes remain required and cannot be replaced by the D7 focused tests.

## Current Progress

- D6 minimal replacement plan/audit: 100%.
- Common contracts confidence: 94%.
- Source refactor readiness: 50%.
- Source bloat reduction: 0%.
- Tier 1 coverage estimate: 78 tests / partial.
- Manual pre-refactor gate reproducibility: 95%.
- CI enforcement: 0%.
- Runtime/product enablement: 0%.
- Release/commercial readiness: 0% / NO-GO.

## Risks

P0=0, P1=0, P2=0.

P3:

- D7 can accidentally turn the D4 candidate into authority if it replaces packet-local counters instead of only centralizing a duplicated common-boundary proof.
- The D5 allowed-reference guard must be narrowed carefully so one D7 source reference does not permit broad source adoption.
- Reentry wording is historical and read-only; a source edit can create output drift if not covered by existing Safety/Recipes tests.

P4:

- Source bloat reduction remains effectively zero until D7 or later source-facing blocks.
- Product Ledger latest-state/handoff/writer duplication remains deferred.
- Tier 1 is still manual/discovery-only and partial.

## D6 Non-Goals Preserved

- no replacement implemented;
- no `src/` changes;
- no tests changed;
- no CI changed;
- no source bloat reduction claimed;
- no runtime/product enablement;
- no public/product or Production route;
- no latest pointer, active read precedence or product authority;
- no command execution, shell/subprocess, DB/migration, provider/cloud/network or KMS/WORM/external trust;
- no release/commercial readiness.

## Next Recommended Block

`AUTHORIZE_NODAL_OS_BLOCK_D7_MINIMAL_REPLACEMENT_IMPLEMENTATION_NO_RUNTIME_CHANGE`

D7 must require explicit Diego authorization.

## D7 Implementation Note

D7 was later authorized and executed as `NODAL_OS_BLOCK_D7_MINIMAL_REPLACEMENT_IMPLEMENTATION_NO_RUNTIME_CHANGE`.

Actual D7 source change:

- `src/OneBrain.Core/Approval/ReentryDecisionPacketReadOnly.cs`

Actual D7 tests:

- `tests/OneBrain.Safety.Tests/ReentryDecisionPacketReadOnlyCommonBoundaryD7Tests.cs`
- `tests/OneBrain.Safety.Tests/NodalOsCommonBoundaryClaimsCandidateIsolationHardeningTests.cs`

D7 references `NodalOsCommonBoundaryClaimsCandidate.DefaultBlocked()` only inside `ReentryDecisionPacketReadOnly.cs` as a private, local, read-only fail-closed proof used by `PassesSafetyProof`. The D4 candidate remains non-authoritative: existing reentry counters/statuses, D1/D2 test-only evidence and hard-block tests remain the authority. The D5 allowed-reference guard was narrowed to exactly one additional source file, the selected D7 target, and was not broadened to Product Ledger, route, DI, command, CI or runtime paths.

Runtime/product behavior remains unchanged:

- no route/DI/service registration;
- no command handler;
- no Product Ledger runtime/latest-state/handoff/writer change;
- no public/product exposure;
- no Production route;
- no latest pointer, read precedence or product authority;
- no DB/provider/cloud/network/KMS/WORM/external trust;
- no release/commercial readiness.

Actual bloat impact:

- source bloat reduction remains effectively `0%`.
- D7 is additive proof-only: it adds a private common-boundary proof in the selected source target and focused tests rather than deleting existing contracts.

Next recommended block after D7:

`D8 post-replacement isolation/equivalence audit`.
