# Sidepanel Token Patch 3 - M617

Decision target: SIDEPANEL_TOKEN_PATCH_3_NAV_STATUS_READY

Remaps navigation, status badges, and blocked-state visuals to Research OS tokens.

## Navigation Changes

| Selector | Property | Old | New |
|----------|----------|-----|-----|
| .tab | border | var(--line) | var(--nos-color-border) |
| .tab | background | #fff | var(--nos-color-bg) |
| .tab | color | var(--muted) | var(--nos-color-text-muted) |
| .tab.active | border-color | var(--black) | var(--nos-color-accent) |
| .tab.active | background | var(--black) | var(--nos-color-accent) |

Active tab now uses Research OS accent (#5B6CFF) instead of black.

## Header Glass Correction

.app-header background restored with slight alpha via color-mix():
color-mix(in srgb, var(--nos-color-bg-muted) 94%, transparent)

This preserves the backdrop-filter glass effect with Research OS warmth.

## Status Badges (Timeline)

| Badge Group | Token Applied |
|-------------|--------------|
| status-done, status-ready, status-evidence-ready | --nos-color-success |
| status-blocked, status-not-allowed, status-failed | --nos-color-danger |
| status-warning, status-needs-human, status-evidence-required | --nos-color-warning |
| timeline-blocker-card | --nos-color-danger |
| timeline-safe-action | --nos-color-success |
| timeline-grounding-warning | --nos-color-danger |
| timeline-grounding-card blocked-sensitive | --nos-color-danger |

All applied via color-mix() with appropriate opacity for dark timeline background.

## Forbidden Selectors Preserved

Not modified: button.primary, button.danger, .stop-button, .consent-surface, .handoff-surface, .command-surface, .human-banner, all #id action buttons.

## What Remains for Patch 4

Action buttons (primary/danger), recording state, focus rings, and remaining operational colors.
