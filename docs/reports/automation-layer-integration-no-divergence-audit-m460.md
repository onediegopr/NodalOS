# NODAL OS Automation Layer Integration No-Divergence Audit M460

## Scope

M460 reviews Claude M458-M459 follow-up items for the Automation Layer before any runtime implementation exists.

This audit covers:

- Automation Event and Evidence contracts.
- Selector Safety and Human Handoff contracts.
- Recipe Risk Classifier and DSL Decision contracts.
- AgentOperations dependency direction.

This audit does not implement runtime, scheduler, UI, recorder, replay, queue, browser automation, DSL parser, or classifier hardening.

## Claude Audit Summary

Claude decision: `AUTOMATION_LAYER_AUDIT_PASS_WITH_MINOR_CLEANUP`.

Confirmed healthy:

- No critical findings.
- No high findings.
- Mission Control-first identity preserved.
- No classic RPA direction.
- No copied UI.Vision, TagUI, OpenRPA, or OpenIAP code.
- No external RPA dependency.
- No recorder, replay, queue, scheduler, timer, background worker, browser automation, parser, API, UI, persistence runtime, or execution.
- EvidenceBridge and common redaction are consistently integrated.
- Selector safety, human handoff, and DSL no-runtime posture are sound.

## MEDIUM-1 Runtime-Gated

Claude observed that the Recipe Risk Classifier uses keyword matching. A benign keyword can match while an uncovered dangerous synonym remains invisible.

Example: `drop table after reading status` could match read/status while the dangerous phrase is not treated as a blocking signal if not covered.

M460-M462 does not harden the classifier. The classifier is still contract-only and cannot authorize action. The issue is registered as a runtime-gated blocker before any future runtime, approval gate, recorder/replay, browser automation, DSL parser runtime, or recipe/step execution uses the classifier for real authority.

## MEDIUM-2 Test Plan

Add cross-layer tests that compose:

- `NodalOsAutomationEvent`.
- `NodalOsAutomationEvidence`.
- `NodalOsSelectorSafetyPolicy`.
- `NodalOsSelectorCandidate`.
- `NodalOsSelectorSafetyEvaluation`.
- `NodalOsHumanHandoffContract`.
- `NodalOsRecipeStepRiskClassification`.
- `NodalOsRecipeRiskProfile`.
- `NodalOsRecipeDslDecisionRecord`.

The tests preserve:

- `RuntimeExecutionAllowed=false`.
- `RuntimeExecutionDeferred=true`.
- `RequiresEvidenceRedaction=true`.
- `RequiresGlobalPolicyEvaluation=true`.
- `CanAuthorizeAction=false`.
- Evidence references validate through `NodalOsEvidenceRefBridge`.
- Common redaction rejects sensitive summaries without exposing the raw value.

## MEDIUM-3 Test Plan

Add dependency-direction tests for:

- `OneBrain.AgentOperations.Contracts` does not reference `OneBrain.BrowserExecutor.Cdp`.
- `OneBrain.AgentOperations.Core` does not reference `OneBrain.BrowserExecutor.Cdp`.
- `OneBrain.AgentOperations.Adapters.Browser` does not reference `OneBrain.BrowserExecutor.Cdp`.
- AgentOperations projects do not reference Chrome/CDP packages or browser automation packages.
- `OneBrain.BrowserExecutor.Cdp` remains the temporary browser runtime host.

## Flags To Preserve

- Automation contracts remain contract-only.
- Selector safety remains observation-only and cannot authorize action.
- Human handoff remains contract-only and cannot authorize action.
- Recipe risk classifier remains contract-only and cannot authorize action.
- DSL remains representation-only, not parser/runtime.

## Dependency Direction Expected

- AgentOperations.Contracts: browser/CDP-free.
- AgentOperations.Core: browser/CDP-free.
- AgentOperations.Adapters.Browser: skeleton-only and no Cdp reference.
- BrowserExecutor.Cdp: temporary host for real browser runtime.

## Not Implemented

- No classifier hardening.
- No runtime behavior change.
- No execution.
- No scheduler, timer, or background worker.
- No API/HTTP/gRPC.
- No UI.
- No recorder or replay.
- No queue or trigger.
- No browser automation.
- No DSL parser.
- No external RPA dependency.

## Decision

Proceed with tests, backlog documentation, artifact, report, and roadmap only.
