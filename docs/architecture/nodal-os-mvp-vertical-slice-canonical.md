# NODAL OS MVP Technical Safety Vertical Slice Canonical

Date: 2026-07-15

Decision: `GO_WITH_FINDINGS_CANONICAL_MAIN_AND_MVP_VERTICAL_SLICE_FROZEN`

Purpose: freeze one ordered technical safety path, prevent parallel runtime drift and prove the control chain used by the future sellable MVP.

This document does **not** claim that the current fixture/local-dev implementation is an installable or sellable product. Productization, real user-workspace enablement, BYOK live use, desktop packaging, release operations and private-beta validation are tracked separately in `nodal-os-current-mvp-roadmap-compact.md`.

Initial operator: Diego/operator running a local-first NODAL OS workspace.

Technical use case: use a fixture or test-owned workspace, create one low-risk mission, generate a reviewed plan, approve one bounded non-destructive/reversible action, execute only through the controlled boundary, verify the result, record redacted evidence/timeline, and produce a human handoff.

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
- WCU/OCR/Recipes/live browser actions stay outside this technical slice until separately authorized.
- Stealth Core Auditado remains protected scope; do not change stealth, anti-fingerprint, captcha, proxy or handoff internals through this slice.

## Stage Requirements

- Explanation: each action proposal includes a human-readable reason and scope.
- Policy: sensitive action classification and fail-closed policy evaluation run before approval.
- Approval: no sensitive controlled action runs without an approval decision bound to the expected identity.
- Timeout: startup/action/verification timeouts fail closed.
- Cancellation: cancellation is honored before validation, before dispatch and during verification where the FSM allows it.
- Rollback/handoff: rollback plan is redacted and shown before approval; if rollback is unavailable, human handoff is required.
- Errors: errors do not include raw secrets, raw DOM/network payloads or unredacted local paths.
- UI: the technical surface only needs local workspace/mission/plan/approval/action/verifier/evidence/handoff status with blockers visible.
- Performance budget: fixture workspace load and plan preview target under 2 seconds; controlled read-only browser capture target under 10 seconds once the pinned CloakBrowser binary is available.

## Technical Definition Of Done

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

## Implementation History

1. Freeze this matrix and guard it with Safety architecture tests.
2. Add CloakBrowser direct CDP descriptor/preflight/read-only capture path.
3. Connect controlled fixture evidence and verification.
4. Validate workspace -> mission -> plan -> approval -> action -> verification -> evidence -> handoff.
5. Add test-owned create and exact-hash update actions.
6. Add bounded workspace understanding, Advisor and handoff export.
7. Add Living Skills foundations and application-scoped Windows observation.

These steps prove architecture and guardrails. They do not replace the productization roadmap.

## Sellable MVP entry criteria

Before private beta, the project still requires:

- `main` as the actual protected GitHub default branch;
- one coherent dark-first Mission Control shell;
- real local workspace selection and persistence;
- one approved reversible operation in an explicitly selected user workspace;
- usable BYOK configuration and one real provider path;
- a reproducible Windows installer/update strategy;
- product license and security/privacy documentation;
- clean-install and onboarding validation.

Production runtime and release/commercial readiness remain `0% / NO-GO` until those gates are implemented and evidenced.

## Exclusions

- No customer data in the technical fixture slice.
- No login, payment, purchase, submit, delete, upload or real download as required test targets.
- No public internet target as a required test target.
- No production deployment.
- No DB/cloud/network/provider requirement for the technical fixture slice.
- No KMS/WORM/external trust.
- No release/commercial claim.
