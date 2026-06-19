# Mission Control Static UX Acceptance M489-M491

## Purpose

This pack defines static UX acceptance criteria for the NODAL OS Mission Control preview before any productive frontend implementation.

Rendered preview artifact:

- `artifacts/agent-operations/m491/mission-control-static-ux-preview.html`

## Acceptance Criteria

- The surface is clearly understood as Mission Control.
- The surface does not look like a classic ERP/dashboard.
- The surface does not look like an RPA/workflow designer.
- Dark-first visual direction is applied.
- Timeline central area is visible.
- Approval disabled/no-authority state is visible.
- Evidence ref-only behavior is visible.
- Observability/log preview is visible.
- Guardrail explainers are visible.
- Read-only/no-runtime/no-cloud/no-LLM indicators are visible.
- NODAL OS is the operational product name.
- Forbidden historical/external product names are not used in new runtime/export/rendered surfaces.
- No sensitive values appear.
- No raw payload appears.
- No action appears to execute.

## UX Checklist

- Top bar shows mission name, status, progress, read-only, no-runtime, no-cloud, and no-LLM indicators.
- Sidebar is minimal and Mission Control-first.
- Central workspace shows active mission, current phase, next safe step, and timeline.
- Right panel shows approvals, evidence, and guardrails.
- Bottom panel shows observability/log preview.
- Disabled actions remain visible and explain why they are disabled.

## Guardrail Checklist

- Runtime execution remains unavailable.
- Browser automation remains deferred.
- Cloud sync remains disabled.
- LLM provider calls remain unavailable.
- Evidence remains ref-only.
- Approval display remains no-authority.
- Positive execution gate remains missing and required before runtime.
- Productive persistence is not introduced.
- Telemetry and analytics are not introduced.

## Visual Direction Checklist

- Dark-first palette.
- Premium Mission Control tone.
- Strong contrast for badges and safety indicators.
- Vertical timeline readability.
- Compact but not table-heavy.
- No heavy workflow designer metaphor.

## Content And Microcopy Checklist

- Copy explains what is visible.
- Copy explains what is disabled.
- Copy states the next safe step.
- Copy avoids generic admin-panel language.
- Copy reinforces that the preview is read-only.

## No-Runtime Checklist

- No runtime call.
- No browser runtime reference.
- No cloud call.
- No LLM call.
- No scheduler.
- No worker.
- No queue.
- No recorder or replay.
- No DSL parser runtime.
- No shell/subprocess.

## Naming Checklist

- NODAL OS appears as the operational name.
- New rendered surfaces avoid historical/external product names.
- New rendered surfaces avoid legacy naming.

## Accessibility Basics Checklist

- High contrast text and panel boundaries.
- Visible focus/read-only semantics in copy.
- Disabled controls remain legible.
- Layout supports compact, standard, wide, and ultrawide desktop.
- Critical guardrails are not hidden behind optional panels.

## Next UX Gaps

- Real component audit when productive frontend begins.
- Keyboard navigation spec.
- Screen-reader labels for future interactive controls.
- Visual screenshot review after a real frontend route exists.
- Copy review for first-run onboarding.
