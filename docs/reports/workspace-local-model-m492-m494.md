# Workspace Local Model M492-M494

## Executive Summary

M492-M494 adds contract-first local workspace, path jail binding, and project import wizard models for NODAL OS. The block is mock-safe and read-only: it does not scan, read, create, modify, delete, import, or persist real user files.

Decision target: `WORKSPACE_LOCAL_MODEL_PATH_JAIL_IMPORT_CONTRACT_READY`.

## Initial Git State

- Worktree: `C:\DESARROLLO\NodalOS\Codigo-m12-audit`
- Branch: `chrome-lab-001-extension-local-ai-bridge`
- Base commit: `339da9a72538e99be04f175069d2dd48ef0423a8`
- Remote: `https://github.com/onediegopr/NodalOS.git`
- Forbidden path not used: `C:\Users\diego\OneDrive\PERSONAL\ONE Brain\Codigo`

## Objective

Represent local workspaces, path jail boundaries, and project import wizard flow without runtime execution, productive persistence, filesystem mutation, filesystem scan, file picker, cloud, LLM, scheduler, worker, browser automation, recorder, replay, queue, or DSL runtime.

## M492 - Workspace Local Model

Implemented:

- `NodalOsWorkspaceLocalModel`.
- Workspace lifecycle states from draft to archived.
- Redacted local root representation and fingerprint.
- Read-only preview, no-runtime, no-cloud, no-LLM, no-authority flags.
- Path jail binding ref, mission refs, evidence refs, timeline refs, UI state ref, import wizard ref, guardrail summary, capabilities, disabled capabilities, and next safe steps.
- Validator, serializer, and fixtures.

## M493 - Path Jail Binding

Implemented:

- `NodalOsPathJailBinding`.
- Redacted root path, canonical fingerprint, allowed/denied policy, symlink placeholder, case sensitivity note.
- Future operation types and disabled operations.
- Contract flags: `CanMutateFilesystem=false`, `CanExecuteShell=false`, `CanAccessOutsideJail=false`, `RequiresPositiveExecutionGate=true`, `RequiresApprovalForFutureMutations=true`.
- Text-only relative path validation for traversal, absolute paths, drive mismatch, UNC paths, mixed separators, empty path, and sensitive token-like values.

## M494 - Project Import Wizard Contract

Implemented:

- `NodalOsProjectImportWizardContract`.
- Eight-step import preview flow.
- Workspace draft/mock creation.
- Path jail validation result binding.
- Warnings, blockers, disabled future actions, guardrail explainers, user options, and no-op intents.
- Explicit no scan, no folder creation, no import, no productive persistence, no runtime, no cloud, and no LLM flags.

## No-Op / No-Authority / No-Runtime / No-Filesystem Confirmation

- Workspace model does not execute.
- Path jail binding does not touch filesystem.
- Import wizard does not open real file picker, scan folders, create folders, import files, or persist productively.
- Approval and Mission Control continuity remains no-authority.

## Files Created Or Modified

Created:

- `src/OneBrain.AgentOperations.Contracts/NodalOsWorkspaceContracts.cs`
- `src/OneBrain.AgentOperations.Core/NodalOsWorkspaceServices.cs`
- `tests/OneBrain.Safety.Tests/NodalOsWorkspaceLocalModelM492M494Tests.cs`
- `docs/reports/workspace-local-model-m492-m494.md`
- `artifacts/agent-operations/m494/workspace-local-model-summary.json`

Modified:

- `docs/roadmap/nodal-os-roadmap-vnext.md`
- `docs/roadmap/nodal-os-unified-roadmap-post-pause.md`

## Tests New

Added `NodalOsWorkspaceLocalModelM492M494Tests` covering workspace, path jail, import wizard, boundary guards, serialization safety, and existing safety continuity.

## Validations Executed

- `dotnet restore .\OneBrain.slnx`: passed.
- `dotnet build .\OneBrain.slnx`: passed.
- Filtered test suite: 246 passed, 0 failed.
- Full suite: 3879 passed, 37 skipped, 0 failed.

## Guardrails Confirmed

- No runtime execution.
- No positive execution gate implementation.
- No browser automation.
- No real filesystem scan.
- No filesystem mutation.
- No real file picker.
- No productive persistence.
- No cloud.
- No LLM provider calls.
- No scheduler, worker, queue, recorder, replay, or DSL parser runtime.
- No telemetry or analytics.

## Not Implemented

- Workspace storage.
- Real project import.
- Real path resolution against filesystem.
- Symlink resolution.
- Directory listing.
- File reading/writing/deleting.
- Cloud sync.
- Productive frontend workflow.

## Risks And Pending Work

- Future implementation must bind this contract to a real path jail only after dedicated filesystem safety review.
- Symlink and case-sensitivity behavior remain placeholders.
- Productive import requires storage mock, mission binding, and workspace switcher contracts first.

## Percentages

- NODAL OS global: 97.3%.
- Agent Operations / Automation Layer: 97.5%.
- Core Runtime: 76%.
- Evidence/Timeline foundation: 82%.
- Approval foundation: 81%.
- Redaction/Safety foundation: 84%.
- Productization foundation: 56%.
- Mission Control UX: 62%.
- Workspace Local: 28%.

## Next Recommended Block

`M495+M496+M497 - Workspace Storage Mock + Mission Binding + Workspace Switcher Contract`.

## Final Decision

`M492+M493+M494 CERRADO / WORKSPACE_LOCAL_MODEL_PATH_JAIL_IMPORT_CONTRACT_READY`
