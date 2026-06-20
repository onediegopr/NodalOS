# M504-M506 - User-Provided Context Capture + Context Review Cards + Context Evidence Linking

## Executive Summary

M504-M506 adds user-provided context capture, safe context review cards, and ref-only context evidence linking for NODAL OS.

The block remains contract-first, mock-safe, read-only/no-op, redacted-by-default, and non-authoritative. It does not implement real project understanding, filesystem scan, file reads, directory listing, git commands, embeddings, LLM/BYOK, prompt creation, cloud, runtime, or productive persistence.

Decision target:

`M504+M505+M506 CERRADO / USER_PROVIDED_CONTEXT_REVIEW_LINKING_READY`

## Initial Git State

- Worktree: `C:\DESARROLLO\NodalOS\Codigo-m12-audit`
- Branch: `chrome-lab-001-extension-local-ai-bridge`
- Initial HEAD: `bafba1421a610dde98685d32e274b7c5a5519136`
- Initial origin branch HEAD: `bafba1421a610dde98685d32e274b7c5a5519136`
- Remote: `https://github.com/onediegopr/NodalOS.git`
- Forbidden path not used: `C:\Users\diego\OneDrive\PERSONAL\ONE Brain\Codigo`

## Objective

Advance safe user-provided context handling:

- M504: User-Provided Context Capture.
- M505: Context Review Cards.
- M506: Context Evidence Linking.

## M504 - User-Provided Context Capture

Implemented:

- Context capture id, workspace id, optional mission id, actor/source, capture type, redacted content, redacted metadata.
- Declared provenance, confidence, freshness, sensitivity level, boundary decision, allowed/disallowed usage.
- Missing information, static/template questions, evidence refs, timeline refs, guardrail refs, validation result.
- Capture types: UserSummary, UserTechStack, UserFolderStructureHint, UserImportantFileHint, UserConstraint, UserRiskNote, UserBusinessContext, UserArchitectureNote, UserTodo, UserUnknown.

Guardrails:

- User-provided only.
- No file read.
- No path existence validation.
- No git.
- No embeddings/vector index.
- No LLM.
- No prompt creation.
- No real project understanding.
- No execution authorization.
- No productive workspace mutation.
- Safe Context Boundary applied.

## M505 - Context Review Cards

Implemented:

- Review card id, context capture id, workspace id, optional mission id.
- Title, summary, details, provenance/confidence/freshness/sensitivity labels.
- Status: Draft, SafeForDisplay, SafeForExport, RequiresReview, BlockedSensitive, BlockedSecret, BlockedRawPayload, DiscardedMock.
- Allowed/disallowed usage chips, missing info, questions, warnings, guardrail refs, evidence refs, timeline refs.
- No-op user options: accept for display, mark needs clarification, edit draft, discard draft, request explanation, link evidence ref, open guardrails.

Guardrails:

- Review cards do not authorize execution.
- Review cards do not call LLM.
- Review cards do not create prompts.
- Review cards do not mutate runtime.
- User options are no-op/mock-safe.
- Context remains user-provided and not verified.

## M506 - Context Evidence Linking

Implemented:

- Context evidence link id, context capture id, optional review card id, evidence ref id, optional timeline ref id, workspace id, optional mission id.
- Link types: SupportsContext, UserClaimReference, ClarificationNeeded, ContradictionSuspected, RelatedTimelineEvent, RelatedEvidenceRef, FutureVerificationNeeded.
- Link statuses: DraftLink, LinkedRefOnly, RequiresReview, BlockedUnsafeEvidence, RemovedMock.
- Link reason, provenance, validation result, redacted serializer.

Guardrails:

- Evidence linking is ref-only.
- No raw payload.
- No screenshot inline.
- No DOM raw.
- No network raw.
- No file read.
- No real content validation.
- No execution.
- No LLM.
- No cloud.
- Claim remains unverified and non-authoritative.

## No-Op / No-Authority / No-Runtime / No-Filesystem / No-LLM / No-Prompt Confirmation

- `CanAuthorizeExecution=false`.
- `RuntimeExecutionAllowed=false`.
- No positive execution gate implementation.
- No runtime.
- No filesystem scan.
- No directory listing.
- No file read/write/delete.
- No file hashing.
- No git command.
- No embeddings.
- No real project understanding.
- No LLM provider calls.
- No prompt creation.
- No cloud calls.
- No productive mutation.

## Files Created

- `src/OneBrain.AgentOperations.Contracts/NodalOsUserContextContracts.cs`
- `src/OneBrain.AgentOperations.Core/NodalOsUserContextServices.cs`
- `tests/OneBrain.Safety.Tests/NodalOsUserContextM504M506Tests.cs`
- `docs/reports/user-context-capture-review-linking-m504-m506.md`
- `artifacts/agent-operations/m506/user-context-capture-review-linking-summary.json`

## Files Modified

- `docs/roadmap/nodal-os-roadmap-vnext.md`
- `docs/roadmap/nodal-os-unified-roadmap-post-pause.md`

## Tests Added

The new test file covers:

- User-provided context capture types and guardrails.
- Blocking of credential/raw payload style context.
- Context review card statuses and no-op options.
- Context evidence linking types/statuses and ref-only semantics.
- Boundary checks against forbidden runtime primitives.
- Existing safety continuity from M477-M503.
- Artifact guardrail flags.

## Validations

- `dotnet restore .\OneBrain.slnx`: passed.
- `dotnet build .\OneBrain.slnx`: passed.
- Filtered test run for `UserContext|WorkspaceReadinessContext|WorkspaceMetadataHealth|WorkspaceStorageMissionSwitcher|WorkspaceLocalModel|MissionControlVisualPolish|MissionControlGuidance|MissionControlInteractionNoOp|MissionControlShellReadOnly|AuditAPreUiBoundaryNaming|ApprovalUxHandoffObservability|ApprovalTimelineEvidence|CoreRuntimeRegistryEventBusRedaction|NewTopicsIntake|NamingCleanup`: `337 passed / 0 skipped / 0 failed`.
- Full suite first run: `3969 passed / 37 skipped / 1 failed`; failure was outside M504-M506 in `BrowserRuntimeSmokeRunnerExecutesAllGatesOnFixture`, Gate 9 WebSocket aborted.
- Full suite rerun: `3970 passed / 37 skipped / 0 failed`.
- Frontend/Tauri/Rust validation: not applicable; no relevant `package.json` or `Cargo.toml` was found outside ignored build/dependency folders.

## Guardrails Confirmed

- No runtime execution.
- No positive execution gate implementation.
- No browser automation.
- No recorder/replay.
- No queue/scheduler/worker.
- No DSL parser runtime.
- No LLM provider calls.
- No prompt creation.
- No cloud sync.
- No productive persistence.
- No filesystem mutation.
- No filesystem scan.
- No directory listing.
- No file read/write/delete.
- No file hashing.
- No git command.
- No embeddings.
- No project understanding real.
- No telemetry or analytics.
- NODAL OS remains the operational project name.

## What Was Not Implemented

- Real project understanding.
- Real context extraction.
- Prompt creation.
- LLM/BYOK.
- Git integration.
- Embeddings/vector index.
- File/directory verification.
- Runtime or execution gate.
- Cloud.

## Risks And Pending Items

- Captured context remains user-provided and unverified.
- Evidence links support review but do not prove truth.
- Context review cards are no-op and cannot approve execution.
- Future UI capture still needs preview/review work.
- One existing BrowserRuntime smoke test showed a transient WebSocket aborted failure on the first full-suite run and passed on rerun; no M504-M506 code path references that runtime.

## Updated Percentages

If validation remains clean:

- NODAL OS global: 97.7%.
- Agent Operations / Automation Layer: 97.5%.
- Core Runtime: 76%.
- Evidence/Timeline foundation: 84%.
- Approval foundation: 81%.
- Redaction/Safety foundation: 87%.
- Productization foundation: 62%.
- Mission Control UX: 67%.
- Workspace Local: 67%.
- Project Understanding foundation: 30%.
- LLM/Assignment: 25%.
- Cloud optional: 10%.
- Automation future: 35%.

## Next Recommended Milestone

`M507+M508+M509 - Context Intake UI Preview + Context Validation Summary + Project Understanding Readiness Report`

## Final Decision

`M504+M505+M506 CERRADO / USER_PROVIDED_CONTEXT_REVIEW_LINKING_READY`
