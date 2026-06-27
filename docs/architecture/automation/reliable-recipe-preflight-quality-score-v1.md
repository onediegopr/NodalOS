# Reliable Recipe Preflight Quality Score v1

## Purpose

M2 moves the M1 Reliable Recipe foundation from passive contracts into a deterministic preflight and quality scoring layer.

The quality score does not execute recipes. It explains whether a reliable recipe can remain draft-only, can be considered fixture dry-run ready, or must stay blocked/needs review.

## M1 Dependency

M2 builds on:

- `ReliableRecipeDefinition`
- `ReliableRecipePreflightValidator`
- `ReliableActionResolutionPolicyEvaluator`
- `ReliableValidationEvaluator`
- `ReliableSandboxReadinessEvaluator`
- `ReliablePerceptionConfidenceEvaluator`
- `ReliableHumanInterventionRequest`

M2 does not replace the existing Recipe Runtime preflight. It composes with it by accepting an optional `RecipePolicyPreflightResult`; if the existing runtime policy is stricter, the stricter result wins.

## Quality Categories

- Limits
- Validation
- Evidence
- Risk
- Target resolution
- Perception
- Sandbox
- Human intervention
- Replayability
- Recorder draft safety

## Composition Model

`ReliableRecipePreflightComposer` evaluates:

1. M1 Reliable Recipe preflight.
2. Evidence completeness.
3. Validation completeness.
4. Target resolution quality.
5. Risk posture.
6. Sandbox readiness.
7. Human intervention plan quality.
8. Optional existing Recipe Runtime policy result.

Blocking wins over score. A high quality score cannot override policy rejection, high-risk posture, missing validation/evidence, recorder draft limits, or blocked run modes.

## Evidence Completeness

Critical blocks require reference-only evidence expectations for:

- before state
- after state
- action proposal
- action result
- validation report
- timeline event

Downloads also require download artifact evidence. Human approval and high-risk actions require approval evidence expectations. Evidence labels that look secret-like prevent completion.

## Validation Completeness

Submit-like actions require post-action validation. Downloads require file/download validation. Loops require termination validation. Human intervention requires manual confirmation/timeline criteria.

No block can be treated as successful without validation coverage.

## Target Resolution Quality

Deterministic target signals score highest:

- known target
- stable selector
- DOM/CDP snapshot reference
- accessibility tree reference

OCR can improve confidence when it agrees with stronger signals. OCR-only read extraction remains low-confidence/reviewable. OCR-only sensitive actions block. AI semantic fallback blocks for high-risk/sensitive actions.

## Sandbox Readiness

Current readiness is fixture/dry-run only. Desktop future, unrestricted network, raw credential policy, missing rollback and missing evidence policy lower or block sandbox readiness.

M2 does not create a VM, container, remote desktop session or sandbox runtime.

## Human Intervention Quality

Handoff quality requires:

- specific reason
- what happened
- what was tried
- what the user must do
- options where clarification is needed
- evidence refs
- resume/stop policy

Generic "blocked" messages are insufficient.

## OCR Protected Status

OCR is an existing protected capability. M2 does not modify OCR files, OCR activation gates, OCR privacy/redaction gates, OCR/WCU interop behavior, screenshot capture or OCR-based action authority.

M2 only consumes OCR as a fixture/reference perception signal.

## Explicit No-Live Boundary

This architecture does not add runtime automation, browser execution, CDP runtime calls, desktop control, recorder runtime, sandbox runtime, provider calls, connector calls, vault access, screenshot capture, OCR runtime activation or external side effects.

## Next Block

M3 should expose reliable recipe quality reports in a read-only product surface or Recipe Lab view model.
