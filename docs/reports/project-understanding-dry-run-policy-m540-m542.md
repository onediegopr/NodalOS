# NODAL OS M540-M542 Project Understanding Dry Run Policy

## 1. Executive Summary

M540-M542 adds the next Project Understanding pre-scan policy layer for NODAL OS. The block defines a Secret Detection Policy Preview, an Exclusion Policy Pack, and a Scan Dry Run Contract. All outputs are contract-first, preview-only, deterministic, redacted, and non-operational.

Decision: `PROJECT_UNDERSTANDING_DRY_RUN_POLICY_READY`

## 2. Initial Git State

- Worktree: `C:\DESARROLLO\NodalOS\Codigo-m12-audit`
- Branch: `chrome-lab-001-extension-local-ai-bridge`
- Initial HEAD: `f8c53a7f5048c106a49140704865d8727f653661`
- Initial origin HEAD: `f8c53a7f5048c106a49140704865d8727f653661`
- Initial status: clean
- Remote: `https://github.com/onediegopr/NodalOS.git`
- Forbidden OneDrive path: not used for repository commands.

## 3. Objective

Prepare mandatory policies before any future Project Understanding implementation by modeling sensitive-data policy, exclusion policy, and dry-run behavior without accessing real content or enabling operational capabilities.

## 4. M540 Implemented

- `NodalOsSecretDetectionPolicyPreview` models future sensitive-data detection policy categories and strategy refs.
- The preview declares `IsPreviewOnly=true`, `UsesRealContent=false`, `ReadsFiles=false`, and `PerformsSecretDetectionOnRealData=false`.
- Readiness remains blocked for real sensitive-data detection, real scan, and LLM context build.
- Forbidden capabilities remain false for content inspection, sensitive-value emission, sensitive-value persistence, LLM handoff, and cloud handoff.

## 5. M541 Implemented

- `NodalOsExclusionPolicyPack` models default exclusion groups as policy rules only.
- Groups cover dependency folders, build outputs, caches, VCS metadata, binary/media-heavy areas, environment files, sensitive-marker files, generated artifacts, logs, temporary files, vendor folders, node-modules-like folders, and bin-obj-like folders.
- Readiness remains blocked for real exclusion enforcement, real scan, indexing, and vectorization.
- The pack cannot apply to a real filesystem, create an index, or build LLM context.

## 6. M542 Implemented

- `NodalOsScanDryRunRequest` models a future dry-run request as contract-only.
- `NodalOsScanDryRunResult` keeps the M539 Real Scan Audit Gate blocked and declares real dry-run, real scan, content access, indexing, vectorization, and LLM context readiness as false.
- Events are preview-only and do not emit to a real event bus or productive persistence.
- JSON and HTML artifacts are deterministic and redacted.

## 7. No Operational Capability Confirmation

- No real sensitive-data detection.
- No real exclusion enforcement.
- No real scan.
- No folder enumeration.
- No content access.
- No content fingerprinting.
- No source-control operations.
- No indexing.
- No vectorization.
- No LLM context construction.
- No prompt construction.
- No provider activity.
- No network.
- No cloud.
- No runtime.
- No productive persistence.

## 8. Files Created Or Modified

Created:

- `src/OneBrain.AgentOperations.Contracts/NodalOsSecretDetectionPolicyPreviewContracts.cs`
- `src/OneBrain.AgentOperations.Contracts/NodalOsExclusionPolicyPackContracts.cs`
- `src/OneBrain.AgentOperations.Contracts/NodalOsScanDryRunContracts.cs`
- `src/OneBrain.AgentOperations.Core/NodalOsSecretDetectionPolicyPreviewServices.cs`
- `src/OneBrain.AgentOperations.Core/NodalOsExclusionPolicyPackServices.cs`
- `src/OneBrain.AgentOperations.Core/NodalOsScanDryRunServices.cs`
- `tests/OneBrain.Safety.Tests/NodalOsProjectUnderstandingDryRunPolicyM540M542Tests.cs`
- `docs/reports/project-understanding-dry-run-policy-m540-m542.md`
- `artifacts/agent-operations/m542/secret-detection-policy-preview.json`
- `artifacts/agent-operations/m542/exclusion-policy-pack.json`
- `artifacts/agent-operations/m542/scan-dry-run-contract.json`
- `artifacts/agent-operations/m542/project-understanding-dry-run-policy-preview.html`

Modified:

- `docs/roadmap/nodal-os-roadmap-vnext.md`
- `docs/roadmap/nodal-os-unified-roadmap-post-pause.md`

## 9. Tests Added

- `NodalOsProjectUnderstandingDryRunPolicyM540M542Tests`

Coverage includes preview-only sensitive policy, exclusion policy rules, dry-run request/result, M539 gate linkage, deterministic serializers, HTML safety, artifact safety, and boundary guardrails.

## 10. Validations

Completed:

- `dotnet restore .\OneBrain.slnx`: passed.
- `dotnet build .\OneBrain.slnx`: passed after correcting test helper names.
- `dotnet build .\OneBrain.slnx --no-restore`: passed.
- Focused `dotnet test` filter for M540-M542: 12 passed, 0 failed.
- Full suite: 4116 passed, 37 skipped, 0 failed.
- Guard checks over new files and artifacts: passed.

## 11. Guardrails Confirmed

The implementation is policy-preview-only and dry-run-contract-only. It does not introduce operational scan behavior, real content inspection, provider activity, runtime wiring, productive persistence, usage metrics, browser automation, or cloud behavior.

## 12. Not Implemented

- Real Path Jail.
- Real sensitive-data detection.
- Productive consent UI.
- Operational dry-run.
- Real scan.
- Index creation.
- Vectorization implementation.
- LLM context build.
- Prompt construction.
- Runtime execution.
- Cloud sync.

## 13. Flaky

None observed. The known browser gate flaky did not appear.

## 14. Risks And Pending Items

- Real scan remains blocked until Path Jail implementation audit, consent activation, sensitive-data policy implementation audit, exclusion enforcement audit, no-mutation semantics, cancellation semantics, and evidence/timeline implementation are complete.
- Dry-run remains contract-only and does not satisfy real scan readiness.
- Future implementation must keep sensitive findings redacted and reviewable before any operational use.

## 15. Updated Percentages

- NODAL OS global: 98.9%
- Agent Operations / Automation Layer: 98.1%
- Core Runtime: 76%
- Evidence/Timeline foundation: 89%
- Approval foundation: 83%
- Redaction/Safety foundation: 95%
- Productization foundation: 74%
- Mission Control UX: 74%
- Workspace Local: 72%
- Project Understanding foundation: 66%
- LLM/Assignment: 74%
- Cloud optional: 10%

## 16. Recommended Next Block

`M543+M544+M545 — Project Understanding Dry Run UI Preview + Scan Consent Review Cards + Dry Run Evidence Plan`

## 17. Final Decision

`M540+M541+M542 CERRADO / PROJECT_UNDERSTANDING_DRY_RUN_POLICY_READY`
