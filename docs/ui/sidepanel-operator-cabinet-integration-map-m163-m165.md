# NODAL OS Sidepanel Operator Cabinet Integration Map M163-M165

## Current Sidepanel Files

- `browser-extension/onebrain-chrome-lab/sidepanel.html`
- `browser-extension/onebrain-chrome-lab/sidepanel.css`
- `browser-extension/onebrain-chrome-lab/sidepanel.js`

## Existing Timeline / Stepper Components Found

HTML timeline anchors:

- `operatorTimeline`
- `learningTimeline`
- `recipeStepTimeline`

JavaScript renderer and adapters:

- `renderTimeline(node, items)`
- `renderTimelineStep(item, index)`
- `renderTimelineSubStep(subStep)`
- `renderTimelineBlocker(blocker)`
- `normalizeTimelineStep(item, index)`
- `buildStructuredTaskTimeline(goal)`
- `buildRecipeRunTimeline(run)`

CSS classes:

- `.nodal-timeline`
- `.timeline-step`
- `.timeline-node-dot`
- `.timeline-card`
- `.timeline-card-head`
- `.timeline-badge`
- `.timeline-chip`
- `.timeline-substeps`
- `.timeline-evidence`
- `.timeline-blocker-card`
- `.timeline-safe-action`

Backend contracts/adapters:

- `NodalOsTimeline`
- `NodalOsTimelineStep`
- `NodalOsTimelineAdapter`
- `NodalOsTimelineStabilizationReviewer`

## Do Not Touch / Do Not Duplicate

- Do not replace `renderTimeline`.
- Do not create another timeline component.
- Do not delete existing timeline CSS.
- Do not rewrite the stable sidepanel layout.
- Do not turn service worker into policy/plan authority.
- Do not create a new cab visual shell in M163-M165.

## Operator Cab Conceptual Sections

These are sections that can be represented using existing sidepanel surfaces and the existing timeline renderer:

- Current goal.
- Plan preview.
- Current step.
- Runtime state.
- Blockers.
- Approvals.
- Logs/evidence.
- Recovery options, future only.
- Screenshot/grounding, future only.
- Recipes/skills, future only.

## Extension Points

Recommended safe integration points:

- Add Core-produced `NodalOsExecutionPlanPreview` as data to existing `state.operator`.
- Map plan steps through `NodalOsPlanPreviewToTimelineAdapter`.
- Render plan preview using the existing `renderTimeline` path.
- Reuse `operatorTimeline` until a dedicated section is justified.
- Feed blockers/evidence into existing timeline cards.
- Keep approval state as display only; approval is not simulated in UI.

## Data Needed From Core

- `planId`
- `goal`
- `status`
- ordered plan steps
- allowed and denied domains
- risk summary
- approval requirements
- evidence requirements
- policy summary
- sensitive actions detected
- human approval required
- Core authority required
- timeline compatibility mapping
- redaction summary

## Future Sections Not Implemented Here

- Anti-loop / stagnation detector.
- Recovery UX.
- DOM + screenshot grounding.
- Visual grounding overlay.
- Recipes/skills V1.
- Cost dashboard.
- Research mode.

## Reuse Strategy

Plan preview should produce timeline-compatible step objects and pass through the existing timeline renderer. The operator cab is a composition concept over current sidepanel areas, not a second UI framework.

## Scope Guard

Operator cab integration does not open production, public SaaS, public API real, real credentials, sensitive sites, submit/pay/sign/delete, productive recorder/replay or external CDP general-ready.
