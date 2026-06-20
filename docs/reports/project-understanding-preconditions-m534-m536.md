# NODAL OS — Project Understanding Preconditions Report — M534-M536

**Project:** NODAL OS
**Milestones:** M534 (Governed Project Understanding Preconditions) + M535 (Assignment Archive Review) + M536 (Next Phase ADR)
**Date:** 2026-06-20
**Decision:** M534+M535+M536 CERRADO / PROJECT_UNDERSTANDING_PRECONDITIONS_READY

---

## 1. Executive Summary

M534-M536 closes the transition from Assignment/Planner Governance (M519-M533) to the next governed phase. No real Project Understanding capability is implemented. Instead:

- M534 defines all preconditions required before any real scan, filesystem access, LLM context build, indexing, embeddings, or cloud sync.
- M535 archives the Assignment/Planner line (M519-M533) as a governance baseline only — not a runtime baseline, not a planner implementation, not an LLM prompt source, not a filesystem authority.
- M536 records a formal ADR deciding that NODAL OS does not advance directly to real Project Understanding and defining all required future gates.

All caps-sensitive capabilities remain blocked. Build: 0 errors. Tests: full suite green.

---

## 2. M534 — Governed Project Understanding Preconditions

### Preconditions Defined

| Precondition | Required |
|---|---|
| Explicit user consent | yes |
| Path jail validation | yes |
| Scan scope preview | yes |
| Redaction policy | yes |
| Secret detection policy | yes |
| Exclusion policy | yes |
| Symlink policy | yes |
| Case sensitivity policy | yes |
| Max file limits | yes |
| Cancellation semantics | yes |
| Evidence plan | yes |
| Timeline events | yes |
| No mutation guarantee | yes |
| No cloud default | yes |
| No LLM before context approval | yes |
| Audit before real scan | yes |

### Readiness Result

| Capability | Ready |
|---|---|
| Real Project Understanding | **false** |
| Filesystem Scan | **false** |
| LLM Context Build | **false** |
| Embeddings | **false** |
| Indexing | **false** |
| Cloud Sync | **false** |

### Outputs

- `src/OneBrain.AgentOperations.Contracts/NodalOsProjectUnderstandingPreconditionsContracts.cs`
- `src/OneBrain.AgentOperations.Core/NodalOsProjectUnderstandingPreconditionsServices.cs`
- `artifacts/agent-operations/m536/project-understanding-preconditions-summary.json`
- `artifacts/agent-operations/m536/project-understanding-preconditions-preview.html`

---

## 3. M535 — Assignment Archive Review

### Archive Status

| Gate | Status |
|---|---|
| CanArchiveAsGovernanceBaseline | **true** |
| CanUseAsRuntimeBaseline | **false** |
| CanUseAsPlannerImplementation | **false** |
| CanUseAsLlmPromptSource | **false** |
| CanUseAsFilesystemAuthority | **false** |

### Closed Milestones Coverage

- M519-M521: Assignment Engine Contracts / TaskGraph Draft / Planner Readiness Gate
- M522-M524: Mission Plan Preview / Review Cards / Evidence Linking
- M525-M527: UI Preview Static / No-Op Interactions / UX Acceptance
- M528-M530: Review Persistence Mock / Handoff Contract / Safety Audit Pack
- M531-M533: History Mock / Compare Preview / Governance Closeout

### Outputs

- `src/OneBrain.AgentOperations.Contracts/NodalOsAssignmentArchiveReviewContracts.cs`
- `src/OneBrain.AgentOperations.Core/NodalOsAssignmentArchiveReviewServices.cs`
- `artifacts/agent-operations/m536/assignment-archive-review.json`
- `artifacts/agent-operations/m536/assignment-archive-review.md`

---

## 4. M536 — Next Phase ADR

### Decisions Recorded

- NoDirectMoveToRealProjectUnderstanding
- RealScanBlockedUntilFutureMilestone
- FilesystemScanBlockedUntilGatesDefined
- LlmContextBuildBlockedUntilFutureMilestone
- EmbeddingsBlockedUntilFutureMilestone
- IndexingBlockedUntilFutureMilestone
- CloudSyncBlockedUntilFutureMilestone
- AssignmentOutputsAreRefsAndGovernanceContextOnly
- MockHistoryIsNotSourceOfTruth
- ByokAndProviderPolicyRequiredBeforeLlm
- PathJailAndConsentRequiredBeforeFilesystem
- PositiveExecutionGateRequiredBeforeRuntime
- SeparateAuditRequiredBeforeRuntime

### Outputs

- `src/OneBrain.AgentOperations.Contracts/NodalOsNextPhaseAdrContracts.cs`
- `src/OneBrain.AgentOperations.Core/NodalOsNextPhaseAdrServices.cs`
- `docs/architecture/project-understanding-preconditions-before-real-scan-llm-context.md`
- `artifacts/agent-operations/m536/next-phase-adr-summary.json`

---

## 5. Tests

Tests file: `tests/OneBrain.Safety.Tests/NodalOsProjectUnderstandingPreconditionsM534M536Tests.cs`

23 test methods covering:
- All readiness flags false (real project understanding, filesystem scan, LLM context build, embeddings, indexing, cloud sync).
- Required preconditions (consent, path jail, scan scope preview, redaction, secret detection, exclusion, cancellation, audit, no-mutation).
- Archive review status (governance baseline only; not runtime/planner/LLM/filesystem authority).
- ADR decisions (all blocked flags; refs/governance-only for assignment outputs; BYOK/provider before LLM; path jail/consent/audit before filesystem).
- Static HTML preview (no external scripts, CDN, network calls, telemetry).
- Boundary checks (no BrowserExecutor.Cdp, no HttpClient, no dangerous APIs).
- Artifact existence and safety (no sensitive markers, no forbidden names).

---

## 6. Guardrails Confirmed

- No real filesystem scan implemented.
- No directory listing implemented.
- No file read or file hashing implemented.
- No git commands implemented.
- No embeddings implemented.
- No index creation implemented.
- No LLM context building implemented.
- No prompt generation implemented.
- No provider calls implemented.
- No BYOK implemented.
- No cloud sync implemented.
- No runtime execution implemented.
- No real planner implemented.
- No BrowserExecutor.Cdp references.
- No HttpClient, ClientWebSocket, or Process.Start.
- No scheduler, worker, or queue.
- No productive persistence.
- No telemetry.
