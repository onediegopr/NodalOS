# NODAL OS Product Ledger Local/Dev Next Action Plan

Date: 2026-07-07

Mode: docs-only / plan-only / roadmap-only.

Baseline HEAD: `440f806837596ac680c3d68da06ee779de204419`.

Decision target: `GO_WITH_FINDINGS_PRODUCT_LEDGER_LOCAL_DEV_NEXT_ACTION_PLAN_READY`.

Selected next recommended block: `NODAL_OS_BLOCK_E4_PRODUCT_LEDGER_LOCAL_DEV_STALE_DOC_CROSSLINK_CLEANUP_DOCS_ONLY`.

E4 cross-link index: `docs/architecture/nodal-os-product-ledger-local-dev-stale-entrypoint-crosslink-index.md`.

## 1. Executive Status

Product Ledger local/dev is still evidence-only and local/dev. It is not public/product, not a Production route, not a latest pointer, not active read precedence, not product authority and release/commercial remains `0% / NO-GO`.

E2 created the current canon:

`docs/architecture/nodal-os-product-ledger-local-dev-safety-backlog-canon.md`

The canon reconciles path/canonicalization, local approval/execution, handoff drafts, latest-state evidence, operator surface/local-dev UI, runtime local-only/internal evidence, public/product blocker maps, redaction/retention, integration/property and broad user-workspace boundary docs. Historical QA, roadmap and handoff docs remain preserved, but current interpretation should enter through the canon.

E3 does not authorize E4. E3 only selects the safest next recommended block.

E4 later cross-linked selected stale Product Ledger entrypoints and recommended `NODAL_OS_BLOCK_E5_PRODUCT_LEDGER_LOCAL_DEV_CANON_GUARD_TEST_ONLY`. E4 does not authorize E5.

E5 later added focused manual/discovery-only Safety guard tests for the Product Ledger local/dev canon and selected `NODAL_OS_BLOCK_E6_EXTERNAL_AUDIT_PACKET_PRODUCT_LEDGER_LOCAL_DEV_READ_ONLY`. E5 does not authorize E6, and it does not change runtime/product, CI, Tier 1 enforcement or product readiness.

E6 later added the read-only audit packet at `docs/audit/product-ledger-local-dev/README.md` and selected `NODAL_OS_BLOCK_E7_EXTERNAL_AUDIT_PACKET_REVIEW_READ_ONLY`. E6 does not execute an external audit, submit anything externally or authorize E7.

## 2. Current Percentages

- E3 next-action planning: `100%` after validation.
- Product Ledger local/dev readiness: `81%` for local/dev evidence posture after E3 planning.
- Product Ledger runtime/product enablement: `0%`.
- Tier1/manual gate confidence: `98%` manual/discovery-only.
- CI enforcement: `0%`.
- Release/commercial readiness: `0% / NO-GO`.

## 3. Candidate Next Actions

| Candidate | Objective | Scope | Why now | Touches runtime/product | Touches latest-state/handoff/writer | Touches public/product or Production | Touches latest pointer/read precedence/product authority | Required gates | Value | Risk |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| `NODAL_OS_BLOCK_E4_PRODUCT_LEDGER_LOCAL_DEV_CANON_GUARD_TEST_ONLY` | Add a small Safety guard proving the canon contains required blocked-state wording | Test-only/docs-update | Useful if canon drift becomes the top risk | No | No behavior change | No | No | Core/Pilot/Solution, Product Ledger Safety/Recipes, Tier1, static guards | Medium | P3 if wording guard becomes brittle; P4 if it adds noise |
| `NODAL_OS_BLOCK_E4_PRODUCT_LEDGER_LOCAL_DEV_STALE_DOC_CROSSLINK_CLEANUP_DOCS_ONLY` | Cross-link old Product Ledger QA/handoff/roadmap docs to the E2 canon without deleting history | Docs-only | E2 identified stale entrypoints as the clearest remaining P4/P3 drift risk | No | Docs references only | No | No | Full E3 manual gates plus docs-only diff and overclaim scan | High | P3 if cleanup over-edits history; P4 if links remain incomplete |
| `NODAL_OS_BLOCK_E4_EXTERNAL_AUDIT_PACKET_PRODUCT_LEDGER_LOCAL_DEV_READ_ONLY` | Package canon, evidence and blockers for external/read-only review | Read-only/docs-only | Useful after old entrypoints are less misleading | No | No | No | No | Read-only inventory, docs scan, existing gate evidence | Medium/high | P3 if audit reviews stale docs before cross-links |
| `NODAL_OS_BLOCK_E4_PRODUCT_LEDGER_LOCAL_DEV_SAFETY_GATE_STABILIZATION_DOCS_TEST_ONLY` | Clarify manual gate order or add small gate metadata | Docs/test-only | Useful if gate discoverability blocks next work | No | No behavior change | No | No | Product Ledger Safety/Recipes, Tier1, category discovery | Medium | P3 if labels are mistaken for CI enforcement |
| `STOP_FOR_OPERATOR_REVIEW` | Pause for Diego to choose next lane | No change | Useful only if ambiguity remains | No | No | No | No | Clean repo | Low/medium | P4 unnecessary delay |

## 4. Selected Next Block

Selected:

`NODAL_OS_BLOCK_E4_PRODUCT_LEDGER_LOCAL_DEV_STALE_DOC_CROSSLINK_CLEANUP_DOCS_ONLY`

Why selected:

- E2 already created the canon and found the strongest remaining drift risk in older per-block QA/handoff/roadmap entrypoints.
- A test-only canon guard is useful, but the current Product Ledger Safety/Recipes, hard-block labels and static guards already protect the runtime/product boundaries.
- External audit is more valuable after stale entrypoints are cross-linked, because the reviewer should see current authority first.
- Gate stabilization is valuable later, but the manual gates are currently clear enough and green.

## 5. Rejected Alternatives

Rejected for E4:

- Runtime/product implementation: out of scope and explicitly blocked.
- Public/product route activation: out of scope and explicitly blocked.
- Production route activation: out of scope and explicitly blocked.
- Latest pointer/read precedence/product authority activation: out of scope and explicitly blocked.
- CI enforcement: out of scope and `0%`.
- Broad source refactor: out of scope; D-series closure remains current.

## 6. Exact Scope For Selected E4

E4 should:

- Update selected high-visibility Product Ledger local/dev QA, handoff and roadmap docs with a short current-canon pointer.
- Preserve history and avoid deleting old evidence.
- Mark old docs as historical/reconciled entrypoints where needed.
- Avoid duplicating the entire E2 canon.
- Avoid changing `src/`, tests, CI, route behavior, writer behavior, latest-state behavior or runtime/product behavior.
- Keep all blocked frontiers explicit: public/product, Production route, latest pointer, read precedence, product authority, DB/cloud/network/provider, KMS/WORM/external trust, live Browser/CDP/WCU/OCR/Recipes and release/commercial.

E4 should not:

- Implement runtime/product behavior.
- Modify Product Ledger writer, latest-state, handoff or route source.
- Add command handlers or UI product actions.
- State Product Ledger has public/product readiness.
- State Tier1 has CI enforcement.
- Delete historical QA/handoff/roadmap docs by default.

## 7. Required Gates For E4

Run from repo root:

```powershell
dotnet build src/OneBrain.Core/OneBrain.Core.csproj
dotnet build src/OneBrain.Pilot/OneBrain.Pilot.csproj
dotnet build OneBrain.slnx
dotnet test tests/OneBrain.Safety.Tests/OneBrain.Safety.Tests.csproj --no-build --filter "ProductLedger" -v:minimal
dotnet test tests/OneBrain.Recipes.Tests/OneBrain.Recipes.Tests.csproj --no-build --filter "ProductLedger" -v:minimal
dotnet test tests/OneBrain.Safety.Tests/OneBrain.Safety.Tests.csproj --no-build --filter "TestCategory=NodalOsTier1Safety" -v:minimal
dotnet test tests/OneBrain.Safety.Tests/OneBrain.Safety.Tests.csproj --no-build --filter "TestCategory=ProductLedger" -v:minimal
dotnet test tests/OneBrain.Safety.Tests/OneBrain.Safety.Tests.csproj --no-build --filter "TestCategory=CommonContracts" -v:minimal
dotnet test tests/OneBrain.Safety.Tests/OneBrain.Safety.Tests.csproj --no-build --filter "TestCategory=NoRuntimeWiring" -v:minimal
dotnet test tests/OneBrain.Safety.Tests/OneBrain.Safety.Tests.csproj --no-build --filter "TestCategory=NoAuthority" -v:minimal
dotnet test tests/OneBrain.Safety.Tests/OneBrain.Safety.Tests.csproj --no-build --filter "TestCategory=NoDoubleTruth" -v:minimal
dotnet test tests/OneBrain.Safety.Tests/OneBrain.Safety.Tests.csproj --no-build --filter "FullyQualifiedName~NodalOsStaticGuardCatalogTests" -v:minimal
dotnet test tests/OneBrain.Safety.Tests/OneBrain.Safety.Tests.csproj --no-build --filter "TestCategory=PublicProductBlock" -v:minimal
dotnet test tests/OneBrain.Safety.Tests/OneBrain.Safety.Tests.csproj --no-build --filter "TestCategory=ProductionRouteBlock" -v:minimal
dotnet test tests/OneBrain.Safety.Tests/OneBrain.Safety.Tests.csproj --no-build --list-tests
dotnet test tests/OneBrain.Recipes.Tests/OneBrain.Recipes.Tests.csproj --no-build --list-tests
git diff --check
```

These remain manual/discovery-only. They are not CI enforcement.

## 8. Non-Goals Preserved

E3 changes no source, tests or CI. It does not enable runtime/product, open public/product, open Production route, create latest pointer, activate read precedence, create product authority, enable command/shell/subprocess execution, enable provider/cloud/network/DB, enable KMS/WORM/external trust, enable Browser/CDP/WCU/OCR/Recipes live automation or change release/commercial posture.

Safety/Recipes remain the authoritative evidence surface for Product Ledger local/dev.

## 9. Findings

P0: 0.

P1: 0.

P2: 0.

P3:

- Historical docs can still be used as entrypoints without seeing the E2 canon first.
- Future cross-link cleanup must preserve history and avoid over-editing old evidence.
- Gates remain manual/discovery-only.

P4:

- A future test-only canon guard may be useful after cross-link cleanup if drift continues.
- External audit is best deferred until old entrypoints point at the canon.

## 10. Validation Result

Validation result: PASS.

- Core build: PASS, 0 warnings, 0 errors.
- Pilot build: first parallel attempt failed with a transient `CS2012` file lock on `OneBrain.Core.dll`; sequential rerun PASS, 0 warnings, 0 errors.
- Solution build: PASS, 0 errors, 33 inherited warnings.
- Product Ledger Safety focused: PASS 275/275.
- Product Ledger Recipes focused: PASS 72/72.
- `TestCategory=NodalOsTier1Safety`: PASS 127/127.
- `TestCategory=ProductLedger`: PASS 69/69.
- `TestCategory=CommonContracts`: PASS 101/101.
- `TestCategory=NoRuntimeWiring`: PASS 101/101.
- `TestCategory=NoAuthority`: PASS 63/63.
- `TestCategory=NoDoubleTruth`: PASS 63/63.
- `FullyQualifiedName~NodalOsStaticGuardCatalogTests`: PASS 9/9.
- `TestCategory=PublicProductBlock`: PASS 46/46.
- `TestCategory=ProductionRouteBlock`: PASS 39/39.
- MSTest discovery: Safety 6469 tests, Recipes 1580 tests.
- `git diff --check`: PASS; line-ending normalization warnings only.
- Origin sync before commit/push: PASS `0 0`.
- Changed-file scope: docs-only.
