# Sidepanel Token Patch 4 - M618

Decision target: SIDEPANEL_TOKEN_PATCH_4_ACTION_FOCUS_READY

Remaps action buttons, focus rings, and recording state visuals to Research OS tokens.

## Button Changes

| Selector | Property | Old | New |
|----------|----------|-----|-----|
| button | border | var(--line) | var(--nos-color-border) |
| button | background | #fff | var(--nos-color-bg) |
| button | color | var(--ink) | var(--nos-color-text) |
| button | border-radius | 8px | var(--nos-radius-control) |
| button.primary | border/background | green rgba | --nos-color-accent via color-mix |
| button.primary | color | var(--green) | var(--nos-color-accent) |
| button.danger | border/background | red rgba | --nos-color-danger via color-mix |
| button.danger | color | var(--red) | var(--nos-color-danger) |
| .stop-button | background | var(--red) | var(--nos-color-danger) |
| .stop-button | border-radius | 8px | var(--nos-radius-control) |

## Focus Rings (New)

Added focus-visible styles for buttons, inputs, and textareas:
box-shadow: var(--nos-focus-ring)
border-radius: var(--nos-radius-control)

## Recording State

.recording-state: blue operational -> amber warning
background: color-mix(in srgb, var(--nos-color-warning) 12%, transparent)
color: var(--nos-color-warning)

## Authority Mitigations

- Primary buttons use accent indigo (editorial) instead of green (operational authority)
- Stop button uses warm danger orange-red instead of alarming pure red
- Recording uses warning amber instead of active blue (not runtime-authorized)
- Disabled state preserved (opacity 0.42, cursor default)
- No pointer-events, display, or visibility changes
- Consent, handoff, and command surfaces untouched

## What Remains for Patch 5

Accessibility, contrast ratios, final visual QA, and dead-style cleanup.
