# Sidepanel Visual QA - M619

Decision target: SIDEPANEL_VISUAL_QA_READY

M619 is an audit-only checkpoint after M615-M618. It verifies that the sidepanel token patches remain CSS-only and documents visual/accessibility risks before any broader Patch 5 work.

## Scope

Audited read-only:

- `browser-extension/onebrain-chrome-lab/sidepanel.css`
- `browser-extension/onebrain-chrome-lab/sidepanel.html`
- `browser-extension/onebrain-chrome-lab/sidepanel.js`

Not modified:

- `browser-extension/onebrain-chrome-lab/sidepanel.html`
- `browser-extension/onebrain-chrome-lab/sidepanel.css`
- `browser-extension/onebrain-chrome-lab/sidepanel.js`
- `browser-extension/onebrain-chrome-lab/manifest.json`

## Result

M615-M618 remain safe as visual CSS-only patches. No new behavior coupling was found in this block. No runtime, provider/cloud, filesystem, productive consent, capability, or source-of-truth coupling was introduced.

The audit did not find a blocking visual regression. It did find visual debt that should be handled in Patch 5 or a Claude design audit before more CSS migration:

- Some Research OS tokens are declared but not applied.
- Several legacy variables remain for compatibility.
- Timeline still contains a dark technical island.
- Handoff and consent surfaces still use hardcoded legacy colors.
- Warning and accent contrast should be manually reviewed.

## Contrast Findings

Static contrast estimates:

| Pair | Estimate | Result |
|------|----------|--------|
| Base text `#171717` on `#F5F3EE` | 16.17:1 | Pass |
| Muted text `#5C5C5C` on `#F5F3EE` | 6.03:1 | Pass |
| White active tab text on `#5B6CFF` | 4.17:1 | Risk |
| Primary accent text `#5B6CFF` on warm background | 3.76:1 | Risk |
| Warning text `#D97706` on warm background | 2.87:1 | Risk |
| Timeline muted text `#8EA59A` on dark card | 6.97:1 | Pass |

Interpretation:

- Main text and muted text are strong.
- Timeline dark-card text is readable, but the dark island conflicts with the Research OS visual direction.
- Active tab, primary action and warning/recording states need manual rendered review before Patch 5.

## Focus Findings

Current focus styles:

- `button:focus-visible`
- `input:focus-visible`
- `textarea:focus-visible`
- `box-shadow: var(--nos-focus-ring)`
- `border-radius: var(--nos-radius-control)`

Risk:

- `outline` is removed, but a replacement focus ring exists.
- The current focus alpha may be subtle on warm editorial surfaces.
- Patch 5 should run keyboard review and tune ring opacity if necessary.

## Visual Semantics

Pass:

- Primary buttons no longer use green execution-authority styling.
- Danger buttons use warm danger styling instead of harsh red.
- Recording state no longer uses active blue.
- Disabled buttons retain opacity and cursor affordance.
- Blocked timeline cards retain danger-token emphasis.

Risks:

- `.timeline-badge.status-running` still uses hardcoded blue and can imply live runtime authority.
- `.recording-state` uses warning semantics but needs contrast review.
- Technical logs still feel like raw operational output, not Mission Activity Feed.

## Dead-Style Candidates

Candidates for Patch 5 or Claude audit:

- Legacy root variables: `--green`, `--red`, `--blue`, `--black`.
- Declared but currently unused Research OS tokens: `--nos-color-bg-soft`, `--nos-color-surface-raised`, `--nos-color-accent-alt`, `--nos-color-risk`, `--nos-space-panel`, `--nos-space-section`, `--nos-font-heading`, `--nos-font-ui`.
- `.timeline-card` and nested timeline color declarations.
- `.human-banner`, `.handoff-surface`, `.consent-surface` hardcoded legacy colors.
- `.log-pane` / `pre` raw technical presentation.

## Recommendation

Recommended next step: `M620 or Claude audit`.

Do not proceed with broad UI rewrite. The next safe step is a small Patch 5 plan focused on rendered visual QA, contrast tuning, dead-style cleanup and timeline/log semantic review.
