# ADR: Vertical Timeline / Stepper UI for NODAL OS M157-M159

## Decision

NODAL OS uses a reusable vertical timeline / stepper pattern for operator-facing task structuring, recipes, execution state, evidence, blockers, human intervention and private preview summaries.

The pattern is presentation-only. It does not authorize actions, does not change Core decisions and does not hide blockers.

## Applies To

- Browser extension side panel.
- Structured user task previews.
- Recipe preview and recipe execution.
- Operator UX summaries.
- Evidence/log summaries.
- Blocker explanations.
- Issue triage summaries.
- Private preview run summaries.

## Does Not Apply To

- Production or SaaS public release claims.
- External CDP general-ready claims.
- Billing/email/credential enablement.
- Sensitive sites.
- Submit/pay/sign/delete execution.
- Productive recorder/replay.

## Data Contract

The M157 contract models:

- `NodalOsTimeline`
- `NodalOsTimelineStep`
- `NodalOsTimelineSubStep`
- `NodalOsTimelineNode`
- `NodalOsTimelineStatusCard`
- `NodalOsTimelineEvidenceRef`
- `NodalOsTimelineBlocker`
- `NodalOsTimelineDecision`

Each step can show title, description, status, node type, subtasks, redacted evidence refs, blockers, safe next action, blocked options, risk, scope and Core authority requirement.

## Safety Rules

- Timeline never grants authority.
- Core remains authoritative for every non-observe action.
- UI/Admin/Companion remain non-authoritative.
- ReadyWithRestrictions is never rendered as production ready.
- Evidence refs are redacted.
- Secrets, cookies, tokens, credentials, raw DOM/body and sensitive raw data are not displayed.
- Blocked and needs-human states must remain visible.

## Side Panel UX

The side panel renderer uses a compact dark stepper:

- circular nodes connected by a vertical line
- stacked cards
- indented subtasks
- status badges
- blocker cards
- evidence chips
- human intervention chips
- narrow-panel responsive spacing

The style is NODAL OS-specific and does not copy external components or brands.

## Accessibility / Legibility

The renderer uses semantic ordered lists, stable text labels, clear badge text and readable contrast in the dark timeline cards. It is intended for a narrow browser side panel.

## Scope Lock

M157-M159 does not expand scope. The timeline makes blocked options explicit and keeps production, public SaaS, real credentials, sensitive sites, external general CDP, submit/pay/sign/delete and productive recorder/replay blocked.
