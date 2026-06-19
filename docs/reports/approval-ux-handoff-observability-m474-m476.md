# NODAL OS M474-M476 - Approval UX, Handoff, Observability

## Executive Summary

M474-M476 adds contract-first product-facing foundations for Approval Center UX preview, safe export/handoff data packs, and runtime observability reports.

The milestone does not implement UI, runtime execution, cloud, LLM provider calls, scheduler/worker, browser automation, recorder/replay, queue, DSL parser runtime, shell/subprocess, or persistence DB.

Decision: `APPROVAL_UX_HANDOFF_OBSERVABILITY_FOUNDATION_READY`.

## Initial Git State

- Worktree: `C:\DESARROLLO\NodalOS\Codigo-m12-audit`.
- Branch: `chrome-lab-001-extension-local-ai-bridge`.
- Base commit: `031f55a19053361dfd7926b67fbc60c812333cd6`.
- Remote: `https://github.com/onediegopr/NodalOS.git`.
- Forbidden path not used: `C:\Users\diego\OneDrive\PERSONAL\ONE Brain\Codigo`.

## Objective

Connect the M471-M473 approval/timeline/evidence foundation to safe future product surfaces:

- a UI-ready approval preview contract without real UI;
- a redacted export/handoff data pack;
- a technical runtime observability report for a future LOG/copy-report action.

## M474 - Approval Center UX Contract Preview

Created contract models:

- `NodalOsApprovalCardPreview`;
- `NodalOsApprovalUxPreview`.

Created services:

- `NodalOsApprovalUxPreviewService`;
- `NodalOsApprovalUxHandoffObservabilityValidator`;
- `NodalOsApprovalUxHandoffObservabilityJsonSerializer`.

The preview contains human title, short summary, full explanation, risk/severity, status, requested action, resources, policy gate reason, registry/event/timeline/evidence refs, user options, rollback/no-rollback information, expected evidence and attention/handoff flags.

The preview cannot authorize execution and keeps `RuntimeExecutionAllowed=false`, `RuntimeExecutionDeferred=true`, `CanAuthorizeExecution=false`.

## M475 - Export / Handoff Data Pack

Created `NodalOsHandoffDataPack` and `NodalOsHandoffDataPackService`.

The pack captures:

- project metadata;
- milestone/current decision;
- request/decision summaries;
- registry entries;
- approval previews/decisions;
- timeline entries;
- evidence refs;
- warnings/failures;
- handoff requirements;
- redaction summary;
- guardrails summary;
- next steps.

Export is metadata/ref-only and redacted-by-default. Screenshot, DOM, network, headers, cookies, body and secret payloads remain forbidden as raw export content.

## M476 - Runtime Observability Report

Created `NodalOsRuntimeObservabilityReport` and `NodalOsRuntimeObservabilityReportService`.

The report captures:

- user request and system interpretation;
- execution registry summary;
- event bus summary;
- timeline summary;
- approval summary;
- evidence summary;
- redaction applied summary;
- guardrails summary;
- blocked actions;
- failures/warnings;
- human handoff requirements;
- correlation ids and next recommended action.

The report is safe for future LOG/copy-report use, but it does not depend on UI, cloud or LLM calls.

## AUDIT-A Roadmap Entry

The next recommended milestone is:

`AUDIT-A - Claude Full Project Architecture & Safety Audit before UI real / Mission Control Shell`.

Audit scope should include architecture, safety, naming, guardrails, duplication, evidence/timeline, approval, redaction, roadmap and readiness for UI/LLM/cloud.

## Files Created

- `src/OneBrain.AgentOperations.Contracts/NodalOsApprovalUxHandoffObservabilityContracts.cs`
- `src/OneBrain.AgentOperations.Core/NodalOsApprovalUxHandoffObservabilityServices.cs`
- `tests/OneBrain.Safety.Tests/NodalOsApprovalUxHandoffObservabilityM474M476Tests.cs`
- `docs/reports/approval-ux-handoff-observability-m474-m476.md`
- `artifacts/agent-operations/m476/approval-ux-handoff-observability-summary.json`

## Files Modified

- `docs/roadmap/nodal-os-roadmap-vnext.md`
- `docs/roadmap/nodal-os-unified-roadmap-post-pause.md`

## New Tests

Added tests for:

- approval preview creation and validation;
- required user-facing fields and options;
- registry/event/timeline/evidence ref preservation;
- rollback/no-rollback representation;
- no execution authority;
- handoff pack JSON/Markdown redaction;
- unsafe evidence payload rejection;
- runtime observability summaries and ids;
- cross-layer no-divergence;
- dependency direction;
- naming and guardrails;
- AUDIT-A roadmap entry.

## Validations Executed

- `dotnet build .\OneBrain.slnx --no-restore` during implementation.
- Final required validation commands are recorded in the milestone artifact and final report.

## Guardrails Confirmed

- Operational project name remains NODAL OS.
- No NEXA operational naming introduced.
- No NODRIX/HOTEP operational naming introduced.
- No runtime execution.
- No UI or frontend.
- No cloud.
- No LLM provider calls.
- No scheduler or worker runtime.
- No browser automation.
- No recorder/replay.
- No queue.
- No DSL parser runtime.
- No shell/subprocess.
- No persistence DB.

## What Was Not Implemented

- Real UI / Mission Control visual shell.
- Runtime execution.
- External actions.
- LLM calls.
- Browser automation.
- Scheduler/worker.
- Recorder/replay/queue.
- DSL parser runtime.
- PDF/DOCX production export.
- Cloud sync or commercial surfaces.

## Risks / Pending

- UI implementation still requires AUDIT-A first.
- Export rendering remains JSON/Markdown contract-first only.
- Observability report is model/service-only and does not stream from a productive runtime.
- Approval preview is derived data and intentionally cannot mutate registry state.

## Updated Percentages

- NODAL OS global: 96.3%.
- Agent Operations / Automation Layer: 97%.
- Core Runtime: 74%.
- Evidence/Timeline foundation: 75%.
- Approval foundation: 67%.
- Redaction/Safety foundation: 77%.
- Productization foundation: 38%.
- Mission Control UX: 20%.
- LLM/Assignment: 25%.
- Cloud optional: 10%.
- Automation future: 35%.

## Next Recommended Block

`AUDIT-A - Claude Full Project Architecture & Safety Audit before UI real / Mission Control Shell`.

## Final Decision

`M474+M475+M476 CERRADO / APPROVAL_UX_HANDOFF_OBSERVABILITY_FOUNDATION_READY`
