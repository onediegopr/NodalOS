# NODAL OS Naming Consolidation Design

Date: 2026-07-07

Mode: design-only / docs-only. This block does not rename source files, refactor source, change tests, change routes, delete history, alter runtime behavior, or enable product authority.

Baseline: Block A current architecture and full-system bloat audit.

## 1. Executive Verdict

The current naming layer over-encodes safety posture in type, route, test and document names. That has been useful for audit traceability, but it now hides the small local product kernel behind repeated status suffixes such as `Local`, `ReadOnly`, `Candidate`, `DesignOnly`, `Protected`, `CreateOnly`, `NotAuthority` and `NotPrecedence`.

The recommended direction is not an immediate source rename. The recommended direction is a design map from long status-suffix names to compact domain nouns plus explicit policy fields. Future implementation should be behavior-preserving and should keep safety facts visible as data, tests and guard claims instead of burying them in every class name.

Decision: `GO_WITH_FINDINGS_NAMING_CONSOLIDATION_DESIGN_READY`.

Findings: P0 0, P1 0, P2 0. P3 3 and P4 2 remain as maintainability/readability risks only.

## 2. Naming Bloat Diagnosis

The bloat pattern is consistent across Product Ledger, Approval, Durable audit trail, latest-state evidence, public preview guards, tests and docs:

- Domain nouns are often secondary to policy suffixes.
- Safety claims are duplicated across class names, route names, test names, ADR titles, QA reports and handoffs.
- State that should be represented by enum/status fields is encoded into names.
- Future/blocked authority is sometimes represented by negative names instead of a stable concept with `authority=None` or `precedence=None`.
- Routes repeat implementation history rather than a product/operator model.

Example:

`ProductLedgerLocalDurableLatestStateAuxiliaryEvidenceNotPrecedenceNotAuthority` should eventually become a `LatestStateEvidence` concept with fields such as:

- `evidenceRole=Auxiliary`
- `scope=LocalInternalDev`
- `authority=None`
- `precedence=None`
- `mutability=ReadOnly`
- `exposure=Internal`

That keeps the safety claim auditable without making the class name carry five policy dimensions.

## 3. Naming Principles

1. Prefer stable domain nouns over policy suffix chains.
2. Move policy facts into typed fields.
3. Keep safety claims visible in guard data, tests, documentation and static scans.
4. Preserve audit history during migration; do not delete historical names in the rename block.
5. Use "FutureOnly" explicitly only inside policy enums or docs, not as a class-name suffix.
6. Do not collapse distinct authority boundaries unless the policy model can represent the distinction.
7. Keep public/product naming separate from local/internal/dev naming until product exposure is explicitly authorized.
8. Avoid repo-wide `ZeroReadOnly` or unscoped no-runtime naming while Pilot `/run` and lab/dev runtime footprints exist.

## 4. Target Domain Names

The compact target vocabulary should fit the current local-only Product Ledger line:

| Target name | Purpose |
| --- | --- |
| `ApprovalDecision` | Human/operator decision and approval state. |
| `ApprovedAction` | Approved action request/result, including no-op and bounded local markers. |
| `EvidenceLedger` | Append-only local ledger kernel and verification model. |
| `HandoffDraft` | Local draft artifact written after approved local-only actions. |
| `WorkspaceDraft` | Workspace-scoped draft, with jail or allowlist policy as fields. |
| `SurfaceSnapshot` | Operator surface snapshot evidence. |
| `EvidenceManifest` | Manifest/index describing evidence entries. |
| `LatestStateEvidence` | Snapshot, manifest, reader candidate and auxiliary evidence under `evidenceRole`. |
| `OperatorSurfaceState` | One read model for local operator UI/diagnostics. |
| `GuardPolicy` | Fail-closed boundary policy for scope, exposure, authority and write mode. |
| `CapabilityMode` | Compact representation of disabled, preview, no-op, bounded and future active modes. |

## 5. Policy-As-Fields Proposal

The next source-refactor design should introduce a small policy vocabulary before renaming classes:

| Field | Suggested values | Notes |
| --- | --- | --- |
| `scope` | `LocalInternalDev`, `TestJail`, `WorkspaceAllowlisted` | Describes where the action/evidence may operate. |
| `authority` | `None`, `Candidate`, `ProductAuthorityFutureOnly` | `ProductAuthorityFutureOnly` is a marker, not current authority. |
| `precedence` | `None`, `Candidate`, `ActiveFutureOnly` | Active read precedence remains future-only. |
| `mutability` | `CreateOnly`, `AppendOnly`, `ReadOnly` | Replaces create/read/write suffixes. |
| `writeMode` | `Disabled`, `LocalTempTestOnly`, `LocalOnlyActive` | Consolidates writer variants. |
| `environment` | `FixtureSafe`, `LocalDev`, `LocalInternal`, `FutureProduction` | Current production remains unavailable. |
| `exposure` | `Internal`, `PublicFutureOnly` | Public UI actions remain future-only. |
| `safetyLevel` | `FailClosed`, `PreviewOnly`, `NoOp`, `BoundedNonDestructive` | Keeps action safety explicit. |
| `evidenceRole` | `Snapshot`, `Manifest`, `ReaderCandidate`, `Auxiliary` | Merges latest-state evidence concepts. |

Suggested enums:

```csharp
public enum Scope { LocalInternalDev, TestJail, WorkspaceAllowlisted }
public enum Authority { None, Candidate, ProductAuthorityFutureOnly }
public enum Precedence { None, Candidate, ActiveFutureOnly }
public enum Mutability { CreateOnly, AppendOnly, ReadOnly }
public enum Exposure { Internal, PublicFutureOnly }
public enum EvidenceRole { Snapshot, Manifest, ReaderCandidate, Auxiliary }
```

These names are illustrative only. They should not be added in this design-only block.

## 6. Old To New Naming Direction

| Current name family | Proposed domain name | Policy fields that carry the old suffixes |
| --- | --- | --- |
| `DurableAuditTrailAppendOnlyMinimal` | `EvidenceLedger` | `writeMode=LocalTempTestOnly`, `mutability=AppendOnly`, `environment=FixtureSafe` |
| `DurableAuditTrailAppendOnlyCandidate` | `EvidenceLedger` | `authority=Candidate`, `mutability=AppendOnly` |
| `ProductLedgerLocalApprovalDecisionStateStore` | `ApprovalDecisionStore` | `scope=LocalInternalDev`, `exposure=Internal` |
| `ProductLedgerLocalApprovedActionNoOpExecutor` | `ApprovedActionExecutor` | `safetyLevel=NoOp`, `scope=LocalInternalDev` |
| `ProductLedgerLocalBoundedApprovedActionExecutor` | `ApprovedActionExecutor` | `safetyLevel=BoundedNonDestructive`, `scope=LocalInternalDev` |
| `ProductLedgerLocalApprovedHandoffReportDraftExecutor` | `HandoffDraftWriter` | `scope=LocalInternalDev`, `mutability=CreateOnly` |
| `ProductLedgerLocalWorkspaceTestJailHandoffDraftExecutor` | `WorkspaceDraftWriter` | `scope=TestJail`, `mutability=CreateOnly` |
| `ProductLedgerLocalUserWorkspaceAllowlistedHandoffDraftExecutor` | `WorkspaceDraftWriter` | `scope=WorkspaceAllowlisted`, `mutability=CreateOnly` |
| `ProductLedgerLocalOperatorSurfaceLatestStateSnapshotExecutor` | `LatestStateEvidenceWriter` | `evidenceRole=Snapshot`, `mutability=CreateOnly` |
| `ProductLedgerLocalOperatorSurfaceLatestStateManifestWriter` | `LatestStateEvidenceWriter` | `evidenceRole=Manifest`, `mutability=CreateOnly` |
| `ProductLedgerLocalDurableLatestStateReaderCandidateValidator` | `LatestStateEvidenceValidator` | `evidenceRole=ReaderCandidate`, `authority=Candidate`, `precedence=Candidate` |
| `ProductLedgerLocalDurableLatestStateAuxiliaryEvidencePresenter` | `LatestStateEvidencePresenter` | `evidenceRole=Auxiliary`, `authority=None`, `precedence=None` |
| `ProductLedgerPublicUiReadOnlyDisabledPreview` | `OperatorSurfaceState` | `exposure=PublicFutureOnly`, `mutability=ReadOnly`, `writeMode=Disabled` |
| `*DesignOnlyProtected` and `*AntiCapabilityProof` | `GuardPolicy` plus static scan | `safetyLevel=FailClosed`, explicit anti-capability fields |

The full mapping table is in `docs/architecture/nodal-os-naming-consolidation-map.csv`.

## 7. Route Naming Consolidation Proposal

Current local dev routes repeat the implementation chain:

| Current route | Future route family | Fields |
| --- | --- | --- |
| `/internal/product-ledger/approval/decision` | `/internal/product-ledger/approval` | `action=decide` |
| `/internal/product-ledger/approval/state` | `/internal/product-ledger/approval` | `view=state` |
| `/internal/product-ledger/approval/execute` | `/internal/product-ledger/action` | `mode=no-op` |
| `/internal/product-ledger/approval/execute-bounded` | `/internal/product-ledger/action` | `mode=bounded` |
| `/internal/product-ledger/approval/create-local-handoff-draft` | `/internal/product-ledger/handoff-draft` | `scope=LocalInternalDev` |
| `/internal/product-ledger/approval/create-workspace-test-jail-handoff-draft` | `/internal/product-ledger/workspace-draft` | `scope=TestJail` |
| `/internal/product-ledger/approval/create-user-workspace-allowlisted-handoff-draft` | `/internal/product-ledger/workspace-draft` | `scope=WorkspaceAllowlisted` |
| `/internal/product-ledger/operator-surface/create-latest-state-snapshot` | `/internal/product-ledger/evidence/latest-state` | `role=Snapshot`, `action=create` |
| `/internal/product-ledger/operator-surface/latest-state-snapshot-state` | `/internal/product-ledger/operator-surface` | `panel=latest-state`, `role=Snapshot` |
| `/internal/product-ledger/operator-surface/create-latest-state-manifest` | `/internal/product-ledger/evidence/latest-state` | `role=Manifest`, `action=create` |
| `/internal/product-ledger/operator-surface/latest-state-manifest-state` | `/internal/product-ledger/operator-surface` | `panel=latest-state`, `role=Manifest` |
| `/internal/product-ledger/operator-surface/durable-latest-state-reader-candidate` | `/internal/product-ledger/evidence/latest-state` | `role=ReaderCandidate`, `mode=validate` |
| `/internal/product-ledger/operator-surface/durable-latest-state-auxiliary-evidence` | `/internal/product-ledger/evidence/latest-state` | `role=Auxiliary`, `mode=present` |

This route proposal is future design only. Current routes remain unchanged in this block.

## 8. Documentation Naming Proposal

Documentation should stop mirroring every name suffix. Future docs should use:

- One canonical architecture document for current truth.
- One rolling QA log for routine validation.
- One rolling handoff log for routine handoff.
- ADRs only for durable architectural decisions.
- Audit packets only when a block changes risk posture or creates a reusable review artifact.
- Short canonical names such as `Product Ledger Local Evidence`, `Latest State Evidence`, `Operator Surface`, `Approval Decision`, `Workspace Draft Boundary`.

Historical files should be indexed before archive/compaction. They should not be deleted in naming consolidation.

## 9. Migration Phases

| Phase | Scope | Allowed changes | Required guard |
| --- | --- | --- | --- |
| B1 | Design-only | This document and mapping table | `git diff --check`; docs-only diff |
| B2 | Test/static scan design | Test tier naming and scanner names | No coverage loss |
| B3 | Contract merge design | Introduce target model shape on paper | P0/P1 = 0 |
| B4 | Source compatibility aliases | Add compact types while keeping old names | Full Product Ledger Safety/Recipes |
| B5 | Route aliasing | Add compact internal routes while preserving old routes | Route smoke and DOM contract |
| B6 | Historical rename/archive | Move docs/tests names only after index coverage | Link/static checks |

No source rename should happen before B4. No public/product route should happen inside this sequence unless separately authorized.

## 10. Risks

P3 risks:

- A careless rename can weaken audit grepability for `NotAuthority`, `ReadOnly`, `DesignOnly`, `Protected` and similar load-bearing safety claims.
- Compact names can hide real authority distinctions if policy fields are not mandatory and fail-closed.
- Route aliases can accidentally look like product surface if `internal`, `local-only` and `default-off` claims are not visible.

P4 risks:

- Long historical names will remain in old docs and tests for a while, so the repo will carry mixed vocabulary during migration.
- Documentation compaction can break backlinks if archive/index work is skipped.

Mitigation:

- Add compatibility aliases first.
- Keep old tests until compact tests prove equivalent safety coverage.
- Keep static scans for old and new names during transition.
- Preserve historical names in mapping docs.

## 11. Next Recommended Block

`NODAL_OS_BLOCK_C_TEST_TIERING_AND_STATIC_SCAN_CONSOLIDATION_DESIGN_ONLY`.

Reason: after naming design, the next source of bloat is mirrored Safety/Recipes and repeated static no-enable scans. A design-only test-tier matrix should define required smoke gates versus extended suites before any source rename or contract merge.

Do not proceed to source refactor, active read precedence, latest pointer, product authority, public/product exposure, Production route, broader workspace action, cloud, DB, KMS/WORM, external trust, live Browser/CDP/WCU/OCR/Recipes automation, release or commercial readiness without a separate explicit GO.
