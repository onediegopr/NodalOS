# NODAL OS D3 Source Refactor Plan Audit

Date: 2026-07-07

Mode: audit-only / plan-only / docs-only. This block does not modify `src/`, tests, CI, runtime behavior, public/product exposure, Production routes, latest pointer, read precedence, product authority, command execution, provider/cloud/network, DB/migration, KMS/WORM/external trust, release or commercial readiness.

## 1. Decision

Decision: `GO_WITH_FINDINGS_SOURCE_REFACTOR_PLAN_AUDIT_ONLY_READY`.

D3 reviewed D1/D2 common-contract evidence and current Product Ledger source shape. The first future source-facing move should be a minimal parallel source candidate for common boundary claims, not a migration of an existing Product Ledger model.

Selected future D4 candidate:

`AUTHORIZE_NODAL_OS_BLOCK_D4_MINIMAL_SOURCE_CANDIDATE_NO_RUNTIME_WIRING`

Recommended D4 working name:

`NodalOsCommonBoundaryClaimsCandidate`

D4 is not authorized by this document. It requires explicit Diego authorization before any source file is added or changed.

## 2. Current Evidence

| Evidence | Current state |
| --- | --- |
| D1 common contracts | Safety-test-only candidate contract surface exists and remains non-authoritative. |
| D2 mapping adapters | Safety-test-only mapper proves hard-block concepts map to blocked common claims. |
| Tier 1 | Manual/discovery-only, 45 tests after D2. |
| CommonContracts | 19/19 pass after D2. |
| MappingAdapters | 14/14 pass after D2. |
| Product Ledger Safety | 272/272 pass after D2. |
| Product Ledger Recipes | 72/72 pass after D2. |
| CI enforcement | 0%. |
| Runtime/product enablement | 0%. |
| Release/commercial readiness | 0% / NO-GO. |

## 3. Candidate Inventory

| Candidate | Files/classes | Duplicated concept | Current protection | D1/D2 coverage | Risk | First move verdict |
| --- | --- | --- | --- | --- | --- | --- |
| Minimal parallel common boundary claims source candidate | New future file, likely `src/OneBrain.Core/Approval/NodalOsCommonBoundaryClaimsCandidate.cs` | blocked boundary claims, evidence boundary, no runtime/product authority | D1/D2 tests, Tier 1, Product Ledger Safety/Recipes, static guard checks | direct match to D1/D2 | P3 if mistaken for authority | Recommended D4 first move. |
| Internal operator UI preview boundary/status extraction | `ProductLedgerInternalOperatorUiPreview.cs` | local/internal preview, disabled actions, no public/product/release | Safety/Recipes internal preview tests, public/product guards | partial | P3/P4 route-surface drift risk | Defer until source candidate exists. |
| Renderable operator surface model extraction | `ProductLedgerRenderableOperatorSurface.cs` | surface render model and no external/release claims | Recipes render tests, route/render tests | partial | P3 DOM/render drift | Defer. |
| Product Ledger local dev route preview consolidation | `ProductLedgerLocalDevRoutePreview.cs` | route preview, nested surface state, local/dev boundary | broad Safety/Recipes/HTTP route tests | partial | P2/P3 because route-adjacent and large | Not first move. |
| Path readiness guard result extraction | `ProductLedgerPathReadinessScaffold.cs` | guard evaluation, canonicalization/reparse/authority previews | Path readiness/canonicalization tests | partial | P3 path boundary drift | Defer until boundary claim candidate exists. |
| Latest-state auxiliary evidence extraction | `ProductLedgerLocalDurableLatestStateAuxiliaryEvidencePresenter.cs` | not-authority, not-precedence, no latest pointer | Tier 1 C6 labels, Safety/Recipes latest-state tests | strong for hard blocks | P2/P3 authority/precedence drift | Defer; good D6/D7 candidate after D5 hardening. |
| Latest-state reader candidate extraction | `ProductLedgerLocalDurableLatestStateReaderCandidateValidator.cs` | candidate not authority, no read precedence/latest pointer | Safety/Recipes reader candidate tests | strong for hard blocks | P2/P3 authority drift | Defer; pair with auxiliary later. |
| Handoff draft writer mode extraction | local approved/test-jail/allowlisted handoff draft executors | create-only writer mode and path boundary | handoff writer tests, path tests | partial | P2/P3 filesystem/path drift | Not first move. |
| Static guard catalog source promotion | current `NodalOsStaticGuardCatalog` under tests | hard-block category representation | static guard catalog tests | strong | P3 if promoted too early | Do not move to source now. |
| `/run` claim coherence status model | docs/tests around Pilot `/run` separation | runtime overclaim prevention | claim coherence docs/tests | D2 maps PilotRunCoupling blocked | P3 if confused with runtime policy | Defer; keep as docs/test guard. |

## 4. Selected D4 Candidate

Selected candidate: minimal parallel source candidate for common boundary claims.

Why this is the safest first source-facing move:

- it can be a new isolated source file;
- it does not require changing existing Product Ledger behavior;
- it does not require changing routes, DI, command handlers or CI;
- it can be protected by no-runtime-wiring and no-production-reference scans;
- it directly mirrors D1/D2 evidence without replacing existing contracts;
- rollback is simple: remove the new file and its tests;
- it gives later D6/D7 migrations a stable source-side type to compare against.

Why C7 is not required before D4:

- D2 increased Tier 1 to 45 tests and added mapping coverage for the exact hard-block concepts D4 would represent.
- C7 additional labels would improve discovery, but D4 can be guarded by targeted no-runtime/no-reference tests plus full Product Ledger Safety/Recipes.
- C7 should remain available if D4 scope expands beyond one isolated candidate file.

## 5. D4 Implementation Plan

D4 is not authorized yet. D4 requires explicit authorization from Diego.

Expected files likely to change:

- new source file only: `src/OneBrain.Core/Approval/NodalOsCommonBoundaryClaimsCandidate.cs`;
- new Safety tests for no-runtime wiring, default blocked state and source isolation;
- docs/log updates.

Files forbidden to change in D4:

- `src/OneBrain.Pilot/**`;
- route mappers and `Program.cs`;
- existing `ProductLedger*.cs` behavior files;
- existing D1/D2 Safety test-only candidates unless only adding reference assertions;
- CI workflow/build scripts;
- database/provider/cloud/KMS/WORM code;
- any public/product or release/commercial docs that would imply enablement.

Expected D4 source diff target:

- one new source file;
- no edits to existing source files;
- no runtime references to the new source file;
- no service registration, route registration or command handler.

Expected D4 tests:

- candidate defaults all relevant capabilities to blocked;
- public/product, Production route, latest pointer, read precedence, product authority, command execution, provider/cloud/network, DB/migration, KMS/WORM, release/commercial and CI enforcement remain false/blocked;
- no production source references the candidate except its own file;
- no Pilot route or DI registration references the candidate;
- D1/D2 mapper remains test-only and non-authoritative;
- unknown/unsupported future mappings still fail closed.

Expected unchanged tests:

- Product Ledger Safety;
- Product Ledger Recipes;
- `NodalOsTier1Safety`;
- `CommonContracts`;
- `DesignOnly`;
- `MappingAdapters`;
- static guard catalog;
- public/product and Production route filters.

Required D4 validation commands:

```powershell
dotnet build src/OneBrain.Core/OneBrain.Core.csproj --no-restore -v:minimal
dotnet build src/OneBrain.Pilot/OneBrain.Pilot.csproj --no-restore -v:minimal
dotnet build OneBrain.slnx --no-restore -v:minimal
dotnet test tests/OneBrain.Safety.Tests/OneBrain.Safety.Tests.csproj --no-build --filter "TestCategory=NodalOsTier1Safety" -v:minimal
dotnet test tests/OneBrain.Safety.Tests/OneBrain.Safety.Tests.csproj --no-build --filter "TestCategory=CommonContracts" -v:minimal
dotnet test tests/OneBrain.Safety.Tests/OneBrain.Safety.Tests.csproj --no-build --filter "TestCategory=MappingAdapters" -v:minimal
dotnet test tests/OneBrain.Safety.Tests/OneBrain.Safety.Tests.csproj --no-build --filter "FullyQualifiedName~ProductLedger" -v:minimal
dotnet test tests/OneBrain.Recipes.Tests/OneBrain.Recipes.Tests.csproj --no-build --filter "FullyQualifiedName~ProductLedger" -v:minimal
dotnet test tests/OneBrain.Safety.Tests/OneBrain.Safety.Tests.csproj --no-build --filter "FullyQualifiedName~NodalOsStaticGuardCatalog" -v:minimal
dotnet test tests/OneBrain.Safety.Tests/OneBrain.Safety.Tests.csproj --no-build --filter "TestCategory=ProductLedger&TestCategory=PublicProductBlock" -v:minimal
dotnet test tests/OneBrain.Safety.Tests/OneBrain.Safety.Tests.csproj --no-build --filter "TestCategory=ProductLedger&TestCategory=ProductionRouteBlock" -v:minimal
git diff --check
git status --short
git rev-list --left-right --count HEAD...'@{u}'
```

Rollback strategy:

- remove the new candidate source file;
- remove D4-specific tests;
- leave D1/D2 test-only artifacts intact;
- rerun the same D4 validation gate.

GO criteria for D4:

- exact explicit authorization;
- one isolated candidate source file only;
- no existing source behavior changes;
- all required validations pass;
- no public/product, Production route, latest pointer, read precedence, product authority, command execution, provider/cloud/network, DB/migration, KMS/WORM or release/commercial drift.

NO-GO criteria for D4:

- any need to edit existing Product Ledger behavior files;
- any need to touch route/DI/CI;
- any failing Product Ledger Safety/Recipes or Tier 1 tests;
- any ambiguity about whether the source candidate is authoritative.

Immediate revert triggers:

- source candidate appears in route, DI or command-handler code;
- source candidate changes runtime output;
- unknown mappings become allowed;
- public/product, Production route, latest pointer, read precedence, product authority, command execution or release/commercial claims become true.

## 6. Remaining Blocked Boundaries

- No public/product exposure.
- No Production route.
- No active read precedence.
- No latest pointer.
- No product authority.
- No shell/subprocess or command execution enablement.
- No provider/cloud/network, DB/migration, KMS/WORM/external trust.
- No release/commercial/public readiness.
- No CI enforcement.
- No runtime/product enablement.

## 7. D4 Execution Note

D4 was executed as `NODAL_OS_BLOCK_D4_MINIMAL_SOURCE_CANDIDATE_NO_RUNTIME_WIRING`.

Actual D4 source artifact:

- `src/OneBrain.Core/Approval/NodalOsCommonBoundaryClaimsCandidate.cs`

Actual D4 scope:

- exactly one minimal source candidate;
- parallel-only and non-authoritative;
- no existing source behavior changes;
- no runtime/product wiring;
- no route, DI, service registration or command handler;
- no CI enforcement;
- no public/product or Production route;
- no latest pointer, read precedence or product authority;
- no release/commercial readiness.

Actual D4 test evidence:

- `tests/OneBrain.Safety.Tests/NodalOsCommonBoundaryClaimsCandidateTests.cs`
- `TestCategory=SourceCandidate`
- `TestCategory=NoRuntimeWiring`
- D1/D2 compatibility assertions remain test/design-only and do not promote D1/D2 artifacts to source.

Recommended next step after D4: D5 equivalence hardening/no-runtime reference audit. Do not proceed to a broad source refactor from D4 alone.

## 8. D5 Isolation Hardening Note

D5 was executed as `NODAL_OS_BLOCK_D5_EQUIVALENCE_HARDENING_NO_RUNTIME_REFERENCE_AUDIT`.

Actual D5 scope:

- Safety tests only;
- docs/log updates only;
- no `src/` changes;
- no new source candidate;
- no modification to `NodalOsCommonBoundaryClaimsCandidate`;
- no replacement or promotion of D1/D2 test-only artifacts;
- no runtime/product wiring;
- no source bloat reduction.

Actual D5 test evidence:

- `tests/OneBrain.Safety.Tests/NodalOsCommonBoundaryClaimsCandidateIsolationHardeningTests.cs`
- `TestCategory=NoAuthority`
- `TestCategory=NoDoubleTruth`
- hardened no-runtime-reference and allowed-reference scans;
- hardened D1/D2/D4 fail-closed equivalence checks.

Recommended next step after D5: D6 minimal replacement plan/audit only or STOP_FOR_AUDIT. Do not proceed to broad replacement or implementation from D5 alone.

## 9. Risks

P0=0, P1=0, P2=0.

P3:

- A source candidate can be mistaken for production authority if naming/docs are not explicit.
- Future D4 can grow beyond one file if not tightly bounded.
- D1/D2 mapper output can create double-truth if treated as more than translation evidence.

P4:

- Source bloat remains unchanged until a later source-facing implementation.
- Tier 1 coverage is stronger but still partial.
- Historical docs/tests retain mixed vocabulary during transition.
