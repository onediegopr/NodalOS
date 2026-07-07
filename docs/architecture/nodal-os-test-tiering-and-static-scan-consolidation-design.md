# NODAL OS Test Tiering And Static Scan Consolidation Design

Date: 2026-07-07

Mode: design-only / docs-only / audit-only / test-plan-only. This block does not delete, move, rename or rewrite tests. It does not change static scan implementation, CI/build behavior, source behavior, routes, runtime, features, active read precedence, latest pointer, product authority, public/product exposure, cloud/DB/KMS/WORM, release or commercial readiness.

Baseline: Block A documentation compaction, Block B naming consolidation design and the full-system bloat audit.

## 1. Executive Verdict

The test suite is security-heavy and valuable, but it has grown into a repeated assertion layer. Product Ledger, Approval, route/DOM and static-guard checks often prove the same negative capability in both Safety and Recipes, with feature-specific forbidden-token arrays repeated across many files. This is useful while the system is expanding, but it is now a source of `TEST_NOISE` and a refactor risk: developers cannot easily tell which tests are load-bearing and which are mirrors.

Decision: `GO_WITH_FINDINGS_TEST_TIERING_STATIC_SCAN_CONSOLIDATION_DESIGN_READY`.

The safe direction is to keep every current guard until an implementation block proves equivalence, then move toward:

- a small Tier 1 required gate for source refactor safety;
- a Tier 2 integration suite for local route/operator/product-ledger flows;
- a Tier 3 audit/property/corpus suite for adversarial and historical scans;
- a Tier 4 legacy/periodic suite for warning-heavy or historical runtime areas;
- one conceptual central static guard catalog, not many feature-local copies.

Findings: P0 0, P1 0, P2 0 new. P3 risks remain around coverage loss if duplicate tests are moved too aggressively. P4 risks remain around mixed vocabulary during migration.

## 2. Test Noise Diagnosis

Read-only inventory found approximately:

| Area | Approx count | Signal |
| --- | ---: | --- |
| Total Safety + Recipes test files | 674 | Large enough that required gates need explicit tiers. |
| Product Ledger files | 77 | Main local product line and highest-value tiering target. |
| Approval files | 25 | Human review and decision gates mirror Product Ledger constraints. |
| Route/DOM/UI/surface files | 71 | Many label/render assertions are integration or DOM-contract checks. |
| Static scan / forbidden / no-enable files | 21 | Many more forbidden-token assertions exist inside non-scan files. |
| Negative guard / boundary files | 26 | Load-bearing, but repeated per boundary. |
| Latest-state/snapshot/manifest/reader/auxiliary files | 14 | Good candidate for shared corpus later. |
| Pilot/Run/Chrome/CDP/Browser files | 130 | Separate lab/dev/runtime footprints; not Product Ledger authority. |
| OCR/Paddle/ONNX legacy-warning files | 55 | Periodic/legacy bucket candidate due inherited warnings. |
| Normalized Safety/Recipes duplicate names | 42 | Clear mirror-test consolidation candidate. |

The suite has the correct paranoia. The design problem is packaging: the same "no public/product, no Production route, no command execution, no cloud/DB/KMS/WORM, no release/commercial" family appears across many Product Ledger and Approval tests.

## 3. Tier Model

### Tier 1 - Required Safety Gate

Purpose: the minimum gate before any source refactor, naming migration, route consolidation or contract merge.

Required contents:

- `dotnet build src/OneBrain.Core/OneBrain.Core.csproj --no-restore -v:minimal`
- `dotnet build src/OneBrain.Pilot/OneBrain.Pilot.csproj --no-restore -v:minimal`
- `dotnet build OneBrain.slnx --no-restore -v:minimal`
- Product Ledger core Safety tests.
- Approval/Product Ledger safety boundary tests.
- Product Ledger path canonicalization and path confinement.
- Redaction-before-persistence and metadata guard coverage.
- Append-only/hash/checkpoint/read verification coverage.
- Product Ledger writer mode and active policy safety.
- Product Ledger source activation static guard.
- `/run` claim-coherence guard.
- No public/product, no Production route and no product command execution guards.
- `git diff --check`.

Tier 1 must stay small enough to run before source refactors, but strict enough to catch P0/P1/P2 boundary regressions.

### Tier 2 - Extended Integration

Purpose: local internal product confidence after behavior-preserving source changes.

Recommended contents:

- Product Ledger Recipes focused suite.
- HTTP in-process route response tests.
- Rendered route interaction and DOM/render contract tests.
- Internal operator UI preview tests.
- Command preview/router tests when they remain preview-only.
- Approval route chains.
- Snapshot/manifest/reader/auxiliary latest-state evidence route flows.
- Operator acceptance local-only matrix.

Tier 2 may be required before route consolidation or operator surface work, but not for every docs-only block.

### Tier 3 - Audit / Property / Corpus

Purpose: adversarial confidence and historical claim/audit coverage.

Recommended contents:

- Product Ledger integration property/corpus tests.
- Traversal and encoded traversal corpus.
- Windows drive-like and UNC variants.
- Overlong identifiers and unsafe option flags.
- Duplicate logical metadata keys and raw secret corpus.
- Full historical docs claim scans.
- Full negative static scan across source and docs.
- Audit packet consistency checks.

Tier 3 should run before public/product readiness, high-risk refactors and final external audits.

### Tier 4 - Legacy / Periodic

Purpose: preserve historical/runtime coverage without making it the default Product Ledger refactor gate.

Recommended contents:

- OCR/Paddle/ONNX legacy-warning tests.
- Browser/CDP/ChromeLab broad runtime tests not directly touching Product Ledger authority.
- Expensive suites and any tests with known timeout/flakiness behavior.
- External/manual audit prompts.
- Historical closeout docs scans whose claims are superseded by current canon.

Tier 4 should not be deleted by default. It should be scheduled or invoked before release/commercial decisions, which remain `0% / NO-GO`.

## 4. Test Inventory Summary

| Group | Location | Approx count | Protects | Load-bearing | Duplication signal | Proposed tier |
| --- | --- | ---: | --- | --- | --- | --- |
| Product Ledger Safety | `tests/OneBrain.Safety.Tests/ProductLedger*.cs` | 51 | local ledger path, writer, approval, route, latest-state, public/product blockers | Yes | Mirrors 26 Recipes names and repeats forbidden claims | Tier 1 core, Tier 2 route/DOM, Tier 3 property |
| Product Ledger Recipes | `tests/OneBrain.Recipes.Tests/ProductLedger*.cs` | 26 | behavior recipes, route/render integration, writer scenarios | Yes | Many mirror Safety files | Tier 2 by default; Tier 1 only for canonical writer/read model smoke |
| Approval Safety/Recipes | `tests/OneBrain.*.Tests/Approval*.cs` | 25 | human review, approval mutation, durable audit, no product action/export | Yes | Safety/Recipes mirrors exist | Tier 1 for core approval guards; Tier 2 for packet/render checks |
| Route/DOM/UI/surface | `*Route*`, `*Rendered*`, `*Renderable*`, `*OperatorSurface*`, `*Ui*` | 71 | Development routes, Production 404, labels, disabled actions | Yes for product surfaces | Label assertions often duplicate no-public/no-release scans | Tier 2 |
| Static scan / forbidden | `*StaticScan*`, `*Boundary*`, inline `forbidden` arrays | 21 named plus many inline | no enablement, no forbidden claims, source activation | Yes | Highly repeated token lists | Tier 1 central scanner for source; Tier 3 docs/historical |
| Negative guard / boundary | `*NegativeGuard*`, `*Boundary*`, guard assertion methods | 26 | fail-closed behavior and anti-capabilities | Yes | Repeated by boundary | Tier 1 for hard safety; Tier 2/3 for variants |
| Latest-state evidence | `*LatestState*`, `*Snapshot*`, `*Manifest*`, `*ReaderCandidate*`, `*Auxiliary*` | 14 | not-authority, not-precedence, create-only evidence | Yes | Roles can share corpus | Tier 1 selected; Tier 2 role flows; Tier 3 corpus |
| Product Ledger property/corpus | `ProductLedgerIntegrationPropertyTestPackTests` and related | 2 named | tamper, redaction, traversal, corpus | Yes, but heavier | Central corpus candidate | Tier 3 with one Tier 1 smoke |
| Pilot `/run` and lab/runtime | `Pilot*`, `*Run*`, `Chrome*`, `Cdp*`, `Browser*` | 130 | separate lab/dev runtime footprints and claim coherence | Yes for claim isolation | Not Product Ledger authority | Tier 1 claim-coherence only; Tier 4 broad runtime |
| OCR/Paddle/ONNX legacy | `*Ocr*`, `*Paddle*`, `*Onnx*` | 55 | OCR legacy and active ONNX path | Some | Current solution build warnings cluster here | Tier 4 periodic unless touching OCR |

## 5. Static Scan Inventory Summary

| Scan family | Current shape | False positive risk | Load-bearing | Consolidation candidate | Proposed catalog category |
| --- | --- | --- | --- | --- | --- |
| Forbidden source activation | Feature-local token arrays for `Process.Start`, `System.Diagnostics.Process`, service registration, handlers | Medium; negative wording in docs can match | Yes | Yes | `SourceActivationOnly` |
| Public/product exposure | Repeated "public/product", public route and public UI assertions | Medium; future design docs mention no-go | Yes | Yes | `PublicProductExposure` |
| Production routes | `ProductionAllowed`, Production 404 and route wording checks | Low/medium | Yes | Yes | `ProductionRoutes` |
| Durable authority | not authority, candidate, durable authority blockers | Medium | Yes | Yes | `DurableAuthority` |
| Latest pointer | latest pointer and overwrite prohibition scans | Medium | Yes | Yes | `LatestPointer` |
| Read precedence | active read precedence and not-precedence checks | Medium | Yes | Yes | `ReadPrecedence` |
| Command execution | command execution, command handler, shell/subprocess, Pilot `/run` isolation | High; command preview is allowed | Yes | Yes | `CommandExecution`, `ShellSubprocess`, `RunClaimCoherence` |
| Cloud/network/DB | provider/cloud/network/DB/migration strings | High; many negative/no-go mentions | Yes | Yes | `CloudNetworkDb` |
| KMS/WORM/compliance | KMS/WORM/external trust/compliance custody claims | High; negative claims are expected | Yes | Yes | `KmsWormCompliance` |
| Release/commercial | release ready, commercial ready, production ready | High; negative readiness is common | Yes | Yes | `ReleaseCommercial` |
| `/run` claim coherence | ZeroReadOnly and unscoped `NO_RUNTIME_NO_EXECUTION` wording | Medium | Yes | Yes | `RunClaimCoherence` |
| Docs negative claim allowlists | Mostly implicit per test | High | Yes for docs scans | Yes | `DocsNegativeClaimAllowlist` |

## 6. Duplication Map

The following duplicate types should not be deleted now. They should be mapped before any implementation block:

| Duplicate family | Current examples | Future disposition |
| --- | --- | --- |
| Safety vs Recipes mirror names | 42 normalized duplicates including `ProductLedgerPathLocalOnlyActiveWriterTests`, `ProductLedgerRuntimeLocalOnlyInternalEnablementTests`, `ApprovalMutationStoreDesignOnlyProtectedTests` | Keep one Tier 1 invariant; move recipe/story variants to Tier 2 after GO. |
| Route tests vs DOM/render tests | `ProductLedgerHttpInProcessRouteResponse*`, `ProductLedgerRenderedRouteInteraction*`, `ProductLedgerRenderableOperatorSurface*` | Keep route 200/404 and no-write in Tier 1/2; move label polish to Tier 2. |
| Static scans by feature | Route static scan, workspace boundary scans, latest-state scans, public/product scans | Merge into central `NodalOsStaticGuardCatalog` later. |
| Guard tests by boundary | workspace test-jail, allowlisted handoff, snapshot, manifest, reader, auxiliary | Keep one per authority class in Tier 1; role variants can share Tier 3 corpus. |
| Docs claim scans | repeated no cloud/no release/no WORM/no product checks in docs-focused tests | Central docs scan with allowlisted negative wording. |
| Latest-state role tests | snapshot, manifest, reader candidate, auxiliary | Use shared `LatestStateEvidence` role corpus after Block D/E design. |
| OCR/legacy warning-heavy tests | Paddle/ONNX historical diagnostics | Tier 4 periodic unless code touched. |

## 7. Central Scanner Design

Conceptual name: `NodalOsStaticGuardCatalog`.

This is design-only. Do not implement in this block.

### Categories

- `RuntimeExecutionClaims`
- `PublicProductExposure`
- `ProductionRoutes`
- `DurableAuthority`
- `LatestPointer`
- `ReadPrecedence`
- `CommandExecution`
- `ShellSubprocess`
- `CloudNetworkDb`
- `KmsWormCompliance`
- `ReleaseCommercial`
- `RunClaimCoherence`
- `SourceActivationOnly`
- `DocsNegativeClaimAllowlist`

### Source Scans vs Docs Scans

Source scans should hard-fail on productive activation patterns:

- unapproved command execution;
- shell/subprocess creation;
- Product Ledger public/product route exposure;
- Production route enablement;
- DB/cloud/provider/network implementation;
- KMS/WORM/external trust implementation;
- release/commercial enablement markers;
- Product Ledger route wiring into Pilot `/run`.

Docs scans should hard-fail on overclaims, but allow negative/no-go wording:

- allowed: "No public/product exposure."
- allowed: "KMS/WORM/external trust remains unimplemented."
- blocked: "KMS enabled."
- blocked: "release ready."
- blocked: "Product Ledger public product route is live."

### Hard Fail vs Warning

Hard fail:

- source activation in forbidden category;
- Product Ledger command execution enablement;
- Production route enablement;
- active read precedence/latest pointer/product authority implementation without GO;
- public/product exposure;
- DB/cloud/KMS/WORM/provider/network implementation;
- release/commercial readiness claims.

Warning:

- docs mention future-only capabilities with explicit no-go wording;
- historical archived docs contain superseded phrases;
- recipe/product copy uses action verbs only in blocked/disabled context.

### Allowlist Pattern Design

Allowlist entries should be structured, not scattered strings:

- `category`
- `scope`
- `pathPrefix`
- `allowedContext`
- `expiryOrReviewBlock`
- `rationale`

Avoid allowlisting whole files when only one negative sentence is expected.

### False Positive Reporting

Future scanner output should report:

- category;
- path;
- line;
- matched token;
- surrounding scope;
- whether match is source, test, docs or generated artifact;
- suggested fix or allowlist category.

## 8. Required Gates By Scenario

| Scenario | Required gate |
| --- | --- |
| Docs-only/design-only/audit-only | `git diff --check`, docs-only diff guard, optional build evidence if requested. |
| Naming migration design | Tier 1 design checklist plus no source/test diff. |
| Source refactor / contract merge | Full Tier 1 plus Product Ledger focused Safety and Recipes. |
| Route/operator surface change | Tier 1 plus Tier 2 route/DOM/operator surface suite. |
| Product Ledger writer/path change | Tier 1 plus Tier 3 path/corpus/tamper checks. |
| Public/product readiness decision | Tier 1 + Tier 2 + Tier 3 + selected Tier 4 runtime/legacy audit. |
| Release/commercial readiness | All tiers plus external/human release/security/commercial review; currently `0% / NO-GO`. |

## 9. Future Implementation Plan

### C1 - Add central static guard catalog, test-only

- Expected files: new test helper/catalog under tests, not production source.
- Risks: false positives and accidentally weakening source activation scans.
- Tests required: existing Product Ledger static/negative guards must stay green.
- Stop conditions: source behavior change, deleted tests, product enablement.

### C2 - Repoint duplicate scans to catalog

- Expected files: test-only edits in static scan tests.
- Risks: category mismatch hides a forbidden token.
- Tests required: before/after equivalent negative corpus.
- Stop conditions: any guardrail loss or need to alter source.

### C3 - Mark Tier 1/Tier 2/Tier 3 suites in docs

- Expected files: docs and maybe test trait plan.
- Risks: confusing labels with actual CI behavior.
- Tests required: docs diff check.
- Stop conditions: CI/build behavior change without GO.

### C4 - Create focused pre-refactor gate

- Expected files: script or test command doc in a future authorized block.
- Risks: gate too small misses route/authority regression.
- Tests required: run focused gate and compare with current Product Ledger Safety/Recipes.
- Stop conditions: any failing required safety test.

### C5 - Move duplicate tests to extended, with GO

- Expected files: test trait/category changes only.
- Risks: deleting instead of moving; losing Safety/Recipes mirror needed for coverage.
- Tests required: full Safety build, Product Ledger Safety, Product Ledger Recipes, scanner equivalence.
- Stop conditions: test deletion, assertion weakening, P0/P1/P2.

### C6 - Verify no guardrail loss

- Expected files: audit report and coverage matrix.
- Risks: overconfidence from labels.
- Tests required: Tier 1 + Tier 2 + selected Tier 3.
- Stop conditions: any previously load-bearing guard missing from the matrix.

## 10. Risks And Non-Negotiable Guardrails

P3 risks:

- Moving duplicate tests too early can remove the only assertion for a specific boundary.
- A central scanner can become too broad and noisy, causing future teams to ignore it.
- A central scanner can become too permissive if allowlists are file-wide.
- Route/DOM label checks can look redundant while still protecting product-facing claims.

P4 risks:

- Mixed Safety/Recipes naming will remain until implementation blocks.
- Historical docs will keep superseded no-runtime wording and need scoped allowlists.
- OCR/legacy warning tests continue to create noisy solution-build output.

Non-negotiable guardrails:

- No test deletion without a future explicit GO and coverage equivalence proof.
- No assertion weakening.
- No source behavior change.
- No static scan behavior change in this design block.
- No public/product, Production route, product authority, active read precedence or latest pointer.
- No shell/subprocess, command execution, cloud/network/DB, KMS/WORM/external trust.
- No release/commercial readiness claim.

## 11. Next Recommended Block

`NODAL_OS_BLOCK_D_MODEL_CONTRACT_MERGE_DESIGN_ONLY`.

Reason: Block A created current-state docs, Block B mapped naming, and this Block C maps tests/scans. The next safe high-value block is design-only modeling for shared `LocalOnlyResult<T>`, `BoundaryClaims`, shared blockers and `WriterMode`, with no source implementation until a later explicit GO.
