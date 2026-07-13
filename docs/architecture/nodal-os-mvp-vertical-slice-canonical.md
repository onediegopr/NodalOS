# NODAL OS MVP Vertical Slice Canonical

Date: 2026-07-13

Decision: `GO_WITH_FINDINGS_CANONICAL_MAIN_AND_MVP_VERTICAL_SLICE_FROZEN`

Purpose: freeze one product path for the first sellable MVP and stop parallel roadmap drift.

Initial user: Diego/operator running a local-first NODAL OS workspace.

Initial use case: use a fixture or test-owned workspace, create one low-risk mission, generate a reviewed plan, approve one bounded non-destructive/reversible action, execute only through the controlled boundary, verify the result, record redacted evidence/timeline, and produce a human handoff.

## Flow

```text
Workspace
  -> Mission
  -> Plan
  -> Approval
  -> Controlled Action
  -> Verification
  -> Evidence/Timeline
  -> Handoff
```

Order is load-bearing: approval precedes any sensitive controlled action; verification precedes evidence promotion; handoff is read-only and never mutates the result.

## Canonical Stage Matrix

| Stage | Canonical model | Canonical service | Input | Output | Persistence | Events/evidence |
| --- | --- | --- | --- | --- | --- | --- |
| Workspace | `NodalOsWorkspaceLocalModel` | `NodalOsWorkspaceServices` plus `NodalOsPathJailPrototypeServices` | Fixture/test-owned local workspace ref and redacted path metadata | validated local workspace draft/read-only workspace | local/in-memory/test-owned until productive persistence receives a separate GO | `NodalOsEvidenceBridgeRef`, workspace timeline refs |
| Mission | `NodalOsWorkspaceMissionBinding` | `NodalOsWorkspaceMissionServices` | workspace id, mission title/summary redacted, path-jail binding | mission binding with active evidence/timeline refs | local/in-memory/test-owned until durable workspace storage is authorized | mission refs, approval refs, observability refs |
| Plan | `NodalOsMissionPlanDraftPreview` | `NodalOsMissionPlanPreviewServices` | mission binding plus assignment/task graph draft | read-only task graph review cards and plan preview | no runtime schedule; plan remains draft/review until approval | assignment evidence links and timeline refs |
| Approval | `NodalOsApprovalCard` and `NodalOsApprovalDecision` | `ApprovalPolicy`, `SensitiveActionClassifier`, `ApprovalBindingValidator` | proposed action, policy, affected resources, rollback/evidence plan | explicit approve/reject/defer/handoff decision | local decision/evidence refs only; no product authority implied | approval card/decision evidence |
| Controlled Action | `SafeExecutionRequest` | `SafeExecutionFsm` | approved contract, candidates, dispatch request, cancellation token | bounded execution result or fail-closed block | no uncontrolled writes; all future filesystem/browser actions require scoped adapter authority | FSM ledger entries |
| Verification | `StepVerificationResult` | `IStepVerifier` through `SafeExecutionFsm` | dispatch result plus approved identity | success/failure verification report | verification result attached before evidence promotion | verified transition or failure reason |
| Evidence/Timeline | `NodalOsTimelineEntry` and `NodalOsEvidenceRegistryAttachment` | `EvidenceLedger` plus redaction-before-persistence policy | verification result, approval refs, redacted metadata | timeline entry and evidence attachment refs | redacted, local-first; Product Ledger is local/dev review evidence, not authority | timeline/evidence refs |
| Handoff | `NodalOsPlannerHandoffPack` | `NodalOsPlannerHandoffServices` | mission, assignment, blockers, evidence/timeline refs | deterministic redacted handoff markdown/html | read-only render/export preview until physical export receives GO | handoff refs only |

## Boundaries

- CloakBrowser with pinned direct CDP is the only canonical product browser runtime.
- ChromeLab and the extension are `LAB_LEGACY_TRANSITION` only.
- System Chrome/Edge is never a silent product-runtime fallback.
- Playwright default Chromium is never a product-runtime substitute.
- Product Ledger remains local/dev review evidence/read model/supporting timeline until a separate product-authority GO. It is not latest pointer authority, read precedence authority, product authority or release authority.
- WCU/OCR/Recipes/live browser actions stay outside the MVP slice until separately authorized.
- Stealth Core Auditado remains protected scope; do not change stealth, anti-fingerprint, captcha, proxy or handoff internals for this freeze.

## Stage Requirements

- Explanation: each action proposal must include human-readable reason and scope.
- Policy: sensitive action classification and fail-closed policy evaluation run before approval.
- Approval: no sensitive controlled action runs without an approval decision bound to the expected identity.
- Timeout: startup/action/verification timeouts fail closed.
- Cancellation: cancellation is honored before validation, before dispatch and during verification where the FSM allows it.
- Rollback/handoff: rollback plan is redacted and shown before approval; if rollback is not available, human handoff is required.
- Errors: errors must not include raw secrets, raw DOM/network payloads or unredacted local paths.
- UI: first UI only needs local workspace/mission/plan/approval/action/verifier/evidence/handoff status with disabled blockers visible.
- Performance budget: fixture workspace load and plan preview should stay under 2 seconds; controlled action read-only browser capture target should stay under 10 seconds once CloakBrowser binary is available.

## Definition Of Done

1. One fixture workspace can be selected.
2. One mission can bind to that workspace.
3. One plan preview can be generated and reviewed.
4. One approval card and decision can be produced.
5. One non-destructive/reversible controlled action can run only through `SafeExecutionFsm`.
6. Verification runs before evidence/timeline promotion.
7. Evidence is redacted before persistence.
8. Handoff renders deterministic redacted output and does not mutate state.
9. ChromeLab is not used as product runtime.
10. CloakBrowser direct CDP has either a passing read-only local smoke or an explicit `BLOCKED_EXTERNAL_CLOAKBROWSER_BINARY` preflight.

## Duplications And Classification

| Candidate | Classification | Current action |
| --- | --- | --- |
| `NodalOsWorkspaceLocalModel` / workspace services | `CANONICAL_KEEP` | use for MVP workspace |
| Workspace switcher/import wizard mock contracts | `CANONICAL_WITH_ADAPTER` | keep as operator surface/input adapter only |
| `NodalOsWorkspaceMissionBinding` | `CANONICAL_KEEP` | use for mission binding |
| Older `NexaMission` references | `DUPLICATE_CONSOLIDATE` | migrate gradually into workspace mission binding when touched |
| `NodalOsMissionPlanDraftPreview` | `CANONICAL_KEEP` | use as MVP plan artifact |
| Task graph review cards | `CANONICAL_WITH_ADAPTER` | use under plan preview only |
| Approval packet/read-only surfaces | `CANONICAL_WITH_ADAPTER` | UI/read-only adapter over approval card/decision |
| Product Ledger local/dev surfaces | `LEGACY_READ_ONLY` | supporting evidence/read model only |
| ChromeLab bridge/extension | `LAB_ONLY` | lab validation and transition only |
| BrowserRuntime/CloakBrowser direct CDP | `CANONICAL_KEEP` | product browser runtime target |
| WCU/OCR/Recipes live execution | `OUT_OF_SLICE` | blocked until separate GO |
| Evidence Intelligence export/read-only previews | `CANONICAL_WITH_ADAPTER` | evidence/handoff support only, no export authority |

## Implementation Order

1. Freeze this matrix and guard it with Safety architecture tests.
2. Add CloakBrowser direct CDP descriptor/preflight/read-only capture path.
3. Connect the read-only browser capture as evidence for the controlled action boundary.
4. Add one local fixture vertical slice test over workspace -> mission -> plan -> approval -> controlled action -> verification -> evidence -> handoff.
5. Only after that, consider a bounded non-destructive action inside a test-owned local target.

## Private Beta Criteria

- Roadmap/governance: branch governance documented, default branch protection either applied or explicitly blocked by GitHub credentials.
- Implementation technical: one vertical slice fixture passes locally.
- Product integration: CloakBrowser direct CDP is the browser target; ChromeLab is not the product runtime.
- MVP sellable: one low-risk local mission produces a reviewed handoff with evidence.
- Production runtime: remains `0%` until separate authorization.
- Release/commercial: remains `0% / NO-GO`.

## Exclusions

- No customer data.
- No login, payment, purchase, submit, delete, upload or real download.
- No public internet target as required test target.
- No production deployment.
- No DB/cloud/network/provider requirement.
- No KMS/WORM/external trust.
- No CI enforcement or workflow change from this freeze.
- No release/commercial claim.
