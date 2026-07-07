# NODAL OS Model Contract Merge Design

Date: 2026-07-07

Mode: design-only / docs-only / audit-only. This block does not modify `src/`, rename classes, rename files, delete contracts, change tests, change scanners, change runtime behavior, activate features, expose product/public routes, introduce active read precedence, latest pointer, product authority, cloud/network/DB, KMS/WORM, release or commercial readiness.

Baseline: Block A current architecture, Block B naming consolidation, Block C test tiering/static scan design and the full-system bloat audit.

## 1. Executive Verdict

NODAL OS has a strong local-only Product Ledger kernel hidden behind repeated contract families. The repeated shape is now clear: most Product Ledger and Approval nodes define their own `Decision`, `State`, `ActionKind`, `Blocker`, `Options`, `Request`, `Snapshot` or `Result`, even when the underlying semantics are the same: local/internal scope, fail-closed validation, no public/product exposure, no Production route, no command execution, no cloud/network/DB, no KMS/WORM and no release/commercial claim.

Decision: `GO_WITH_FINDINGS_MODEL_CONTRACT_MERGE_DESIGN_READY`.

The future merge should introduce shared contracts in parallel first, then migrate one low-risk capability, then role-based latest-state evidence and writer modes. The merge must preserve safety as data and tests, not just shorter names.

Findings: P0 0, P1 0, P2 0 new. P3 risks remain around accidental guardrail loss in a future implementation. P4 risks remain around mixed old/new contract vocabulary during transition.

## 2. Contract Bloat Diagnosis

Read-only inventory confirms the bloat pattern flagged by the full-system audit:

| Family | Representative files | Signal |
| --- | --- | --- |
| Approval decision and execution | `ProductLedgerLocalApprovalDecisionStateStore.cs`, `ProductLedgerLocalApprovedActionNoOpExecutor.cs`, `ProductLedgerLocalBoundedApprovedActionExecutor.cs` | Repeated decision/state/blocker/request/snapshot shapes. |
| Handoff drafts | `ProductLedgerLocalApprovedHandoffReportDraftExecutor.cs`, `ProductLedgerLocalWorkspaceTestJailHandoffDraftExecutor.cs`, `ProductLedgerLocalUserWorkspaceAllowlistedHandoffDraftExecutor.cs` | Same create-only writer semantics with different scope and output boundary. |
| Latest-state evidence | `ProductLedgerLocalOperatorSurfaceLatestStateSnapshotExecutor.cs`, `ProductLedgerLocalOperatorSurfaceLatestStateManifestWriter.cs`, `ProductLedgerLocalDurableLatestStateReaderCandidateValidator.cs`, `ProductLedgerLocalDurableLatestStateAuxiliaryEvidencePresenter.cs` | Same evidence chain with different role, authority and precedence claims. |
| Writer variants | `ProductLedgerPathWriterScaffoldDisabled.cs`, `ProductLedgerPathLocalTempWriterTestOnly.cs`, `ProductLedgerPathLocalOnlyActiveWriter.cs` | Same ledger writer family split by mode. |
| Durable audit trail variants | `DurableAuditTrailAppendOnlyMinimal.cs`, `DurableAuditTrailAppendOnlyCandidate.cs` | Minimal/candidate names encode authority and maturity in separate contracts. |
| Route/read-model DTOs | `ProductLedgerLocalDevRoutePreview.cs`, `ProductLedgerRenderableOperatorSurface.cs`, `ProductLedgerInternalOperatorUiPreview.cs`, `ProductLedgerOperatorSurfaceModel.cs`, `ProductLedgerOperatorSurfaceReadModelProvider.cs` | Multiple surface DTO layers describe the same local/internal operator state. |
| Static guard/result models | `ProductLedgerPathReadinessScaffold.cs`, `ProductLedgerInternalCommandPreviewRouter.cs`, design-only protected classes | Repeated blocker and anti-capability result models. |

Large Product Ledger nodes range from roughly 600 to 1000 lines and commonly define 7 to 10 local types. That is acceptable while proving boundaries, but expensive when every new boundary repeats the same guard shape.

## 3. Inventory Summary

| Current contract | File path | Purpose | Duplicated concepts | Policy encoded in name | Proposed target | Classification | Priority |
| --- | --- | --- | --- | --- | --- | --- | --- |
| `ProductLedgerLocalApprovalDecisionSnapshot` | `src/OneBrain.Core/Approval/ProductLedgerLocalApprovalDecisionStateStore.cs` | Persist approval decision state | decision, state, blockers, evidence refs | Local | `LocalOnlyResult<ApprovalDecision>` + `BoundaryClaims` | merge | High |
| `ProductLedgerLocalApprovedActionExecutionSnapshot` | `src/OneBrain.Core/Approval/ProductLedgerLocalApprovedActionNoOpExecutor.cs` | No-op approved action state | decision, state, blockers | Local, no-op | `LocalOnlyResult<ApprovedAction>` | merge | High |
| `ProductLedgerLocalBoundedApprovedActionSnapshot` | `src/OneBrain.Core/Approval/ProductLedgerLocalBoundedApprovedActionExecutor.cs` | Bounded local marker | decision, state, blockers | Local, bounded | `LocalOnlyResult<ApprovedAction>` | merge | High |
| `ProductLedgerLocalApprovedHandoffReportDraftSnapshot` | `src/OneBrain.Core/Approval/ProductLedgerLocalApprovedHandoffReportDraftExecutor.cs` | Local handoff draft create-only | request/options/snapshot/blockers | Local, draft | `LocalOnlyResult<HandoffDraft>` | merge | High |
| `ProductLedgerLocalWorkspaceTestJailHandoffDraftSnapshot` | `src/OneBrain.Core/Approval/ProductLedgerLocalWorkspaceTestJailHandoffDraftExecutor.cs` | Workspace test-jail draft | request/options/snapshot/blockers | Local, test jail, create-only | `LocalOnlyResult<WorkspaceDraft>` | merge | High |
| `ProductLedgerLocalUserWorkspaceAllowlistedHandoffDraftSnapshot` | `src/OneBrain.Core/Approval/ProductLedgerLocalUserWorkspaceAllowlistedHandoffDraftExecutor.cs` | Allowlisted workspace draft | request/options/snapshot/blockers | Local, allowlisted, create-only | `LocalOnlyResult<WorkspaceDraft>` | merge | High |
| `ProductLedgerLocalOperatorSurfaceLatestStateSnapshotResult` | `src/OneBrain.Core/Approval/ProductLedgerLocalOperatorSurfaceLatestStateSnapshotExecutor.cs` | Latest-state snapshot evidence | action kind, result, payload, blockers | Local, snapshot, create-only | `LatestStateEvidence` | merge | High |
| `ProductLedgerLocalOperatorSurfaceLatestStateManifestResult` | `src/OneBrain.Core/Approval/ProductLedgerLocalOperatorSurfaceLatestStateManifestWriter.cs` | Manifest evidence | action kind, result, payload, entry, blockers | Local, manifest, create-only | `LatestStateEvidence` | merge | High |
| `ProductLedgerLocalDurableLatestStateReaderCandidateResult` | `src/OneBrain.Core/Approval/ProductLedgerLocalDurableLatestStateReaderCandidateValidator.cs` | Reader candidate validation | validation, result, blockers | Local, durable, candidate | `LatestStateEvidence` | merge | High |
| `ProductLedgerLocalDurableLatestStateAuxiliaryEvidenceResult` | `src/OneBrain.Core/Approval/ProductLedgerLocalDurableLatestStateAuxiliaryEvidencePresenter.cs` | Auxiliary non-authority evidence | validation, result, blockers | Local, auxiliary, not authority, not precedence | `LatestStateEvidence` | merge | High |
| `DurableAuditTrailAppendOnlyMinimalResult` | `src/OneBrain.Core/Approval/DurableAuditTrailAppendOnlyMinimal.cs` | Append-only test ledger result | policy, request, entry, result, verification | Minimal, append-only | `EvidenceLedgerResult` | simplify | Medium |
| `DurableAuditTrailAppendOnlyCandidateResult` | `src/OneBrain.Core/Approval/DurableAuditTrailAppendOnlyCandidate.cs` | Candidate append-only preview | gate, request, preview, counts, result | Candidate | `EvidenceLedgerResult` | simplify | Medium |
| `ProductLedgerPathLocalOnlyAppendResult` | `src/OneBrain.Core/Approval/ProductLedgerPathLocalOnlyActiveWriter.cs` | Active local-only writer append | activation, append, entry, checkpoint | Local-only active | `EvidenceLedgerResult` + `WriterMode` | merge | High |
| `ProductLedgerPathLocalTempWriterResult` | `src/OneBrain.Core/Approval/ProductLedgerPathLocalTempWriterTestOnly.cs` | Local-temp test writer | request, entry, checkpoint, result | Local temp test-only | `EvidenceLedgerResult` + `WriterMode` | merge | High |
| `ProductLedgerPathWriterScaffoldResult` | `src/OneBrain.Core/Approval/ProductLedgerPathWriterScaffoldDisabled.cs` | Disabled writer scaffold | request/result/blockers | Disabled scaffold | `EvidenceLedgerResult` + `WriterMode=Disabled` | delete-candidate future | Medium |
| `ProductLedgerLocalDevRoutePreviewResult` | `src/OneBrain.Core/Approval/ProductLedgerLocalDevRoutePreview.cs` | Local dev route preview | route decision, blockers, nested surface states | Local dev preview | `OperatorSurfaceReadModel` | merge | Medium |
| `ProductLedgerRenderableOperatorSurfaceResult` | `src/OneBrain.Core/Approval/ProductLedgerRenderableOperatorSurface.cs` | Renderable surface result | request/model/result/blockers | Renderable surface | `OperatorSurfaceReadModel` | merge | Medium |
| `ProductLedgerInternalOperatorUiPreviewResult` | `src/OneBrain.Core/Approval/ProductLedgerInternalOperatorUiPreview.cs` | Internal UI preview | header/sections/actions/severity | Internal preview | `OperatorSurfaceReadModel` | merge | Medium |
| `ProductLedgerPathReadinessResult` | `src/OneBrain.Core/Approval/ProductLedgerPathReadinessScaffold.cs` | Readiness scaffold | risk previews, blockers | Readiness scaffold | `GuardEvaluationResult` | merge | Medium |
| `ProductLedgerInternalCommandPreviewResult` | `src/OneBrain.Core/Approval/ProductLedgerInternalCommandPreviewRouter.cs` | Internal command preview | command kind, blockers, preview | Internal command preview | `GuardEvaluationResult` + `CapabilityMode` | freeze/merge | Medium |

The full row-level merge map is in `docs/architecture/nodal-os-model-contract-merge-map.csv`.

## 4. Common Contract Proposal

This is a future design. Do not implement in this block.

### `LocalOnlyResult<T>`

Purpose: common envelope for Product Ledger local/internal operations.

Suggested fields:

- `success`
- `state`
- `value`
- `blockers`
- `warnings`
- `evidenceRefs`
- `classification`
- `safeNextStep`
- `claims`

Notes:

- `success=false` must be the default for null/malformed/unauthorized inputs.
- `value` must not carry raw secrets, raw paths or unredacted payloads.
- `claims` should contain the safety/authority posture instead of encoding it in names.

### `BoundaryClaims`

Purpose: one explicit safety posture record used by local-only results and guard scanners.

Suggested fields:

- `scope`
- `authority`
- `precedence`
- `exposure`
- `mutability`
- `writeMode`
- `environment`
- `publicProductAllowed`
- `productionAllowed`
- `commandExecutionAllowed`
- `cloudAllowed`
- `databaseAllowed`
- `kmsWormExternalTrustAllowed`
- `releaseCommercialAllowed`
- `pilotRunCoupled`

Required invariant:

Current Product Ledger local/internal results must set public/product, Production, command execution, cloud, DB, KMS/WORM, release/commercial and Pilot `/run` coupling to false unless a future explicit GO changes that boundary.

### `Blocker`

Purpose: common fail-closed reason model.

Suggested fields:

- `code`
- `severity`
- `message`
- `source`
- `evidenceRefs`
- `category`

Suggested categories:

- `MissingInput`
- `UnsafeInput`
- `BoundaryRejected`
- `AuthorityRejected`
- `PrecedenceRejected`
- `PathRejected`
- `RedactionRejected`
- `TamperDetected`
- `ExternalCapabilityRejected`
- `ReleaseCommercialRejected`

### `EvidenceRef`

Use existing canonical evidence reference concepts where possible. Do not create a parallel evidence reference hierarchy if `ProductLedgerOperatorSurfaceEvidenceRef`, string evidence refs or ledger entry refs already satisfy a local result.

Target shape, if needed later:

- `id`
- `kind`
- `hash`
- `safeSummary`
- `source`
- `classification`

### `WriterMode`

Suggested values:

- `ReadOnly`
- `CreateOnly`
- `AppendOnly`
- `VersionedCreateOnly`
- `Disabled`

Mapping:

- no-op and route previews: `ReadOnly`
- handoff drafts and latest-state snapshot: `CreateOnly`
- manifest: `VersionedCreateOnly`
- ledger append: `AppendOnly`
- disabled scaffold/public preview: `Disabled`

### `EvidenceRole`

Suggested values:

- `Snapshot`
- `Manifest`
- `ReaderCandidate`
- `Auxiliary`
- `HandoffDraft`
- `WorkspaceDraft`
- `LedgerEntry`
- `Checkpoint`

### `LatestStateEvidence`

Purpose: merge snapshot, manifest, reader candidate and auxiliary evidence under role and claims.

Suggested fields:

- `id`
- `role`
- `refs`
- `hashes`
- `staleState`
- `tamperState`
- `authority`
- `precedence`
- `evidenceRefs`
- `classification`
- `claims`
- `safeSummary`

Required current roles:

- `Snapshot`: historical evidence only, create-only.
- `Manifest`: versioned evidence index, not latest pointer.
- `ReaderCandidate`: candidate only, not product authority.
- `Auxiliary`: not authority, not precedence.

### `GuardEvaluationResult`

Purpose: common result for static guards, readiness scaffolds and command previews.

Suggested fields:

- `decision`
- `blockers`
- `warnings`
- `claims`
- `scannedScope`
- `matchedTokens`
- `allowedNegativeContexts`
- `safeNextStep`

This should align with the future `NodalOsStaticGuardCatalog` from Block C.

## 5. Merge Map

| Current family | Proposed target | Fields kept | Moved to `BoundaryClaims` | Moved to role/mode | Security implication | Tests required |
| --- | --- | --- | --- | --- | --- | --- |
| Approval decision snapshots | `LocalOnlyResult<ApprovalDecision>` | decision id, operator decision, candidate action, evidence refs | local/internal, no product, no command | none | approval remains state only, not execution | Tier 1 approval/Product Ledger safety |
| Approved no-op/bounded action snapshots | `LocalOnlyResult<ApprovedAction>` | action id, candidate, state, evidence refs | no command/product/cloud | `CapabilityMode=NoOp/Bounded` | bounded marker must not become real destructive action | Tier 1 + route/DOM if surfaced |
| Handoff draft snapshots | `LocalOnlyResult<HandoffDraft>` / `WorkspaceDraft` | draft id, path-safe relative refs, evidence hash | scope, mutability, no overwrite, no arbitrary path | `EvidenceRole=HandoffDraft/WorkspaceDraft`, `WriterMode=CreateOnly` | path confinement and create-only must stay explicit | Tier 1 path/redaction + Tier 3 corpus |
| Latest-state snapshot/manifest | `LatestStateEvidence` | id, payload refs, hash, state, blockers | local/internal, no public/product | `EvidenceRole=Snapshot/Manifest`, `WriterMode=CreateOnly/VersionedCreateOnly` | manifest must not become latest pointer | Tier 1 latest-state + Tier 2 route |
| Reader candidate/auxiliary evidence | `LatestStateEvidence` | validation state, refs, stale/tamper state | authority, precedence, no product | `EvidenceRole=ReaderCandidate/Auxiliary` | reader candidate not authority; auxiliary not precedence | Tier 1 not-authority/not-precedence |
| DurableAuditTrail Minimal/Candidate | `EvidenceLedgerResult` | append result, verification, counts | authority/candidate status | `WriterMode=AppendOnly` | candidate must not imply product authority | Tier 1 durable/Product Ledger ledger tests |
| Writer scaffold/temp/active | `EvidenceLedgerResult` | entry, checkpoint, hash, sequence | scope, environment, write permission | `WriterMode=Disabled/AppendOnly` | disabled/temp/active must remain distinguishable | Tier 1 writer/path + Tier 3 tamper |
| Route/read-model DTOs | `OperatorSurfaceReadModel` | sections, actions, disabled actions, status | exposure, public/product, production | surface mode | operator route remains local/dev/internal | Tier 2 route/DOM |
| Static guard/readiness DTOs | `GuardEvaluationResult` | blockers, warnings, tokens, status | public/product/cloud/release booleans | guard category | centralization must not weaken hard-fail scans | Tier 1 static guard + Tier 3 docs scan |

## 6. Security Preservation Design

| Invariant | Lives today | After merge | Tests required |
| --- | --- | --- | --- |
| Fail-closed null/malformed inputs | Per-node blockers and rejected states | `LocalOnlyResult<T>.success=false` plus `Blocker` | Tier 1 Product Ledger Safety and Approval Safety |
| Redaction-before-persistence | `RedactionBeforePersistenceService`, metadata guard and writer tests | Keep service separate; only reference redaction state in common result | Redaction-focused Safety plus Product Ledger writer tests |
| Path confinement | path validators, handoff draft writers, canonicalization tests | Keep validators separate; claims and blockers report path boundary | Tier 1 path canonicalization plus Tier 3 traversal corpus |
| No overwrite/create-only | handoff, snapshot, manifest writers | `WriterMode=CreateOnly/VersionedCreateOnly`, explicit blocker category | Handoff/latest-state Safety and Recipes |
| No authority/no precedence/no latest pointer | reader/auxiliary/latest-state contracts | `BoundaryClaims.authority`, `BoundaryClaims.precedence`, `LatestStateEvidence.role` | Latest-state not-authority/not-precedence tests |
| No public/product or Production route | route/static scan tests and surface models | `BoundaryClaims.exposure`, `productionAllowed=false` | Route 200/404 and static no-public/no-Production scans |
| Hash/checkpoint/tamper validation | active/temp writers and Durable audit trail | Keep kernel separate; report verification in `EvidenceLedgerResult` | Tier 1 writer/hash/checkpoint and Tier 3 tamper |
| `/run` claim-coherence separation | docs, Pilot gate tests and Product Ledger route tests | `BoundaryClaims.pilotRunCoupled=false` for Product Ledger | `/run` claim-coherence guard and Product Ledger route tests |
| No cloud/DB/KMS/WORM/release | per-node blockers and docs/static scans | common `BoundaryClaims` booleans plus central scanner | Tier 1 static guard and Tier 3 docs scan |

Non-negotiable rule: do not merge a safety service into an envelope if that makes the safety service optional. Redaction, path canonicalization and hash/checkpoint verification remain real components, not just fields.

## 7. Migration Phases

### D1 - Add common contracts in parallel

- Objective: add `LocalOnlyResult<T>`, `BoundaryClaims`, `Blocker`, `WriterMode`, `EvidenceRole` and `GuardEvaluationResult` without removing old contracts.
- Expected files: future source under Core/Approval or a shared Core namespace, plus tests.
- Risks: names look authoritative before adoption.
- Required tests: Tier 1.
- Rollback: remove new parallel contracts.
- Stop conditions: behavior changes, public/product claim, scanner weakening.
- Implementation status: completed as design/test-only in `NODAL_OS_BLOCK_D1_COMMON_CONTRACTS_PARALLEL_DESIGN_TEST_ONLY`.
- Actual D1 artifact location: `tests/OneBrain.Safety.Tests/NodalOsCommonContractsDesignOnlyCandidate.cs`.
- Actual D1 scope: candidate contracts live only in Safety tests and are explicitly descriptive/non-wired. No `src/` contracts were added, replaced or referenced.
- Added test evidence: `NodalOsCommonContractsDesignOnlyCandidateTests` validates blocked public/product, Production route, latest pointer, read precedence, product authority, command execution, shell/subprocess, provider/cloud/network, DB/migration, KMS/WORM/external trust, release/commercial, Pilot `/run` coupling and CI-enforcement claims.
- D1 does not authorize D2 migration by itself; D2 must add mapping adapters/equivalence in parallel and keep old contracts until old/new behavior is proven equivalent.

### D2 - Adapt one low-risk capability to common contracts

- Objective: adapt a read-only or preview-only capability first, such as internal operator UI preview.
- Expected files: target capability plus compatibility adapter.
- Risks: route/render surface shifts unexpectedly.
- Required tests: Tier 1 + Tier 2 route/DOM if surfaced.
- Rollback: revert adapter.
- Stop conditions: output claim drift, route behavior drift.
- Implementation status: completed as test/design-only mapping equivalence in `NODAL_OS_BLOCK_D2_MAPPING_ADAPTERS_EQUIVALENCE_EXPANSION_TEST_ONLY`.
- Actual D2 artifact location: `tests/OneBrain.Safety.Tests/NodalOsCommonBoundaryMappingDesignOnlyAdapter.cs`.
- Actual D2 scope: the mapper lives only in Safety tests, maps existing hard-block concepts into the D1 candidate envelope, and cannot override existing hard-block decisions.
- Added test evidence: `NodalOsCommonBoundaryMappingDesignOnlyAdapterTests` proves public/product, Production route, latest pointer, read precedence, product authority, command execution, release/commercial, Product Ledger local/design-only boundary, `/run` claim coherence and static guard hard blocks map to blocked common claims.
- Fail-closed rule: unsupported, unknown or non-authoritative source concepts map to rejected/blocked, never allowed.
- D2 does not create a production adapter. The next source-facing step must be D3 plan/audit or a minimal parallel source candidate with a separate explicit GO and no-runtime wiring guard.

### D3 - Source refactor plan/audit

Objective: choose the first future source-facing move using D1/D2 evidence before touching `src/`.

Implementation status: completed as docs/audit-only in `NODAL_OS_BLOCK_D3_SOURCE_REFACTOR_PLAN_AUDIT_ONLY`.

Canonical D3 plan:

- `docs/architecture/nodal-os-d3-source-refactor-plan-audit.md`

Decision: recommend `AUTHORIZE_NODAL_OS_BLOCK_D4_MINIMAL_SOURCE_CANDIDATE_NO_RUNTIME_WIRING` as the next source-facing block. D3 does not authorize D4 and does not implement any source change.

Selected D4 first move: add a minimal, non-wired, parallel source candidate for common boundary claims. Do not migrate latest-state, handoff, route, UI or writer models first.

Stop conditions: any need to edit existing Product Ledger behavior files, routes, DI, CI, public/product claims, Production route, latest pointer, read precedence, product authority, command execution or release/commercial claims.

### D4 - Minimal source candidate, no runtime wiring

- Objective: add one isolated source-side common boundary-claims candidate with no consumers and no runtime/product authority.
- Expected files: one new Core/Approval source file plus no-runtime/no-reference tests.
- Implementation status: completed in `NODAL_OS_BLOCK_D4_MINIMAL_SOURCE_CANDIDATE_NO_RUNTIME_WIRING`.
- Actual D4 source artifact: `src/OneBrain.Core/Approval/NodalOsCommonBoundaryClaimsCandidate.cs`.
- Actual D4 scope: exactly one parallel-only, non-authoritative source candidate. It is not registered, routed, service-wired, command-wired, CI-enforced or consumed by runtime/product code.
- Actual D4 test evidence: `NodalOsCommonBoundaryClaimsCandidateTests` validates fail-closed defaults, public/product, Production route, latest pointer, read precedence, product authority, command execution, release/commercial, runtime/product no-go, unknown/ambiguous fail-closed behavior, no wiring/no authority guards and D1/D2 compatibility.
- Non-goals preserved: existing Product Ledger models/contracts remain unchanged; D1/D2 remain test/design-only; existing hard-block authorities remain authoritative.
- Risks: candidate can be mistaken for authority if named or documented poorly.
- Required tests: Tier 1, CommonContracts, MappingAdapters, Product Ledger Safety/Recipes, static guard and no-reference scans.
- Rollback: remove the new candidate source file and D4 tests.
- Stop conditions: any route/DI/command-handler/CI reference or existing behavior drift.
- Next step: D5 equivalence hardening/no-runtime reference audit, then D6 minimal replacement plan/audit only or STOP_FOR_AUDIT; not a broad refactor.

### D5 - Equivalence hardening, no runtime reference audit

- Objective: harden D1/D2/D4 equivalence and candidate isolation before any replacement plan.
- Implementation status: completed in `NODAL_OS_BLOCK_D5_EQUIVALENCE_HARDENING_NO_RUNTIME_REFERENCE_AUDIT`.
- Actual files: Safety tests and docs/log updates only.
- Actual D5 evidence: `NodalOsCommonBoundaryClaimsCandidateIsolationHardeningTests`.
- New labels: `NoAuthority`, `NoDoubleTruth`.
- Non-goals preserved: no `src/` changes, no new source candidate, no D4 candidate modification, no existing contract replacement, no runtime/product wiring, no CI enforcement and no source bloat reduction.
- Next step: D6 minimal replacement plan/audit only or STOP_FOR_AUDIT.

### D6 - Minimal replacement plan/audit only

- Objective: choose one future minimal replacement path related to the D4 source candidate without implementing it.
- Implementation status: completed as docs/audit/plan-only in `NODAL_OS_BLOCK_D6_MINIMAL_REPLACEMENT_PLAN_AUDIT_ONLY`.
- Canonical D6 plan: `docs/architecture/nodal-os-d6-minimal-replacement-plan-audit.md`.
- Actual files: docs/log updates only.
- Selected future D7 recommendation: `AUTHORIZE_NODAL_OS_BLOCK_D7_MINIMAL_REPLACEMENT_IMPLEMENTATION_NO_RUNTIME_CHANGE`.
- Selected D7 source target: `src/OneBrain.Core/Approval/ReentryDecisionPacketReadOnly.cs`.
- Selection rationale: the reentry packet is read-only, fixture-safe, non-route, non-DI, non-command, not Product Ledger runtime-facing and has existing Safety/Recipes evidence.
- Non-goals preserved: no `src/` changes, no test changes, no CI changes, no source replacement, no source bloat reduction, no runtime/product wiring and no release/commercial readiness.
- D7 remains unauthorized until explicit Diego GO.

### D7 - Minimal replacement implementation, no runtime change

- Objective: apply the single D6-selected source-facing replacement without changing runtime/product behavior.
- Implementation status: completed in `NODAL_OS_BLOCK_D7_MINIMAL_REPLACEMENT_IMPLEMENTATION_NO_RUNTIME_CHANGE`.
- Actual source target: `src/OneBrain.Core/Approval/ReentryDecisionPacketReadOnly.cs`.
- Actual test evidence: `ReentryDecisionPacketReadOnlyCommonBoundaryD7Tests` plus a one-file D5 allowed-reference guard update.
- Candidate usage: `NodalOsCommonBoundaryClaimsCandidate.DefaultBlocked()` is referenced only in the selected reentry packet target as a private local fail-closed proof input for `PassesSafetyProof`.
- Non-authority rule: existing reentry counters/statuses, D1/D2 test-only contracts and existing hard-block tests remain authoritative. The D4 candidate cannot override hard blocks and is not product/runtime authority.
- Non-goals preserved: no route/DI/service registration, command handler, Product Ledger runtime/latest-state/handoff/writer, public/product exposure, Production route, latest pointer/read precedence/product authority, CI change or release/commercial readiness.
- Actual bloat impact: effectively `0%` net source bloat reduction; D7 is additive proof-only rather than deletion/removal.
- Next step: D8 post-replacement isolation/equivalence audit or STOP_FOR_AUDIT, not broad refactor.

### D8 - Post-replacement isolation/equivalence audit

- Objective: audit the D7 replacement without implementing another replacement.
- Implementation status: completed in `NODAL_OS_BLOCK_D8_POST_REPLACEMENT_ISOLATION_EQUIVALENCE_AUDIT`.
- Actual evidence: `ReentryDecisionPacketReadOnlyPostReplacementD8Tests`.
- Scope: test/audit/docs-only; no `src/` changes; no D4 candidate change; no Reentry source change; no second replacement.
- Verified: the D7 command guard exception is exact to `ReentryDecisionPacketReadOnly.cs`, does not authorize command handlers, shell/subprocess, runtime command execution, product command execution, route/DI/service registration or similar future files.
- Verified: candidate references remain limited to the candidate file, the D7 target, Safety tests and docs/logs.
- Verified: D4 remains non-authoritative, Reentry remains read-only/non-runtime, D1/D2 remain design/test-only and existing hard-block tests remain authoritative.
- Runtime/product enablement remains `0%`; CI enforcement remains `0%`; release/commercial remains `0% / NO-GO`.
- Source bloat reduction remains `0%`; D7 was additive proof-only with net +70 source lines.
- Next step: `STOP_FOR_AUDIT` or `D9 second minimal replacement plan/audit only`, not broad refactor.

### D9 - Second minimal replacement plan/audit only

- Objective: decide whether one equally safe second source-facing replacement exists after D7/D8, without implementing it.
- Implementation status: completed as docs/audit/plan-only in `NODAL_OS_BLOCK_D9_SECOND_MINIMAL_REPLACEMENT_PLAN_AUDIT_ONLY`.
- Canonical D9 plan: `docs/architecture/nodal-os-d9-second-minimal-replacement-plan-audit.md`.
- Actual files: docs/log updates only.
- Selected future D10 recommendation: `AUTHORIZE_NODAL_OS_BLOCK_D10_SECOND_MINIMAL_REPLACEMENT_IMPLEMENTATION_NO_RUNTIME_CHANGE`.
- Selected future D10 source target: `src/OneBrain.Core/Approval/ApprovalExecutionDesignOnlyProtected.cs`.
- Selection rationale: the target is a deterministic Core Approval fixture, read-only/design-only/preview-only, non-route, non-DI, non-service-registered, non-writer, not Product Ledger runtime/latest-state/handoff-facing and already covered by focused Safety/Recipes tests.
- Required D10 guard rule: if authorized, the D4 candidate may be referenced only as a private local fail-closed proof in the selected target, with exact source-reference, no-authority, no-double-truth and command-guard exception tests.
- Non-goals preserved: no `src/` changes, no tests changed, no CI change, no second replacement, no D4 candidate modification, no D7 target modification, no candidate reference broadening, no source bloat reduction, no runtime/product enablement and no release/commercial readiness.
- D10 remained unauthorized until explicit Diego GO and was later executed as the next section records.

### D10 - Second minimal replacement implementation, no runtime change

- Objective: apply the D9-selected second minimal source-facing replacement without runtime/product behavior change.
- Implementation status: completed in `NODAL_OS_BLOCK_D10_SECOND_MINIMAL_REPLACEMENT_IMPLEMENTATION_NO_RUNTIME_CHANGE`.
- Actual source target: `src/OneBrain.Core/Approval/ApprovalExecutionDesignOnlyProtected.cs`.
- Actual test evidence: `ApprovalExecutionDesignOnlyProtectedCommonBoundaryD10Tests` plus exact D4/D5/D7/D8 allowed-reference guard updates.
- Candidate usage: `NodalOsCommonBoundaryClaimsCandidate.DefaultBlocked()` is referenced only in `ApprovalExecutionAntiCapabilityProof` as a private local fail-closed proof input for `Passes`.
- Non-authority rule: existing Approval execution readiness fields, gates, previews, anti-capability booleans, D1/D2 test-only contracts and existing hard-block tests remain authoritative. The D4 candidate cannot override hard blocks and is not product/runtime authority.
- Non-goals preserved: no route/DI/service registration, command handler, Product Ledger runtime/latest-state/handoff/writer, public/product exposure, Production route, latest pointer/read precedence/product authority, CI change, Browser/CDP/WCU/OCR/Recipes live behavior or release/commercial readiness.
- Actual bloat impact: source bloat reduction remains `0%`; D10 is additive proof-only with net `+70` lines in the selected source file. Cumulative D7+D10 source impact is net `+140` source lines.
- Next step: D11 post-second-replacement isolation/equivalence audit or STOP_FOR_AUDIT, not a third replacement or broad refactor.

### D11 - Post-second-replacement isolation/equivalence audit

- Objective: audit the D10 second minimal replacement without implementing another replacement.
- Implementation status: completed in `NODAL_OS_BLOCK_D11_POST_SECOND_REPLACEMENT_ISOLATION_EQUIVALENCE_AUDIT`.
- Actual evidence: `ApprovalExecutionPostSecondReplacementD11Tests`.
- Scope: test/audit/docs-only; no `src/` changes; no D4 candidate, D7 target or D10 target source changes; no third replacement.
- Verified: the D10 command/execution exception remains exact to `ApprovalExecutionDesignOnlyProtected.cs`, D7 and D10 exceptions are independent and file-exact, and no broad command/execution allowlist was introduced.
- Verified: candidate references remain limited to the candidate file, D7 target, D10 target, Safety tests and docs/logs.
- Verified: D4 remains non-authoritative, D7 and D10 remain read-only/design-only/non-runtime, D1/D2 remain design/test-only and existing hard-block tests remain authoritative.
- Runtime/product enablement remains `0%`; CI enforcement remains `0%`; release/commercial remains `0% / NO-GO`.
- Source bloat reduction remains `0%`; D7+D10 cumulative source impact remains net `+140` lines. The D-series has so far proven equivalence/isolation, not reduced source bloat.
- Next step: `D12 source-reduction plan/audit only` or STOP_FOR_AUDIT, not another proof-only replacement by default.

### D12 - Source reduction plan/audit only

- Objective: choose whether actual source reduction can safely start after D7/D10/D11, without implementing it.
- Implementation status: completed as docs/audit/plan-only in `NODAL_OS_BLOCK_D12_SOURCE_REDUCTION_PLAN_AUDIT_ONLY`.
- Canonical D12 plan: `docs/architecture/nodal-os-d12-source-reduction-plan-audit.md`.
- Actual files: docs/log updates only.
- Selected future D13 recommendation: `AUTHORIZE_NODAL_OS_BLOCK_D13_MINIMAL_SOURCE_REDUCTION_IMPLEMENTATION_NO_RUNTIME_CHANGE`.
- Selected future D13 source target: `src/OneBrain.Core/Approval/ApprovalExecutionDesignOnlyProtected.cs`.
- Selection rationale: D10 is the newer duplicated proof target, read-only/design-only/protected, non-route, non-DI, non-service-registered and already covered by D10/D11 focused tests. Compacting its repeated private claim/state proof is lower risk than touching D4, D7 or Product Ledger contracts.
- Required D13 guard rule: if authorized, D13 may compact repeated expected-claim checks inside the D10 target only while preserving D4 non-authority, D7 isolation, D10/D11 focused guard meaning and the exact allowed source reference set.
- Non-goals preserved: no `src/` changes in D12, no tests changed, no CI change, no reduction implemented, no D4 candidate modification, no D7 target modification, no Product Ledger source behavior change, no runtime/product enablement and no release/commercial readiness.
- Source bloat reduction remains `0%`; D7+D10 cumulative source impact remains net `+140` lines until a future D13 actually removes lines and passes gates.
- Next step: `AUTHORIZE_NODAL_OS_BLOCK_D13_MINIMAL_SOURCE_REDUCTION_IMPLEMENTATION_NO_RUNTIME_CHANGE` with explicit Diego authorization.

### Future later phase - Migrate latest-state snapshot/manifest/reader/auxiliary

- Objective: migrate four latest-state roles to `LatestStateEvidence`.
- Expected files: latest-state contracts and tests.
- Risks: manifest becomes latest pointer by accident; reader candidate becomes authority.
- Required tests: Tier 1 latest-state + Tier 2 route + Tier 3 role corpus.
- Rollback: keep old role-specific contracts as compatibility aliases.
- Stop conditions: any active read precedence/latest pointer/product authority drift.

### Future later phase - Migrate handoff draft variants

- Objective: migrate local, test-jail and allowlisted workspace drafts to role/mode fields.
- Expected files: handoff writer contracts and tests.
- Risks: scope broadening or path boundary weakening.
- Required tests: Tier 1 path/redaction + Tier 3 traversal corpus.
- Rollback: old writers remain compatibility path.
- Stop conditions: arbitrary path, overwrite, user-selected path, filesystem scan.

### Future later phase - Merge DurableAuditTrail Minimal/Candidate

- Objective: merge minimal/candidate into one evidence ledger result shape.
- Expected files: Durable audit trail contracts/tests.
- Risks: candidate status overclaims product authority.
- Required tests: Tier 1 Durable/Product Ledger ledger tests.
- Rollback: retain old minimal/candidate wrappers.
- Stop conditions: runtime/product enablement or authority claim.

### Future later phase - Remove/deprecate old contracts

- Objective: only after compatibility and test equivalence, deprecate old names.
- Expected files: source/test/docs with explicit GO.
- Risks: lost grepability and audit history.
- Required tests: Tier 1 + Product Ledger Safety/Recipes + selected Tier 3.
- Rollback: revert deletion/deprecation commit.
- Stop conditions: coverage gap, missing old/new scanner coverage.

### Future later phase - Audit no guardrail loss

- Objective: compare old and new guard coverage.
- Expected files: audit report and matrix.
- Risks: overconfidence from passing narrow tests.
- Required tests: Tier 1 + Tier 2 + selected Tier 3.
- Rollback: pause further merge.
- Stop conditions: any P0/P1/P2, TRUE_RISK, public/product/release drift.

## 8. What Not To Merge Yet

Do not merge or hide these as generic fields in the first implementation block:

- Product Ledger authority semantics.
- `RedactionBeforePersistenceService`.
- Path canonicalization validators.
- Hash-chain/checkpoint kernel.
- `/run` and Pilot runtime execution boundary.
- ChromeLab/Browser/OCR/WCU tracks.
- Release/commercial docs.
- Public/product gates.
- Active read precedence and latest pointer.
- Product read-model authority.

These are load-bearing boundaries or separate runtime footprints. They may be referenced by shared claims, but not collapsed into vague generic status until dedicated design and tests prove no loss.

## 9. Risks

P3 risks:

- Common envelopes can hide hard boundary facts if `BoundaryClaims` is optional.
- Generic blockers can erase specific failure modes that current tests expect.
- Role/mode fields can accidentally turn non-authority evidence into active authority if defaults are unsafe.
- Compatibility aliases can leave two truth sources if not audited.

P4 risks:

- Old and new contracts will coexist for at least one migration phase.
- Historical docs/tests will keep long names.
- Scanner and test tier implementation must follow Block C before any deletion.

Mitigations:

- Add common contracts in parallel.
- Default all claims to denied/fail-closed.
- Keep old contracts until test equivalence is proven.
- Require old-name and new-name static scans during transition.
- Run Tier 1 for every source migration and Tier 2/Tier 3 for route/writer changes.

## 10. Next Recommended Block

`NODAL_OS_BLOCK_E_SOURCE_REFACTOR_READINESS_AUDIT_DESIGN_ONLY`.

Reason: Blocks A-D now define current-state docs, naming, test tiering/static scan consolidation and contract merge design. The next safe step should be a readiness audit that checks whether an implementation GO can be scoped to one low-risk parallel-contract pilot. It should remain design/audit-only unless Diego explicitly authorizes source implementation.
