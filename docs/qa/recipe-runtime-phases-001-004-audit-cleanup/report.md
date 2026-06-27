# Recipe Runtime Phases 1-4 Audit P1 Micro-Cleanup Report

Block: `NODAL_RECIPE_RUNTIME_001_004_AUDIT_P1_MICRO_CLEANUP`

Decision: `GO_RECIPE_RUNTIME_PHASES_001_004_AUDIT_P1_CLEANUP_READY_FOR_PHASE_5`

## Scope

This cleanup addresses the Claude deep audit result `AUDIT_NO_GO_WITH_P0_P1_FINDINGS`.

- P0 findings: none.
- P1 fixed: `F-001` approval decisions are now bound to options offered by the approval narrative.
- P2 documented/deferred: `F-002`, `F-003`, `F-004`.
- Phase 5 is unblocked for contract/fixture-safe work only.
- No Phase 5 implementation was started.

## Line Status

- Total phases: 9.
- Closed phases: 1-4.
- Cleanup block: audit P1 micro-cleanup.
- Overall Recipe Runtime line completion before cleanup: 49%.
- Overall Recipe Runtime line completion after cleanup: 51%.
- Next phase: Phase 5/9 - Tool Trust Registry + Secrets by Reference.

## Fix Summary

`RecipeApprovalDecisionPolicy.Decide(...)` now rejects or safely downgrades caller-supplied options that are not present in `RecipeApprovalNarrative.DecisionOptions`.

For 2FA and CAPTCHA/challenge narratives, caller-supplied `ApproveFixtureRunOnly` or `ApproveDryRunOnly` no longer maps to an approved status. The decision is kept blocked with a reason stating that the option was not offered by the narrative.

Allowed safe options remain narrative-bound:

- `KeepBlocked` is accepted when offered.
- `MarkManuallyResolved` is accepted when offered.
- `RequestMoreEvidence` remains available only when offered or when missing evidence downgrades an offered approval request.

## P2 Handling

- `F-002`: documented for follow-up. Phase 2+ readiness should use `RecipePolicyPreflightEvaluator` as canonical policy preflight.
- `F-003`: documented for Phase 5/near-term policy hardening. Sensitive categories introduced or refined in Phase 5 must require approval/human paths unless explicitly blocked.
- `F-004`: documented in the Phase 5 prompt. `FutureConnectorRuntime` must remain blocked or explicitly gated until Tool Trust + Secrets policy exists; failed blocking validation evidence must not be treated as complete evidence.

## Validation Summary

- `dotnet restore .\OneBrain.slnx`: PASS.
- `dotnet build .\OneBrain.slnx --no-restore`: PASS with existing warnings.
- `RecipeRuntimeFoundation`: PASS 11/11.
- `RecipeLimitsValidationRiskPolicy`: PASS 20/20.
- `RecipeEvidencePackTimelineProjection`: PASS 22/22.
- `RecipeHumanInterventionApprovalNarrative`: PASS 33/33.
- Full `OneBrain.Recipes.Tests`: PASS 721/721.
- `OneBrain.Safety.Tests` `FullyQualifiedName~Recipe`: PASS 155/156, 1 skipped.

## Safety Matrix

| Boundary | Status |
| --- | --- |
| OpenRPA dependency | NO |
| Code copy | NO |
| XAML import | NO |
| Browser extension/native messaging | NO |
| Real browser automation | NO |
| Real desktop automation | NO |
| CDP/Playwright/Selenium/Puppeteer | NO |
| Scheduler/background worker | NO |
| Recorder/replay | NO |
| File watcher/OS hook | NO |
| Network/API calls | NO |
| Real screenshot/HAR/DOM/accessibility capture | NO |
| CAPTCHA/2FA bypass | NO |
| Approval unlocks live runtime | NO |
| Secrets exposed | NO |
| Live runtime enabled | NO |

## Phase 5 Readiness

Phase 5 is now unblocked as a contract/fixture-safe phase only.

Required Phase 5 guardrails:

- No live runtime.
- No connector execution.
- No vault/API/network integration.
- Secrets by reference only.
- Tool trust by passive contracts only.
- Approval decisions remain narrative-bound.
- Future connector capture/runtime modes remain blocked or explicitly gated.
