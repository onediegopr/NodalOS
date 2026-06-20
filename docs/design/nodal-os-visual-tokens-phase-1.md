# NODAL OS Visual Tokens Phase 1

Decision target: `VISUAL_FOUNDATION_PHASE_1_READY`.

## Scope

This is the Phase 1 visual foundation for NODAL OS. It provides tokens, base primitives, and static preview guidance only. It does not rewrite the productive sidepanel or connect to runtime.

## Color Tokens

```css
:root {
  --nos-color-bg: #F5F3EE;
  --nos-color-bg-muted: #F2F0EA;
  --nos-color-bg-soft: #EFECE6;

  --nos-color-text: #171717;
  --nos-color-text-muted: #5C5C5C;

  --nos-color-border: #DDD8D0;

  --nos-color-accent: #5B6CFF;
  --nos-color-accent-alt: #7265FF;

  --nos-color-success: #3D8F5A;
  --nos-color-warning: #D97706;
  --nos-color-risk: #E67E22;
  --nos-color-danger: #C2410C;
}
```

Rules:

- Do not use pure white as the main background.
- Use warm editorial surfaces.
- Use accent color for policy focus, active mission, and clear interaction states.
- Use status colors sparingly and only when state needs attention.

## Typography Tokens

```css
:root {
  --nos-font-heading: Georgia, "Times New Roman", serif;
  --nos-font-ui: Inter, Geist, "SF Pro Text", "Segoe UI", system-ui, sans-serif;
  --nos-font-mono: "Cascadia Mono", Consolas, monospace;
}
```

Rules:

- Headings use an editorial serif with Georgia fallback.
- UI uses a clean sans stack with no remote font dependency.
- No font files are added in this phase.
- No remote font service is used in this phase.

## Layout Tokens

```css
:root {
  --nos-shell-max-width: 1440px;
  --nos-sidebar-width: 232px;
  --nos-right-rail-width: 328px;
  --nos-panel-spacing: 24px;
  --nos-card-padding: 18px;
  --nos-hero-padding: 34px;
  --nos-radius-sm: 10px;
  --nos-radius-md: 16px;
  --nos-radius-lg: 24px;
  --nos-border: 1px solid var(--nos-color-border);
  --nos-focus-ring: 0 0 0 3px rgba(91, 108, 255, 0.24);
  --nos-timeline-gap: 14px;
  --nos-badge-height: 28px;
}
```

## Component Style Primitives

### Shell

Three-region desktop composition: sidebar, mission canvas, contextual rail. Mobile collapses to a single column.

### Sidebar

Minimal navigation, warm surface, active mission section highlighted with accent wash.

### Mission Hero

Dominant current-mission panel with mission title, phase, progress, and next safe action.

### Timeline Step

Journal-like row with phase, decision ref, evidence ref, readiness state, and blocker if present.

### Evidence Card

Case-file style: artifact, type, status, linked mission, commit/ref, validation summary, and redaction state.

### Decision Card

Executive card: decision, recommendation, risk, evidence, options, and status.

### Advisor Note

Strategic architecture note with concern, impact, recommendation, and question. It is not a chat bubble.

### Readiness Gate

Shows capability, missing requirements, required evidence, user action, and blocked capability.

### Permission / Capability Card

Governance console card with capability, status, scope, evidence, risk, and action.

### Activity Feed Row

Human-readable mission event with actor, action, result, evidence, decision, validation, and commit/ref.

### Blocked State Panel

Required anatomy:

- Why this is blocked.
- What is missing.
- What evidence is required.
- What user action is needed.
- What remains intentionally disabled.

### Runtime Status Card

Local-first safety card showing local-only status, network status, content access status, indexing status, representation build status, provider activity status, and safety reason.

### Model Policy Card

Shows primary model, fallback, configuration state, policy boundary, and approval requirement.

## Phase 1 Boundary

- Static preview only.
- No runtime changes.
- No operational access.
- No productive consent.
- No capability enablement.
- No LLM or cloud activity.
