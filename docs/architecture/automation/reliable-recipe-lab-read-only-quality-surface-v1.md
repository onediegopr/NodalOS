# Reliable Recipe Lab Read-Only Quality Surface v1

## Purpose

M3 exposes M2 Reliable Recipe quality and preflight reports as a read-only Recipe Lab/Product Surface view model.

It is a product-facing explanation layer, not a runtime layer.

## Dependencies

- M1 Reliable Recipe foundation contracts.
- M2 Reliable Recipe quality/preflight composition.
- Existing Recipe Product Surface convention: Core DTO/viewmodel records plus fixture-safe tests.

## Implementation Placement

M3 adds `ReliableRecipeLabViewModels.cs` under `OneBrain.Core.Recipes`.

No frontend component, API endpoint or runtime service was added. This keeps the surface deterministic, testable and fixture-safe.

## Read-Only Surface

The lab surface shows:

- readiness and mode labels
- overall quality score
- decision tone
- category cards
- blocking findings
- warnings
- recommended fixes
- evidence panel
- validation panel
- target confidence panel
- risk panel
- sandbox panel
- human intervention panel
- perception/OCR panel
- recorder draft panel
- timeline preview
- runtime-not-enabled notice

## Product Language

Allowed language:

- Draft only
- Dry-run candidate
- Needs review
- Blocked by policy
- Missing validation
- Evidence incomplete
- Human handoff required
- OCR signal only, not enough for sensitive action
- Runtime not enabled

Forbidden language:

- claims of autonomous execution readiness.
- claims that live operation is currently safe.
- guarantee-style claims.
- claims of full automation.
- claims of challenge circumvention.
- claims of automated login solving.
- claims that the product executes recipes for the operator.
- production automation readiness claims.

## OCR Presentation

OCR is a protected existing capability. M3 does not change OCR internals, activation gates, privacy/redaction gates, WCU interop, screenshot capture or OCR-based action authority.

The Recipe Lab may display:

- OCR signal used as supporting signal.
- OCR-only sensitive target blocked.
- OCR is read-only supporting evidence, not action authority.

## Human Handoff Presentation

Challenge, credential and ambiguity flows are shown as human handoff. The surface says what happened, what was tried and what the operator must do. It does not present bypass, solver, login automation or credential capture.

## Sandbox Readiness Presentation

Sandbox status is shown as fixture/design readiness only. The surface does not create a VM, container, desktop session, file watcher, OS hook or runtime sandbox.

## Future M4

M4 should expand recorder-to-recipe fixture drafts and show recorder draft quality gaps in the same read-only lab surface.
