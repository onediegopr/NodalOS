# NODAL OS — Full System Cloud/Editorial Bloat & Architecture / Product Readiness Audit (Read-Only)

Baseline HEAD: `9e8a1f357d2a99565006f5997b0d60eb201b2b95` · branch `chrome-lab-001-extension-local-ai-bridge`
Mode: read-only / docs-only. No code changed, no refactor, no deletion, no runtime/product activation.

## 1. Executive verdict

NODAL OS has a **small, genuinely good local kernel** (append-only hash-chained local ledger, fail-closed everywhere, hash-only payload storage) buried under a **large, self-similar scaffold** of policy-status contracts, per-step "executor/presenter" classes, and an enormous audit-doc corpus. The engineering quality of individual pieces is high; the **system-level economics are poor**: adding each capability currently costs ~600–1000 LOC of near-boilerplate plus dual test files plus ADR+QA+handoff. This is the same failure mode flagged for VERIDIA Fabric. The fat is real and is now the primary risk to maintainability and to reaching a usable local product.

## 2. GO / NO-GO

**`GO_WITH_FINDINGS`** — simplify (docs + naming + design-only merge plans) **before** advancing to active read precedence / latest pointer / product authority. This audit is itself read-only/docs-only; it introduces no P0/P1/P2. The build/test posture is unchanged (docs-only).

## 3. Is there architectural fat? — Yes, measurable

| Dimension | Count | Signal |
|---|---|---|
| `Core/Approval/` classes | **69** (~29,900 LOC) | one folder holds the whole "product" |
| Test files (Safety / Recipes) | **516 / 156** | ~672 test files for one active line |
| ADRs | **331** | far more decisions than components |
| QA report dirs | **231** | one per micro-block |
| Handoffs | **221** | near 1:1 with QA |
| decision-log | **1,810 lines** | too granular to be a source of truth |
| Total docs (.md) | **1,729** | doc corpus dwarfs code |
| Per-chain-node cost | 4 enums + ~5 records + 1 executor ≈ **600–994 LOC** | boilerplate-per-idea |
| Naming: files containing "ReadOnly" | **61 / 69** | vocabulary is policy-status, not domain |
| "Candidate" / "Preview" / "Boundary" | 37 / 30 / 30 files | status suffixes dominate |
| Writer variants of the same ledger | **3** (Disabled / TempTestOnly / LocalOnlyActive) | triplication |

## 4. Critical fat detected (fix/plan first)

- **F1 — Contract explosion per node.** Every step (`…LatestStateSnapshotExecutor` 994 LOC, `…AuxiliaryEvidencePresenter` 598 LOC, etc.) redeclares its own `Decision`/`State`/`ActionKind`/`Blocker` enums + `Options`/`Request`/`Result`/`Validation`/`Payload` records. These are ~80% identical across nodes. → one shared `LocalOnlyResult<T>` + shared `BoundaryClaims` + shared `Blocker` taxonomy would remove thousands of LOC.
- **F2 — Status-suffix naming as domain model.** `…LocalDurableLatestStateAuxiliaryEvidenceNotPrecedenceNotAuthority` encodes 5 policy facts in a type name. Policy belongs in a `status`/enum field, not the class name. 61/69 files carry "ReadOnly".
- **F3 — Writer triplication + multiple ledgers/evidence notions.** `ProductLedgerPathWriterScaffoldDisabled` / `…LocalTempWriterTestOnly` / `…LocalOnlyActiveWriter` + Durable Stage1/Stage2 + LocalTempCheckpointEvidence all re-implement append-only hash-chain+checkpoint.
- **F4 — Doc corpus is the biggest artifact in the repo.** 1,729 md, 331 ADRs, 221 handoffs, 1,810-line decision-log. No single "current architecture" source of truth; readers must reconstruct state from a long chain of near-identical closeouts.
- **F5 — `*DesignOnlyProtected` / `*AntiCapabilityProof` classes** (7 files) are compiled ceremony that assert negatives ("no execution", "no cloud") as code + dual tests. Valuable as ONE guard; wasteful as many.
- **F6 — Carried P1 from the prior system audit (still relevant):** `OneBrain.Pilot` `/run` executes allowlisted recipes via `Process.Start` while advertising `PilotSafetySummary.ZeroReadOnly`, and the canonical banner says `NO_RUNTIME_NO_EXECUTION`. This is a claim-coherence issue, not new fat, but it must be reconciled before any "product readiness" statement.

## 5. Acceptable fat (kept for real security)

- Fail-closed validation on every writer/reader (throws → blocked). Keep.
- Hash-only payload storage + bounded safe-metadata allowlist. Keep.
- Head-checkpoint tail-deletion detection. Keep.
- Non-temp path confinement + `IsUnderLocalTemp` guards. Keep.
- The static "no-enable" source scan (as **one** consolidated guard). Keep the intent, compact the count.

## 6. Essential components (must survive)

1. The append-only hash-chained local ledger kernel (`ProductLedgerPathLocalOnlyActiveWriter` core: append/checkpoint/verify).
2. Canonicalization + path-confinement validators.
3. Persisted-candidate registry / active policy (as ONE gate).
4. Approval decision state (`ApprovalModels`/`ApprovalPolicy`/binding).
5. `RedactionBeforePersistenceService` (the one real redaction implementation).
6. One operator-surface read-model.
7. One local evidence/report writer (`LocalReportExportService`).
8. One handoff-draft writer.
9. The consolidated policy/guard + static no-enable scan.

## 7. Merge candidates

- All per-node `*Result`/`*Options`/`*Request`/`*Validation`/`*Payload` → generic `LocalOnlyResult<T>` + shared `BoundaryClaims` record + shared `Blocker` enum.
- 3 writers → 1 writer with a `WriterMode { Disabled, LocalTempTest, LocalActive }`.
- Durable Stage1/Stage2/LocalTempCheckpoint → modes of the one ledger kernel.
- 4+ UI/preview surfaces (`RenderableOperatorSurface`, `InternalOperatorUiPreview`, `LocalDevRoutePreview`, `PublicUiReadOnlyDisabledPreview`) → one `OperatorSurfaceReadModel` + one render.
- `LatestStateSnapshotExecutor` + `LatestStateManifestWriter` + `AuxiliaryEvidencePresenter` + `ReaderCandidateValidator` → one `LatestStateEvidence` component with a `role` field (snapshot/manifest/aux/reader).
- Command handler + command preview router → one `…CommandPreview` until a real command exists.

## 8. Deletion candidates (future block, do NOT delete now)

- `ProductLedgerPathWriterScaffoldDisabled` (dead once writer merges).
- `PublicUiReadOnlyDisabledPreview` (once a real disabled-state is a flag on the surface model).
- Redundant `*DesignOnlyProtected` duplicates after one guard is canonical.
- Mirrored Safety+Recipes test files that assert the identical thing.

## 9. Docs/tests that should be compacted

- Replace the 1,810-line decision-log with a short **current-state head** + archived history.
- Compact 331 ADRs: keep decisions that changed architecture; archive per-micro-block closeouts.
- Fold 221 handoffs + 231 QA into a rolling `latest-architecture-state.md` + an archive index.
- Tier tests: a small **required smoke/guard gate** (writer round-trip, tamper fail-closed, path-confinement, one static no-enable scan) vs an **extended suite** (the corpus/flag/mirror tests) run on demand.

## 10. Minimum recommended architecture (local product)

Ten components, thin interfaces, policy-as-data:

1. `ProductLedgerAuthority` (append/verify/read; wraps the kernel; `WriterMode` enum).
2. `ApprovalDecisionStore` (persisted approval state).
3. `ApprovedActionExecutor` (no-op/bounded now; one seam for future real actions).
4. `LocalEvidenceWriter` (report/export; path-confined).
5. `OperatorSurfaceReadModel` (one DTO the UI renders).
6. `LocalHandoffDraftWriter` (the 3 handoff executors merged; allowlist as data).
7. `SnapshotEvidenceWriter` + `ManifestEvidenceIndex` (LatestState, merged; `role` field).
8. `AuxiliaryEvidencePresenter` (one presenter, explicitly non-authority).
9. `RedactionBeforePersistenceService` (already real).
10. `Policy/Guard` layer (one `BoundaryClaims` + one static no-enable scan).

Everything else becomes a mode/flag/field on these, or moves to `archive`.

## 11. Product-usable-local recommendation

- **MVP-local minimal path:** operator opens one local route → sees `OperatorSurfaceReadModel` (ledger head + last entries + status) → triggers an **approved no-op/bounded** action → an evidence entry + handoff draft is written → operator sees it reflected. That loop already exists in pieces; it needs ONE route and ONE surface, not more nodes.
- **Demo-local internal path:** the dev-gated route rendering the latest snapshot + manifest as "Advanced diagnostics".
- **Do NOT show the user:** the DesignOnlyProtected/AntiCapabilityProof/NotAuthority machinery, the 3 writer variants, or raw blocker enums.
- **Keep dev/audit-only:** snapshot/manifest/aux evidence presenters as "Advanced diagnostics".
- **Removable without hurting the demo:** disabled scaffolds, duplicate previews, mirrored tests.

## 12. Risk of adding MORE boundaries

Each new boundary today adds a class-family + dual tests + 3 docs, deepens the naming problem, and raises onboarding cost — while adding near-zero user value (they are negatives). Continuing will make active-read-precedence and product authority **harder** to land cleanly. Boundary growth has passed the point of diminishing returns.

## 13. Risk of pruning too much

The fail-closed guards and hash/checkpoint logic are load-bearing. A careless merge could drop the writer lock (already missing on the active writer), weaken path-confinement, or collapse a real negative-guard into wording. Pruning must be **design-only first**, behavior-preserving, and gated by the required smoke suite. Do not delete source in the same block that merges contracts.

## 14–16. Pruning plan / freeze plan / what NOT to implement now

See `simplification-plan.md` (Blocks A–H). Freeze: the ledger kernel semantics, redaction service, path-confinement, and the static no-enable intent — refactor their packaging, not their behavior. **Do not implement now:** active read precedence, latest pointer, product read-model authority, public/product exposure, Production route, broader workspace actions, edit/update/delete, shell/subprocess, DB/KMS/WORM/cloud, release/commercial.

## 17. Next recommended block

**BLOCK A — Docs compaction + single current-architecture source of truth** (docs-only, zero code risk), immediately followed by **BLOCK B — naming consolidation design-only**. Reason: the doc/naming noise is the cheapest, highest-leverage fat to remove and it unblocks a clean contract-merge design (Block D) later.

## Scorecard (0–100)

| Metric | Score |
|---|---:|
| Product clarity | 35 |
| Architecture simplicity | 25 |
| Security posture | 82 |
| Evidence quality | 70 |
| Test value | 55 |
| Test noise | 70 |
| Documentation value | 45 |
| Documentation noise | 85 |
| Runtime efficiency | 62 |
| Maintainability | 32 |
| Onboarding clarity | 20 |
| Product readiness (local MVP) | 30 |
| Release readiness | 0 |

| Composite | Score |
|---|---:|
| BLOAT_SCORE | 78 |
| SIMPLIFICATION_URGENCY | 80 |
| PRODUCT_CORE_HEALTH | 62 |
| SECURITY_CORE_HEALTH | 82 |
| DOC_NOISE | 85 |
| TEST_NOISE | 68 |

## KEEP / MERGE / SIMPLIFY / DELETE / FREEZE matrix

| Component/area | Classification |
|---|---|
| Ledger append/verify/checkpoint kernel | KEEP · FREEZE (behavior) |
| Canonicalization + path-confinement validators | KEEP |
| Redaction-before-persistence service | KEEP · FREEZE |
| Persisted-candidate registry + active policy | MERGE (→ one gate) |
| 3 writer variants | MERGE (→ WriterMode) |
| Durable Stage1/2 + LocalTempCheckpoint | MERGE (→ ledger modes) |
| Per-node Result/Options/Request/Validation/Payload | SIMPLIFY (→ generic) |
| Status-suffix class names | KEEP_BUT_RENAME (→ status field) |
| 4+ UI/preview surfaces | MERGE (→ one read-model) |
| LatestState snapshot/manifest/aux/reader | MERGE (→ one evidence, role field) |
| Command handler + preview router | KEEP_BUT_RENAME (…Preview) |
| `*DesignOnlyProtected` / `*AntiCapabilityProof` | MERGE + HIDE_FROM_PRODUCT |
| Disabled writer scaffold | DELETE_CANDIDATE (future) |
| Mirrored Safety/Recipes tests | SIMPLIFY (de-dup, tier) |
| 331 ADRs / 221 handoffs / 231 QA | ARCHIVE + COMPACT |
| decision-log (1,810 lines) | COMPACT (head + archive) |
| Pilot `/run` + `ZeroReadOnly` label | KEEP_BUT_RENAME + gate (claim coherence) |
| Boundary-growth (new negatives) | FREEZE (stop expanding) |
| active read precedence / latest pointer / authority | DEFER |
