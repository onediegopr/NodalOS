# Sidepanel Token Patch 2 - M616

Decision target: SIDEPANEL_TOKEN_PATCH_2_BASE_SURFACE_READY

Remaps base background, text, and border tokens in safe selectors to Research OS --nos-* variables.

## Selectors Modified

| Selector | Property | Old Value | New Value |
|----------|----------|-----------|-----------|
| body | color | var(--ink) | var(--nos-color-text) |
| body | background | #f7f8f4 / var(--bg) | var(--nos-color-bg-muted) / var(--nos-color-bg) |
| .app-header | background | rgba(249,250,247,0.96) | var(--nos-color-bg-muted) |
| .app-header | border-bottom | var(--line) | var(--nos-color-border) |
| .brand-block p | color | var(--muted) | var(--nos-color-text-muted) |
| .surface | border | var(--line) | var(--nos-color-border) |
| .surface | background | rgba(255,255,255,0.9) | var(--nos-color-surface) |
| label | color | var(--muted) | var(--nos-color-text-muted) |
| textarea, input | border | var(--line) | var(--nos-color-border) |
| textarea, input | background | #fffefa | var(--nos-color-surface) |
| textarea, input | color | var(--ink) | var(--nos-color-text) |
| dt | color | var(--muted) | var(--nos-color-text-muted) |
| .candidate/.recipe-item/.log-item | border | var(--line) | var(--nos-color-border) |
| .candidate/.recipe-item/.log-item | background | #fbfcf8 | var(--nos-color-bg) |
| .muted group | color | var(--muted) | var(--nos-color-text-muted) |
| details | border | var(--line) | var(--nos-color-border) |
| details | background | #fffefa | var(--nos-color-surface) |

## Tokens Applied

- --nos-color-bg: background for body, cards
- --nos-color-bg-muted: background for header, body gradient start
- --nos-color-surface: background for panels, inputs, details
- --nos-color-text: text for body, inputs
- --nos-color-text-muted: secondary text for labels, metadata, dt
- --nos-color-border: borders for surfaces, inputs, cards, details

## Forbidden Selectors Preserved

Not modified: .tab.active, .tab, .tabs, .consent-surface, .handoff-surface, .command-surface, button.primary, button.danger, .stop-button, .human-banner, .runtime-command, all #id selectors.

## Visual Impact

Expected warm editorial base: cream-toned background, soft warm borders, slightly muted text. No layout changes. No behavioral changes.

## What Remains for Patch 3

Navigation, status badges, block states, and accent remapping.
