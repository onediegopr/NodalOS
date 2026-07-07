# NODAL OS Source Refactor Readiness Audit

Date: 2026-07-07

Mode: design-only / audit-only / docs-only / readiness-only. This block does not modify `src/`, rename classes, rename files, implement contracts, change tests, change scanner behavior, activate features, expose product/public routes, introduce active read precedence, latest pointer, product authority, cloud/network/DB, KMS/WORM, release or commercial readiness.

Baseline: Block A documentation compaction, Block B naming consolidation design, Block C test tiering/static scan design, Block D model/contract merge design and the full-system bloat audit.

## 1. Executive Verdict

Decision: `GO_WITH_FINDINGS_SOURCE_REFACTOR_READINESS_DESIGN_READY`.

NODAL OS is ready for a first refactor-readiness implementation block, but not for a first source contract refactor yet. The safest next implementation is `NODAL_OS_BLOCK_C1_STATIC_GUARD_CATALOG_TEST_ONLY_IMPLEMENTATION`: a test-only central static guard catalog that preserves current scattered negative guards and proves equivalence before any source contracts are introduced or migrated.

Blocks B-D identify real bloat, but the current protection is still distributed across many Safety/Recipes files and inline forbidden-token arrays. Adding common contracts first would create a second vocabulary before the guard rail is centralized. C1 reduces refactor risk without changing runtime behavior.

Findings: P0 0, P1 0, P2 0 new. P3 risks remain around future scanner false positives, future double-truth during parallel contract introduction and high-risk source files. P4 risks remain around mixed legacy/current vocabulary until implementation blocks complete.

## 2. Is Source Refactor Ready?

Source refactor of production/Core contracts: not yet.

Readiness block for test-only static guard consolidation: yes.

| Area | State | Readiness |
| --- | --- | --- |
| Current architecture canon | Present in `nodal-os-current-local-internal-architecture.md` | Ready |
| Documentation governance | Present and scoped | Ready |
| Naming consolidation map | Present, 60 mapped rows | Ready for future migration design |
| Test tiering and static scan design | Present, 13 tier groups | Ready for C1 implementation |
| Model/contract merge design | Present, 23 mapped contract rows | Ready for D1 only after C1 |
| Central static guard catalog | Design only; no implementation found | Not ready until C1 |
| Common contracts | Design only; no implementation found | Not ready until D1 |
| Source refactor | Requires C1, then D1, then a low-risk D2 adapter | Not ready |

## 3. Recommended First Implementation Block

Recommended first block: `NODAL_OS_BLOCK_C1_STATIC_GUARD_CATALOG_TEST_ONLY_IMPLEMENTATION`.

Status: GO candidate for a future explicit implementation window.

Why C1 first:

- It touches tests/test helpers only, not production source behavior.
- It centralizes hard-fail negative categories before any source package changes.
- It lets old and new vocabulary be scanned in one place during future migration.
- It lowers the risk of common contracts hiding authority, runtime or product claims.
- It is easier to roll back than adding shared source contracts.

Do not start D1 before C1 unless the future GO explicitly accepts the double-truth risk and defines an equivalent guard strategy.

## 4. Candidate Inventory

| Candidate | Expected files touched in future | Source risk | Test risk | Security risk | Migration risk | Double-truth risk | Rollback strategy | Required tests | Expected benefit | Recommended order | GO/NO-GO |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| C1 - `NodalOsStaticGuardCatalog` central, test-only | `tests/` helper/catalog and 1-2 static scan tests | Low | Medium | Low/Medium due false positives | Low | Low | Revert test helper and repointed tests | Tier 1 static guards, Product Ledger Safety/Recipes focused, `git diff --check` | Central safety net before source refactor | 1 | GO next |
| D1 - Common contracts added in parallel | New Core shared contracts plus invariant tests | Medium | Medium | Medium | Medium | Medium/High | Remove unused contracts | Tier 1 plus contract invariant tests | Prepares source simplification | 2 | GO only after C1 |
| D2 - One low-risk capability adapter | One preview/read-only capability plus compatibility adapter | Medium | Medium/High | Medium | Medium | High | Revert adapter, old path remains | Tier 1 + Tier 2 route/DOM if surfaced | Proves contract pattern on real code | 3 | NO-GO until C1+D1 |
| D3 - Latest-state evidence consolidation | Snapshot/manifest/reader/auxiliary contracts and tests | High | High | High | High | High | Keep old role contracts as compatibility aliases | Tier 1 latest-state + Tier 2 route + Tier 3 role corpus | Large bloat reduction | 5 | NO-GO first refactor |
| D4 - Handoff draft variants consolidation | Handoff draft writers and path tests | High | High | High | High | High | Restore old writers | Tier 1 path/redaction + Tier 3 traversal corpus | Reduces writer variants | 6 | NO-GO first refactor |
| D5 - DurableAuditTrail Minimal/Candidate merge | Durable audit trail contracts/tests | Medium/High | Medium | Medium/High | Medium | Medium | Keep old wrappers | Tier 1 durable/Product Ledger ledger tests | Reduces ledger duplicate shape | 7 | NO-GO first refactor |
| Product surface simplification | Route/read-model/source and DOM tests | Medium | High | Medium/High | Medium | Medium | Revert route/read-model changes | Tier 1 + Tier 2 route/DOM/operator tests | Product clarity | 8 | NO-GO until source guard story is stable |
| Docs archive physical move | `docs/archive`, indexes, links | Low source risk | Low/Medium link risk | Low | Medium | Low | Revert docs move | `git diff --check`, link/static checks | Reduces doc noise | 4 or parallel docs-only | GO in docs-only window |

## 5. High-Risk Do-Not-Touch List

| Area | Representative files/concepts | Why not first |
| --- | --- | --- |
| Redaction service | `src/OneBrain.Core/Approval/RedactionBeforePersistenceService.cs` | It is a real safety service, not boilerplate. A wrapper must not make redaction optional. |
| Path canonicalization validators | `ProductLedgerPathCanonicalizationValidator`, path confinement tests | Path safety is load-bearing and high blast radius. |
| Hash/checkpoint kernel | `ProductLedgerLocalAppendOnlyHashing`, active/temp writer hash and checkpoint code | Tamper and truncation evidence must remain behavior-frozen. |
| Append-only writer kernel | `ProductLedgerPathLocalOnlyActiveWriter`, local-temp writer | Writer mode consolidation is useful but high risk before central scans. |
| `/run` and Pilot execution boundary | `src/OneBrain.Pilot/Program.cs`, Pilot gate/executor docs/tests | Separate gated runtime footprint; not Product Ledger authority. |
| ChromeLab/Browser/OCR/WCU tracks | Chrome/CDP/Browser/OCR/WCU runtime/lab code and tests | Separate runtime families; do not hide them under Product Ledger simplification. |
| Product Ledger authority semantics | active read precedence, latest pointer, product read-model authority | Still blocked or candidate-only; must not be collapsed into generic names. |
| Static no-enable load-bearing claims | Product Ledger public/product, Production route, command execution, cloud/DB/KMS/WORM/release scans | They are the safety net that C1 should centralize, not weaken. |
| Production/public gates | public/product UI, Production route, release/commercial readiness | These are blocked frontiers, not refactor targets. |

## 6. First Implementation Block Decision

| Option | Safety | Simplicity | Confidence | Coverage protection | Bloat reduction | Double-truth risk | False-positive risk | Implementation size | Rollback ease | Recommendation |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| Option 1: C1 central static guard catalog, test-only | High | High | High | High | Medium | Low | Medium | Small/Medium | High | First |
| Option 2: D1 common contracts in parallel, unused by runtime | Medium | Medium | Medium | Medium | Medium | Medium/High | Low | Medium | High | Second, after C1 |
| Option 3: D2 adapt one low-risk capability | Medium/Low | Medium | Medium/Low | Medium | High | High | Low | Medium/High | Medium | Later |
| Option 4: Do nothing; more design/audit | High | High | Medium | No improvement | Low | Low | Low | None | N/A | Not needed now |

Conclusion: C1 first. D1 is safe only after C1 exists or after an explicitly scoped equivalent guard strategy is accepted.

## 7. Required Pre-Refactor Gate

Before any source refactor, contract implementation, class rename or migration adapter:

1. `dotnet build src/OneBrain.Core/OneBrain.Core.csproj --no-restore -v:minimal`
2. `dotnet build src/OneBrain.Pilot/OneBrain.Pilot.csproj --no-restore -v:minimal`
3. `dotnet build OneBrain.slnx --no-restore -v:minimal`
4. Product Ledger Safety focused gate.
5. Product Ledger Recipes focused gate, or explicit focused fallback if full focused run times out without failures.
6. Product Ledger path canonicalization, redaction-before-persistence, writer/checkpoint/tamper and latest-state no-authority tests.
7. `/run` claim-coherence guard.
8. No public/product and no Production route guards.
9. No latest pointer, no active read precedence and no product read-model authority guards.
10. No command execution, shell/subprocess, provider/cloud/network, DB/migration, KMS/WORM/external trust and release/commercial guards.
11. Central static guard catalog or documented current equivalent.
12. `git diff --check`.
13. Docs/log update describing changed risk posture.

Do not claim full suite PASS if a suite times out or is not run.

## 8. C1 Design

Future block name: `NODAL_OS_BLOCK_C1_STATIC_GUARD_CATALOG_TEST_ONLY_IMPLEMENTATION`.

Required shape:

- Add a central test-only catalog such as `NodalOsStaticGuardCatalog`.
- Keep it under tests/test helpers unless the repository has an existing test helper convention that makes another location safer.
- Do not change runtime or production source behavior.
- Do not delete old scans in C1.
- Migrate only 1-2 duplicated scans to the catalog as proof.
- Keep old-name and new-name scanning categories during migration.
- Use structured categories for source activation, public/product exposure, Production routes, durable authority, latest pointer, read precedence, command execution, shell/subprocess, cloud/network/DB, KMS/WORM/compliance, release/commercial, `/run` claim coherence and docs negative-claim allowlists.
- Use scoped allowlists; do not allowlist whole files when one negative sentence is intended.
- Report category, path, line, matched token, source/test/docs scope and suggested disposition.

Stop conditions:

- any source behavior change;
- any deleted load-bearing test;
- any weakened forbidden-token assertion;
- any product/public/runtime/release enablement claim;
- any P0/P1/P2 or TRUE_RISK.

## 9. D1 Design

Future block name: `NODAL_OS_BLOCK_D1_COMMON_CONTRACTS_PARALLEL_IMPLEMENTATION`.

Required shape:

- Add common contracts only.
- Keep contracts unused by runtime until a later D2 adapter block.
- Default all boundary claims to denied/fail-closed.
- Do not add aliases that imply authority, precedence, product read-model or public/product exposure.
- Do not migrate redaction, path canonicalization or hash/checkpoint services into optional envelopes.
- Add invariant tests for denied defaults, scoped local/internal claims and no raw sensitive data in common blockers/evidence refs.
- Keep old contracts and old tests.

Stop conditions:

- runtime behavior change;
- service registration/product DI;
- command handler/product UI exposure;
- active read precedence/latest pointer/product authority;
- old/new contract double truth without a documented migration map;
- P0/P1/P2 or TRUE_RISK.

## 10. Risks

P3:

- C1 can become noisy if source and docs scans share unscoped token lists.
- D1 can create double truth if common contracts exist without explicit default-deny claims.
- D2 can accidentally shift route/read-model output if a preview surface is chosen too early.
- Latest-state and handoff consolidation are high-value but high-risk because they guard authority, precedence, path and create-only semantics.

P4:

- Historical docs and test names will keep old vocabulary during migration.
- Some solution build warnings may remain inherited from non-Product-Ledger areas.
- Documentation compaction remains incomplete until a separate archive/index block.

Mitigations:

- Implement C1 before D1.
- Keep old scans and old tests until equivalence is proven.
- Use Tier 1 as a hard gate before source edits.
- Keep redaction, path canonicalization and hash/checkpoint behavior frozen.

## 11. Exact Next GO

`AUTHORIZE_NODAL_OS_BLOCK_C1_STATIC_GUARD_CATALOG_TEST_ONLY_IMPLEMENTATION`

Allowed in that future GO:

- test-only helper/catalog;
- migrate 1-2 duplicated static scans as proof;
- no source behavior change;
- no test deletion;
- no assertion weakening;
- no runtime/product enablement;
- no public/product route;
- no Production route;
- no active read precedence/latest pointer/product authority;
- no cloud/network/DB/KMS/WORM/release/commercial.

If C1 closes without P0/P1/P2, the next safe source-readiness implementation should be `NODAL_OS_BLOCK_D1_COMMON_CONTRACTS_PARALLEL_IMPLEMENTATION`.
