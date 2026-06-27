# Recipe Lab Product Surface

Product-surface phase: 1/4.

Recipe Lab is a read-only summary surface for a Recipe Lab snapshot. It makes readiness, blocked reasons, evidence references, human-review requirements, tool and secret references, trigger status, locator repair preview, and capture draft state visible without adding any runtime capability.

## Sections

Recipe Lab can summarize:

- overview.
- readiness and preflight.
- limits, criteria, validation, and risk.
- evidence and timeline references.
- human intervention and approval narrative.
- tool trust and secrets by reference.
- trigger observe-only state.
- locator repair preview.
- capture draft summary.
- template mapping.
- blocked run modes.
- safe next action.

## Notebook Cells

Cells are inspection-only. They can represent overview, preflight, workitem, trigger observation, tool trust, secret reference, action resolution, planned action, validation, evidence, timeline, human intervention, approval narrative, failure, safe next action, locator candidate, locator repair, handoff, and cleanup summaries.

No cell can mutate a recipe, change a workitem, reveal a secret, operate a connector, apply locator repair, replay a locator, start capture, or unlock live runtime.

## Canonical Readiness

The Lab surface exposes Phase 2+ policy preflight and composite template readiness as canonical product status. Foundation-only readiness is not presented as final product readiness.

## Safety Copy

Operator-facing Lab copy must clearly identify read-only, preview-safe, fixture-safe, reference-only, live-blocked, future-gated, missing evidence, missing validation, human-review, and redacted states.
