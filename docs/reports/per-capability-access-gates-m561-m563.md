# NODAL OS M561-M563 Per-Capability Access Gates

## 1. Executive Summary

M561-M563 separates future operational access into per-capability contract gates, synthetic failure modes, and consent enforcement preview.

Decision: `PER_CAPABILITY_ACCESS_GATES_READY`

## 2. Initial Git State

- Worktree: `C:\DESARROLLO\NodalOS\Codigo-m12-audit`
- Branch: `chrome-lab-001-extension-local-ai-bridge`
- Initial HEAD: `61d0cbe6d54f652a8fa66b4564639bdc0c46f1bb`
- Initial origin HEAD: `61d0cbe6d54f652a8fa66b4564639bdc0c46f1bb`
- Initial status: clean
- Remote: `https://github.com/onediegopr/NodalOS.git`
- Forbidden OneDrive path: not used for repository commands.

## 3. Objective

Model per-capability gates, synthetic fail-closed scenarios, and consent enforcement preview without operational filesystem access, operational scan behavior, indexing, representation build, LLM context, provider activity, cloud, or runtime.

## 4. M561 Implemented

- Per-capability gates for path canonicalization, folder enumeration, content access, content fingerprinting, sensitive-data policy, exclusion enforcement, indexing, representation build, LLM context, cloud sync, provider activity, and runtime execution.
- Every gate is disabled by default, contract-only, consent-required, audit-required, and fail-closed.
- Dependency matrix separates capabilities and keeps runtime execution dependent on a positive execution gate.

## 5. M562 Implemented

- Synthetic failure mode matrix with all required failure categories.
- Failure behavior is fail-closed, no automatic retry, synthetic evidence/timeline only, and no runtime escalation.

## 6. M563 Implemented

- Consent enforcement preview with per-capability rules.
- Preview is no-op, cannot authorize operational capability, cannot persist consent, and cannot bypass consent.

## 7. No Operational Capability Confirmation

- No operational Path Jail.
- No OS path resolution.
- No operational scan behavior.
- No folder enumeration.
- No content access.
- No content fingerprinting.
- No source-control operations.
- No indexing.
- No representation build.
- No LLM context construction.
- No prompt construction.
- No provider activity.
- No network.
- No cloud.
- No runtime.
- No productive persistence.

## 8. Files Created Or Modified

Created:

- `src/OneBrain.AgentOperations.Contracts/NodalOsPerCapabilityAccessGateContracts.cs`
- `src/OneBrain.AgentOperations.Contracts/NodalOsSyntheticFailureModesContracts.cs`
- `src/OneBrain.AgentOperations.Contracts/NodalOsConsentEnforcementPreviewContracts.cs`
- `src/OneBrain.AgentOperations.Core/NodalOsPerCapabilityAccessGateServices.cs`
- `src/OneBrain.AgentOperations.Core/NodalOsSyntheticFailureModesServices.cs`
- `src/OneBrain.AgentOperations.Core/NodalOsConsentEnforcementPreviewServices.cs`
- `tests/OneBrain.Safety.Tests/NodalOsPerCapabilityAccessGatesM561M563Tests.cs`
- `docs/reports/per-capability-access-gates-m561-m563.md`
- `artifacts/agent-operations/m563/per-capability-access-gates.json`
- `artifacts/agent-operations/m563/synthetic-failure-modes.json`
- `artifacts/agent-operations/m563/consent-enforcement-preview.json`
- `artifacts/agent-operations/m563/per-capability-access-gates-preview.html`

Modified:

- `docs/roadmap/nodal-os-roadmap-vnext.md`
- `docs/roadmap/nodal-os-unified-roadmap-post-pause.md`

## 9. Tests Added

- `NodalOsPerCapabilityAccessGatesM561M563Tests`

## 10. Validations

- `dotnet restore .\OneBrain.slnx`: passed.
- `dotnet build .\OneBrain.slnx`: passed with inherited preview SDK and legacy OCR warnings only.
- `dotnet build .\OneBrain.slnx --no-restore`: passed.
- Focused `dotnet test` filter for M561-M563: passed, 12 passed, 0 failed, 0 skipped in Safety tests.
- Complete suite: passed, 4,200 passed, 0 failed, 37 skipped.
- Guard checks over new files, artifacts, and roadmap diffs: clean.

## 11. Guardrails Confirmed

Implementation is disabled-by-default, per-capability-gate-only, synthetic-failure-only, and consent-enforcement-preview-only.

## 12. Not Implemented

- Operational Path Jail.
- OS path resolution.
- Operational scan behavior.
- Operational sensitive-data detection.
- Operational exclusion enforcement.
- Index creation.
- Representation build implementation.
- LLM context build.
- Prompt construction.
- Runtime execution.
- Cloud sync.

## 13. Flaky

None observed. The complete suite passed on the first run without rerun.

## 14. Risks And Pending Items

- Per-capability gates are contracts only.
- Consent enforcement is preview-only.
- Future productive enforcement needs a separate audited consent store and fail-closed implementation.

## 15. Updated Percentages

- NODAL OS global: 99.6%
- Agent Operations / Automation Layer: 98.8%
- Core Runtime: 76%
- Evidence/Timeline foundation: 92%
- Approval foundation: 86%
- Redaction/Safety foundation: 96%
- Productization foundation: 81%
- Mission Control UX: 77%
- Workspace Local: 78%
- Project Understanding foundation: 84%
- LLM/Assignment: 74%
- Cloud optional: 10%

## 16. Recommended Next Block

`M564+M565+M566 - Capability Gate UI Review + Consent Scope Ledger Mock + Fail-Closed Acceptance Pack`

## 17. Final Decision

`M561+M562+M563 CERRADO / PER_CAPABILITY_ACCESS_GATES_READY`
